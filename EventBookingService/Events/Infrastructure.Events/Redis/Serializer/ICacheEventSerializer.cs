using Domain.Events;

using Shared.Exceptions;

namespace Infrastructure.Events.Redis.Serializer;

/// <summary>
/// Интерфейс сериализации и десериализации элементов из кэша
/// </summary>
public interface ICacheEventSerializer
{
    /// <summary>
    /// Получить событие по строке-json
    /// </summary>
    /// <param name="json">Строка, из которой получается объект</param>
    /// <returns>Событие</returns>
    /// <exception cref="ValidationException"/>
    Event GetEvent(string json);

    /// <summary>
    /// Получить список событий по строке-json
    /// </summary>
    /// <param name="json">Строка со списком объектов</param>
    /// <returns>Список событий</returns>
    /// <exception cref="ValidationException"/>
    List<Event> GetEventList(string json);

    /// <summary>
    /// Преобразовать событие в строковый json
    /// </summary>
    /// <param name="event">Событие</param>
    /// <returns>Строка-json</returns>
    string GetJsonEvent(Event @event);

    /// <summary>
    /// Преобразовать событие в строковый json
    /// </summary>
    /// <param name="list">Список событий</param>
    /// <returns>Строка-json</returns>
    string GetJsonEventList(List<Event> list);
}
