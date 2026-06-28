using Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Application.Users.Infrastructure;
using Infrastructure.Users.Settings;

namespace Infrastructure.Users.Security;

/// <summary>
/// Реализация с Jwt токеном
/// </summary>
public class JwtTokenGenerator(IOptions<JwtSettings> options) : ITokenGenerator
{
    private readonly JwtSettings _jwtSettings = options.Value ?? throw new ArgumentNullException("Не удалось инициализировать JwtSettings");

    public string GenerateToken(User user)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [ClaimTypes.Role] = user.Role.ToString()
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var currentTimestamp = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            Claims = claims,
            NotBefore = currentTimestamp,
            Expires = currentTimestamp.AddMinutes(_jwtSettings.ExpiryMinutes),
            IssuedAt = currentTimestamp,
            SigningCredentials = creds
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
