using EventBookingService.Common.Validations;
using EventBookingService.Models.Events;
using EventBookingService.Models.Events.Requests;

namespace EventBookingService.Application.Events.Implementation;

/// <summary>
/// Реализация <see cref="IEventRepository"/> с хранением данных в памяти приложения
/// </summary>
public class MemoryEventRepository : IEventRepository
{
    private static readonly List<Event> _events = new();

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
        var removed = _events.RemoveAll(t => t.Id == id);

        return Task.FromResult(ResultCreator.Success(removed > 0));
    }

    public Task<ValidationResult<IReadOnlyCollection<Event>?>> GetAllEventsAsync(CancellationToken token = default)
    {
        IReadOnlyCollection<Event> all = _events.AsReadOnly();

        return Task.FromResult(ResultCreator.Success(all));
    }

    public Task<ValidationResult<Event?>> GetEventByIdAsync(Guid id, CancellationToken token = default)
    {
        var entity = _events.FirstOrDefault(t => t.Id == id);
        // чтобы нельзя было модифицировать полученный объект в обход, выкидываем копию
        return Task.FromResult(ResultCreator.Success(entity?.Clone()));
    }

    public Task<ValidationResult<Event?>> ModifyEventAsync(Guid id, ModifyEventRequest request, CancellationToken token = default)
    {
        var target = _events.FirstOrDefault(t => t.Id == id);

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
}

