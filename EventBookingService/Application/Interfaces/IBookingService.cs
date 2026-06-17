using Domain.Bookings;

namespace Application.Interfaces;

/// <summary>
/// Сервис бронирования событий
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создать заявку на бронирование события
    /// </summary>
    /// <param name="eventId">Идентификатор события, на которое подается бронь</param>
    /// <param name="userId">Идентификатор пользователя, на которого бронируется событие</param>
    /// <param name="token">Токен отмены операции</param>
    /// <returns>Объект брони</returns>
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken token = default);

    /// <summary>
    /// Возвращает заявку на бронирование с заданным идентификатором
    /// </summary>
    /// <param name="bookingId">Идентификатор брони</param>
    /// <param name="token">Токен отмены операции</param>
    /// <returns>Объект брони</returns>
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken token = default);

    /// <summary>
    /// Отменяет бронирование с заданным идентификатором
    /// </summary>
    /// <param name="bookingId">Идентификатор брони</param>
    /// <param name="token">Токен отмены операции</param>
    /// <returns>Асинхронная задача</returns>
    Task CancelBookingAsync(Guid bookingId, CancellationToken token = default);

    /// <summary>
    /// Обработка конкретной брони
    /// </summary>
    /// <param name="bookingId">Идентификатор брони для обработки</param>
    /// <param name="token">Токен отмены операции</param>
    /// <returns>Асинхронная задача</returns>
    Task ProcessBookingAsync(Guid bookingId, CancellationToken token = default);
}
