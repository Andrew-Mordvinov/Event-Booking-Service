using FluentAssertions;
using Validation;

namespace Tests.Validations;

public partial class ValidationResultTests
{
    #region Contructor Tests

    [Fact]
    public void Contructor_ErrorArgText_ValidationResultIsBad()
    {
        var expected = new ValidationItem("Ошибка");

        var validationResult = new ValidationResult("Ошибка");

        validationResult.IsSuccessful.Should().BeFalse();
        validationResult.Errors.Should().BeEquivalentTo([expected]);
    }

    [Fact]
    public void Contructor_ErrorArgsText_ValidationResultIsBad()
    {
        var expected = new[] { new ValidationItem("Ошибка"), new ValidationItem("Ошибка2") };

        var validationResult = new ValidationResult(["Ошибка", "Ошибка2"]);

        validationResult.IsSuccessful.Should().BeFalse();
        validationResult.Errors.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Contructor_ErrorItemsArgs_ValidationResultIsBad()
    {
        var expected = new[] 
        { 
            new ValidationItem("Ошибка", ItemCategory.ValidationError),
            new ValidationItem("Ошибка2"),
            new ValidationItem("Ошибка3", ItemCategory.ConflictError)
        };

        var validationResult = new ValidationResult(expected);

        validationResult.IsSuccessful.Should().BeFalse();
        validationResult.Errors.Should().BeEquivalentTo(expected);
    }

    #endregion

    #region AddError/AddItem

    [Theory]
    [MemberData(nameof(AddItem_SomeErrorItem))]
    public void AddItem_SomeErrorItem_ValidationResultIsBad(ValidationItem item)
    {
        var validationResult = new ValidationResult();

        validationResult.AddItem(item);

        validationResult.IsSuccessful.Should().BeFalse();
        validationResult.Errors.Should().BeEquivalentTo([item]);
    }

    [Fact]
    public void AddError_SomeErrorText_ValidationResultIsBad()
    {
        var expected = new ValidationItem("Ошибка");
        var validationResult = new ValidationResult();

        validationResult.AddError("Ошибка");

        validationResult.IsSuccessful.Should().BeFalse();
        validationResult.Errors.Should().BeEquivalentTo([expected]);
    }

    #endregion

    #region AddErrors/AddItems

    [Theory]
    [MemberData(nameof(AddItems_SomeErrorItems))]
    public void AddItems_SomeErrorItems_ValidationResultIsBad(IEnumerable<ValidationItem> items)
    {
        var validationResult = new ValidationResult();

        validationResult.AddItems(items);

        validationResult.IsSuccessful.Should().BeFalse();
        validationResult.Errors.Should().BeEquivalentTo(items);
    }

    [Fact]
    public void AddErrors_SomeErrorsText_ValidationResultIsBad()
    {
        var expected = new[] { new ValidationItem("Ошибка"), new ValidationItem("Ошибка2") };
        var validationResult = new ValidationResult();

        validationResult.AddErrors(["Ошибка", "Ошибка2"]);

        validationResult.IsSuccessful.Should().BeFalse();
        validationResult.Errors.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(AddItems_NotOnlyErrorItems))]
    public void AddItems_NotOnlyErrorItems_ValidationResultSuccess(IEnumerable<ValidationItem> items)
    {
        var validationResult = new ValidationResult();

        validationResult.AddItems(items);

        validationResult.Errors.Should().BeEquivalentTo(items);
    }

    #endregion

    #region HasCategory

    [Fact]
    public void HasCategory_CategoryExistsInResult_ReturnTrue()
    {
        var items = new[] 
        { 
            new ValidationItem("Ошибка"),
            new ValidationItem("Ошибка2", ItemCategory.ConflictError),
            new ValidationItem("Предупреждение", ItemCategory.Warning),
            new ValidationItem("Инфо", ItemCategory.Info),
            new ValidationItem("Инфо2", ItemCategory.Info),
            new ValidationItem("Ошибка", ItemCategory.ValidationError)
        };
        var validationResult = new ValidationResult(items);

        validationResult.HasCategory(ItemCategory.ConflictError).Should().BeTrue();
    }

    [Fact]
    public void HasCategory_CategoryNotExistsInResult_ReturnFalse()
    {
        var items = new[]
        {
            new ValidationItem("Ошибка"),
            new ValidationItem("Ошибка2", ItemCategory.ConflictError),
            new ValidationItem("Инфо", ItemCategory.Info),
            new ValidationItem("Инфо2", ItemCategory.Info),
            new ValidationItem("Ошибка", ItemCategory.ValidationError)
        };
        var validationResult = new ValidationResult(items);

        validationResult.HasCategory(ItemCategory.Warning).Should().BeFalse();
    }

    [Fact]
    public void HasCategory_NoElements_ReturnFalse()
    {
        var validationResult = new ValidationResult();

        validationResult.HasCategory(ItemCategory.Warning).Should().BeFalse();
    }

    #endregion

    #region IsSuccessFul

    [Fact]
    public void IsSuccessFul_NoElements_ReturnTrue()
    {
        var validationResult = new ValidationResult();

        validationResult.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void IsSuccessFul_HasSomeErrors_ReturnTrue()
    {
        var items = new[]
        {
            new ValidationItem("Ошибка"),
            new ValidationItem("Ошибка2", ItemCategory.ConflictError),
            new ValidationItem("Инфо", ItemCategory.Info),
            new ValidationItem("Инфо2", ItemCategory.Info),
            new ValidationItem("Ошибка", ItemCategory.ValidationError)
        };

        var validationResult = new ValidationResult(items);

        validationResult.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public void IsSuccessFul_NoErrors_ReturnFalse()
    {
        var items = new[]
        {
            new ValidationItem("Инфо", ItemCategory.Info),
            new ValidationItem("Инфо2", ItemCategory.Info),
            new ValidationItem("Предупреждение", ItemCategory.Warning)
        };

        var validationResult = new ValidationResult(items);

        validationResult.IsSuccessful.Should().BeTrue();
    }

    #endregion
}
