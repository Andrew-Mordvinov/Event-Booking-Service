using Application.Events.Interfaces;
using Confluent.Kafka;
using Contracts.Messages;
using Contracts.Settings;
using Contracts.Topics;
using Infrastructure.Events.ExtensionMethods;
using Infrastructure.Events.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Infrastructure.Events.Background;

/// <summary>
/// Фоновый сервис обработки списания мест у забронированных событий
/// </summary>
public class BookEventBackgroundService(
    IServiceScopeFactory _scopeFactory,
    IOptions<KafkaSettings> _kafkaSettings,
    IOptions<KafkaConsumerSettings> _kafkaConsumerSettings,
    ILogger<BookEventBackgroundService> _logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Сервис {nameof(BookEventBackgroundService)} начал работу");

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.Value.BootstrapServer,
            GroupId = _kafkaConsumerSettings.Value.GroupId,
            AutoOffsetReset = _kafkaConsumerSettings.Value.AutoOffsetReset,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(BookingEventsTopic.Name);

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

        _logger.LogInformation($"Сервис {nameof(BookEventBackgroundService)} остановлен");
    }

    internal async Task ProcessMessage(IConsumer<string, string> consumer, CancellationToken stoppingToken)
    {
        var consumeResult = consumer.Consume(stoppingToken);

        var message = JsonSerializer.Deserialize<BookingConfirmed>(consumeResult.Message.Value);
        
        if (message is null)
        {
            _logger.LogError("Сообщение не десериализовано корректно {message}", consumeResult.Message.Value);
            // TODO нужно добавить dlq
            consumer.Commit(consumeResult);
            return;
        }

        _logger.LogInformation("Сообщение: {@Message}", message);

        using var scope = _scopeFactory.CreateScope();

        var eventProcessingService = scope.ServiceProvider.GetRequiredService<IEventProcessingService>();

        await eventProcessingService.ProcessConfirmationAsync(message.ToBookingConfirmedRequest(), stoppingToken);

        consumer.Commit(consumeResult);
    }
}
