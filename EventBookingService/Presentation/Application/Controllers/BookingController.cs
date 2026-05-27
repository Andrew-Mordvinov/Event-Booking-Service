using Bookings.Service;

using DTO.Presentation.Bookings.Response;

using Microsoft.AspNetCore.Mvc;

namespace Web.Application.Controllers;

[Route("bookings")]
[ApiController]
public class BookingController(IBookingService _bookingService) : ControllerBase
{
    [HttpGet("{id}", Name = nameof(GetBookingByIdAsync))]
    public async Task<ActionResult<BaseBookingResponse>> GetBookingByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetBookingByIdAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(BaseBookingResponse.FromBooking(result));
    }
}
