namespace Contracts.Settings;

/// <summary>
/// Настройки подключения к Kafka
/// </summary>
public class KafkaSettings
{
    public required string BootstrapServer { get; init; }
}
