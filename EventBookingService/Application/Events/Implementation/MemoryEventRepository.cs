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
            return Task.FromResult(new ValidationResult<Event?>(null, errors));
        }
        _events.Add(entity);

        return Task.FromResult(new ValidationResult<Event?>(entity));
    }

    public Task<ValidationResult<bool>> DeleteEventByIdAsync(Guid id, CancellationToken token = default)
    {
        var removed = _events.RemoveAll(t => t.Id == id);

        return Task.FromResult(new ValidationResult<bool>(removed > 0));
    }

    public Task<ValidationResult<IReadOnlyCollection<Event>>> GetAllEventsAsync(CancellationToken token = default)
    {
        return Task.FromResult(new ValidationResult<IReadOnlyCollection<Event>>(_events.AsReadOnly()));
    }

    public Task<ValidationResult<Event?>> GetEventByIdAsync(Guid id, CancellationToken token = default)
    {
        return Task.FromResult(new ValidationResult<Event?>(_events.FirstOrDefault(t => t.Id == id)));
    }

    public Task<ValidationResult<Event?>> ModifyEventAsync(Guid id, ModifyEventRequest request, CancellationToken token = default)
    {
        var target = _events.FirstOrDefault(t => t.Id == id);

        if (target == null)
        {
            return Task.FromResult(new ValidationResult<Event?>(null));
        }

        // Создаем объект, чтобы прогнать все валидации, т.к. поля в ModifyEventRequest nullable
        var (source, errors) = Event.TryCreate(Guid.NewGuid(), request.Title, request.StartAt, request.EndAt, request.Description);
        if (source is null)
        {
            return Task.FromResult(new ValidationResult<Event?>(null, errors));
        }

        target.FillFrom(source);

        return Task.FromResult(new ValidationResult<Event?>(target));
    }
}

