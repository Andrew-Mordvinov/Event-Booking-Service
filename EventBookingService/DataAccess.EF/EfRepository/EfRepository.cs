using DataAccess.Abstract.Common;
using DataAccess.Abstract.Enums;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Shared.Interfaces;
using Shared.Paging;
using System.Linq.Expressions;

namespace DataAccess.EF.EfRepository;

/// <summary>
/// Обобщенный простой репозиторий EF Core
/// </summary>
public class EfRepository<T> : IRepository<T> where T : class, IHasId
{
    private readonly AppDbContext _appDbContext;
    private readonly DbSet<T> _items;

    protected AppDbContext AppDbContext => _appDbContext;
    protected DbSet<T> Items => _items;

    public EfRepository(AppDbContext dbContext)
    {
        _appDbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _items = dbContext.Set<T>();
    }

    public Task AddAsync(T item, CancellationToken token = default)
    {
        Items.Add(item);

        return Task.CompletedTask;
    }

    public Task<T?> GetByIdAsync(Guid id, GetMode getMode = GetMode.Edit, CancellationToken token = default)
    {
        var query = Items.AsQueryable();

        if (getMode == GetMode.Readonly)
        {
            query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(e => e.Id == id, token);
    }

    public async Task<PaginatedResult<T>?> GetPageAsync(Expression<Func<T, bool>>? filter, int page, int pageSize, CancellationToken token = default)
    {
        var errors = new List<string>();
        if (page < 1)
        {
            errors.Add(EfRepositoryErrors.PageMustBePositive);
        }

        if (pageSize < 1)
        {
            errors.Add(EfRepositoryErrors.PageSizeMustBePositive);
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var filtered = filter is not null ? Items.Where(filter) : Items;

        var count = await filtered.CountAsync(token).ConfigureAwait(false);

        if (count < 1)
        {
            return null;
        }

        var totalPages = (count + pageSize - 1) / pageSize;

        if (totalPages < page)
        {
            errors.Add(EfRepositoryErrors.PageNotFound(page, totalPages));
            throw new ValidationException(errors);
        }

        var dataPage = filtered.Skip((page - 1) * pageSize).Take(pageSize);
        var result = new PaginatedResult<T>
        {
            CurrentPage = page,
            TotalPages = totalPages,
            FilteredCount = count,
            Items = [.. dataPage]
        };

        return result;
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken token = default)
    {
        var @event = await Items.FindAsync([id], token).ConfigureAwait(false);

        if (@event is null)
        {
            return false;
        }

        Items.Remove(@event);

        return true;
    }
}
