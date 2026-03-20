using EventBookingService.Application.Events;
using EventBookingService.Common.Validations;
using EventBookingService.Models.Events;
using EventBookingService.Models.Events.Requests;
using EventBookingService.Models.Events.Response;

using Microsoft.AspNetCore.Mvc;

namespace EventBookingService.Application.Controllers;

[ApiController]
[Route("events")]
public class EventController(IEventService _eventRepository) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<BaseEventResponse>> GetEventByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _eventRepository.GetEventByIdAsync(id, cancellationToken);

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

        var result = await _eventRepository.GetEventsAsync(eventFilers, request.EffectivePage, request.EffectivePageSize, cancellationToken);

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
        var result = await _eventRepository.CreateEventAsync(request, cancellationToken);

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
        var result = await _eventRepository.ModifyEventAsync(id, request, cancellationToken);

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
        var result = await _eventRepository.DeleteEventByIdAsync(id, cancellationToken);

        if (result.Value != true)
        {
            return result.IsSuccessful
                ? NotFound()
                : BadRequest(result.ToProblemDetails(HttpContext));
        }

        return Ok();
    }
}

