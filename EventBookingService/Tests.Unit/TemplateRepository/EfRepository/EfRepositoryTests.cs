using DataAccess.Abstract.Common;
using DataAccess.Abstract.Enums;
using DataAccess.EF;
using DataAccess.EF.EfRepository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Exceptions;
using System.Linq.Expressions;
using Tests.TemplateRepository;
using Tests.TemplateRepository.EfRepository;

namespace Tests.EfInfrastructure;

public partial class EfRepositoryTests
{
    private static (TestDbContext Context, EfRepository<TestItem> Repository) CreateRepository()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new TestDbContext(options);
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var repository = new EfRepository<TestItem>(context, unitOfWorkMock.Object, nameof(TestItem));

        return (context, repository);
    }

    #region AddAsync

    [Fact]
    public async Task AddAsync_ShouldAddEntityToTrackerWithAddedState()
    {
        var (context, repository) = CreateRepository();
        var entity = new TestItem { Id = Guid.NewGuid(), TextField = "Test" };

        await repository.AddAsync(entity, TestContext.Current.CancellationToken);

        context.Entry(entity).State.Should().Be(EntityState.Added);
        (await context.Set<TestItem>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ReadonlyMode_ShouldReturnDetachedEntity()
    {
        var (context, repository) = CreateRepository();
        var entity = new TestItem { Id = Guid.NewGuid(), TextField = "Test" };
        await repository.AddAsync(entity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var result = await repository.GetByIdAsync(entity.Id, GetMode.Readonly, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        context.Entry(result).State.Should().Be(EntityState.Detached);
    }

    // Так как Edit завязан на взятие блокировки с for update, InMemory провайдер падает, и это видимо лучше вынести
    // в на будущее в интеграционные тесты
    [Fact]
    public async Task GetByIdAsync_NotFound_ShouldReturnNull()
    {
        var (_, repository) = CreateRepository();

        var result = await repository.GetByIdAsync(Guid.NewGuid(), GetMode.Readonly, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    #endregion

    #region RemoveAsync

    [Fact]
    public async Task RemoveAsync_ExistingEntity_ShouldMarkAsDeletedInTracker()
    {
        var (context, repository) = CreateRepository();
        var entity = new TestItem { Id = Guid.NewGuid(), TextField = "Test" };
        await repository.AddAsync(entity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var removed = await repository.RemoveAsync(entity.Id, TestContext.Current.CancellationToken);

        removed.Should().BeTrue();
        context.ChangeTracker
            .Entries<TestItem>()
            .FirstOrDefault(t => t.Entity.Id == entity.Id)?.State
            .Should().Be(EntityState.Deleted);
        // Так как репозиторий сам не сохраняет изменения, в БД все еще должна быть запись
        (await context.Set<TestItem>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task RemoveAsync_NonExistingEntity_ShouldReturnFalse()
    {
        var (context, repository) = CreateRepository();

        var removed = await repository.RemoveAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        removed.Should().BeFalse();
        context.ChangeTracker.HasChanges().Should().BeFalse();
    }

    #endregion

    #region GetPageAsync

    [Theory]
    [MemberData(nameof(GetPageAsync_ValidFilterAndPageParams))]
    public async Task GetPageAsync_ValidFilterAndPageParams_ShouldReturnCorrectPage(
        IEnumerable<TestItem> items,
        Expression<Func<TestItem, bool>>? filter,
        int page,
        int pageSize,
        Guid[] expectedIds,
        int filteredCount,
        int totalPages)
    {
        var (context, repository) = CreateRepository();
        await context.Set<TestItem>().AddRangeAsync(items, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var result = await repository.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.CurrentPage.Should().Be(page);
        result.TotalPages.Should().Be(totalPages);
        result.FilteredCount.Should().Be(filteredCount);
        result.Items.Select(t => t.Id).Should().BeEquivalentTo(expectedIds);
    }

    [Theory]
    [MemberData(nameof(GetPageAsync_BadPaging))]
    public async Task GetPageAsync_BadPaging_ThrowException(
        IEnumerable<TestItem> items,
        Expression<Func<TestItem, bool>>? filter,
        int page,
        int pageSize,
        string[] errors)
    {
        var (context, repository) = CreateRepository();
        await context.Set<TestItem>().AddRangeAsync(items, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var act = async () => await repository.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        var assertion = await act.Should()
            .ThrowExactlyAsync<ValidationException>();

        assertion.Which.Errors.Should().BeEquivalentTo(errors);
    }


    [Theory]
    [MemberData(nameof(GetPageAsync_NoElementAfterFilter))]
    public async Task GetPageAsync_NoElementAfterFilter_ReturnNull(
        IEnumerable<TestItem> items,
        Expression<Func<TestItem, bool>>? filter,
        int page,
        int pageSize)
    {
        var (context, repository) = CreateRepository();
        await context.Set<TestItem>().AddRangeAsync(items, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var pageResult = await repository.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        pageResult.Should().BeNull();
    }

    #endregion
}