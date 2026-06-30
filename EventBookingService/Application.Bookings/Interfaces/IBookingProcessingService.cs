namespace Application.Bookings.Interfaces;

/// <summary>
/// Сервис для внутренних операций с бронированиями, напрямую не связанных с действиями пользователей
/// </summary>
public interface IBookingProcessingService
{
    /// <summary>
    /// Обработка конкретной брони
    /// </summary>
    /// <param name="bookingId">Идентификатор брони для обработки</param>
    /// <param name="token">Токен отмены операции</param>
    /// <returns>Асинхронная задача</returns>
    Task ProcessBookingAsync(Guid bookingId, CancellationToken token = default);
}
