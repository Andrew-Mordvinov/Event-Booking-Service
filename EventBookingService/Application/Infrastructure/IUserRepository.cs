using Application.Infrastructure.Common;
using Domain.Users;

namespace Application.Infrastructure;

/// <summary>
/// Репозиторий пользователей
/// </summary>
public interface IUserRepository : IBaseStorage<User>
{
    /// <summary>
    /// Получить пользователя по логину
    /// </summary>
    /// <param name="login">Логин</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Пользователь или null если не найден</returns>
    Task<User?> GetByLoginAsync(string login, CancellationToken token = default);
}
