namespace Domain.Bookings.Exceptions;

/// <summary>
/// Исключение при попытке пользователя взаимодействовать с чужим бронированием
/// </summary>
public class BookingOwnershipException(string message) : Exception(message)
{
}
