using Bookings.Service;

using DTO.Bookings.Response;
using DTO.Events.Requests;
using DTO.Events.Response;
using DTO.Generic;

using Events.Models;
using Events.Service;

using Microsoft.AspNetCore.Mvc;

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

        if (result is null)
        {
            return NotFound();
        }

        return Ok(BaseEventResponse.FromEvent(result));
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEventAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _eventService.DeleteEventByIdAsync(id, cancellationToken);

        return result
            ? Ok()
            : NotFound();
    }

    [HttpPost("{eventId}/book")]
    public async Task<ActionResult<BookingAcceptedResponse>> BookEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _bookingService.CreateBookingAsync(eventId, cancellationToken);

        if (result is null)
        {

        }

        return AcceptedAtRoute(
            nameof(BookingController.GetBookingByIdAsync),
            new { id = result.Id },
            BookingAcceptedResponse.FromBooking(result));
    }
}

