using Application.DTO.Users;

namespace Application.Interfaces;

/// <summary>
/// Сервис работы с пользователями
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Регистрирует пользователя по запросу
    /// </summary>
    /// <param name="request">Запрос на регистрацию пользователя</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Асинхронная задача</returns>
    Task RegisterUserAsync(RegisterUserRequest request, CancellationToken token = default);

    /// <summary>
    /// Аутентификация пользователя
    /// </summary>
    /// <param name="request">Запрос с данными для аутентификации</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Строка-токен</returns>
    Task<string> AuthUserAsync(AuthUserRequest request, CancellationToken token = default);
}
