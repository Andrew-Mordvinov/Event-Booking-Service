using Application.Infrastructure.Common;
using Infrastructure.Ef.ExceptionPatterns;
using System.Reflection;

namespace Infrastructure.Ef;

/// <summary>
/// Работает с транзакциями и ChangeTracker. Транзакции могут создаваться явно
/// или неявно в самих репозиториях, но репозиторий сам не сохраняет изменения
/// </summary>
public class EfUnitOfWork(AppDbContext appDbContext) : IUnitOfWork
{
    private static readonly List<ExceptionPattern> _exceptionPatterns;

    static EfUnitOfWork()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var exceptionPatternTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ExceptionPattern).IsAssignableFrom(t))
            .ToArray();

        _exceptionPatterns = new(exceptionPatternTypes.Length);
        _exceptionPatterns.AddRange
        (
            exceptionPatternTypes
                .Select(t => Activator.CreateInstance(t))
                .OfType<ExceptionPattern>()
        );
    }

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
        catch (Exception ex)
        {
            if (appDbContext.Database.CurrentTransaction != null)
            {
                await appDbContext.Database.CurrentTransaction.RollbackAsync(token).ConfigureAwait(false);
            }
            appDbContext.ChangeTracker.Clear();
            // Если исключение попадает под один из зарегистрированных паттернов, то будет выкинуто оно, а не корневое исключение
            _exceptionPatterns.ForEach(e => e.RethrowIfMatch(ex));

            throw;
        }
    }
}
