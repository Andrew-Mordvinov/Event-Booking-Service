using Domain.Bookings;

namespace Application.Bookings.Infrastructure;

/// <summary>
/// Интерфейс продюсера событий
/// </summary>
public interface IBookingEventsProducer
{
    /// <summary>
    /// Отправка события Бронирование подтверждено в хранилище исходящих сообщений для брокера
    /// </summary>
    /// <param name="booking">Бронь, по которой произошло событие</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Асинхронная задача</returns>
    Task BookingConfirmedAsync(Booking booking, CancellationToken token);
}
