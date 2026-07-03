namespace Infrastructure.Bookings.Http.Exceptions;

/// <summary>
/// Исключение, возникающее при некорретном пользователе (его claims) внутри http контекста
/// </summary>
public class WrongUserFormatException(string message) : Exception(message)
{
}
