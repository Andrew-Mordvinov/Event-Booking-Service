using Shared.Interfaces;
using Shared.Paging;
using System.Linq.Expressions;
using Shared.Exceptions;

namespace DataAccess.Storage;

/// <summary>
/// Интерфейс хранилища данных
/// </summary>
public interface IStorage<T> where T : IHasId, IFillable<T>, ICopyable<T>
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

    /// <summary>
    /// Получение объекта по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Объект или null</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken token = default);

    /// <summary>
    /// Добавление объекта в хранилище
    /// </summary>
    /// <param name="item">Объект</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Асинхронная задача</returns>
    /// <exception cref="ConflictException"></exception>
    Task AddAsync(T item, CancellationToken token = default);

    /// <summary>
    /// Удаление объекта из хранилища
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>true, если удалено успешно, или false, если не найден</returns>
    Task<bool> RemoveAsync(Guid id, CancellationToken token = default);

    /// <summary>
    /// Обновление объекта в хранилище
    /// </summary>
    /// <param name="item">Объект, который должен заменить лежащий в хранилище</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>true, если обновлено успешно, или false, если не найден</returns>
    Task<bool> UpdateAsync(T item, CancellationToken token = default);

    /// <summary>
    /// Признак наличия хотя бы одного элемента в хранилище
    /// </summary>
    bool HasAny { get; }
}
