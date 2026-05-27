using DataAccess.Abstract.Enums;
using Shared.Interfaces;

namespace DataAccess.Abstract.Common;

/// <summary>
/// Базовое хранилище
/// </summary>
public interface IBaseStorage<T> where T : IHasId
{
    /// <summary>
    /// Получение объекта по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <param name="getMode">Режим выборки из хранилища - для чтения или для модификации. Режим для чтения
    /// возвращает сущность, изменения которой не будут учитываться хранилищем</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Объект или null</returns>
    Task<T?> GetByIdAsync(Guid id, GetMode getMode = GetMode.Edit, CancellationToken token = default);

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
}
