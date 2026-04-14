using FluentAssertions;
using Validation;

namespace Tests.Validations;

public class ValidationItemTests
{
    [Fact]
    public void IsError_CategoryInfo_ReturnFalse()
    {
        var item = new ValidationItem("Инфо", ItemCategory.Info);

        item.IsError.Should().BeFalse();
    }

    [Fact]
    public void IsError_CategoryWarning_ReturnFalse()
    {
        var item = new ValidationItem("Предупреждение", ItemCategory.Warning);

        item.IsError.Should().BeFalse();
    }

    [Fact]
    public void IsError_CategoryValidationError_ReturnTrue()
    {
        var item = new ValidationItem("Ошибка", ItemCategory.ValidationError);

        item.IsError.Should().BeTrue();
    }

    [Fact]
    public void IsError_CategoryWarning_ReturnTrue()
    {
        var item = new ValidationItem("Конфликт", ItemCategory.ConflictError);

        item.IsError.Should().BeTrue();
    }
}
