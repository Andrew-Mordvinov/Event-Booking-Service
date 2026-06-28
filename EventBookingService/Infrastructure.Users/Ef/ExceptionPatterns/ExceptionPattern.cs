namespace Infrastructure.Users.Ef.ExceptionPatterns;

/// <summary>
/// Базовый класс для паттерна поиска (преобразования) исключения
/// базы данных в доменные
/// </summary>
internal abstract class ExceptionPattern
{
    /// <summary>
    /// Если переданное исключение попадает под паттерн проверки, то метод выкинет более конкретное исключение для уровня бизнес-логики
    /// </summary>
    public abstract void RethrowIfMatch(Exception exception);
}
