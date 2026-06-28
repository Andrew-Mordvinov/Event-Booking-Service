namespace Shared.Interfaces.Infrastructure.Enums;

/// <summary>
/// Режим выборки из хранилища
/// </summary>
public enum GetMode
{
    /// <summary>
    /// Получение элемента только для чтения
    /// </summary>
    Readonly,

    /// <summary>
    /// Получение элемента для дальнейшей модификации
    /// </summary>
    Edit
}
