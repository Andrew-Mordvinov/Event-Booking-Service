using System.Text.Json;

using Application.Events.Infrastructure;

using Domain.Events;

using FluentAssertions;

using Infrastructure.Events.Redis;

using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

using static Infrastructure.Events.Redis.Serializer.CacheEventSerializer;

namespace Tests.Integration.Events.Redis;

[Collection(SharedFixture.RedisTests)]
public class RedisEventCacheTests(SharedFixture sharedFixture) : IAsyncLifetime
{
    private readonly SharedFixture _sharedFixture = sharedFixture;

    public async ValueTask InitializeAsync()
    {
        await _sharedFixture.PrepareRedisAsync();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    #region Helping

    private async Task<string?> GetJsonFromCache(string key)
    {
        var multiplexer = _sharedFixture.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        var db = multiplexer.GetDatabase();
        return await db.StringGetAsync(key);
    }

    private Event? GetEventFromJson(string json)
    {
        var eventModel = JsonSerializer.Deserialize<EventCacheModel>(json);

        if (eventModel is null) 
        {
            return null;
        }

        var (@event, error) = Event.TryCreate
        (
            eventModel.Id,
            eventModel.Title,
            eventModel.Start,
            eventModel.End,
            eventModel.TotalSeats,
            eventModel.AvailableSeats,
            eventModel.Description
        );

        return @event;
    }

    private List<Event?> GetListEventFromJson(string json)
    {
        var rawList = JsonSerializer.Deserialize<List<EventCacheModel>>(json);

        if (rawList is null)
        {
            return [];
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

                return @event;
            })
            .ToList();

        return topList;
    }

    private async Task AddEventsAsTopToCache(params Event[] events)
    {
        var multiplexer = _sharedFixture.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        var db = multiplexer.GetDatabase();
        await db.StringSetAsync
        (
            RedisEventCache.TopSalesKey,
            JsonSerializer.Serialize(events.Select(t => new EventCacheModel(t.Id, t.Title, t.StartAt, t.EndAt, t.TotalSeats, t.AvailableSeats, t.Description)))
        );
    }

    private async Task AddEventsAsBrokenTopToCache(params Event[] events)
    {
        var multiplexer = _sharedFixture.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        var db = multiplexer.GetDatabase();
        await db.StringSetAsync
        (
            RedisEventCache.TopSalesKey,
            JsonSerializer.Serialize(events.Select(t => new EventCacheModel(t.Id, string.Empty, t.EndAt, t.StartAt, t.TotalSeats, t.AvailableSeats, t.Description)))
        );
    }

    private async Task AddEventsToCache(params Event[] events)
    {
        var multiplexer = _sharedFixture.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        var db = multiplexer.GetDatabase();

        foreach (var @event in events)
        {
            var cacheModel = new EventCacheModel(@event.Id, @event.Title, @event.StartAt, @event.EndAt, @event.TotalSeats, @event.AvailableSeats, @event.Description);

            await db.StringSetAsync
            (
                RedisEventCache.EventKeyTemplate(@event.Id),
                JsonSerializer.Serialize(cacheModel)
            );
        }
    }

    private async Task AddBrokenEventsToCache(params Event[] events)
    {
        var multiplexer = _sharedFixture.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        var db = multiplexer.GetDatabase();

        foreach (var @event in events)
        {
            // Ломаем модельку, специально, чтобы при восстановлении Event бросал ошибку
            var cacheModel = new EventCacheModel(@event.Id, string.Empty, @event.EndAt, @event.StartAt, @event.TotalSeats, @event.AvailableSeats, @event.Description);

            await db.StringSetAsync
            (
                RedisEventCache.EventKeyTemplate(@event.Id),
                JsonSerializer.Serialize(cacheModel)
            );
        }
    }

    private static Event CreateTestEvent() => new
    (
        Guid.NewGuid(),
        "Title",
        DateTimeOffset.UtcNow.AddHours(1),
        DateTimeOffset.UtcNow.AddHours(2),
        15,
        10,
        "Desc"
    );

    private static Event[] CreateTestTop() =>
    [
        // Для теста кэша не важно, что их там 10 должно быть и в определенном порядке
        CreateTestEvent(),
        CreateTestEvent(),
        CreateTestEvent(),
        CreateTestEvent()
    ];

    #endregion

    #region GetEventAsync

    [Fact]
    public async Task GetEventAsync_ExistInCache_ReturnSuccessfully()
    {
        // Arrange
        var @event = CreateTestEvent();
        await AddEventsToCache(@event);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var eventCache = scope.ServiceProvider.GetRequiredService<IEventCache>();
        // Act
        var (state, result)  = await eventCache.GetEventAsync(@event.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(@event);
        state.Should().BeTrue();
    }

    [Fact]
    public async Task GetEventAsync_NotExistInCache_ReturnNull()
    {
        // Arrange
        var @event = CreateTestEvent();
        await AddEventsToCache(@event);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var eventCache = scope.ServiceProvider.GetRequiredService<IEventCache>();
        // Act
        var (state, result) = await eventCache.GetEventAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
        state.Should().BeFalse();
    }

    [Fact]
    public async Task GetEventAsync_EventWasBroken_ReturnNullWithoutException()
    {
        // Arrange
        var @event = CreateTestEvent();
        await AddBrokenEventsToCache(@event);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var eventCache = scope.ServiceProvider.GetRequiredService<IEventCache>();
        // Act
        var (state, result) = await eventCache.GetEventAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
        state.Should().BeFalse();
    }

    #endregion

    #region GetTopSalesEventAsync

    [Fact]
    public async Task GetTopSalesEventAsync_ExistInCache_ReturnSuccessfully()
    {
        // Arrange
        var events = CreateTestTop();
        await AddEventsAsTopToCache(events);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var eventCache = scope.ServiceProvider.GetRequiredService<IEventCache>();
        // Act
        var (state, result) = await eventCache.GetTopSalesEventAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(events);
        state.Should().BeTrue();
    }

    [Fact]
    public async Task GetTopSalesEventAsync_NotExistInCache_ReturnEmpty()
    {
        // Act
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var eventCache = scope.ServiceProvider.GetRequiredService<IEventCache>();

        var (state, result) = await eventCache.GetTopSalesEventAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
        state.Should().BeFalse();
    }

    [Fact]
    public async Task GetTopSalesEventAsync_ListWasBroken_ReturnEmpty()
    {
        // Arrange
        var events = CreateTestTop();
        await AddEventsAsBrokenTopToCache(events);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var eventCache = scope.ServiceProvider.GetRequiredService<IEventCache>();
        // Act
        var (state, result) = await eventCache.GetTopSalesEventAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
        state.Should().BeFalse();
    }

    #endregion

    #region SetEventAsync

    [Fact]
    public async Task SetEventAsync_CorrectEvent_SetSuccessfully()
    {
        // Arrange
        var @event = CreateTestEvent();

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var eventCache = scope.ServiceProvider.GetRequiredService<IEventCache>();
        // Act
        await eventCache.SetEventAsync(@event.Id, @event, TestContext.Current.CancellationToken);

        // Assert
        var json = await GetJsonFromCache(RedisEventCache.EventKeyTemplate(@event.Id));
        var result = GetEventFromJson(json ?? string.Empty);

        result.Should().BeEquivalentTo(@event);
    }

    [Fact]
    public async Task SetEventAsync_AfterExpiration_CacheInvalidated()
    {
        // Arrange
        var @event = CreateTestEvent();

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var eventCache = scope.ServiceProvider.GetRequiredService<IEventCache>();

        // Act
        await eventCache.SetEventAsync(@event.Id, @event, TestContext.Current.CancellationToken);
        await Task.Delay(101, TestContext.Current.CancellationToken);

        // Assert
        var json = await GetJsonFromCache(RedisEventCache.EventKeyTemplate(@event.Id));
        json.Should().BeNull();
    }

    #endregion

    #region SetTopSalesEventAsync

    [Fact]
    public async Task SetTopSalesEventAsync_CorrectEvent_SetSuccessfully()
    {
        // Arrange
        var events = CreateTestTop();

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var eventCache = scope.ServiceProvider.GetRequiredService<IEventCache>();
        // Act
        await eventCache.SetTopSalesEventAsync([..events], TestContext.Current.CancellationToken);

        // Assert
        var json = await GetJsonFromCache(RedisEventCache.TopSalesKey);
        var result = GetListEventFromJson(json ?? string.Empty);

        result.Should().BeEquivalentTo(events, op => op.WithStrictOrdering());
    }

    [Fact]
    public async Task SetTopSalesEventAsync_AfterExpiration_CacheInvalidated()
    {
        // Arrange
        var @event = CreateTestEvent();

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var eventCache = scope.ServiceProvider.GetRequiredService<IEventCache>();

        // Act
        await eventCache.SetEventAsync(@event.Id, @event, TestContext.Current.CancellationToken);
        await Task.Delay(1001, TestContext.Current.CancellationToken);

        // Assert
        var json = await GetJsonFromCache(RedisEventCache.TopSalesKey);
        json.Should().BeNull();
    }

    #endregion
}
