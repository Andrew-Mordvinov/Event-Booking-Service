using Domain.Users;

namespace Application.DTO.Users;

/// <summary>
/// Запрос на регистрацию пользователя
/// </summary>
public class RegisterUserRequest
{
    /// <summary>
    /// Логин пользователя
    /// </summary>
    public required string Login { get; init; }

    /// <summary>
    /// Пароль
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Роль
    /// </summary>
    public Roles Role { get; init; }
}
