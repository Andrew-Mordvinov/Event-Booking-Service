using System.ComponentModel.DataAnnotations;

using Application.DTO.Users;
using Application.Implementation;

namespace Presentation.DTO.Users.Request;

public class AspAuthUserRequest
{
    /// <summary>
    /// Логин
    /// </summary>
    [Required(ErrorMessage = UserServiceErrors.InvalidCredentials, AllowEmptyStrings = false)]
    public required string Login { get; init; }

    /// <summary>
    /// Пароль
    /// </summary>
    [Required(ErrorMessage = UserServiceErrors.InvalidCredentials, AllowEmptyStrings = false)]
    public required string Password { get; init; }

    public AuthUserRequest ToAuthUserRequest() => new()
    {
        Login = Login,
        Password = Password
    };
}
