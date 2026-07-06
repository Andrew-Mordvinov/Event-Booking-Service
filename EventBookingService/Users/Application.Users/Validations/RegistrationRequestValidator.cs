using Application.Users.DTO;
using Application.Users.Validations.ErrorTexts;

namespace Application.Users.Validations;

/// <summary>
/// Валидатор запроса на регистрацию пользователя
/// </summary>
public class RegistrationRequestValidator : IValidator<RegisterUserRequest>
{
    public IEnumerable<string> Validate(RegisterUserRequest item)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(item.Login))
        {
            errors.Add(RegistrationRequestErrors.LoginError);
        }

        if (string.IsNullOrWhiteSpace(item.Password))
        {
            errors.Add(RegistrationRequestErrors.PasswordError);
        }

        return errors;
    }
}
