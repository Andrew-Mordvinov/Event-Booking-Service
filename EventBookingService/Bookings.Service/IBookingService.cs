using Bookings.Models;

namespace Bookings.Service;

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
    Task<Booking?> CreateBookingAsync(Guid eventId, CancellationToken token = default);

    /// <summary>
    /// Возвращает заявку на бронирование с заданным идентификатором
    /// </summary>
    /// <param name="bookingId">Идентификатор брони</param>
    /// <param name="token">Токен отмены операции</param>
    /// <returns>Объект брони или null вместе с возникшими ошибками</returns>
    Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken token = default);

    /// <summary>
    /// Обработка ожидающих броней. Получает брони из хранилища и обрабатывает
    /// </summary>
    /// <param name="maxCount">Максимальное количество броней, обрабатываемых за вызов метода</param>
    /// <param name="token">Токен отмены операции</param>
    /// <returns>Результат выполнения операции</returns>
    Task ProcessPendingBookingsAsync(int maxCount = 100, CancellationToken token = default);

    /// <summary>
    /// Обработка конкретной брони
    /// </summary>
    /// <param name="booking">Бронь для обработки</param>
    /// <param name="token">Токен отмены операции</param>
    /// <returns>Асинхронная задача</returns>
    Task ProcessBookingAsync(Booking booking, CancellationToken token = default);
}
