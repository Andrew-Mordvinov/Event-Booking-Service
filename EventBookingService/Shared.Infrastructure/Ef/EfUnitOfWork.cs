using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Ef.ExceptionPatterns;
using Shared.Interfaces.Infrastructure;

namespace Shared.Infrastructure.Ef;

/// <summary>
/// Базовая реализация UoW. Работает с транзакциями и ChangeTracker. Работает в паре с базовыми репозиториями, где 
/// транзакции могут создаваться явно или неявно в самих репозиториях, но репозиторий сам никогда не сохраняет изменения
/// </summary>
public class EfUnitOfWork<T>(T _appDbContext, IExceptionPatternsProvider _patternsProvider) : IUnitOfWork where T : DbContext
{
    public virtual Task EnsureTransactionAsync(CancellationToken token = default)
    {
        if (_appDbContext.Database.CurrentTransaction == null)
        {
            return _appDbContext.Database.BeginTransactionAsync(token);
        }

        return Task.CompletedTask;
    }

    public virtual async Task RollbackChangesAsync(CancellationToken token = default)
    {
        if (_appDbContext.Database.CurrentTransaction == null)
        {
            _appDbContext.ChangeTracker.Clear();
            return;
        }

        await _appDbContext.Database.CurrentTransaction.RollbackAsync(token).ConfigureAwait(false);
        _appDbContext.ChangeTracker.Clear();

        return;
    }

    public virtual async Task SaveChangesAsync(CancellationToken token = default)
    {
        try
        {
            await _appDbContext.SaveChangesAsync(token).ConfigureAwait(false);
            if (_appDbContext.Database.CurrentTransaction != null)
            {
                await _appDbContext.Database.CurrentTransaction.CommitAsync(token).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            if (_appDbContext.Database.CurrentTransaction != null)
            {
                await _appDbContext.Database.CurrentTransaction.RollbackAsync(token).ConfigureAwait(false);
            }
            _appDbContext.ChangeTracker.Clear();

            foreach (var pattern in _patternsProvider.GetExceptionPatterns())
            {
                pattern.RethrowIfMatch(ex);
            }

            throw;
        }
    }
}
