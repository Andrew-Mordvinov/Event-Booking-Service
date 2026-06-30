using Application.Events.DTO.Requests;
using Application.Events.Infrastructure;
using Application.Events.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Abstract;

namespace Application.Events.Implementation;

public class EventProcessingService(
    IEventRepository _eventStorage,
    IBookingEventsInboxRepository _inboxManager,
    IUnitOfWork _unitOfWork,
    ILogger<EventProcessingService> _logger) : IEventProcessingService
{
    // Возможно слою приложения лучше отвязаться от BookingConfirmed и использовать dto
    public async Task ProcessConfirmationAsync(BookingConfirmedRequest bookingConfirmed, CancellationToken token = default)
    {
        var messageAlreadyProcessed = await _inboxManager.CheckIfProcessedAsync(bookingConfirmed, token);

        if (messageAlreadyProcessed)
        {
            _logger.LogWarning("Сообщение {@Message} уже обработано, пропуск", bookingConfirmed);

            return;
        }

        var @event = await _eventStorage.GetByIdAsync(bookingConfirmed.EventId, token: token);
        if (@event is null)
        {
            await _inboxManager.AddAsync(bookingConfirmed, token);
            _logger.LogError("Ошибка при обработке {@Message}: не найдено событие", bookingConfirmed);

            await _unitOfWork.SaveChangesAsync(token);

            return;
        }

        if (!@event.TryReserveSeats(bookingConfirmed.Seats))
        {
            _logger.LogError("Ошибка при обработке {@Message}: недостаточно мест", bookingConfirmed);
        }

        await _inboxManager.AddAsync(bookingConfirmed, token);
        await _unitOfWork.SaveChangesAsync(token);
    }
}

