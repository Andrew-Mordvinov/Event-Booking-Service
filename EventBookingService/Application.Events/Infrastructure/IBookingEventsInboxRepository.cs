using Application.Events.DTO.Requests;

namespace Application.Events.Infrastructure;

/// <summary>
/// Менеджер, управляющий потоком входящих событий, для поиска обработанных и обновления списка
/// </summary>
public interface IBookingEventsInboxRepository
{
    /// <summary>
    /// Проверить, существует ли это событие в списке обработанных
    /// </summary>
    /// <param name="incoming">Входящее событие</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Истина, если событие уже есть в списке обработанных</returns>
    Task<bool> CheckIfProcessedAsync(BookingConfirmedRequest incoming, CancellationToken token = default);

    /// <summary>
    /// Добавляет событие в список обработанных (без фиксации данных в БД)
    /// </summary>
    /// <param name="incoming">Входящее событие</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Асинхронная задача</returns>
    Task AddAsync(BookingConfirmedRequest incoming, CancellationToken token = default);
}
