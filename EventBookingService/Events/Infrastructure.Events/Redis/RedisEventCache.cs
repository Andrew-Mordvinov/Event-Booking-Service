using Application.Events.Infrastructure;

using Domain.Events;

using Infrastructure.Events.Redis.Serializer;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

namespace Infrastructure.Events.Redis;

/// <summary>
/// Реализация кэша событий с помощью Redis
/// </summary>
public class RedisEventCache(
    IConnectionMultiplexer _connectionMultiplexer,
    ICacheEventSerializer _eventSerializer,
    IOptions<TTLSettings> _ttlSettings,
    ILogger<RedisEventCache> _logger) : IEventCache
{
    /// <summary>
    /// Метод формирования ключа для одиночного события
    /// </summary>
    /// <param name="id">Id события</param>
    /// <returns>Строка ключа</returns>
    internal static string EventKeyTemplate(Guid id) => $"events:{id}";

    /// <summary>
    /// Ключ топа событий по продажам
    /// </summary>
    internal const string TopSalesKey = "events:top10";

    private readonly Lazy<IDatabase> _database = new(_connectionMultiplexer.GetDatabase());

    private readonly TimeSpan _singleEventTTL = TimeSpan.FromMilliseconds(_ttlSettings.Value.SingleEventMsec);
    private readonly TimeSpan _topTTL = TimeSpan.FromSeconds(_ttlSettings.Value.TopSalesSec);

    public async Task<(bool, Event?)> GetEventAsync(Guid id, CancellationToken token = default)
    {
        if (!_connectionMultiplexer.IsConnected)
        {
            _logger.LogWarning("Соединение с Redis не установлено, обращение к кэшу не удалось");

            return (false, null);
        }

        try
        {
            var value = await _database.Value.StringGetAsync(EventKeyTemplate(id));

            if (!value.HasValue)
            {
                return (false, null);
            }

            var stringValue = value.ToString();

            return (true, _eventSerializer.GetEvent(stringValue));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Возникло исключение при извлечении значения из кэша");

            return (false, null);
        }
    }

    public async Task<(bool, List<Event>)> GetTopSalesEventAsync(CancellationToken token = default)
    {
        if (!_connectionMultiplexer.IsConnected)
        {
            return (false, []);
        }

        try
        {
            var value = await _database.Value.StringGetAsync(TopSalesKey);

            if (!value.HasValue)
            {
                return (false, []);
            }

            var stringValue = value.ToString();

            return (true, _eventSerializer.GetEventList(stringValue));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Возникло исключение при извлечении значения из кэша");

            return (false, []);
        }
    }

    public async Task SetEventAsync(Guid id, Event? @event, CancellationToken token = default)
    {
        if (!_connectionMultiplexer.IsConnected)
        {
            return;
        }

        try
        {
            var value = @event is null ? RedisValue.Null : RedisValue.Unbox(_eventSerializer.GetJsonEvent(@event));
            var result = await _database.Value.StringSetAsync(EventKeyTemplate(id), value, _singleEventTTL);

            if (!result)
            {
                _logger.LogError("Не удалось установить значение в кэш по ключу {Key}", EventKeyTemplate(id));
            }

            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Возникло исключение при установке значения в кэш");

            return;
        }
    }

    public async Task SetTopSalesEventAsync(List<Event>? topEvents, CancellationToken token = default)
    {
        if (!_connectionMultiplexer.IsConnected)
        {
            return;
        }

        try
        {
            var value = topEvents is null ? RedisValue.Null : RedisValue.Unbox(_eventSerializer.GetJsonEventList(topEvents));
            var result = await _database.Value.StringSetAsync(TopSalesKey, value, _topTTL);

            if (!result)
            {
                _logger.LogError("Не удалось установить значение в кэш по ключу {Key}", TopSalesKey);
            }

            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Возникло исключение при установке значения в кэш");

            return;
        }
    }
}
