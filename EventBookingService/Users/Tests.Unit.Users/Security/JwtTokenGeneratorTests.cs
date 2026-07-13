using System.Security.Claims;

using Contracts.Settings;

using Domain.Users;

using FluentAssertions;

using Infrastructure.Users.Security;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

using Shared.Roles;

namespace Tests.Unit.Users.Security;

public class JwtTokenGeneratorTests
{
    private static (JwtTokenGenerator gen, JwtSettings settings) CreateValidGenerator()
    {
        var settings = new JwtSettings
        {
            SecretKey = "secret-key-for-testing-purpose-only",
            Audience = "TestAudience",
            Issuer = "TestIssuer",
            ExpiryMinutes = 15
        };

        return (new JwtTokenGenerator(Options.Create(settings)), settings);
    }

    [Fact]
    public void GenerateToken_CommonHappyPath_ValidJwtWithClaims()
    {
        // Arrange
        var (generator, settings) = CreateValidGenerator();
        var userId = Guid.NewGuid();
        var user = new User(userId, "user", "somehash", Roles.User);
        var periodValidity = TimeSpan.FromMinutes(settings.ExpiryMinutes);

        // Act
        var token = generator.GenerateToken(user);
        var handler = new JsonWebTokenHandler();
        var jsonToken = handler.ReadJsonWebToken(token);

        // Assert
        userId.ToString().Should().Be(jsonToken.Subject);
        user.Role.ToString().Should().Be(jsonToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        settings.Issuer.Should().Be(jsonToken.Issuer);
        jsonToken.Audiences.Count().Should().Be(1);
        settings.Audience.Should().Be(jsonToken.Audiences.First());
        periodValidity.Should().BeCloseTo((jsonToken.ValidTo - DateTime.UtcNow), TimeSpan.FromSeconds(2));
    }
}
