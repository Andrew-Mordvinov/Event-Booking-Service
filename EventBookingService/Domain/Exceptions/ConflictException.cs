namespace Domain.Exceptions;

/// <summary>
/// Исключение, возникающее при попытке создать конфликтное состояние без формальных ошибок в аргументах
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {

    }
}
