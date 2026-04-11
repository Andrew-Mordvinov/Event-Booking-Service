using Bookings.Service;

using DTO.Bookings.Response;
using DTO.Events.Requests;
using DTO.Events.Response;
using DTO.Generic;

using Events.Models;
using Events.Service;

using Microsoft.AspNetCore.Mvc;

using Web.Common.Validations;

namespace Web.Application.Controllers;

[ApiController]
[Route("events")]
public class EventController(
    IEventService _eventService,
    IBookingService _bookingService) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<BaseEventResponse>> GetEventByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _eventService.GetEventByIdAsync(id, cancellationToken);

        if (result.Value is null)
        {
            return result.IsSuccessful
                ? NotFound()
                : BadRequest(result.ToProblemDetails(HttpContext));
        }

        return Ok(BaseEventResponse.FromEvent(result.Value));
    }

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

        if (!result.IsSuccessful)
        {
            return BadRequest(result.ToProblemDetails(HttpContext));
        }

        return result.Value is { } value
            ? Ok(PaginatedResponse<BaseEventResponse>.FromPaginatedResult(value, request.EffectivePageSize, BaseEventResponse.FromEvent))
            : Ok(new PaginatedResponse<BaseEventResponse>
            {
                CurrentPage = 1,
                FilteredCount = 0,
                TotalPages = 1,
                PageSize = 10,
                Items = []
            });
    }

    [HttpPost]
    public async Task<ActionResult<BaseEventResponse>> CreateEventAsync([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventService.CreateEventAsync(request, cancellationToken);

        if (result.Value is null)
        {
            return BadRequest(result.ToProblemDetails(HttpContext));
        }

        return CreatedAtAction
        (
            nameof(GetEventByIdAsync),
            new { id = result.Value.Id },
            BaseEventResponse.FromEvent(result.Value)
        );
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BaseEventResponse>> ModifyEventAsync(Guid id, [FromBody] ModifyEventRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventService.ModifyEventAsync(id, request, cancellationToken);

        if (result.Value is null)
        {
            return result.IsSuccessful
                ? NotFound()
                : BadRequest(result.ToProblemDetails(HttpContext));
        }

        return Ok(BaseEventResponse.FromEvent(result.Value));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEventAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _eventService.DeleteEventByIdAsync(id, cancellationToken);

        if (result.Value != true)
        {
            return result.IsSuccessful
                ? NotFound()
                : BadRequest(result.ToProblemDetails(HttpContext));
        }

        return Ok();
    }

    [HttpPost("{eventId}/book")]
    public async Task<ActionResult<BookingAcceptedResponse>> BookEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _bookingService.CreateBookingAsync(eventId, cancellationToken);

        if (!result.IsSuccessful)
        {
            return BadRequest(result.ToProblemDetails(HttpContext));
        }

        if (result.Value is null)
        {
            return NotFound();
        }

        return AcceptedAtRoute(
            nameof(BookingController.GetBookingByIdAsync),
            new { id = result.Value.Id },
            BookingAcceptedResponse.FromBooking(result.Value));
    }
}

