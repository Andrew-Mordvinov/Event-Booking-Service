using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Events.Redis;

/// <summary>
/// Настройки подключения к Redis
/// </summary>
public class RedisSettings
{
    /// <summary>
    /// Эндпоинты Redis
    /// </summary>
    [MinLength(1, ErrorMessage = "Необходим как минимум один эндпоинт для Redis")]
    public required string[] EndPoints { get; init; }

    /// <summary>
    /// Пароль. Необязательно
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Таймаут для выполнения операций
    /// </summary>
    public int Timeout { get; init; } = 5000;

    /// <summary>
    /// Таймаут установки подключения
    /// </summary>
    public int ConnectTimeout { get; init; } = 5000;

    /// <summary>
    /// Количество ретраев подключения
    /// </summary>
    public int Retries { get; init; } = 3;
}
