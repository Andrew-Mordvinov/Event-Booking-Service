namespace EventBookingService.Models.Events.Response;

/// <summary>
/// Базовый ответ на запрос с полями как в <see cref="Event"/>. Представляет собой
/// проекцию <see cref="Event"/> с теми свойствами, которые должны быть переданы в
/// качестве ответа на запрос (пока что все)
/// </summary>
public class BaseEventResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    public static BaseEventResponse FromEvent(Event entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        StartAt = entity.StartAt,
        EndAt = entity.EndAt
    };
}
