using System.ComponentModel.DataAnnotations;

using Application.Users.DTO;
using Application.Users.Validations.ErrorTexts;

using Shared.Roles;

namespace Presentation.Users.DTO.Request
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
