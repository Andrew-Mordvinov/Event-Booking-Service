namespace Domain.Exceptions.Bookings;

/// <summary>
/// Исключение при попытке забронировать уже начавшееся событие
/// </summary>
public class EventWasStartedException(string message) : Exception(message)
{
}
