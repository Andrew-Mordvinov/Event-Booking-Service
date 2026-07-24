namespace Shared.Infrastructure.Kafka.Settings;

/// <summary>
/// Настройки подключения к Kafka
/// </summary>
public class KafkaSettings
{
    public required string BootstrapServer { get; init; }
}
