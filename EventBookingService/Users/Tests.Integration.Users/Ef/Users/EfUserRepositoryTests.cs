using Application.Users.Infrastructure;

using Domain.Users;

using FluentAssertions;

using Infrastructure.Users.Ef;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shared.Roles;

namespace Tests.Integration.Users.Ef.Users;

[Collection("PostgresTests")]
public partial class EfUserRepositoryTests(SharedFixture sharedFixture) : IAsyncLifetime
{
    private readonly SharedFixture _sharedFixture = sharedFixture;

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask InitializeAsync()
    {
        await _sharedFixture.PrepareTestDbAsync();
    }

    #region Helping

    private async Task AddUserAsync(string login)
    {
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

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

    private async Task AddUsersAsync(IEnumerable<User> users)
    {
        if (!users.Any())
        {
            return;
        }

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        db.Users.AddRange(users);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_CorrectUser_SavedSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, "user", "passwordhash", Roles.User);

        // Act
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

            await repository.AddAsync(user, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

            var userFromDb = await db.Users.FirstOrDefaultAsync(t => t.Id == userId, TestContext.Current.CancellationToken);
            userFromDb.Should().BeEquivalentTo(user);
        }
    }

    [Fact]
    public async Task AddAsync_NotUniqueLogin_ThrowDbUpdate()
    {
        // Arrange
        var notUniqueLogin = "not_unique_login";
        var user = new User(Guid.NewGuid(), notUniqueLogin, "passwordhash", Roles.User);
        await AddUserAsync(notUniqueLogin);

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

            await repository.AddAsync(user, TestContext.Current.CancellationToken);

            // Act
            var act = async () => await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Assert
            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }

    [Theory]
    [MemberData(nameof(GetByLoginAsync_UserExist))]
    public async Task GetByLoginAsync_UserExist_ReturnSuccessfully(List<User> users, string login, User expected)
    {
        // Arrange
        User? result;
        await AddUsersAsync(users);

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // Act
            result = await repository.GetByLoginAsync(login, TestContext.Current.CancellationToken);
        }

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(GetByLoginAsync_UserNotExist))]
    public async Task GetByLoginAsync_UserNotExist_ReturnNull(List<User> users, string login)
    {
        // Arrange
        User? result;
        await AddUsersAsync(users);

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // Act
            result = await repository.GetByLoginAsync(login, TestContext.Current.CancellationToken);
        }

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
