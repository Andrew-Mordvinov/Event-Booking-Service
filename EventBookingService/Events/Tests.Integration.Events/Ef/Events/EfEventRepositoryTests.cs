using System.Linq.Expressions;

using Application.Events.Infrastructure;

using Domain.Events;

using FluentAssertions;

using Infrastructure.Events.Ef;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shared.Exceptions;
using Shared.Infrastructure.Abstract.Enums;

namespace Tests.Integration.Events.Ef.Events;

[Collection(SharedFixture.PostgresTests)]
public partial class EfEventRepositoryTests(SharedFixture sharedFixture) : IAsyncLifetime
{
    private readonly SharedFixture _sharedFixture = sharedFixture;

    public async ValueTask InitializeAsync()
    {
        await _sharedFixture.PrepareTestDbAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    #region Helping

    private async Task AddEventAsync(Guid id)
    {
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        db.Events.Add(new Event
        (
            id,
            "Don't Care Title",
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
            10
        ));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task AddEventsAsync(IEnumerable<Event> events)
    {
        if (!events.Any())
        {
            return;
        }

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        db.Events.AddRange(events);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_EditMode_SuccessfullyModifiedAndSaved()
    {
        // Arrange
        var targetGuid = Guid.NewGuid();
        var titleToSet = "Title to set";

        await AddEventAsync(targetGuid);

        // Act
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            var @event = await repository.GetByIdAsync(targetGuid, GetMode.Edit, TestContext.Current.CancellationToken);
            if (@event is null)
            {
                Assert.Fail("Событие не получено из репозитория");
            }

            @event.Title = titleToSet;
            if (db.Database.CurrentTransaction is null)
            {
                Assert.Fail("В контексте получения события должна быть открыта транзакция");
            }
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            await db.Database.CommitTransactionAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            var result = await db.Events.FirstOrDefaultAsync(t => t.Id == targetGuid, TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result.Title.Should().Be(titleToSet);
        }
    }

    [Fact]
    public async Task GetByIdAsync_ReadonlyMode_ShouldReturnDetachedEntity()
    {
        // Arrange
        var targetGuid = Guid.NewGuid();

        await AddEventAsync(targetGuid);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        // Act
        var result = await repository.GetByIdAsync(targetGuid, GetMode.Readonly, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        db.Entry(result).State.Should().Be(EntityState.Detached);
    }

    [Theory]
    [InlineData(GetMode.Readonly)]
    [InlineData(GetMode.Edit)]
    public async Task GetByIdAsync_NotFound_ShouldReturnNull(GetMode mode)
    {
        // Arrange
        await AddEventAsync(Guid.NewGuid());

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid(), mode, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_Common_SavedSuccessfully()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        var @event = new Event
        (
            eventId,
            "Don't Care Title",
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
            10
        );

        // Act
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            await repository.AddAsync(@event, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            var result = await db.Events.FirstOrDefaultAsync(b => b.Id == eventId, TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(@event);
        }
    }

    #endregion

    #region RemoveAsync

    [Fact]
    public async Task RemoveAsync_ExistingEntity_RemovedSuccessfully()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        await AddEventAsync(eventId);

        // Act
        var removed = false;
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            removed = await repository.RemoveAsync(eventId, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            var result = await db.Events.FirstOrDefaultAsync(b => b.Id == eventId, TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }
    }

    [Fact]
    public async Task RemoveAsync_NonExistingEntity_ShouldReturnFalse()
    {
        // Arrange
        await AddEventAsync(Guid.NewGuid());

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        // Act
        var removed = await repository.RemoveAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        removed.Should().BeFalse();
    }

    #endregion

    #region GetPageAsync

    [Theory]
    [MemberData(nameof(GetPageAsync_ValidFilterAndPageParams))]
    public async Task GetPageAsync_ValidFilterAndPageParams_ShouldReturnCorrectPage(
        IEnumerable<Event> items,
        Expression<Func<Event, bool>>? filter,
        int page,
        int pageSize,
        int filteredCount,
        int totalPages,
        Guid[] expectedIds)
    {
        // Arrange
        await AddEventsAsync(items);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        // Act
        var result = await repository.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.CurrentPage.Should().Be(page);
        result.TotalPages.Should().Be(totalPages);
        result.FilteredCount.Should().Be(filteredCount);
        result.Items.Select(t => t.Id).Should().BeEquivalentTo(expectedIds);
    }

    [Theory]
    [MemberData(nameof(GetPageAsync_BadPaging))]
    public async Task GetPageAsync_BadPaging_ThrowException(
        IEnumerable<Event> items,
        Expression<Func<Event, bool>>? filter,
        int page,
        int pageSize,
        string[] errors)
    {
        // Arrange
        await AddEventsAsync(items);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        // Act
        var act = async () => await repository.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        // Assert
        var assertion = await act.Should()
            .ThrowExactlyAsync<ValidationException>();

        assertion.Which.Errors.Should().BeEquivalentTo(errors);
    }

    [Theory]
    [MemberData(nameof(GetPageAsync_NoElementAfterFilter))]
    public async Task GetPageAsync_NoElementAfterFilter_ReturnNull(
        IEnumerable<Event> items,
        Expression<Func<Event, bool>>? filter,
        int page,
        int pageSize)
    {
        // Arrange
        await AddEventsAsync(items);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        // Act
        var pageResult = await repository.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        // Assert
        pageResult.Should().BeNull();
    }

    #endregion

    #region GetTopSalesEventsAsync

    [Theory]
    [MemberData(nameof(GetTopSalesEventsAsync_CommonCase))]
    public async Task GetTopSalesEventsAsync_CommonCase_ReturnCorrectList(
        IEnumerable<Event> all,
        Guid[] expectedIds)
    {
        // Arrange
        await AddEventsAsync(all);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        // Act
        var result = await repository.GetTopSalesEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Select(t => t.Id).Should().BeEqualTo(expectedIds);
        dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task GetTopSalesEventsAsync_NoItems_ReturnEmptyList()
    {
        // Arrange
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        // Act
        var result = await repository.GetTopSalesEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
        dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion
}
