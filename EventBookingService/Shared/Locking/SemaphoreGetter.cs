namespace Shared.Locking;

/// <summary>
/// Реализация с единым семафором для всех экземпляров
/// </summary>
public class SemaphoreGetter : ISemaphoreGetter
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public SemaphoreSlim SemaphoreSlim => _semaphore;
}
