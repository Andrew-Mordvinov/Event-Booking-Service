using Application.Events.Infrastructure;

using Domain.Events;

namespace Infrastructure.Events.Redis;

public class RedisEventCache : IEventCache
{
    // TODO реализовать заглушки, добавить настройки
    public Task<bool> GetEventAsync(Guid id, out Event? @event, CancellationToken token = default)
    {
        @event = null;
        return Task.FromResult(false);
    }

    public Task<bool> GetTopSalesEventAsync(out List<Event> topEvents, CancellationToken token = default)
    {
        topEvents = [];
        return Task.FromResult(false);
    }

    public Task SetEventAsync(Event? @event, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    public Task SetTopSalesEventAsync(List<Event>? topEvents, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }
}
