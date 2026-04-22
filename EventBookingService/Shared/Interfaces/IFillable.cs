namespace Shared.Interfaces;

/// <summary>
/// Интерфейс, который предоставляет метод для заполнения текущего объекта из переданного 
/// </summary>
public interface IFillable<T>
{
    void FillFrom(T source);
}
