using DataAccess.Storage;
using DTO.Events.Requests;
using Events.Models;
using LinqExtensions;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shared.Exceptions;
using Shared.Paging;
using System.Linq.Expressions;
using System.Reflection;

namespace Events.Service.Implementation;

/// <summary>
/// Реализация <see cref="IEventService"/> с хранением данных в памяти приложения
/// </summary>
public class EventService([FromKeyedServices("Mem")]IStorage<Event> events) : IEventService
{
    #region Private fields

    private readonly IStorage<Event> _events = events;

    private static readonly MethodInfo _methodContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]) 
        ?? throw new InvalidOperationException($"Невозможно получить метод {nameof(string.Contains)}");

    #endregion

    #region Base overrides

    public Task<Event?> GetEventByIdAsync(Guid id, CancellationToken token = default) =>
        _events.GetByIdAsync(id, token);

    public Task<bool> DeleteEventByIdAsync(Guid id, CancellationToken token = default) =>
        _events.RemoveAsync(id, token);

    public async Task<Event> CreateEventAsync(
        CreateEventRequest request,
        CancellationToken token = default)
    {
        var (entity, errors) = Event.TryCreate(Guid.NewGuid(), request.Title, request.StartAt, request.EndAt, request.TotalSeats, description: request.Description);
        if (entity is null)
        {
            throw new ValidationException(errors);
        }

        await _events.AddAsync(entity, token);

        return entity;
    }

    public Task<PaginatedResult<Event>?> GetEventsAsync(
        EventFilters filters,
        int page,
        int pageSize,
        CancellationToken token = default)
    {
        var errors = new List<string>();

        if (page < 1)
        {
            errors.Add(EventServiceErrors.InvalidPageNumber);
        }

        if (pageSize < GlobalConst.MinPageSize || pageSize > GlobalConst.MaxPageSize)
        {
            errors.Add(EventServiceErrors.PageSizeOutOfRange(GlobalConst.MinPageSize, GlobalConst.MaxPageSize));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var expression = GetFilterExpression(filters);

        return _events.GetPageAsync(expression, page, pageSize, token);
    }

    public async Task<Event?> ModifyEventAsync(
        Guid id,
        ModifyEventRequest request,
        CancellationToken token = default)
    {
        var baseEvent = await _events.GetByIdAsync(id, token);

        if (baseEvent is null)
        {
            return null;
        }

        // Создаем объект, чтобы прогнать все валидации, т.к. поля в ModifyEventRequest nullable
        var (source, errors) = Event.TryCreate(
            id, 
            request.Title,
            request.StartAt,
            request.EndAt,
            request.TotalSeats,
            request.TotalSeats - baseEvent.TotalSeats + baseEvent.AvailableSeats,
            description: request.Description);

        if (source is null)
        {
            throw new ValidationException(errors);
        }

        var result = await _events.UpdateAsync(source, token);

        return result ? source : null;
    }

    #endregion

    #region Private methods

    private static Expression<Func<Event, bool>>? GetFilterExpression(EventFilters filters)
    {
        var (title, from, to) = filters;

        if (title is null && from is null && to is null)
        {
            return null;
        }

        var predicate = PredicateBuilder.True<Event>();

        if (title is not null)
        {
            var titleLower = title.ToLower();
            predicate = predicate.And(e => e.Title.ToLower().Contains(titleLower));
        }

        if (from is not null)
        {
            predicate = predicate.And(e => e.StartAt >= from);
        }

        if (to is not null)
        {
            predicate = predicate.And(e => e.EndAt <= to);
        }

        return predicate;
    }

    #endregion
}

