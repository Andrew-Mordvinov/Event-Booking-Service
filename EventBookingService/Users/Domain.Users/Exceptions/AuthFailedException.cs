namespace Domain.Users.Exceptions;

/// <summary>
/// Исключение, возникающее при неудачной попытке аутентификации в системе
/// </summary>
public class AuthFailedException(string message) : Exception(message)
{
}
