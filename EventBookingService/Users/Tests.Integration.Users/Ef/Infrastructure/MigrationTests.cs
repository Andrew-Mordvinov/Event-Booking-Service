using FluentAssertions;

using Infrastructure.Users.Ef;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

namespace Tests.Integration.Users.Ef.Infrastructure;

[Collection("PostgresTests")]
public class MigrationTests(SharedFixture sharedFixture)
{
    private readonly SharedFixture _sharedFixture = sharedFixture;

    [Fact]
    public async Task Migration_ShouldBeCorrectlyApplied()
    {
        // Arrange
        NpgsqlConnection.ClearAllPools();

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

            await db.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var act = async () =>
        {
            using var scope = _sharedFixture.ServiceProvider.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

            await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
        };

        // Assert
        await act.Should().NotThrowAsync();
    }
}
