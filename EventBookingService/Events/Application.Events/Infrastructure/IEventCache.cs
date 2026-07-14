using Domain.Events;

namespace Application.Events.Infrastructure;

/// <summary>
/// Интерфейс кэша событий
/// </summary>
public interface IEventCache
{
    /// <summary>
    /// Получить событие из кэша
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <param name="event">Событие, выбранное из кэша</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>true, если запрос в кэш вернул результат</returns>
    Task<bool> GetEventAsync(Guid id, out Event? @event, CancellationToken token = default);

    /// <summary>
    /// Установить событие в кэш
    /// </summary>
    /// <param name="event">Событие или null, если надо сбросить состояние кэша</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Асинхронная задача</returns>
    Task SetEventAsync(Event? @event, CancellationToken token = default);

    /// <summary>
    /// Получить топ-10 событий по продажам из кэша
    /// </summary>
    /// <param name="topEvents">Топ событий, выбранный из кэша</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>true, если запрос в кэш вернул результат</returns>
    Task<bool> GetTopSalesEventAsync(out List<Event> topEvents, CancellationToken token = default);

    /// <summary>
    /// Установить топ-10 событий по продажам в кэш
    /// </summary>
    /// <param name="topEvents">Список самых продаваемых событий или null, 
    /// если надо сбросить состояние кэша</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Асинхронная задача</returns>
    Task SetTopSalesEventAsync(List<Event>? topEvents, CancellationToken token = default);
}
