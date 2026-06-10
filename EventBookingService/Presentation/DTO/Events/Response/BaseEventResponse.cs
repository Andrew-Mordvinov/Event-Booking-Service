using Domain.Events;

namespace Presentation.DTO.Events.Response;

/// <summary>
/// Ответ на запрос о событии
/// </summary>
public class BaseEventResponse
{
    /// <summary>
    /// Идентификатор события
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Наименование события
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание события
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время начала события
    /// </summary>
    public DateTimeOffset StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания события
    /// </summary>
    public DateTimeOffset EndAt { get; set; }

    /// <summary>
    /// Общее число мест у события. Отражает максимальное количество участников
    /// </summary>
    public int TotalSeats { get; set; }

    /// <summary>
    /// Число доступных для бронирования мест
    /// </summary>
    public int AvailableSeats { get; set; }

    public static BaseEventResponse FromEvent(Event entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        StartAt = entity.StartAt,
        EndAt = entity.EndAt,
        TotalSeats = entity.TotalSeats,
        AvailableSeats = entity.AvailableSeats
    };
}
