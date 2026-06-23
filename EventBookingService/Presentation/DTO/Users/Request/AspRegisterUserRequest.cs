using System.ComponentModel.DataAnnotations;

using Application.DTO.Users;
using Application.Validations.ErrorTexts;

using Domain.Users;

namespace Presentation.DTO.Users.Request
{
    public class AspRegisterUserRequest
    {
        /// <summary>
        /// Логин пользователя
        /// </summary>
        [Required(ErrorMessage = RegistrationRequestErrors.LoginError, AllowEmptyStrings = false)]
        public required string Login { get; init; }

        /// <summary>
        /// Пароль
        /// </summary>
        [Required(ErrorMessage = RegistrationRequestErrors.PasswordError, AllowEmptyStrings = false)]
        public required string Password { get; init; }

        /// <summary>
        /// Роль
        /// </summary>
        public Roles? Role { get; init; }

        public RegisterUserRequest ToRegisterUserRequest() => new()
        {
            Login = Login,
            Password = Password,
            Role = Role ?? Roles.User
        };
    }
}
