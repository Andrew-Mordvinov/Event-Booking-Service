namespace Validation;

public class ValidationItem(string text, ItemCategory category = ItemCategory.ValidationError)
{
    public string Text { get; init; } = text;

    public ItemCategory Category { get; init; } = category;

    public bool IsError => Category != ItemCategory.Warning && Category != ItemCategory.Info;
}
