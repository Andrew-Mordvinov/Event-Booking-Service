namespace Validation;

public enum ItemCategory
{
    /// <summary>
    /// Ошибка входных данных
    /// </summary>
    ValidationError,

    /// <summary>
    /// Ошибка конфликта в ресурсах
    /// </summary>
    ConflictError,

    /// <summary>
    /// Предупреждение
    /// </summary>
    Warning,

    /// <summary>
    /// Информационное сообщение
    /// </summary>
    Info
}
