namespace Shared.Locking;

/// <summary>
/// Реализация с семафором для каждого экземпляра отдельного
/// </summary>
public class InstanceSemaphoreGetter : ISemaphoreGetter
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public SemaphoreSlim SemaphoreSlim => _semaphore;
}
