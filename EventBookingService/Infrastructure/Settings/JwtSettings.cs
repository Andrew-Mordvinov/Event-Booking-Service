namespace Infrastructure.Settings;

/// <summary>
/// Настройки для Jwt токенов
/// </summary>
public class JwtSettings
{
    public required string SecretKey { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public int ExpiryMinutes { get; init; }
}
