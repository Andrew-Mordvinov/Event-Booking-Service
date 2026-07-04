using Microsoft.EntityFrameworkCore;
using Shared.Entities;
using Shared.Infrastructure.Abstract;
using Shared.Infrastructure.Abstract.Enums;

namespace Shared.Infrastructure.Ef;

/// <summary>
/// Обобщенный простой репозиторий EF Core
/// </summary>
public class EfRepository<T, DB> : IRepository<T>
    where T : class, IHasId
    where DB : DbContext
{
    private readonly DB _appDbContext;
    private readonly IUnitOfWork _unitOfWork;
    protected readonly DbSet<T> _items;
    private readonly string _table;

    protected DB AppDbContext => _appDbContext;
    protected DbSet<T> Items => _items;

    public EfRepository(
        DB dbContext,
        IUnitOfWork unitOfWork,
        string tableName)
    {
        _appDbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _items = dbContext.Set<T>();
        _unitOfWork = unitOfWork;
        _table = tableName;
    }

    public Task AddAsync(T item, CancellationToken token = default)
    {
        Items.Add(item);

        return Task.CompletedTask;
    }

    public async Task<T?> GetByIdAsync(Guid id, GetMode getMode = GetMode.Edit, CancellationToken token = default)
    {
        if (getMode == GetMode.Readonly)
        {
            return await Items
                .AsQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id, token)
                .ConfigureAwait(false);
        }

        // FOR UPDATE не работает без транзакции, поэтому создаем, если еще нет
        await _unitOfWork.EnsureTransactionAsync(token).ConfigureAwait(false);

        return await Items
            .FromSqlRaw($"SELECT * FROM \"{_table}\" WHERE \"Id\" = {{0}} FOR UPDATE", id)
            .FirstOrDefaultAsync(token)
            .ConfigureAwait(false);
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken token = default)
    {
        var item = await Items.FindAsync([id], token).ConfigureAwait(false);

        if (item is null)
        {
            return false;
        }

        Items.Remove(item);

        return true;
    }
}
