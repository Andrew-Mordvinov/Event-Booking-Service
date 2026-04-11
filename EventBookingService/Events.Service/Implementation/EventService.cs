using DataAccess.Storage;
using DTO.Events.Requests;
using Events.Models;
using LinqExtensions;
using Shared;
using Shared.Paging;
using System.Linq.Expressions;
using System.Reflection;
using Validation;

namespace Events.Service.Implementation;

/// <summary>
/// Реализация <see cref="IEventService"/> с хранением данных в памяти приложения
/// </summary>
public class EventService(IStorage<Event> events) : IEventService
{
    #region Private fields

    private readonly IStorage<Event> _events = events;

    private static readonly MethodInfo _methodContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)]) 
        ?? throw new InvalidOperationException($"Невозможно получить метод {nameof(string.Contains)}");

    #endregion

    #region Base overrides

    public Task<ValidationResult<Event?>> GetEventByIdAsync(Guid id, CancellationToken token = default) =>
        _events.GetByIdAsync(id, token);

    public Task<ValidationResult<bool>> DeleteEventByIdAsync(Guid id, CancellationToken token = default) =>
        _events.RemoveAsync(id, token);

    public async Task<ValidationResult<Event?>> CreateEventAsync(
        CreateEventRequest request,
        CancellationToken token = default)
    {
        var (entity, errors) = Event.TryCreate(Guid.NewGuid(), request.Title, request.StartAt, request.EndAt, request.TotalSeats, description: request.Description);
        if (entity is null)
        {
            return ResultCreator.Fail<Event?>(null, errors);
        }

        var result = await _events.AddAsync(entity, token);

        return result.ToGeneric(entity);
    }

    public Task<ValidationResult<PaginatedResult<Event>?>> GetEventsAsync(
        EventFilters filters,
        int page,
        int pageSize,
        CancellationToken token = default)
    {
        var result = ResultCreator.Success<PaginatedResult<Event>?>(null);

        if (page < 1)
        {
            result.AddError(EventServiceErrors.InvalidPageNumber);
        }

        if (pageSize < GlobalConst.MinPageSize || pageSize > GlobalConst.MaxPageSize)
        {
            result.AddError(EventServiceErrors.PageSizeOutOfRange(GlobalConst.MinPageSize, GlobalConst.MaxPageSize));
        }

        if (!result.IsSuccessful)
        {
            return Task.FromResult(result);
        }

        var expression = GetFilterExpression(filters);

        return _events.GetPageAsync(expression, page, pageSize, token);
    }

    public async Task<ValidationResult<Event?>> ModifyEventAsync(
        Guid id,
        ModifyEventRequest request,
        CancellationToken token = default)
    {
        // Создаем объект, чтобы прогнать все валидации, т.к. поля в ModifyEventRequest nullable
        var (source, errors) = Event.TryCreate(id, request.Title, request.StartAt, request.EndAt, request.TotalSeats, description: request.Description);
        if (source is null)
        {
            return ResultCreator.Fail<Event?>(null, errors);
        }

        var result = await _events.UpdateAsync(source, token);

        return result.IsSuccessful ? 
            ResultCreator.Success(result.Value ? source : null) 
            : ResultCreator.Fail<Event?>(null, result.Errors);
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

