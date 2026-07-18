using System.Linq.Expressions;
using System.Reflection;

using Application.Events.DTO.Requests;
using Application.Events.DTO.Result;
using Application.Events.Infrastructure;
using Application.Events.Interfaces;

using Domain.Events;

using Shared.Exceptions;
using Shared.Helpers.LinqExtensions;
using Shared.Infrastructure.Abstract;
using Shared.Infrastructure.Abstract.Enums;

namespace Application.Events.Implementation;

/// <summary>
/// Реализация <see cref="IEventService"/>
/// </summary>
public class EventService(
    IEventRepository _eventRepository,
    IEventCache _eventCache,
    IUnitOfWork _unitOfWork) : IEventService
{
    #region Private fields

    private static readonly MethodInfo _methodContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])
        ?? throw new InvalidOperationException($"Невозможно получить метод {nameof(string.Contains)}");

    #endregion

    #region Base overrides

    public async Task<Event> GetEventByIdAsync(Guid id, CancellationToken token = default)
    {
        var (status, @event) = await _eventCache.GetEventAsync(id, token);
        if (status && @event is not null)
        {
            return @event;
        }

        @event = await _eventRepository.GetByIdAsync(id, GetMode.Readonly, token) ?? throw new NotFoundException(EventServiceErrors.EventNotFound(id));
        
        await _eventCache.SetEventAsync(id, @event, token);

        return @event;
    }

    public async Task DeleteEventByIdAsync(Guid id, CancellationToken token = default)
    {
        var deleted = await _eventRepository.RemoveAsync(id, token);
        if (deleted)
        {
            await _unitOfWork.SaveChangesAsync(token);
            // Сброс кэша
            await _eventCache.SetEventAsync(id, null, token);
            return;
        }

        throw new NotFoundException(EventServiceErrors.EventNotFound(id));
    }

    public async Task<Event> CreateEventAsync(
        CreateEventRequest request,
        CancellationToken token = default)
    {
        var (entity, errors) = Event.TryCreate(Guid.NewGuid(), request.Title, request.StartAt, request.EndAt, request.TotalSeats, description: request.Description);
        if (entity is null)
        {
            throw new ValidationException(errors);
        }

        await _eventRepository.AddAsync(entity, token);
        await _unitOfWork.SaveChangesAsync(token);

        await _eventCache.SetEventAsync(entity.Id, entity, token);

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

        return _eventRepository.GetPageAsync(expression, page, pageSize, token);
    }

    public async Task<Event> ModifyEventAsync(
        Guid id,
        ModifyEventRequest request,
        CancellationToken token = default)
    {
        var baseEvent = await _eventRepository.GetByIdAsync(id, token: token) ?? throw new NotFoundException(EventServiceErrors.EventNotFound(id));

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

        baseEvent.FillFrom(source);
        await _unitOfWork.SaveChangesAsync(token);

        await _eventCache.SetEventAsync(id,baseEvent, token);

        return source;
    }

    public async Task<List<Event>> GetTopSalesEventsAsync(CancellationToken token = default)
    {
        var (status, result) = await _eventCache.GetTopSalesEventAsync(token);
        if (status && result is not null)
        {
            return result;
        }

        result = await _eventRepository.GetTopSalesEventsAsync(token);

        await _eventCache.SetTopSalesEventAsync(result, token);

        return result;
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

