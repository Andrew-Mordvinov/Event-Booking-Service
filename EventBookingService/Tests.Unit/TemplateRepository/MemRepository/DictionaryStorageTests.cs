using DataAccess.Abstract.Enums;
using DataAccess.Memory.Storage;
using FluentAssertions;
using Shared.Exceptions;
using System.Linq.Expressions;
using Tests.TemplateRepository;

namespace Tests.Storage;

[Obsolete(obsoleteMessage)]
public partial class DictionaryStorageTests
{
    private const string obsoleteMessage = "Хранение в памяти не используется и не развивается, тесты не актуальны";

    #region Constructor Test

    [Fact]
    [Obsolete(obsoleteMessage)]
    public async Task Constructor_DuplicateObjectsGiven_ThrowException()
    {
        var dupliucateId = Guid.NewGuid();
        var duplicateCollection = new TestItem[]
        {
            new TestItem { Id = dupliucateId, IntField = 10, TextField = "First" },
            new TestItem { Id = dupliucateId, IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        };

        var action = () => new DictionaryRepository<TestItem>(duplicateCollection);

        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region AddAsync

    [Fact]
    [Obsolete(obsoleteMessage)]
    public async Task AddAsync_SomeObject_SuccessfullyAdded()
    {
        var storage = new DictionaryRepository<TestItem>();
        var item = new TestItem { Id = Guid.NewGuid(), IntField = 5, TextField = "SomeText" };

        await storage.AddAsync(item, TestContext.Current.CancellationToken);
        var getResult = await storage.GetByIdAsync(item.Id, GetMode.Readonly, TestContext.Current.CancellationToken);

        getResult.Should().BeEquivalentTo(item);
    }

    [Fact]
    [Obsolete(obsoleteMessage)]
    public async Task AddAsync_ObjectExits_ThrowException()
    {
        var item = new TestItem { Id = Guid.NewGuid(), IntField = 5, TextField = "SomeText" };
        var storage = new DictionaryRepository<TestItem>([item]);

        var act = async () => await storage.AddAsync(item, TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowExactlyAsync<ConflictException>()
            .WithMessage(DictionaryRepoErrors.ItemWithIdAlreadyExist(item.Id));
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    [Obsolete(obsoleteMessage)]
    public async Task GetByIdAsync_ValidId_SuccessfullyGet()
    {
        var itemToGet = new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "First" };
        var storage = new DictionaryRepository<TestItem>
        ([
            itemToGet,
            new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);

        var readonlyResult = await storage.GetByIdAsync(itemToGet.Id, GetMode.Readonly, TestContext.Current.CancellationToken);
        var editResult = await storage.GetByIdAsync(itemToGet.Id, GetMode.Edit, TestContext.Current.CancellationToken);

        readonlyResult.Should().BeEquivalentTo(itemToGet);
        editResult.Should().BeEquivalentTo(itemToGet);
    }

    [Fact]
    [Obsolete(obsoleteMessage)]
    public async Task GetByIdAsync_ObjectModifiedAfter_DoesNotAffect()
    {
        // Arrange
        var itemToGet = new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "First" };
        var storage = new DictionaryRepository<TestItem>
        ([
            itemToGet,
            new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);

        // Act
        var getResult = await storage.GetByIdAsync(itemToGet.Id, GetMode.Readonly, TestContext.Current.CancellationToken);

        // Assert
        getResult.Should().BeEquivalentTo(itemToGet);

        // Act
        getResult.IntField = 0;
        var getResultAfterModify = await storage.GetByIdAsync(itemToGet.Id, GetMode.Readonly, TestContext.Current.CancellationToken);

        // Assert
        getResultAfterModify.Should().NotBeNull();
        getResultAfterModify.IntField.Should().Be(itemToGet.IntField);
    }

    [Fact]
    [Obsolete(obsoleteMessage)]
    public async Task GetByIdAsync_WrongId_ReturnNull()
    {
        var wrongId = Guid.NewGuid();
        var storage = new DictionaryRepository<TestItem>
        ([
            new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "First" },
            new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);

        var getResult = await storage.GetByIdAsync(wrongId, GetMode.Readonly, TestContext.Current.CancellationToken);

        getResult.Should().BeNull();
    }

    [Fact]
    [Obsolete(obsoleteMessage)]
    public async Task GetByIdAsync_StorageEmpty_ReturnNull()
    {
        var wrongId = Guid.NewGuid();
        var storage = new DictionaryRepository<TestItem>();

        var getResult = await storage.GetByIdAsync(wrongId, GetMode.Readonly, TestContext.Current.CancellationToken);

        getResult.Should().BeNull();
    }

    #endregion

    #region RemoveAsync

    [Fact]
    [Obsolete(obsoleteMessage)]
    public async Task RemoveAsync_ValidId_SuccessfullyRemoved()
    {
        // Arrange
        var itemToRemove = new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" };
        var storage = new DictionaryRepository<TestItem>
        ([
            new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "First" },
            itemToRemove,
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);

        // Act
        var removeResult = await storage.RemoveAsync(itemToRemove.Id, TestContext.Current.CancellationToken);

        // Assert
        removeResult.Should().BeTrue();

        // Act
        var tryGetAfterDelete = await storage.GetByIdAsync(itemToRemove.Id, GetMode.Readonly, TestContext.Current.CancellationToken);

        // Assert
        tryGetAfterDelete.Should().BeNull();
    }

    [Fact]
    [Obsolete(obsoleteMessage)]
    public async Task RemoveAsync_WrongId_ReturnFalse()
    {
        var wrongId = Guid.NewGuid();
        var storage = new DictionaryRepository<TestItem>
        ([
            new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "First" },
            new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);

        var removeResult = await storage.RemoveAsync(wrongId, TestContext.Current.CancellationToken);

        removeResult.Should().BeFalse();
    }

    #endregion

    #region GetPageAsync

    [Theory]
    [MemberData(nameof(GetPageAsync_ValidFilterAndPageParams))]
    [Obsolete(obsoleteMessage)]
    public async Task GetPageAsync_ValidFilterAndPageParams_ValidPageReturns(
        IEnumerable<TestItem> items,
        Expression<Func<TestItem, bool>>? filter,
        int page,
        int pageSize,
        Guid[] expectedIds,
        int filteredCount,
        int totalPages)
    {
        var storage = new DictionaryRepository<TestItem>(items);

        var pageResult = await storage.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        pageResult.Should().NotBeNull();
        pageResult.CurrentPage.Should().Be(page);
        pageResult.TotalPages.Should().Be(totalPages);
        pageResult.FilteredCount.Should().Be(filteredCount);
        pageResult.Items.Select(t => t.Id).Should().BeEquivalentTo(expectedIds);
    }

    [Theory]
    [MemberData(nameof(GetPageAsync_BadPaging))]
    [Obsolete(obsoleteMessage)]
    public async Task GetPageAsync_BadPaging_ThrowException(
        IEnumerable<TestItem> items,
        Expression<Func<TestItem, bool>>? filter,
        int page,
        int pageSize,
        string[] errors)
    {
        var storage = new DictionaryRepository<TestItem>(items);

        var act = async () => await storage.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        var assertion = await act.Should()
            .ThrowExactlyAsync<ValidationException>();

        assertion.Which.Errors.Should().BeEquivalentTo(errors);
    }

    [Theory]
    [MemberData(nameof(GetPageAsync_NoElementAfterFilter))]
    [Obsolete(obsoleteMessage)]
    public async Task GetPageAsync_NoElementAfterFilter_ReturnNull(
        IEnumerable<TestItem> items,
        Expression<Func<TestItem, bool>>? filter,
        int page,
        int pageSize)
    {
        var storage = new DictionaryRepository<TestItem>(items);

        var pageResult = await storage.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        pageResult.Should().BeNull();
    }

    #endregion
}
