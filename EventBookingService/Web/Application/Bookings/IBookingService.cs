using EventBookingService.Common.Validations.Results;
using EventBookingService.Models.Bookings;

namespace EventBookingService.Application.Bookings;

/// <summary>
/// Сервис бронирования событий
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создать заявку на бронирование события
    /// </summary>
    /// <param name="eventId">Идентификатор события, на которое подается бронь</param>
    /// <param name="token">Токен отмены операции</param>
    /// <returns>Объект брони или null вместе с возникшими ошибками</returns>
    Task<ValidationResult<Booking?>> CreateBookingAsync(Guid eventId, CancellationToken token = default);

    /// <summary>
    /// Возвращает заявку на бронирование с заданным идентификатором
    /// </summary>
    /// <param name="bookingId">Идентификатор брони</param>
    /// <param name="token">Токен отмены операции</param>
    /// <returns>Объект брони или null вместе с возникшими ошибками</returns>
    Task<ValidationResult<Booking?>> GetBookingByIdAsync(Guid bookingId, CancellationToken token = default);
}
