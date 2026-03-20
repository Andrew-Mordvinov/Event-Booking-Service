using System.ComponentModel.DataAnnotations;

using EventBookingService.Common.Validations.Attributes;

namespace EventBookingService.Models.Events.Requests;

/// <summary>
/// Dto для входящего запроса модификации события
/// </summary>
public class ModifyEventRequest
{
    [Required(ErrorMessage = "Наименование мероприятия обязательно для заполнения")]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = "Дата начала мероприятия обязательна для заполнения")]
    public DateTime? StartAt { get; set; }

    [Required(ErrorMessage = "Дата окончания мероприятия обязательна для заполнения")]
    [GreaterThan(nameof(StartAt), ErrorMessage = "Дата окончания должна быть позже даты начала")]
    public DateTime? EndAt { get; set; }
}

