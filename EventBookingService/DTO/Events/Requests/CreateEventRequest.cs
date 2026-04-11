using System.ComponentModel.DataAnnotations;
using Shared.Attributes;

namespace DTO.Events.Requests;

/// <summary>
/// Dto для входящего запроса создания события
/// </summary>
public class CreateEventRequest
{
    [Required(ErrorMessage = "Наименование мероприятия обязательно для заполнения", AllowEmptyStrings = false)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = "Дата начала мероприятия обязательна для заполнения")]
    public DateTime? StartAt { get; set; }

    [Required(ErrorMessage = "Дата окончания мероприятия обязательна для заполнения")]
    [GreaterThan(nameof(StartAt), ErrorMessage = "Дата окончания должна быть позже даты начала")]
    public DateTime? EndAt { get; set; }

    [Required(ErrorMessage = "Общее число мест обязательно для заполнения")]
    [Range(1, int.MaxValue, ErrorMessage = "Общее число мест не должно быть меньше 1")]
    public int? TotalSeats { get; set; }
}
