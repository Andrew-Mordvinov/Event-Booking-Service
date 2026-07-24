using Confluent.Kafka;

namespace Shared.Infrastructure.Kafka.Settings;

/// <summary>
/// Настройки консьюмера Kafka
/// </summary>
public class KafkaConsumerSettings
{
    public required string GroupId { get; init; }
    public AutoOffsetReset AutoOffsetReset { get; init; } = AutoOffsetReset.Earliest;
}
