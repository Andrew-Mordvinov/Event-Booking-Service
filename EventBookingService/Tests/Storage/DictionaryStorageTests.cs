using DataAccess.Storage;
using FluentAssertions;
using Shared.Exceptions;
using System.Linq.Expressions;

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

        await storage.AddAsync(item, TestContext.Current.CancellationToken);
        var getResult = await storage.GetByIdAsync(item.Id, TestContext.Current.CancellationToken);

        getResult.Should().BeEquivalentTo(item);
    }

    [Fact]
    public async Task AddAsync_ObjectModifiedAfter_DoesNotAffect()
    {
        var storage = new DictionaryStorage<TestItem>();
        var item = new TestItem { Id = Guid.NewGuid(), IntField = 5, TextField = "SomeText" };

        await storage.AddAsync(item, TestContext.Current.CancellationToken);
        item.IntField = 10;
        item.TextField = "AnotherText";
        var getResult = await storage.GetByIdAsync(item.Id, TestContext.Current.CancellationToken);

        getResult.Should().NotBeNull();
        getResult.IntField.Should().Be(5);
        getResult.TextField.Should().Be("SomeText");
    }

    [Fact]
    public async Task AddAsync_ObjectExits_ThrowException()
    {
        var item = new TestItem { Id = Guid.NewGuid(), IntField = 5, TextField = "SomeText" };
        var storage = new DictionaryStorage<TestItem>([item]);

        var act = async () => await storage.AddAsync(item, TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowExactlyAsync<ConflictException>()
            .WithMessage(StorageErrors.ItemWithIdAlreadyExist(item.Id));
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

        getResult.Should().BeEquivalentTo(itemToGet);
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
        getResult.Should().BeEquivalentTo(itemToGet);

        // Act
        getResult.IntField = 0;
        var getResultAfterModify = await storage.GetByIdAsync(itemToGet.Id, TestContext.Current.CancellationToken);

        // Assert
        getResultAfterModify.Should().NotBeNull();
        getResultAfterModify.IntField.Should().Be(itemToGet.IntField);
    }

    [Fact]
    public async Task GetByIdAsync_WrongId_ReturnNull()
    {
        var wrongId = Guid.NewGuid();
        var storage = new DictionaryStorage<TestItem>
        ([
            new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "First" },
            new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);

        var getResult = await storage.GetByIdAsync(wrongId, TestContext.Current.CancellationToken);

        getResult.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_StorageEmpty_ReturnNull()
    {
        var wrongId = Guid.NewGuid();
        var storage = new DictionaryStorage<TestItem>();

        var getResult = await storage.GetByIdAsync(wrongId, TestContext.Current.CancellationToken);

        getResult.Should().BeNull();
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
        removeResult.Should().BeTrue();

        // Act
        var tryGetAfterDelete = await storage.GetByIdAsync(itemToRemove.Id, TestContext.Current.CancellationToken);

        // Assert
        tryGetAfterDelete.Should().BeNull();
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

        removeResult.Should().BeFalse();
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
        updateResult.Should().BeTrue();

        // Act
        var getAfterUpdate = await storage.GetByIdAsync(itemToUpdate.Id, TestContext.Current.CancellationToken);

        // Arrange
        getAfterUpdate.Should().BeEquivalentTo(itemToUpdate);
    }

    [Fact]
    public async Task UpdateAsync_WrongObject_ReturnFalse()
    {
        var itemToUpdate = new TestItem { Id = Guid.NewGuid(), IntField = 10, TextField = "SomeText" };
        var storage = new DictionaryStorage<TestItem>
        ([
            new TestItem { Id = Guid.NewGuid(), IntField = 115, TextField = "First" },
            new TestItem { Id = Guid.NewGuid(), IntField = -6, TextField = "Second" },
            new TestItem { Id = Guid.NewGuid(), IntField = 2187, TextField = "Third" },
        ]);

        var updateResult = await storage.UpdateAsync(itemToUpdate, TestContext.Current.CancellationToken);

        updateResult.Should().BeFalse();
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

        pageResult.Should().NotBeNull();
        pageResult.CurrentPage.Should().Be(page);
        pageResult.TotalPages.Should().Be(totalPages);
        pageResult.FilteredCount.Should().Be(filteredCount);
        pageResult.Items.Select(t => t.Id).Should().BeEquivalentTo(expectedIds);
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
        var storage = new DictionaryStorage<TestItem>(items);

        var act = async () => await storage.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

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
        var storage = new DictionaryStorage<TestItem>(items);

        var pageResult = await storage.GetPageAsync(filter, page, pageSize, TestContext.Current.CancellationToken);

        pageResult.Should().BeNull();
    }

    #endregion
}
