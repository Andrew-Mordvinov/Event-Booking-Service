using Application.Infrastructure.Common;

namespace Infrastructure.Ef;

/// <summary>
/// Работает с транзакциями и ChangeTracker. Транзакции могут создаваться явно
/// или неявно в самих репозиториях, но репозиторий сам не сохраняет изменения
/// </summary>
public class EfUnitOfWork(AppDbContext appDbContext) : IUnitOfWork
{
    public Task EnsureTransactionAsync(CancellationToken token = default)
    {
        if (appDbContext.Database.CurrentTransaction == null)
        {
            return appDbContext.Database.BeginTransactionAsync(token);
        }

        return Task.CompletedTask;
    }

    public async Task RollbackChangesAsync(CancellationToken token = default)
    {
        if (appDbContext.Database.CurrentTransaction == null)
        {
            appDbContext.ChangeTracker.Clear();
            return;
        }

        await appDbContext.Database.CurrentTransaction.RollbackAsync(token).ConfigureAwait(false);
        appDbContext.ChangeTracker.Clear();

        return;
    }

    public async Task SaveChangesAsync(CancellationToken token = default)
    {
        try
        {
            await appDbContext.SaveChangesAsync(token).ConfigureAwait(false);
            if (appDbContext.Database.CurrentTransaction != null)
            {
                await appDbContext.Database.CurrentTransaction.CommitAsync(token).ConfigureAwait(false);
            }
        }
        catch
        {
            if (appDbContext.Database.CurrentTransaction != null)
            {
                await appDbContext.Database.CurrentTransaction.RollbackAsync(token).ConfigureAwait(false);
            }
            appDbContext.ChangeTracker.Clear();
            throw;
        }
    }
}
