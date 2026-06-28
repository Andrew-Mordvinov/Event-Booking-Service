using Domain.Users.Exceptions;

namespace Domain.Users.Exceptions;

/// <summary>
/// Исключение, возникающее при попытке добавить пользователя с уже существующим логином
/// </summary>
public class LoginNotUniqueException(string message = LoginNotUniqueException.DefaultErrorText) : Exception(message)
{
    public const string DefaultErrorText = "Пользователь с таким логином уже существует";
}
