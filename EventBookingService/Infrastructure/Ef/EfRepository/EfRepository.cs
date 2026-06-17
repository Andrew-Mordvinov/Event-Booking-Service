using Application.DTO.Generic;
using Application.Infrastructure.Common;
using Application.Infrastructure.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Ef.EfRepository;

/// <summary>
/// Обобщенный простой репозиторий EF Core
/// </summary>
public class EfRepository<T> : IRepository<T> where T : class, IHasId
{
    private readonly AppDbContext _appDbContext;
    private readonly IUnitOfWork _unitOfWork;
    protected readonly DbSet<T> _items;
    private readonly string _table;

    protected AppDbContext AppDbContext => _appDbContext;
    protected DbSet<T> Items => _items;

    public EfRepository(
        AppDbContext dbContext,
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

        // Сортировка нужна для стабильности вывода. В будущем нужно будет добавить отдельный селектор сюда
        var dataPage = filtered.OrderBy(t => t.Id.ToString()).Skip((page - 1) * pageSize).Take(pageSize);
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
        var item = await Items.FindAsync([id], token).ConfigureAwait(false);

        if (item is null)
        {
            return false;
        }

        Items.Remove(item);

        return true;
    }
}
