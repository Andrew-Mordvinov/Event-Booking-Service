
using Application.Bookings.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Presentation.Bookings.DTO.Response;

namespace Presentation.Bookings.Application.Controllers;

/// <summary>
/// Управление бронированиями
/// </summary>
[Route("bookings")]
[ApiController]
[Authorize]
public class BookingController(IBookingService _bookingService) : ControllerBase
{
    /// <summary>
    /// Получение объекта брони по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор брони</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="200">Бронь успешно получена</response>
    /// <response code="401">Пользователь не определен</response>
    /// <response code="403">Бронирование на другого пользователя и запрашивающий не является администратором</response>
    /// <response code="404">Бронь не найдена</response>
    [Produces("application/json")]
    [ProducesResponseType(typeof(BaseBookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id}", Name = nameof(GetBookingByIdAsync))]
    public async Task<ActionResult<BaseBookingResponse>> GetBookingByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetBookingByIdAsync(id, cancellationToken);

        return Ok(BaseBookingResponse.FromBooking(result));
    }

    /// <summary>
    /// Отмена бронирования
    /// </summary>
    /// <param name="id">Идентификатор брони</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="204">Бронь успешно отменена</response>
    /// <response code="401">Пользователь не определен</response>
    /// <response code="403">Бронирование на другого пользователя и запрашивающий не является администратором</response>
    /// <response code="404">Бронь или событие не найдены</response>
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelBookingAsync(Guid id, CancellationToken cancellationToken)
    {
        await _bookingService.CancelBookingAsync(id, cancellationToken);

        return NoContent();
    }
}
