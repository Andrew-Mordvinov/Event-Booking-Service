namespace Shared.Infrastructure.Abstract;

/// <summary>
/// Обеспечивает сохранение изменений из разных репозиториев в одной транзакции
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Открывает транзакцию, если она еще не была открыта
    /// </summary>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Асинхронная задача</returns>
    Task EnsureTransactionAsync(CancellationToken token = default);

    /// <summary>
    /// Сохраняет все изменения в рамках одной транзакции
    /// </summary>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Асинхронная задача</returns>
    Task SaveChangesAsync(CancellationToken token = default);

    /// <summary>
    /// Откат всех изменений в контексте
    /// </summary>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Асинхронная задача</returns>
    Task RollbackChangesAsync(CancellationToken token = default);
}
