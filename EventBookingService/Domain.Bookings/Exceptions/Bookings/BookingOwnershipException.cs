namespace Domain.Bookings.Exceptions.Bookings;

/// <summary>
/// Исключение при попытке отменить уже отмененную бронь
/// </summary>
public class InvalidBookingOperationException(string message) : Exception(message)
{
}
