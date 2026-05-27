namespace Domain.Exceptions;

/// <summary>
/// Исключение, возникающее при ошибках валидации моделей
/// </summary>
public class ValidationException : Exception
{
    public List<string> Errors { get; init; } = [];

    public ValidationException(IEnumerable<string> errors)
        : base()
    {
        Errors.AddRange(errors);
    }
}
