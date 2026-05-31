
using Application.DTO.Bookings.Response;
using Application.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace Presentation.Application.Controllers;

/// <summary>
/// Управление бронированиями
/// </summary>
[Route("bookings")]
[ApiController]
public class BookingController(IBookingService _bookingService) : ControllerBase
{
    /// <summary>
    /// Получение объекта брони по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор брони</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <response code="200">Бронь успешно получена</response>
    /// <response code="404">Бронь не найдена</response>
    [Produces("application/json")]
    [ProducesResponseType(typeof(BaseBookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id}", Name = nameof(GetBookingByIdAsync))]  
    public async Task<ActionResult<BaseBookingResponse>> GetBookingByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetBookingByIdAsync(id, cancellationToken);

        return Ok(BaseBookingResponse.FromBooking(result));
    }
}
