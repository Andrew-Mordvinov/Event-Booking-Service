using Validation;

namespace Tests.Validations;

public partial class ValidationResultTests
{
    public static IEnumerable<object?[]> AddItem_SomeErrorItem =>
    [
        [new ValidationItem("Ошибка")],
        [new ValidationItem("Ошибка", ItemCategory.ConflictError)],
        [new ValidationItem("Ошибка", ItemCategory.ValidationError)]
    ];

    public static IEnumerable<object?[]> AddItems_SomeErrorItems =>
    [
        [new[] { new ValidationItem("Ошибка1"), new ValidationItem("Ошибка2") }],
        [new[] { new ValidationItem("Ошибка1"), new ValidationItem("Ошибка2", ItemCategory.ConflictError) }],
        [new[] { new ValidationItem("Ошибка1", ItemCategory.ConflictError) }],
        [new[] { new ValidationItem("Ошибка1", ItemCategory.ValidationError), new ValidationItem("Ошибка2", ItemCategory.ConflictError) }]
    ];

    public static IEnumerable<object?[]> AddItems_NotOnlyErrorItems =>
    [
        [new[] { new ValidationItem("Ошибка1"), new ValidationItem("Ошибка2"), new ValidationItem("Инфо", ItemCategory.Info) }],
        [new[] { new ValidationItem("Ошибка1"), new ValidationItem("Ошибка2", ItemCategory.Warning) }],
        [new[] { new ValidationItem("Предупреждение", ItemCategory.Warning) }],
        [new[] { new ValidationItem("Инфо", ItemCategory.Info), new ValidationItem("Предупреждение", ItemCategory.Warning) }],
        [new[] { new ValidationItem("Инфо", ItemCategory.Info), new ValidationItem("Ошибка", ItemCategory.ConflictError), new ValidationItem("Предупреждение", ItemCategory.Warning) }],
    ];
}
