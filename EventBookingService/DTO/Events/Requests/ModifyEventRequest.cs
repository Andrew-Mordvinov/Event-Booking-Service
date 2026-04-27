using System.ComponentModel.DataAnnotations;
using Shared.Attributes;

namespace DTO.Presentation.Events.Requests;

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

    // Пока что не требуется определять какую-то логику сохранения числа мест и прочее, поэтому апдейт будет примитивным для
    // сохранения концепции Put как общего обновления ресурса. Будем требовать передачи общего числа мест, хотя такую
    // логику нужно будет менять или ограничивать в будущем
    [Required(ErrorMessage = "Общее число мест обязательно для заполнения")]
    [Range(1, int.MaxValue, ErrorMessage = "Общее число мест не должно быть меньше 1")]
    public int? TotalSeats { get; set; }
}

