namespace Application.Paging;

public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int TotalPages { get; init; } = 0;

    public int CurrentPage { get; init; } = 0;

    public long FilteredCount { get; init; } = 0;
}
