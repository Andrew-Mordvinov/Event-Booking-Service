using System.Text.Json;
using System.Text.Json.Serialization;

using Confluent.Kafka;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Infrastructure.Kafka.Settings;

namespace Shared.Infrastructure.Kafka;

/// <summary>
/// Абстрактный потребитель сообщений из кафки. Пайплайн уже настроен, требуется передать параметры и переопределить
/// <see cref="ProcessMessageAsync"/>>, который обрабатывает переданное сообщение
/// </summary>
/// <typeparam name="TMessage">Класс, в который сериализуется пришедшее сообщение</typeparam>
public abstract class KafkaBackgroundConsumer<TMessage> : BackgroundService
{
    protected readonly IOptions<KafkaSettings> _kafkaSettings;
    protected readonly IOptions<KafkaConsumerSettings> _kafkaConsumerSettings;
    protected readonly ILogger _logger;
    protected readonly string _topic;

    public KafkaBackgroundConsumer(
        IOptions<KafkaSettings> kafkaSettings,
        IOptions<KafkaConsumerSettings> kafkaConsumerSettings,
        ILoggerFactory loggerFactory,
        string topic)
    {
        _kafkaSettings = kafkaSettings;
        _kafkaConsumerSettings = kafkaConsumerSettings;
        _logger = loggerFactory.CreateLogger(GetType());
        _topic = topic;
    }

    public abstract Task ProcessMessageAsync(TMessage message, CancellationToken stoppingToken);

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Сервис {GetType().Name} начал работу");

        await PrepareAndStartAsync(stoppingToken);

        _logger.LogInformation($"Сервис {GetType().Name} остановлен");
    }

    internal async Task PrepareAndStartAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.Value.BootstrapServer,
            GroupId = _kafkaConsumerSettings.Value.GroupId,
            AutoOffsetReset = _kafkaConsumerSettings.Value.AutoOffsetReset,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(_topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessage(consumer, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "При обработке сообщений возникло исключение");
            }
        }
    }

    internal async Task ProcessMessage(IConsumer<string, string> consumer, CancellationToken stoppingToken)
    {
        var consumeResult = consumer.Consume(stoppingToken);
        stoppingToken.ThrowIfCancellationRequested();

        TMessage? message;
        try
        {
            var options = new JsonSerializerOptions
            {
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            };
            message = JsonSerializer.Deserialize<TMessage>(consumeResult.Message.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сериализации сообщения {message}", consumeResult.Message.Value);
            // TODO нужно добавить dlq
            consumer.Commit(consumeResult);
            return;
        }

        if (message is null)
        {
            _logger.LogError("Сообщение не десериализовано корректно {message}", consumeResult.Message.Value);
            // TODO нужно добавить dlq
            consumer.Commit(consumeResult);
            return;
        }

        _logger.LogInformation("Сообщение: {@Message}", message);

        try
        {
            await ProcessMessageAsync(message, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Обработка сообщения вызвала исключение");
        }

        consumer.Commit(consumeResult);
    }
}
