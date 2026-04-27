using DataAccess.Abstract;
using DataAccess.Abstract.Common;
using DataAccess.Abstract.Enums;
using DTO.Presentation.Events.Requests;
using Entities.Events;
using LinqExtensions;
using Shared;
using Shared.Exceptions;
using Shared.Paging;
using System.Linq.Expressions;
using System.Reflection;

namespace Events.Service.Implementation;

/// <summary>
/// Реализация <see cref="IEventService"/> с хранением данных в памяти приложения
/// </summary>
public class EventService(
    IEventRepository _events,
    IUnitOfWork _unitOfWork) : IEventService
{
    #region Private fields

    private static readonly MethodInfo _methodContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]) 
        ?? throw new InvalidOperationException($"Невозможно получить метод {nameof(string.Contains)}");

    #endregion

    #region Base overrides

    public Task<Event?> GetEventByIdAsync(Guid id, CancellationToken token = default) =>
        _events.GetByIdAsync(id, GetMode.Readonly, token);

    public async Task<bool> DeleteEventByIdAsync(Guid id, CancellationToken token = default)
    {
        var deleted = await _events.RemoveAsync(id, token);
        if (deleted)
        {
            await _unitOfWork.SaveChangesAsync(token);
        }      

        return deleted;
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

        await _events.AddAsync(entity, token);
        await _unitOfWork.SaveChangesAsync(token);

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
        var baseEvent = await _events.GetByIdAsync(id, token: token);

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

        baseEvent.FillFrom(source);
        await _unitOfWork.SaveChangesAsync(token);

        return source;
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

