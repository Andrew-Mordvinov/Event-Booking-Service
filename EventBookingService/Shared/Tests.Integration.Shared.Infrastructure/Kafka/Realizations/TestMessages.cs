namespace Tests.Integration.Shared.Infrastructure.Kafka.Realizations;

/// <summary>
/// Целевое "правильное" сообщение
/// </summary>
internal class TestMessage
{
    public required Guid Id { get; init; }

    public required string Payload { get; init; }

    public int TestInt { get; init; }

    public DateTime? TestDate { get; init; }
}

internal record SomeWrongMessage(Guid Id, string Message, int TestInt, DateTime? TestDate);
internal record AnotherWrongMessage(Guid Id, int TestInt, DateTime? TestDate);
