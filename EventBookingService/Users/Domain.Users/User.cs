using Shared.Entities;
using Shared.Roles;

namespace Domain.Users;

/// <summary>
/// Пользователь
/// </summary>
public class User : IHasId
{
    public Guid Id { get; }

    public string Login { get; protected set; } = string.Empty;

    public string PasswordHash { get; protected set; } = string.Empty;

    public Roles Role { get; protected set; }

    protected User()
    {

    }

    public User(Guid id, string login, string passwordHash, Roles role)
    {
        Id = id;
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }

    public bool IsAdmin() => Role == Roles.Admin;
}
