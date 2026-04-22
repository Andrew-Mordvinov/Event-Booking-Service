namespace Shared.Locking;

/// <summary>
/// Интерфейс для получения семафора блокировки
/// </summary>
public interface ISemaphoreGetter
{
    SemaphoreSlim SemaphoreSlim { get; }
}
