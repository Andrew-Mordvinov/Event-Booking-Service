using System.ComponentModel.DataAnnotations;

namespace Application.Settings;

public class BookingSettings
{
    /// <summary>
    /// Максимальное число бронирований (активных) на одного пользователя
    /// </summary>
    [Required(ErrorMessage = "Не найден параметр MaxBookingPerUser в appsettings.json")]
    public int? MaxBookingPerUser { get; init; }
}
