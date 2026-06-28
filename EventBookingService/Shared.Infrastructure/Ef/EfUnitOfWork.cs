using Microsoft.EntityFrameworkCore;
using Shared.Interfaces.Infrastructure;

namespace Shared.Infrastructure.Ef;

/// <summary>
/// Работает с транзакциями и ChangeTracker. Транзакции могут создаваться явно
/// или неявно в самих репозиториях, но репозиторий сам не сохраняет изменения
/// </summary>
public class EfUnitOfWork<T>(T appDbContext) : IUnitOfWork where T : DbContext
{
    // TODO логику перехвата надо доделать
    public virtual Task EnsureTransactionAsync(CancellationToken token = default)
    {
        if (appDbContext.Database.CurrentTransaction == null)
        {
            return appDbContext.Database.BeginTransactionAsync(token);
        }

        return Task.CompletedTask;
    }

    public virtual async Task RollbackChangesAsync(CancellationToken token = default)
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

    public virtual async Task SaveChangesAsync(CancellationToken token = default)
    {
        try
        {
            await appDbContext.SaveChangesAsync(token).ConfigureAwait(false);
            if (appDbContext.Database.CurrentTransaction != null)
            {
                await appDbContext.Database.CurrentTransaction.CommitAsync(token).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
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
