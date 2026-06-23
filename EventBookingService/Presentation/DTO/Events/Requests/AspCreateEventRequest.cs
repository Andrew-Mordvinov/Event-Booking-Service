using System.ComponentModel.DataAnnotations;

using Application.Attributes;
using Application.DTO.Events.Requests;

namespace Presentation.DTO.Events.Requests;

/// <summary>
/// Dto для входящего запроса создания события
/// </summary>
public class AspCreateEventRequest
{
    /// <summary>
    /// Наименование события
    /// </summary>
    [Required(ErrorMessage = "Наименование мероприятия обязательно для заполнения", AllowEmptyStrings = false)]
    public string? Title { get; init; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Дата и время начала события
    /// </summary>
    [Required(ErrorMessage = "Дата начала мероприятия обязательна для заполнения")]
    public DateTimeOffset? StartAt { get; init; }

    /// <summary>
    /// Дата и время окончания события
    /// </summary>
    [Required(ErrorMessage = "Дата окончания мероприятия обязательна для заполнения")]
    [GreaterThan(nameof(StartAt), ErrorMessage = "Дата окончания должна быть позже даты начала")]
    public DateTimeOffset? EndAt { get; init; }

    /// <summary>
    /// Общее число мест у события. Отражает максимальное количество участников
    /// </summary>
    [Required(ErrorMessage = "Общее число мест обязательно для заполнения")]
    [Range(1, int.MaxValue, ErrorMessage = "Общее число мест не должно быть меньше 1")]
    public int? TotalSeats { get; init; }

    public CreateEventRequest ToCreateEventRequest() => new()
    {
        Title = Title,
        Description = Description,
        StartAt = StartAt,
        EndAt = EndAt,
        TotalSeats = TotalSeats
    };
}
