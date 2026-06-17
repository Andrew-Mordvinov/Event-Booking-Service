using Application.Infrastructure;
using Domain.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Infrastructure.Http;

/// <summary>
/// Пользовательский контекст из http контекста
/// </summary>
public class HttpUserContext(IHttpContextAccessor _httpContextAccessor) : IUserContext
{
    private Guid? _userId;

    public Guid UserId
    {
        get
        {
            if (_userId is not null)
            {
                return _userId.Value;
            }

            var sub = _httpContextAccessor.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub) 
                ?? throw new Exception("Неверный формат токена, отсутствует " + JwtRegisteredClaimNames.Sub);

            if (Guid.TryParse(sub.Value, out var userId))
            {
                _userId = userId;

                return userId;
            }

            throw new Exception("Не удалось распарсить id пользователя из токена");
        }
    }

    public Task<bool> IsAdmin(CancellationToken token = default)
    {
        return Task.FromResult
        (
            _httpContextAccessor.HttpContext.User.IsInRole(Roles.Admin.ToString())
        );
    }
}
