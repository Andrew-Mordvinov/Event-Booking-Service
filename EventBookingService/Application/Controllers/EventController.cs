using EventBookingService.Application.Events;
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
                : BadRequest(result.Errors);
        }

        return Ok(BaseEventResponse.FromEvent(result.Value));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BaseEventResponse>>> GetAllEventAsync(CancellationToken cancellationToken)
    {
        var result = await _eventRepository.GetAllEventsAsync(cancellationToken);

        if (!result.IsSuccessful)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value is not null 
            ? result.Value.Select(t => BaseEventResponse.FromEvent(t))
            : []);
    }

    [HttpPost]
    public async Task<ActionResult<BaseEventResponse>> CreateEventAsync([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var result = await _eventRepository.CreateEventAsync(request, cancellationToken);

        if (result.Value is null)
        {
            return BadRequest(result.Errors);
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
                : BadRequest(result.Errors);
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
                : BadRequest(result.Errors);
        }

        return Ok();
    }
}

