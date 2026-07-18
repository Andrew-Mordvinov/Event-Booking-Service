using System.ComponentModel.DataAnnotations;

namespace Contracts.Settings;

/// <summary>
/// Настройки для Jwt токенов
/// </summary>
public class JwtSettings
{
    [MinLength(32, ErrorMessage = "Слишком короткий SecretKey - минимум 32 символа")]
    public required string SecretKey { get; init; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Не указан Issuer")]
    public required string Issuer { get; init; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Не указан Audience")]
    public required string Audience { get; init; }

    [Required(ErrorMessage = "Не указан параметр ExpiryMinutes")]
    [Range(1, 1440, ErrorMessage = "Укажите время жизни от 1 до 1440 минут")]
    public required int ExpiryMinutes { get; init; }
}
