
using Application.DTO.Bookings.Response;
using Application.DTO.Events.Requests;
using Application.DTO.Events.Response;
using Application.DTO.Generic;
using Application.Interfaces;

using Domain.Events;

using Microsoft.AspNetCore.Mvc;

namespace Presentation.Application.Controllers;

/// <summary>
/// Управление событиями
/// </summary>
[ApiController]
[Route("events")]
public class EventController(
    IEventService _eventService,
    IBookingService _bookingService) : ControllerBase
{
    /// <summary>
    /// Получение события по его идентификатору
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="200">Событие успешно получено</response>
    /// <response code="404">Событие не найдено</response>
    [Produces("application/json")]
    [HttpGet("{id}")]
    public async Task<ActionResult<BaseEventResponse>> GetEventByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _eventService.GetEventByIdAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(BaseEventResponse.FromEvent(result));
    }

    /// <summary>
    /// Получение страницы со списком событий по заданным фильтрам
    /// </summary>
    /// <param name="request">Параметры для фильтрации и пагинации запроса событий</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="200">Список событий получен успешно (может быть пустым)</response>
    [Produces("application/json")]
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<BaseEventResponse>>> GetEventsAsync([FromQuery] GetEventsRequest request, CancellationToken cancellationToken)
    {
        var eventFilers = new EventFilters
        {
            Title = request.Title,
            From = request.From,
            To = request.To,
        };

        var result = await _eventService.GetEventsAsync(eventFilers, request.EffectivePage, request.EffectivePageSize, cancellationToken);

        return result is { } value
            ? Ok(PaginatedResponse<BaseEventResponse>.FromPaginatedResult(value, request.EffectivePageSize, BaseEventResponse.FromEvent))
            : Ok(new PaginatedResponse<BaseEventResponse>
            {
                CurrentPage = 1,
                FilteredCount = 0,
                TotalPages = 1,
                PageSize = request.EffectivePageSize,
                Items = []
            });
    }

    /// <summary>
    /// Создание события с заданными параметрами
    /// </summary>
    /// <param name="request">Атрибуты события</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="201">Событие успешно создано</response>
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BaseEventResponse), 201)]
    [HttpPost]
    public async Task<ActionResult<BaseEventResponse>> CreateEventAsync([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventService.CreateEventAsync(request, cancellationToken);

        return CreatedAtAction
        (
            nameof(GetEventByIdAsync),
            new { id = result.Id },
            BaseEventResponse.FromEvent(result)
        );
    }

    /// <summary>
    /// Полное обновление существующего события в системе
    /// </summary>
    /// <param name="id">Идентификатор обновляемого события</param>
    /// <param name="request">Запрос с атрибутами для обновления события</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="200">Событие успешно модифицировано</response>
    /// <response code="404">Событие не найдено</response>
    [Consumes("application/json")]
    [Produces("application/json")]
    [HttpPut("{id}")]
    public async Task<ActionResult<BaseEventResponse>> ModifyEventAsync(Guid id, [FromBody] ModifyEventRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventService.ModifyEventAsync(id, request, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(BaseEventResponse.FromEvent(result));
    }

    /// <summary>
    /// Удаление события
    /// </summary>
    /// <param name="id">Идентификатор удаляемого события</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="200">Событие успешно удалено</response>
    /// <response code="404">Событие не найдено</response>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEventAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _eventService.DeleteEventByIdAsync(id, cancellationToken);

        return result
            ? Ok()
            : NotFound();
    }

    /// <summary>
    /// Бронирование места на событие. Создает ожидающее обработки бронирование и возвращает ссылку на него для отслеживания статуса
    /// </summary>
    /// <param name="eventId">Идентификатор события, на которое бронируется место</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="202">Бронирование создано и ожидает обработки</response>
    [Produces("application/json")]
    [ProducesResponseType(typeof(BookingAcceptedResponse), 202)]
    [HttpPost("{eventId}/book")]
    public async Task<ActionResult<BookingAcceptedResponse>> BookEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _bookingService.CreateBookingAsync(eventId, cancellationToken);

        return AcceptedAtRoute(
            nameof(BookingController.GetBookingByIdAsync),
            new { id = result.Id },
            BookingAcceptedResponse.FromBooking(result));
    }
}

