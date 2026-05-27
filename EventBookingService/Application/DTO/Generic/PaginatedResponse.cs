using Shared.Paging;

namespace DTO.Presentation.Generic;

public class PaginatedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int TotalPages { get; set; } = 0;

    public int CurrentPage { get; set; } = 0;

    public int PageSize { get; set; } = 0;

    public long FilteredCount { get; set; } = 0;

    public static PaginatedResponse<T> FromPaginatedResult<V>(PaginatedResult<V> entity, int size, Func<V, T> selector) => new()
    {
        CurrentPage = entity.CurrentPage,
        FilteredCount = entity.FilteredCount,
        TotalPages = entity.TotalPages,
        PageSize = size,
        Items = [.. entity.Items.Select(selector)]
    };
}
