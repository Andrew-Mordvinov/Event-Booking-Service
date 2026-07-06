namespace Shared.Infrastructure.Abstract.ExceptionPatterns;

/// <summary>
/// Провайдер для коллекции кастомных преобразователей исключений
/// </summary>
public interface IExceptionPatternsProvider
{
    /// <summary>
    /// Получает коллекцию паттернов
    /// </summary>
    /// <returns>Коллекция паттернов</returns>
    IReadOnlyCollection<ExceptionPattern> GetExceptionPatterns();
}
