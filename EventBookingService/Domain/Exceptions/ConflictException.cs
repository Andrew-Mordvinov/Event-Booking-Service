namespace Domain.Exceptions;

/// <summary>
/// Исключение, возникающее при попытке создать конфликтное состояние без формальных ошибок в аргументах
/// </summary>
public class ConflictException(string message) : Exception(message)
{
}
