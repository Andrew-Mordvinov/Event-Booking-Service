using Domain.Bookings;
using Shared.Interfaces.Infrastructure;

namespace Application.Bookings.Infrastructure;

/// <summary>
/// Репозиторий бронирований
/// </summary>
public interface IBookingRepository : IRepository<Booking>
{
    /// <summary>
    /// Возвращает идентификаторы ожидающих обработки бронирования
    /// </summary>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Список идентификаторов броней, ожидающих обработки, или пустой список</returns>
    public Task<List<Guid>> GetPendingBookingsAsync(CancellationToken token = default);

    /// <summary>
    /// Получение числа активных броней для пользователя (принятые или ожидающие обработки)
    /// </summary>
    /// <param name="userId">Пользователь, для которого получаем количество</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Количество активных бронирований</returns>
    public Task<int> GetCountActiveBookingForPersonAsync(Guid userId, CancellationToken token = default);
}
