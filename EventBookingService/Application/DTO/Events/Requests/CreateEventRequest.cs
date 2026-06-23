namespace Application.DTO.Events.Requests;

/// <summary>
/// Dto для входящего запроса создания события
/// </summary>
public record CreateEventRequest
{
    /// <summary>
    /// Наименование события
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Дата и время начала события
    /// </summary>
    public DateTimeOffset? StartAt { get; init; }

    /// <summary>
    /// Дата и время окончания события
    /// </summary>
    public DateTimeOffset? EndAt { get; init; }

    /// <summary>
    /// Общее число мест у события. Отражает максимальное количество участников
    /// </summary>
    public int? TotalSeats { get; init; }
}
