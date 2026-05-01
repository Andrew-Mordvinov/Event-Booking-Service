using Shared.Interfaces;

namespace Tests.TemplateRepository;

/// <summary>
/// Тестовый айтем для хранилища
/// </summary>
public class TestItem : IHasId, ICopyable<TestItem>
{
    public Guid Id { get; set; }

    public string TextField { get; set; } = string.Empty;

    public int IntField { get; set; }

    public TestItem Copy()
    {
        return new TestItem
        {
            Id = Id,
            TextField = TextField,
            IntField = IntField
        };
    }

    public void FillFrom(TestItem source)
    {
        Id = source.Id;
        TextField = source.TextField;
        IntField = source.IntField;
    }
}
