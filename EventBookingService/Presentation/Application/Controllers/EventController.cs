using Application.Interfaces;

using Domain.Events;
using Domain.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Presentation.DTO.Bookings.Response;
using Presentation.DTO.Events.Requests;
using Presentation.DTO.Events.Response;
using Presentation.DTO.Generic;

namespace Presentation.Application.Controllers;

/// <summary>
/// Управление событиями
/// </summary>
[ApiController]
[Route("events")]
[Authorize]
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
    /// <response code="401">Пользователь не определен</response>
    /// <response code="404">Событие не найдено</response>
    [Produces("application/json")]
    [ProducesResponseType(typeof(BaseEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id}")]
    public async Task<ActionResult<BaseEventResponse>> GetEventByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _eventService.GetEventByIdAsync(id, cancellationToken);

        return Ok(BaseEventResponse.FromEvent(result));
    }

    /// <summary>
    /// Получение страницы со списком событий по заданным фильтрам
    /// </summary>
    /// <param name="request">Параметры для фильтрации и пагинации запроса событий</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="200">Список событий получен успешно (может быть пустым)</response>
    /// <response code="400">Некорректный запрос, вернуть страницу невозможно</response>
    /// <response code="401">Пользователь не определен</response>
    [Produces("application/json")]
    [ProducesResponseType(typeof(PaginatedResponse<BaseEventResponse>), StatusCodes.Status200OK)] 
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<BaseEventResponse>>> GetEventsAsync([FromQuery] AspGetEventsRequest request, CancellationToken cancellationToken)
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
    /// <response code="400">Некорректный запрос, событие создать невозможно</response>
    /// <response code="401">Пользователь не определен</response>
    /// <response code="403">Доступ к созданию только для администратора</response>
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BaseEventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<BaseEventResponse>> CreateEventAsync([FromBody] AspCreateEventRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventService.CreateEventAsync(request.ToCreateEventRequest(), cancellationToken);

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
    /// <response code="400">Некорректный запрос, событие изменить невозможно</response>
    /// <response code="401">Пользователь не определен</response>
    /// <response code="403">Доступ к созданию только для администратора</response>
    /// <response code="404">Событие не найдено</response>
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BaseEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<BaseEventResponse>> ModifyEventAsync(Guid id, [FromBody] AspModifyEventRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventService.ModifyEventAsync(id, request.ToModifyEventRequest(), cancellationToken);

        return Ok(BaseEventResponse.FromEvent(result));
    }

    /// <summary>
    /// Удаление события
    /// </summary>
    /// <param name="id">Идентификатор удаляемого события</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="200">Событие успешно удалено</response>
    /// <response code="401">Пользователь не определен</response>
    /// <response code="403">Доступ к созданию только для администратора</response>
    /// <response code="404">Событие не найдено</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEventAsync(Guid id, CancellationToken cancellationToken)
    {
        await _eventService.DeleteEventByIdAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Бронирование места на событие. Создает ожидающее обработки бронирование и возвращает ссылку на него для отслеживания статуса
    /// </summary>
    /// <param name="eventId">Идентификатор события, на которое бронируется место</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="202">Бронирование создано и ожидает обработки</response>
    /// <response code="400">Событие уже началось</response>
    /// <response code="401">Пользователь не определен</response>
    /// <response code="404">Событие не найдено</response>
    /// <response code="409">Бронирование не создано, так как мест на событие не осталось или превышен лимит активных бронирований</response>
    [Produces("application/json")]
    [ProducesResponseType(typeof(BookingAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
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

