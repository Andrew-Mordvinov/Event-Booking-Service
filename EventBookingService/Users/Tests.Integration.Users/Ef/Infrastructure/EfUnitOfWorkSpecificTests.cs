using Domain.Users;
using Domain.Users.Exceptions;
using FluentAssertions;
using Infrastructure.Users.Ef;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Abstract;
using Shared.Roles;

namespace Tests.Integration.Users.Ef.Infrastructure;

/// <summary>
/// Класс для специальных тестов, которые проверяют конвертацию исключений базы в доменные
/// </summary>
[Collection("PostgresTests")]
public class EfUnitOfWorkSpecificTests(SharedFixture sharedFixture) : IAsyncLifetime
{
    private readonly SharedFixture _sharedFixture = sharedFixture;

    public async ValueTask InitializeAsync()
    {
        await _sharedFixture.PrepareTestDbAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task SaveChangesAsync_DuplicateLogin_ThrowLoginNotUnique()
    {
        // Arrange
        var login = "not_unique_login";

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

            db.Users.Add(new User
            (
                Guid.NewGuid(),
                login,
                "passhash",
                Roles.Admin
            ));

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

            db.Users.Add(new User
            (
                Guid.NewGuid(),
                login,
                "passhash",
                Roles.Admin
            ));

            var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Act
            var act = async () => await unit.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Assert
            await act.Should().ThrowExactlyAsync<LoginNotUniqueException>();
        }
    }
}
