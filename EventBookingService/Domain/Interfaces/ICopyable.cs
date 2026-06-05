namespace Domain.Interfaces;

/// <summary>
/// Интерфейс создания копии текущего объекта
/// </summary>
public interface ICopyable<T>
{
    T Copy();
}
