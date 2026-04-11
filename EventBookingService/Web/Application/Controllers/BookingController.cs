using Bookings.Service;

using DTO.Bookings.Response;

using Microsoft.AspNetCore.Mvc;

using Web.Common.Validations;

namespace Web.Application.Controllers;

[Route("bookings")]
[ApiController]
public class BookingController(IBookingService _bookingService) : ControllerBase
{
    [HttpGet("{id}", Name = nameof(GetBookingByIdAsync))]
    public async Task<ActionResult<BaseBookingResponse>> GetBookingByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetBookingByIdAsync(id, cancellationToken);

        if (result.Value is null)
        {
            return result.IsSuccessful
                ? NotFound()
                : BadRequest(result.ToProblemDetails(HttpContext));
        }

        return Ok(BaseBookingResponse.FromBooking(result.Value));
    }
}
