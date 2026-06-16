namespace Domain.Exceptions;

/// <summary>
/// Исключение при попытке отменить уже отмененную бронь
/// </summary>
public class BookingCancelledException(string message) : Exception(message)
{
}
