using Application.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Presentation.DTO.Bookings.Response;

namespace Presentation.Application.Controllers;

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
}
