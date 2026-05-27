using Shared.Paging;
using System.Linq.Expressions;

namespace DataAccess.Abstract.Common;

/// <summary>
/// Интерфейс, позволяющий получить страницу из хранилища по заданному деревом фильтру
/// </summary>
public interface ICanGetPage<T>
{
    /// <summary>
    /// Получение страницы с данными из хранилища
    /// </summary>
    /// <param name="filter">Фильтр, может быть не определен</param>
    /// <param name="page">Номер страницы, больше 0</param>
    /// <param name="pageSize">Размер страницы, больше 0</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Страница с данными или null</returns>
    Task<PaginatedResult<T>?> GetPageAsync(Expression<Func<T, bool>>? filter, int page, int pageSize, CancellationToken token = default);
}
