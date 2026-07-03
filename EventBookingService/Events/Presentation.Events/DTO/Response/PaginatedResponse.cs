using Application.Events.DTO.Result;

namespace Presentation.Events.DTO.Response;

/// <summary>
/// Ответ на запрос страницы с элементами
/// </summary>
/// <typeparam name="T">Тип элемента на странице</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// Элементы на странице
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>
    /// Общее число страниц
    /// </summary>
    public int TotalPages { get; init; } = 0;

    /// <summary>
    /// Текущая страница
    /// </summary>
    public int CurrentPage { get; init; } = 0;

    /// <summary>
    /// Размер страницы (количество элементов на странице, предельное)
    /// </summary>
    public int PageSize { get; init; } = 0;

    /// <summary>
    /// Общее число отфильтрованных элементов
    /// </summary>
    public long FilteredCount { get; init; } = 0;

    public static PaginatedResponse<T> FromPaginatedResult<V>(PaginatedResult<V> entity, int size, Func<V, T> selector) => new()
    {
        CurrentPage = entity.CurrentPage,
        FilteredCount = entity.FilteredCount,
        TotalPages = entity.TotalPages,
        PageSize = size,
        Items = [.. entity.Items.Select(selector)]
    };
}
