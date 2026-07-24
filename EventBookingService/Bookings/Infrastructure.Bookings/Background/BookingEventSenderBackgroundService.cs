using System.Text.Json;

using Confluent.Kafka;

using Contracts.Topics;

using Infrastructure.Bookings.Ef;
using Infrastructure.Bookings.Ef.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Infrastructure.Kafka;
using Shared.Infrastructure.Kafka.Settings;

namespace Infrastructure.Bookings.Background;

/// <summary>
/// Фоновый обработчик корзины с сообщениями об удачных бронированиях, которые надо отправить в кафку
/// </summary>
public class BookingEventSenderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaSettings> kafkaSettings,
    ILoggerFactory loggerFactory)
    : KafkaBackgroundProducer<BookingConfirmedOutboxItem, BookingsDbContext>(
      scopeFactory,
      kafkaSettings,
      loggerFactory,
      BookingEventsTopic.Name)
{
    private readonly int _maxMessageCount = 30;

    public override Message<string, string> CreateTopicMessage(BookingConfirmedOutboxItem message) =>
        new()
        {
            Key = message.EventId.ToString(),
            Value = JsonSerializer.Serialize(message)
        };

    public override Task<List<BookingConfirmedOutboxItem>> GetMessagesAsync(BookingsDbContext dbContext, CancellationToken stoppingToken)
    {
        return dbContext.BookingConfirmed.Take(_maxMessageCount).ToListAsync(stoppingToken);
    }
}
