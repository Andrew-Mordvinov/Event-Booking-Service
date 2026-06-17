using Domain.Users;

namespace Application.Infrastructure;

/// <summary>
/// Генератор токенов для аутентификации
/// </summary>
public interface ITokenGenerator
{
    /// <summary>
    /// Генерация токена согласно настройкам по переданному пользователю
    /// </summary>
    /// <param name="user">Пользователь, по которому генерируется токен</param>
    /// <returns>Строка-токен</returns>
    string GenerateToken(User user);
}
