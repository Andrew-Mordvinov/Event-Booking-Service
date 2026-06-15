namespace Domain.Exceptions;

/// <summary>
/// Исключение, возникающее при превышении лимита активных бронирований у одного пользователя
/// </summary>
public class BookingLimitExceededException(string message) : Exception(message)
{
}
