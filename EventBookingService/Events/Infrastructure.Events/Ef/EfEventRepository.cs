using System.Linq.Expressions;

using Application.Events.DTO.Result;
using Application.Events.Infrastructure;

using Domain.Events;

using Microsoft.EntityFrameworkCore;

using Shared.Exceptions;
using Shared.Infrastructure.Abstract;
using Shared.Infrastructure.Ef;

namespace Infrastructure.Events.Ef;

public class EfEventRepository(EventsDbContext dbContext, IUnitOfWork efUnitOfWork)
    : EfRepository<Event, EventsDbContext>(dbContext, efUnitOfWork, TableNames.Events), IEventRepository
{
    public async Task<PaginatedResult<Event>?> GetPageAsync(Expression<Func<Event, bool>>? filter, int page, int pageSize, CancellationToken token = default)
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
        var result = new PaginatedResult<Event>
        {
            CurrentPage = page,
            TotalPages = totalPages,
            FilteredCount = count,
            Items = [.. dataPage]
        };

        return result;
    }
}
