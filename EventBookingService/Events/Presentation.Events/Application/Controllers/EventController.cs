
using Application.Events.Interfaces;

using Domain.Events;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Presentation.Events.DTO.Requests;
using Presentation.Events.DTO.Response;

namespace Presentation.Events.Application.Controllers;

/// <summary>
/// Управление событиями
/// </summary>
[ApiController]
[Route("events")]
[Authorize]
public class EventController(IEventService _eventService) : ControllerBase
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
    /// Получение топ-10 самых продаваемых событий
    /// </summary>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="200">Список событий успешно получен</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous]
    [HttpGet("top")]
    public async Task<IActionResult> GetTopSalesEventsAsync(CancellationToken cancellationToken)
    {
        var result = await _eventService.GetTopSalesEventsAsync(cancellationToken);

        return Ok(result.Select(BaseEventResponse.FromEvent));
    }
}

