using DataAccess.Storage;
using FluentAssertions;
using System.Linq.Expressions;
using Validation;

namespace Tests.Storage;

public partial class DictionaryStorageTests
{
    #region Constructor Test

    [Fact]
    public async Task Constructor_DuplicateObjectsGiven_ThrowException()
    {
        var dupliucateId = Guid.NewGuid();
        var duplicateCollection = new TestItem[]
        {
            new TestItem { Id = dupliucateId, IntField = 10, TextField = "First" },
            new TestItem { Id = dupliucateId, IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        };

        var action = () => new DictionaryStorage<TestItem>(duplicateCollection);

        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_SomeObject_SuccessfullyAdded()
    {
        var storage = new DictionaryStorage<TestItem>();
        var item = new TestItem { Id = Guid.NewGuid(), IntField = 5, TextField = "SomeText" };

        var result = await storage.AddAsync(item, TestContext.Current.CancellationToken);
        var getResult = await storage.GetByIdAsync(item.Id, TestContext.Current.CancellationToken);

        result.IsSuccessful.Should().BeTrue();
        getResult.IsSuccessful.Should().BeTrue();
        getResult.Value.Should().BeEquivalentTo(item);
    }

    [Fact]
    public async Task AddAsync_ObjectModifiedAfter_DoesNotAffect()
    {
        var storage = new DictionaryStorage<TestItem>();
        var item = new TestItem { Id = Guid.NewGuid(), IntField = 5, TextField = "SomeText" };

        var result = await storage.AddAsync(item, TestContext.Current.CancellationToken);
        item.IntField = 10;
        item.TextField = "AnotherText";
        var getResult = await storage.GetByIdAsync(item.Id, TestContext.Current.CancellationToken);

        result.IsSuccessful.Should().BeTrue();
        getResult.IsSuccessful.Should().BeTrue();
        getResult.Value.Should().NotBeNull();
        getResult.Value.IntField.Should().Be(5);
        getResult.Value.TextField.Should().Be("SomeText");
    }

    [Fact]
    public async Task AddAsync_ObjectExits_ErrorReturn()
    {
        var item = new TestItem { Id = Guid.NewGuid(), IntField = 5, TextField = "SomeText" };
        var storage = new DictionaryStorage<TestItem>([item]);

        var result = await storage.AddAsync(item, TestContext.Current.CancellationToken);

        result.IsSuccessful.Should().BeFalse();
        result.Errors.Count.Should().Be(1);
        result.Errors.First().Should().BeEquivalentTo(new ValidationItem(StorageErrors.ItemWithIdAlreadyExist(item.Id)));
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ValidId_SuccessfullyGet()
    {
        var itemToGet = new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "First" };
        var storage = new DictionaryStorage<TestItem>
        ([
            itemToGet,
            new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);
        
        var getResult = await storage.GetByIdAsync(itemToGet.Id, TestContext.Current.CancellationToken);

        getResult.IsSuccessful.Should().BeTrue();
        getResult.Value.Should().NotBeNull();
        getResult.Value.Should().BeEquivalentTo(itemToGet);
    }

    [Fact]
    public async Task GetByIdAsync_ObjectModifiedAfter_DoesNotAffect()
    {
        // Arrange
        var itemToGet = new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "First" };
        var storage = new DictionaryStorage<TestItem>
        ([
            itemToGet,
            new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);

        // Act
        var getResult = await storage.GetByIdAsync(itemToGet.Id, TestContext.Current.CancellationToken);

        // Assert
        getResult.IsSuccessful.Should().BeTrue();
        getResult.Value.Should().NotBeNull();
        getResult.Value.Should().BeEquivalentTo(itemToGet);

        // Act
        getResult.Value.IntField = 0;
        var getResultAfterModify = await storage.GetByIdAsync(itemToGet.Id, TestContext.Current.CancellationToken);

        // Assert
        getResultAfterModify.IsSuccessful.Should().BeTrue();
        getResultAfterModify.Value.Should().NotBeNull();
        getResultAfterModify.Value.IntField.Should().Be(itemToGet.IntField);
    }

    [Fact]
    public async Task GetByIdAsync_WrongId_EmptySuccessfulResult()
    {
        var wrongId = Guid.NewGuid();
        var storage = new DictionaryStorage<TestItem>
        ([
            new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "First" },
            new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);

        var getResult = await storage.GetByIdAsync(wrongId, TestContext.Current.CancellationToken);

        getResult.IsSuccessful.Should().BeTrue();
        getResult.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_StorageEmpty_EmptySuccessfulResult()
    {
        var wrongId = Guid.NewGuid();
        var storage = new DictionaryStorage<TestItem>();

        var getResult = await storage.GetByIdAsync(wrongId, TestContext.Current.CancellationToken);

        getResult.IsSuccessful.Should().BeTrue();
        getResult.Value.Should().BeNull();
    }

    #endregion

    #region RemoveAsync

    [Fact]
    public async Task RemoveAsync_ValidId_SuccessfullyRemoved()
    {
        // Arrange
        var itemToRemove = new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" };
        var storage = new DictionaryStorage<TestItem>
        ([
            new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "First" },
            itemToRemove,
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);

        // Act
        var removeResult = await storage.RemoveAsync(itemToRemove.Id, TestContext.Current.CancellationToken);

        // Assert
        removeResult.IsSuccessful.Should().BeTrue();
        removeResult.Value.Should().BeTrue();

        // Act
        var tryGetAfterDelete = await storage.GetByIdAsync(itemToRemove.Id, TestContext.Current.CancellationToken);

        // Assert
        tryGetAfterDelete.IsSuccessful.Should().BeTrue();
        tryGetAfterDelete.Value.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WrongId_ReturnFalse()
    {
        var wrongId = Guid.NewGuid();
        var storage = new DictionaryStorage<TestItem>
        ([
            new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "First" },
            new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);

        var removeResult = await storage.RemoveAsync(wrongId, TestContext.Current.CancellationToken);

        removeResult.IsSuccessful.Should().BeTrue();
        removeResult.Value.Should().BeFalse();
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ValidObject_SuccessfullyUpdated()
    {
        // Assert
        var itemToUpdate = new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "First" };
        var storage = new DictionaryStorage<TestItem>
        ([
            itemToUpdate,
            new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);
        itemToUpdate.TextField = "Mod";

        // Act
        var updateResult = await storage.UpdateAsync(itemToUpdate, TestContext.Current.CancellationToken);

        // Arrange
        updateResult.IsSuccessful.Should().BeTrue();
        updateResult.Value.Should().BeTrue();

        // Act
        var getAfterUpdate = await storage.GetByIdAsync(itemToUpdate.Id, TestContext.Current.CancellationToken);

        // Arrange
        getAfterUpdate.IsSuccessful.Should().BeTrue();
        getAfterUpdate.Value.Should().BeEquivalentTo(itemToUpdate);
    }

    [Fact]
    public async Task UpdateAsync_WrongObject_ReturnSuccessfulFalse()
    {
        var itemToUpdate = new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "SomeText" };
        var storage = new DictionaryStorage<TestItem>
        ([
            new TestItem { Id = Guid.NewGuid(), IntField = 115, TextField = "First" },
            new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);

        var updateResult = await storage.UpdateAsync(itemToUpdate, TestContext.Current.CancellationToken);

        updateResult.IsSuccessful.Should().BeTrue();
        updateResult.Value.Should().BeFalse();
    }

    #endregion

    #region GetPageAsync

    [Theory]
    [MemberData(nameof(GetPageAsync_ValidFilterAndPageParams))]
    public async Task GetPageAsync_ValidFilterAndPageParams_ValidPageReturns(
        IEnumerable<TestItem> items,
        Expression<Func<TestItem, bool>>? filter,
        int page,
        int pageSize,
        Guid[] expectedIds,
        int filteredCount,
        int totalPages)
    {
        var storage = new DictionaryStorage<TestItem>(items);

        var pageResult = await storage.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        pageResult.IsSuccessful.Should().BeTrue();
        pageResult.Value.Should().NotBeNull();
        pageResult.Value.CurrentPage.Should().Be(page);
        pageResult.Value.TotalPages.Should().Be(totalPages);
        pageResult.Value.FilteredCount.Should().Be(filteredCount);
        pageResult.Value.Items.Select(t => t.Id).Should().BeEquivalentTo(expectedIds);
    }

    [Theory]
    [MemberData(nameof(GetPageAsync_BadPaging))]
    public async Task GetPageAsync_BadPaging_ErrorReturn(
        IEnumerable<TestItem> items,
        Expression<Func<TestItem, bool>>? filter,
        int page,
        int pageSize,
        string[] errors)
    {
        var storage = new DictionaryStorage<TestItem>(items);

        var pageResult = await storage.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        pageResult.IsSuccessful.Should().BeFalse();
        pageResult.Value.Should().BeNull();
        pageResult.Errors.Should().BeEquivalentTo(errors.Select(t => new ValidationItem(t)));
    }

    [Theory]
    [MemberData(nameof(GetPageAsync_NoElementAfterFilter))]
    public async Task GetPageAsync_NoElementAfterFilter_SuccessfulEmpty(
        IEnumerable<TestItem> items,
        Expression<Func<TestItem, bool>>? filter,
        int page,
        int pageSize)
    {
        var storage = new DictionaryStorage<TestItem>(items);

        var pageResult = await storage.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        pageResult.IsSuccessful.Should().BeTrue();
        pageResult.Value.Should().BeNull();
    }

    #endregion
}
