namespace Shared.Exceptions;

/// <summary>
/// Исключение, возникающее при ошибке получения модели (в случае отсутствия)
/// </summary>
public class NotFoundException(string message) : Exception(message)
{
}
