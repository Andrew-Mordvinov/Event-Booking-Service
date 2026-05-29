using Application.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.DTO.Events.Requests;

/// <summary>
/// Dto для входящего запроса модификации события
/// </summary>
public class ModifyEventRequest
{
    /// <summary>
    /// Наименование события
    /// </summary>
    [Required(ErrorMessage = "Наименование мероприятия обязательно для заполнения")]
    public string? Title { get; set; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата и время начала события
    /// </summary>
    [Required(ErrorMessage = "Дата начала мероприятия обязательна для заполнения")]
    public DateTimeOffset? StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания события
    /// </summary>
    [Required(ErrorMessage = "Дата окончания мероприятия обязательна для заполнения")]
    [GreaterThan(nameof(StartAt), ErrorMessage = "Дата окончания должна быть позже даты начала")]
    public DateTimeOffset? EndAt { get; set; }

    /// <summary>
    /// Общее число мест у события. Отражает максимальное количество участников. При модификации события учитывается актуальное число
    /// занятых мест, поэтому убедитесь, что число мест не меньше числа уже забронированных
    /// </summary>
    [Required(ErrorMessage = "Общее число мест обязательно для заполнения")]
    [Range(1, int.MaxValue, ErrorMessage = "Общее число мест не должно быть меньше 1")]
    public int? TotalSeats { get; set; }
}

