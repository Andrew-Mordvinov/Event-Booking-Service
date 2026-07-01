using Confluent.Kafka;
using Contracts.Settings;
using Contracts.Topics;
using Infrastructure.Bookings.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Infrastructure.Bookings.Background;

/// <summary>
/// Фоновый обработчик корзины с сообщениями об удачных бронированиях, которые надо отправить в кафку
/// </summary>
public class BookingEventSenderBackgroundService(
    IServiceScopeFactory _scopeFactory,
    IOptions<KafkaSettings> _kafkaSettings,
    ILogger<BookingEventSenderBackgroundService> _logger) : BackgroundService
{
    private readonly int _maxMessageCount = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Сервис {nameof(BookingEventSenderBackgroundService)} начал работу");

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaSettings.Value.BootstrapServer,
            Acks = Acks.All
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProduceMessages(producer, stoppingToken);
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

        _logger.LogInformation($"Сервис {nameof(BookingEventSenderBackgroundService)} остановлен");
    }

    internal async Task ProduceMessages(IProducer<string, string> producer, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

        var messages = await dbContext.BookingConfirmed.Take(_maxMessageCount).ToListAsync(stoppingToken);

        _logger.LogInformation("Сообщений для отправки: {Count}", messages.Count);

        if (messages.Count < 1)
        {
            await Task.Delay(1000, stoppingToken);
            return;
        }
        // TODO можно организовать группировку по eventId и отправлять группы параллельно
        foreach (var message in messages)
        {
            try
            {
                var result = await producer.ProduceAsync(BookingEventsTopic.Name, new Message<string, string>()
                {
                    Key = message.EventId.ToString(),
                    Value = JsonSerializer.Serialize(message)
                }, stoppingToken);

                if (result.Status is not PersistenceStatus.Persisted)
                {
                    _logger.LogError("Сообщение не доставлено брокеру {@Message}", result.Value);
                    continue;
                }

                dbContext.Remove(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение при отправке сообщения {@Message}", message);
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }
}
