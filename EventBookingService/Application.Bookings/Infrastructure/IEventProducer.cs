namespace Application.Bookings.Infrastructure;

/// <summary>
/// Интерфейс продюсера событий
/// </summary>
public interface IEventProducer
{
    /// <summary>
    /// Отправка события Бронирование подтверждено
    /// </summary>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Асинхронная задача</returns>
    Task BookingConfirmedAsync(CancellationToken token);
}
