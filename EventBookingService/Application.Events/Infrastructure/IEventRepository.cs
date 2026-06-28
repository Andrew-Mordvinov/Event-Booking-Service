using Application.Events.DTO.Result;
using Domain.Events;
using Shared.Interfaces.Infrastructure;
using System.Linq.Expressions;

namespace Application.Events.Infrastructure;

/// <summary>
/// Репозиторий событий
/// </summary>
public interface IEventRepository : IRepository<Event>
{
    /// <summary>
    /// Получение страницы с данными из хранилища
    /// </summary>
    /// <param name="filter">Фильтр, может быть не определен</param>
    /// <param name="page">Номер страницы, больше 0</param>
    /// <param name="pageSize">Размер страницы, больше 0</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Страница с данными или null</returns>
    Task<PaginatedResult<Event>?> GetPageAsync(Expression<Func<Event, bool>>? filter, int page, int pageSize, CancellationToken token = default);
}
