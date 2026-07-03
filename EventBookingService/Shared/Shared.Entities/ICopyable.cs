namespace Shared.Entities;

/// <summary>
/// Интерфейс создания копии текущего объекта
/// </summary>
public interface ICopyable<T>
{
    T Copy();
}
