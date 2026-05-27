namespace Domain.Exceptions;

/// <summary>
/// Исключение, возникающее при ошибке получения модели (в случае отсутствия)
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {

    }
}
