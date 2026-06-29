namespace Domain.Events.Exceptions;

/// <summary>
/// Исключение, возникающее при отсутствии свободных мест
/// </summary>
public class NoSeatsAvailableException(string message) : Exception(message)
{
}
