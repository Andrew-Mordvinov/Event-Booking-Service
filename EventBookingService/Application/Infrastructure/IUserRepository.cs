using Domain.Users;

namespace Application.Infrastructure;

/// <summary>
/// Репозиторий пользователей
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Получить пользователя по логину
    /// </summary>
    /// <param name="login">Логин</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Пользователь или null если не найден</returns>
    Task<User?> GetByLoginAsync(string login, CancellationToken token = default);

    /// <summary>
    /// Добавление пользователя в хранилище
    /// </summary>
    /// <param name="user">Пользователь</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Асинхронная задача</returns>
    Task AddAsync(User user, CancellationToken token = default);
}
