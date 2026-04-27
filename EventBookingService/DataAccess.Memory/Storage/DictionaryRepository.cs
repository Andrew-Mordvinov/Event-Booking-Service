using DataAccess.Abstract.Common;
using DataAccess.Abstract.Enums;
using Shared.Exceptions;
using Shared.Interfaces;
using Shared.Paging;
using System.Linq.Expressions;

namespace DataAccess.Memory.Storage;

/// <summary>
/// Хранилище Dictionary
/// </summary>
[Obsolete("Более не актуально хранение в памяти")]
public class DictionaryRepository<T> : IRepository<T> where T : IHasId, ICopyable<T>
{
    private readonly Dictionary<Guid, T> _dictionary = [];

    public DictionaryRepository()
    {
        
    }

    public DictionaryRepository(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            _dictionary.Add(item.Id, item);
        }
    }

    public Task<bool> RemoveAsync(Guid id, CancellationToken token = default) => Task.FromResult(_dictionary.Remove(id));

    public Task AddAsync(T item, CancellationToken token = default)
    {
        if (_dictionary.TryAdd(item.Id, item))
        {
            return Task.CompletedTask;
        }

        throw new ConflictException(DictionaryRepoErrors.ItemWithIdAlreadyExist(item.Id));
    }

    public Task<PaginatedResult<T>?> GetPageAsync(Expression<Func<T, bool>>? filter, int page, int pageSize, CancellationToken token = default)
    {
        var errors = new List<string>();
        if (page < 1)
        {
            errors.Add(DictionaryRepoErrors.PageMustBePositive);
        }

        if (pageSize < 1)
        {
            errors.Add(DictionaryRepoErrors.PageSizeMustBePositive);
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        if (_dictionary.Count < 1)
        {
            return Task.FromResult<PaginatedResult<T>?>(null);
        }

        var filtered = filter is not null ? _dictionary.Values.Where(filter.Compile()) : _dictionary.Values;

        var count = filtered.Count();

        if (count < 1)
        {
            return Task.FromResult<PaginatedResult<T>?>(null);
        }

        var totalPages = (count + pageSize - 1) / pageSize;

        if (totalPages < page)
        {
            errors.Add(DictionaryRepoErrors.PageNotFound(page, totalPages));
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

        return Task.FromResult<PaginatedResult<T>?>(result);
    }

    public Task<T?> GetByIdAsync(Guid id, GetMode getMode = GetMode.Edit, CancellationToken token = default)
    {
        if (_dictionary.TryGetValue(id, out var item))
        {
            return Task.FromResult<T?>(getMode == GetMode.Readonly ? item.Copy() : item);
        }

        return Task.FromResult<T?>(default);
    }
}
