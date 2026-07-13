using Application.Events.Interfaces;

using Contracts.Messages;
using Contracts.Topics;

using Infrastructure.Events.ExtensionMethods;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Infrastructure.Kafka;
using Shared.Infrastructure.Kafka.Settings;

namespace Infrastructure.Events.Background;

/// <summary>
/// Фоновый сервис обработки списания мест у забронированных событий
/// </summary>
public class BookEventBackgroundService : KafkaBackgroundConsumer<BookingConfirmed>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BookEventBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaSettings> kafkaSettings,
        IOptions<KafkaConsumerSettings> kafkaConsumerSettings,
        ILoggerFactory loggerFactory)
        : base(kafkaSettings, kafkaConsumerSettings, loggerFactory, BookingEventsTopic.Name)
    {
        _scopeFactory = scopeFactory;
    }

    public override async Task ProcessMessageAsync(BookingConfirmed message, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var eventProcessingService = scope.ServiceProvider.GetRequiredService<IEventProcessingService>();

        await eventProcessingService.ProcessConfirmationAsync(message.ToBookingConfirmedRequest(), stoppingToken);
    }
}
