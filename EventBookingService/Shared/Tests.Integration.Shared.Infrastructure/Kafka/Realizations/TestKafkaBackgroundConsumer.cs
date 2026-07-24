using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Infrastructure.Kafka;
using Shared.Infrastructure.Kafka.Settings;

namespace Tests.Integration.Shared.Infrastructure.Kafka.Realizations;

/// <summary>
/// Тестовая реализация консюмера, которая при обработке события вызывает событие, чтобы в тестах можно было
/// проверить, сколько событий обработал консюмер и что ему пришло на вход
/// </summary>
internal class TestKafkaBackgroundConsumer(
    IOptions<KafkaSettings> kafkaSettings,
    IOptions<KafkaConsumerSettings> kafkaConsumerSettings,
    ILoggerFactory loggerFactory,
    string topic) : KafkaBackgroundConsumer<TestMessage>(kafkaSettings, kafkaConsumerSettings, loggerFactory, topic)
{
    /// <summary>
    /// Событие для подписки на него внутри теста
    /// </summary>
    public event EventHandler<MessageProcessedEventArgs>? MessageProcessed;

    public override Task ProcessMessageAsync(TestMessage message, CancellationToken stoppingToken)
    {
        MessageProcessed?.Invoke(this, new MessageProcessedEventArgs { Message = message });

        return Task.CompletedTask;
    }
}
