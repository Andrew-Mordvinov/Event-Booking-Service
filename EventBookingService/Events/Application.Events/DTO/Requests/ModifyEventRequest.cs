namespace Application.Events.DTO.Requests;

/// <summary>
/// Dto для входящего запроса модификации события
/// </summary>
public record ModifyEventRequest
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
    /// Общее число мест у события. Отражает максимальное количество участников. При модификации события учитывается актуальное число
    /// занятых мест, поэтому убедитесь, что число мест не меньше числа уже забронированных
    /// </summary>
    public int? TotalSeats { get; init; }
}

