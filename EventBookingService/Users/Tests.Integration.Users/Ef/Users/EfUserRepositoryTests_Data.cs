using Domain.Users;
using Shared.Roles;

namespace Tests.Integration.Users.Ef.Users;

public partial class EfUserRepositoryTests
{
    private static readonly List<User> BaseUserList =
    [
        new User(Guid.NewGuid(), "User1", "DummyHash", Roles.User),
        new User(Guid.NewGuid(), "User2", "PassHash", Roles.User),
        new User(Guid.NewGuid(), "SomeUser", "SomeUserPassHash", Roles.User),
        new User(Guid.NewGuid(), "AdminUser", "adminhash", Roles.Admin),
        new User(Guid.NewGuid(), "Another_User", "just_one_more_hash", Roles.User),
        new User(Guid.NewGuid(), "strong_user", "verystrongpasshashever", Roles.User),
    ];

    public static IEnumerable<object?[]> GetByLoginAsync_UserExist() =>
    [
        [BaseUserList, "User1", BaseUserList[0]],
        [BaseUserList, "Another_User", BaseUserList[4]]
    ];

    public static IEnumerable<object?[]> GetByLoginAsync_UserNotExist() =>
    [
        [BaseUserList, "user1"],
        [BaseUserList, "USER2"],
        [BaseUserList, "another-user"]
    ];
}
