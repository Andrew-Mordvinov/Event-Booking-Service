using Domain.Users;

namespace Application.Infrastructure;

/// <summary>
/// Генератор токенов для аутентификации
/// </summary>
public interface ITokenGenerator
{
    string GenerateToken(User user);
}
