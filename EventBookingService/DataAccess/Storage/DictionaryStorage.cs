using Shared.Exceptions;
using Shared.Interfaces;
using Shared.Paging;
using System.Linq.Expressions;

namespace DataAccess.Storage;

/// <summary>
/// Хранилище Dictionary
/// </summary>
public class DictionaryStorage<T> : IStorage<T> where T : IHasId, IFillable<T>, ICopyable<T>
{
    private readonly Dictionary<Guid, T> _dictionary = [];

    public DictionaryStorage()
    {
        
    }

    public DictionaryStorage(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            _dictionary.Add(item.Id, item);
        }
    }

    public bool HasAny => _dictionary.Count > 0;

    public Task<bool> RemoveAsync(Guid id, CancellationToken token = default) => Task.FromResult(_dictionary.Remove(id));

    public Task AddAsync(T item, CancellationToken token = default)
    {
        if (_dictionary.TryAdd(item.Id, item.Copy()))
        {
            return Task.CompletedTask;
        }

        throw new ConflictException(StorageErrors.ItemWithIdAlreadyExist(item.Id));
    }

    public Task<bool> UpdateAsync(T item, CancellationToken token = default)
    {
        if (_dictionary.TryGetValue(item.Id, out var value))
        {
            value.FillFrom(item);

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<PaginatedResult<T>?> GetPageAsync(Expression<Func<T, bool>>? filter, int page, int pageSize, CancellationToken token = default)
    {
        var errors = new List<string>();
        if (page < 1)
        {
            errors.Add(StorageErrors.PageMustBePositive);
        }

        if (pageSize < 1)
        {
            errors.Add(StorageErrors.PageSizeMustBePositive);
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        if (!HasAny)
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
            errors.Add(StorageErrors.PageNotFound(page, totalPages));
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

    public Task<T?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        if (_dictionary.TryGetValue(id, out var item))
        {
            return Task.FromResult<T?>(item.Copy());
        }

        return Task.FromResult<T?>(default);
    }
}
