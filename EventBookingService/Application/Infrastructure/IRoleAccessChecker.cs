using Domain.Users;

namespace Application.Infrastructure
{
    /// <summary>
    /// Облегченный интерфейс для проверки пользователей по идентификатору на определенные роли
    /// </summary>
    public interface IRoleAccessChecker
    {
        Task<bool> CheckUserHasRoleAsync(Guid userId, Roles role, CancellationToken token = default);
    }
}
