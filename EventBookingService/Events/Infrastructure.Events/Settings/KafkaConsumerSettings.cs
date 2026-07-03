using Confluent.Kafka;

namespace Infrastructure.Events.Settings;

/// <summary>
/// Настройки консьюмера Kafka
/// </summary>
public class KafkaConsumerSettings
{
    public required string GroupId { get; init; }
    public AutoOffsetReset AutoOffsetReset { get; init; } = AutoOffsetReset.Earliest;
}
