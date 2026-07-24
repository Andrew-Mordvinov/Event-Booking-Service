using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Events.Redis;

/// <summary>
/// Настройки времени жизни для разных ключей кэша
/// </summary>
public class TTLSettings
{
    /// <summary>
    /// Время жизни единичного события в кэше, миллисекунды
    /// </summary>
    [Required(ErrorMessage = "Не указан параметр SingleEventMs")]
    [Range(100, 5000, ErrorMessage = "Укажите время жизни от 100 до 5000 миллисекунд")]
    public int SingleEventMsec { get; init; }

    /// <summary>
    /// Время жизни топа событий по продажам в кэше, секунды
    /// </summary>
    [Required(ErrorMessage = "Не указан параметр TopSalesMs")]
    [Range(1, 60, ErrorMessage = "Укажите время жизни от 1 до 60 секунд")]
    public int TopSalesSec { get; init; }
}
