using System.Linq.Expressions;

using EventBookingService.Common.Paging;
using EventBookingService.Common.Validations.Results;

namespace EventBookingService.Common.Storage;

/// <summary>
/// Хранилище List
/// </summary>
public class ListStorage<T> : IStorage<T> where T : IHasId, IFillable<T>, ICopyable<T>
{
    private readonly Dictionary<Guid, T> _dictionary = [];

    public ListStorage()
    {
        
    }

    public ListStorage(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            _dictionary.Add(item.Id, item);
        }
    }

    public bool HasAny => _dictionary.Count > 0;

    public Task<ValidationResult<bool>> RemoveAsync(Guid id, CancellationToken token = default) => Task.FromResult(ResultCreator.Success(_dictionary.Remove(id)));

    public Task<ValidationResult> AddAsync(T item, CancellationToken token = default)
    {
        var added = _dictionary.TryAdd(item.Id, item.Copy());

        return added ? 
            Task.FromResult(ResultCreator.Success())
            : Task.FromResult(ResultCreator.Fail(StorageErrors.ItemWithIdAlreadyExist(item.Id)));
    }

    public Task<ValidationResult<bool>> UpdateAsync(T item, CancellationToken token = default)
    {
        if (!_dictionary.ContainsKey(item.Id))
        {
            return Task.FromResult(ResultCreator.Success(false));
        }

        _dictionary[item.Id].FillFrom(item);

        return Task.FromResult(ResultCreator.Success(true));
    }

    public Task<ValidationResult<PaginatedResult<T>?>> GetPageAsync(Expression<Func<T, bool>>? filter, int page, int pageSize, CancellationToken token = default)
    {
        var result = ResultCreator.Success<PaginatedResult<T>?>(null);

        if (page < 1)
        {
            result.AddError(StorageErrors.PageMustBePositive);
        }

        if (pageSize < 1)
        {
            result.AddError(StorageErrors.PageSizeMustBePositive);
        }

        if (!result.IsSuccessful)
        {
            return Task.FromResult(result);
        }

        if (!HasAny)
        {
            return Task.FromResult(result);
        }

        var filtered = filter is not null ? _dictionary.Values.Where(filter.Compile()) : _dictionary.Values;

        var count = filtered.Count();

        if (count < 1)
        {
            return Task.FromResult(result);
        }

        var totalPages = (count + pageSize - 1) / pageSize;

        if (totalPages < page)
        {
            result.AddError(StorageErrors.PageNotFound(page, totalPages));
            return Task.FromResult(result);
        }

        var dataPage = filtered.Skip((page - 1) * pageSize).Take(pageSize);
        result.Value = new PaginatedResult<T>
        {
            CurrentPage = page,
            TotalPages = totalPages,
            FilteredCount = count,
            Items = [.. dataPage]
        };

        return Task.FromResult(result);
    }

    public Task<ValidationResult<T?>> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        if (!_dictionary.ContainsKey(id))
        {
            return Task.FromResult(ResultCreator.Success<T?>(default));
        }

        return Task.FromResult(ResultCreator.Success(_dictionary[id].Copy()));
    }
}
