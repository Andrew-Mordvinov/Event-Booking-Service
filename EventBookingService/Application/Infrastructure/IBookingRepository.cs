using Application.Infrastructure.Common;
using Domain.Bookings;

namespace Application.Infrastructure;

/// <summary>
/// Репозиторий бронирований
/// </summary>
public interface IBookingRepository : IBaseStorage<Booking>
{
    /// <summary>
    /// Возвращает идентификаторы ожидающих обработки бронирования
    /// </summary>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Список идентификаторов броней, ожидающих обработки, или пустой список</returns>
    public Task<List<Guid>> GetPendingBookingsAsync(CancellationToken token = default);
}
