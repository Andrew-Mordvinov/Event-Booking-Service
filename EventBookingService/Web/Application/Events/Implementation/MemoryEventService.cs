using EventBookingService.Common;
using EventBookingService.Common.Paging;
using EventBookingService.Common.Storage;
using EventBookingService.Common.Validations;
using EventBookingService.Models.Events;
using EventBookingService.Models.Events.Requests;

namespace EventBookingService.Application.Events.Implementation;

/// <summary>
/// Реализация <see cref="IEventService"/> с хранением данных в памяти приложения
/// </summary>
public class MemoryEventService([FromKeyedServices("Static")] IStorage<Event> events) : IEventService
{
    #region Private fields

    private readonly IStorage<Event> _events = events;

    #endregion

    #region Base overrides

    public Task<ValidationResult<Event?>> CreateEventAsync(CreateEventRequest request, CancellationToken token = default)
    {
        var (entity, errors) = Event.TryCreate(Guid.NewGuid(), request.Title, request.StartAt, request.EndAt, request.Description);
        if (entity is null)
        {
            return Task.FromResult(ResultCreator.Fail<Event?>(null, errors));
        }
        _events.Add(entity);

        // чтобы нельзя было модифицировать полученный объект в обход, выкидываем копию
        return Task.FromResult(ResultCreator.Success(entity.Clone()));
    }

    public Task<ValidationResult<bool>> DeleteEventByIdAsync(Guid id, CancellationToken token = default)
    {
        var count = _events.Remove(id);

        return Task.FromResult(ResultCreator.Success(count > 0));
    }

    public Task<ValidationResult<PaginatedResult<Event>?>> GetEventsAsync(EventFilters filters, int page, int pageSize, CancellationToken token = default)
    {
        var result = ResultCreator.Success<PaginatedResult<Event>?>(null);

        if (page < 1)
        {
            result.AddError(MemoryEventServiceErrors.InvalidPageNumber);
        }

        if (pageSize < 1 || pageSize > 100)
        {
            result.AddError(MemoryEventServiceErrors.PageSizeOutOfRange(GlobalConst.MinPageSize, GlobalConst.MaxPageSize));
        }

        if (!result.IsSuccessful)
        {
            return Task.FromResult(result);
        }

        var (filtered, count) = ApplyFilter(filters);

        if (count < 1)
        {
            return Task.FromResult(result);
        }

        var totalPages = (count + pageSize - 1) / pageSize;

        if (totalPages < page)
        {
            result.AddError(MemoryEventServiceErrors.PageNotFound(page, totalPages));
        }

        if (!result.IsSuccessful)
        {
            return Task.FromResult(result);
        }

        var dataPage = filtered.Skip((page - 1) * pageSize).Take(pageSize);

        var paginatedResult = new PaginatedResult<Event>
        {
            CurrentPage = page,
            TotalPages = totalPages,
            FilteredCount = count,
            Items = [.. dataPage]
        };

        result.Value = paginatedResult;

        return Task.FromResult(result);
    }

    public Task<ValidationResult<Event?>> GetEventByIdAsync(Guid id, CancellationToken token = default)
    {
        var entity = _events.GetById(id);
        // чтобы нельзя было модифицировать полученный объект в обход, выкидываем копию
        return Task.FromResult(ResultCreator.Success(entity?.Clone()));
    }

    public Task<ValidationResult<Event?>> ModifyEventAsync(Guid id, ModifyEventRequest request, CancellationToken token = default)
    {
        var target = _events.GetById(id);

        if (target == null)
        {
            return Task.FromResult(ResultCreator.Success<Event>(null));
        }

        // Создаем объект, чтобы прогнать все валидации, т.к. поля в ModifyEventRequest nullable
        var (source, errors) = Event.TryCreate(id, request.Title, request.StartAt, request.EndAt, request.Description);
        if (source is null)
        {
            return Task.FromResult(ResultCreator.Fail<Event?>(null, errors));
        }

        target.FillFrom(source);
        // target лежит в коллекции, поэтому на выход копию
        return Task.FromResult(ResultCreator.Success(source));
    }

    #endregion

    #region Private methods

    private (IEnumerable<Event> collection, int count) ApplyFilter(EventFilters filters)
    {
        if (_events.Count == 0)
        {
            return ([], 0);
        }

        var result = _events.GetAll();

        var (title, from, to) = filters;

        if (title is null && from is null && to is null)
        {
            return (result, _events.Count);
        }

        if (title is not null)
        {
            result = result.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        }

        if (from is not null)
        {
            result = result.Where(e => e.StartAt >= from);
        }

        if (to is not null)
        {
            result = result.Where(e => e.EndAt <= to);
        }

        return (result, result.Count());
    }

    #endregion
}

