namespace Domain.Bookings.Exceptions;

/// <summary>
/// Исключение при попытке отменить бронь в некорректном состоянии
/// </summary>
public class InvalidBookingOperationException(string message) : Exception(message)
{
}
