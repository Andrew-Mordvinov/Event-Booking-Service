using Domain.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Tests.Unit.Users;

public class HttpUserContextTests
{
    private static HttpUserContext CreateContext(out Mock<IHttpContextAccessor> contextAccessorMock)
    {
        contextAccessorMock = new Mock<IHttpContextAccessor>();

        return new HttpUserContext(contextAccessorMock.Object);
    }

    #region UserId

    [Fact]
    public void UserId_HasUserId_SuccessfullyReturn()
    {
        // Arrange
        var context = CreateContext(out var contextAccessorMock);
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "dont care");

        contextAccessorMock.Setup(t => t.HttpContext)
            .Returns(new DefaultHttpContext() { User = new ClaimsPrincipal(identity) })
            .Verifiable(Times.Once);

        // Act
        var result = context.UserId;

        // Assert
        contextAccessorMock.Verify();
        result.Should().Be(userId);
    }

    [Fact]
    public void UserId_WrongGuidFormat_ThrowWrongUserFormat()
    {
        // Arrange
        var context = CreateContext(out var contextAccessorMock);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "invalid_guid")
        };

        var identity = new ClaimsIdentity(claims, "dont care");

        contextAccessorMock.Setup(t => t.HttpContext)
            .Returns(new DefaultHttpContext() { User = new ClaimsPrincipal(identity) })
            .Verifiable(Times.Once);

        // Act
        var act = () => context.UserId;

        // Assert
        act.Should().Throw<WrongUserFormatException>();
        contextAccessorMock.Verify();
    }

    [Fact]
    public void UserId_NoUserInClaims_ThrowWrongUserFormat()
    {
        // Arrange
        var context = CreateContext(out var contextAccessorMock);
        var claims = new List<Claim>
        {
            new("invalid_claim_type", Guid.NewGuid().ToString())
        };

        var identity = new ClaimsIdentity(claims, "dont care");

        contextAccessorMock.Setup(t => t.HttpContext)
            .Returns(new DefaultHttpContext() { User = new ClaimsPrincipal(identity) })
            .Verifiable(Times.Once);

        // Act
        var act = () => context.UserId;

        // Assert
        act.Should().Throw<WrongUserFormatException>();
        contextAccessorMock.Verify();
    }

    #endregion

    #region IsAdmin

    [Fact]
    public async Task IsAdmin_CheckRoleOk_ReturnTrue()
    {
        // Arrange
        var context = CreateContext(out var contextAccessorMock);
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, Roles.Admin.ToString())
        };

        var identity = new ClaimsIdentity(claims, "dont care");

        contextAccessorMock.Setup(t => t.HttpContext)
            .Returns(new DefaultHttpContext() { User = new ClaimsPrincipal(identity) })
            .Verifiable(Times.Once);

        // Act
        var result = await context.IsAdmin(TestContext.Current.CancellationToken);

        // Assert
        contextAccessorMock.Verify();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAdmin_UserNoAdmin_ReturnFalse()
    {
        // Arrange
        var context = CreateContext(out var contextAccessorMock);
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, Roles.User.ToString())
        };

        var identity = new ClaimsIdentity(claims, "dont care");

        contextAccessorMock.Setup(t => t.HttpContext)
            .Returns(new DefaultHttpContext() { User = new ClaimsPrincipal(identity) })
            .Verifiable(Times.Once);

        // Act
        var result = await context.IsAdmin(TestContext.Current.CancellationToken);

        // Assert
        contextAccessorMock.Verify();
        result.Should().BeFalse();
    }

    #endregion
}
