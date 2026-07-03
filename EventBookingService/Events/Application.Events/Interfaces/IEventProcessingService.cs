using Application.Events.DTO.Requests;

namespace Application.Events.Interfaces;

/// <summary>
/// Сервис для внутренних операций с событиями, напрямую не связанных с действиями пользователей
/// </summary>
public interface IEventProcessingService
{
    /// <summary>
    /// Обработка успешного бронирования
    /// </summary>
    /// <param name="bookingConfirmed">Cобытие бронирования, которое нужно обработать</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    Task ProcessConfirmationAsync(BookingConfirmedRequest bookingConfirmed, CancellationToken token = default);
}
