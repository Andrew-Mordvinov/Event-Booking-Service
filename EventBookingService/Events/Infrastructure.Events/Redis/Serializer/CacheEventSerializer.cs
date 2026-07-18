using System.Text.Json;
using System.Text.Json.Serialization;

using Domain.Events;

using Shared.Exceptions;

namespace Infrastructure.Events.Redis.Serializer;

public class CacheEventSerializer : ICacheEventSerializer
{
    private static readonly JsonSerializerOptions _serializationOptions = new()
    { 
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow 
    };

    public Event GetEvent(string json)
    {
        var eventModel = JsonSerializer.Deserialize<EventCacheModel>(json, _serializationOptions);

        if (eventModel is null)
        {
            throw new ValidationException([$"Событие получить не удалось: ошибка десериализации из строки. Строка: {json}"]);
        }

        var (@event, errors) = Event.TryCreate
        (
            eventModel.Id,
            eventModel.Title,
            eventModel.Start,
            eventModel.End,
            eventModel.TotalSeats,
            eventModel.AvailableSeats,
            eventModel.Description
        );

        if (@event is null)
        {
            var resultErrors = errors.ToList();
            resultErrors.Add($"Не удалось преобразовать модель из кэша с {eventModel.Id}, возникли ошибки");

            throw new ValidationException(resultErrors);
        }

        return @event;
    }

    public List<Event> GetEventList(string json)
    {
        var rawList = JsonSerializer.Deserialize<List<EventCacheModel>>(json, _serializationOptions);

        if (rawList is null)
        {
            throw new ValidationException([$"Список событий получить не удалось: ошибка десериализации из строки. Строка: {json}"]);
        }

        var topList = rawList
            .Select(t =>
            {
                var (@event, errors) = Event.TryCreate
                (
                    t.Id,
                    t.Title,
                    t.Start,
                    t.End,
                    t.TotalSeats,
                    t.AvailableSeats,
                    t.Description
                );

                if (@event is null)
                {
                    var resultErrors = errors.ToList();
                    resultErrors.Add($"Не удалось преобразовать модель из кэша с {t.Id}, возникли ошибки");

                    throw new ValidationException(resultErrors);
                }

                return @event;
            })
            .ToList();

        return topList;
    }

    public string GetJsonEvent(Event @event)
    {
        var cacheModel = new EventCacheModel(@event.Id, @event.Title, @event.StartAt, @event.EndAt, @event.TotalSeats, @event.AvailableSeats, @event.Description);
        
        return JsonSerializer.Serialize(cacheModel, _serializationOptions);
    }

    public string GetJsonEventList(List<Event> list)
    {
        var cacheList = list.Select(t => new EventCacheModel(t.Id, t.Title, t.StartAt, t.EndAt, t.TotalSeats, t.AvailableSeats, t.Description));

        return JsonSerializer.Serialize(cacheList, _serializationOptions);
    }

    /// <summary>
    /// Модель для сериализации/десериализации в кэш
    /// </summary>
    /// <param name="Id">Идентификатор</param>
    /// <param name="Title">Название</param>
    /// <param name="Start">Дата начала</param>
    /// <param name="End">Дата окончания</param>
    /// <param name="TotalSeats">Общее число мест</param>
    /// <param name="AvailableSeats">Доступное число мест. Допустимо не передавать, тогда по умолчанию будет равно общему числу мест</param>
    /// <param name="Description">Описание</param>
    internal record EventCacheModel(Guid Id, string Title, DateTimeOffset Start, DateTimeOffset End, int TotalSeats, int AvailableSeats, string? Description);
}
