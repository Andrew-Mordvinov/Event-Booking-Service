using System.ComponentModel.DataAnnotations;

using Application.Users.DTO;
using Application.Users.Implementation;

namespace Presentation.Users.DTO.Request;

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
