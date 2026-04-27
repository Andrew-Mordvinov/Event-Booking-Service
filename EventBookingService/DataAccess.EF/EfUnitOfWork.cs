using DataAccess.Abstract.Common;

namespace DataAccess.EF;

/// <summary>
/// Работает с ChangeTracker, а не транзакциями. Все изменения еще только в памяти
/// </summary>
public class EfUnitOfWork(AppDbContext appDbContext) : IUnitOfWork
{
    public Task RollbackChangesAsync(CancellationToken token = default)
    {
        appDbContext.ChangeTracker.Clear();

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken token = default)
    {
        return appDbContext.SaveChangesAsync(token);
    }
}
