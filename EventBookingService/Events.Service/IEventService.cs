using DTO.Events.Requests;
using Events.Models;
using Shared.Paging;
using Validation;

namespace Events.Service;

/// <summary>
/// Сервис для работы с событиями
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Получить события в виде страницы с опциональным применением фильтров
    /// </summary>
    /// <param name="filters">Параметры фильтрации</param>
    /// <param name="page">Страница</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Страничный результат с событиями или null и возникшие ошибки в процессе</returns>
    Task<ValidationResult<PaginatedResult<Event>?>> GetEventsAsync(EventFilters filters, int page, int pageSize, CancellationToken token = default);

    /// <summary>
    /// Получить событие по заданному идентификатору
    /// </summary>
    /// <param name="id">Id события, которое нужно получить</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Событие или null, если не существует, а также ошибки в процессе</returns>
    Task<ValidationResult<Event?>> GetEventByIdAsync(Guid id, CancellationToken token = default);

    /// <summary>
    /// Создать новое событие
    /// </summary>
    /// <param name="request">Запрос на создание нового события</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Событие или null, если создать событие не удалось, а также ошибки в процессе</returns>
    Task<ValidationResult<Event?>> CreateEventAsync(CreateEventRequest request, CancellationToken token = default);

    /// <summary>
    /// Изменить существующее событие
    /// </summary>
    /// <param name="id">Id события, которое будет изменено</param>
    /// <param name="request">Запрос с данными для модификации</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>Измененное событие или null, если не найдено 
    /// или не удалось обновить, а также ошибки в процессе</returns>
    Task<ValidationResult<Event?>> ModifyEventAsync(Guid id, ModifyEventRequest request, CancellationToken token = default);

    /// <summary>
    /// Удалить событие
    /// </summary>
    /// <param name="id">Id события, которое нужно удалить</param>
    /// <param name="token">Токен отмены асинхронной операции</param>
    /// <returns>true если событие успешно удалено, а также ошибки в процессе</returns>
    Task<ValidationResult<bool>> DeleteEventByIdAsync(Guid id, CancellationToken token = default);
}
