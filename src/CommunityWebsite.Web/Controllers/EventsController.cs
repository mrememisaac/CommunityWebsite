using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Services.Interfaces;

namespace CommunityWebsite.Web.Controllers;

/// <summary>
/// Events API controller - RESTful endpoints for event management.
/// Follows REST conventions and proper HTTP semantics.
/// Inherits from <see cref="ApiControllerBase"/> for common functionality.
/// </summary>
[Route("api/[controller]")]
public class EventsController : ApiControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, ILogger<EventsController> logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets upcoming events
    /// </summary>
    [HttpGet("upcoming")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetUpcomingEvents([FromQuery] int limit = 20)
    {
        _logger.LogInformation("GET /api/events/upcoming?limit={Limit}", limit);

        var result = await _eventService.GetUpcomingEventsAsync(limit);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets past events
    /// </summary>
    [HttpGet("past")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetPastEvents([FromQuery] int limit = 20)
    {
        _logger.LogInformation("GET /api/events/past?limit={Limit}", limit);

        var result = await _eventService.GetPastEventsAsync(limit);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets a specific event by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetEvent(int id)
    {
        _logger.LogInformation("GET /api/events/{EventId}", id);

        var result = await _eventService.GetEventByIdAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets events by organizer
    /// </summary>
    [HttpGet("organizer/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEventsByOrganizer(int userId)
    {
        _logger.LogInformation("GET /api/events/organizer/{UserId}", userId);

        var result = await _eventService.GetEventsByOrganizerAsync(userId);

        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Creates a new event
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventRequest request)
    {
        if (CurrentUserId == null)
            return Unauthorized();
        var userId = CurrentUserId.Value;

        _logger.LogInformation("POST /api/events - Creating event by user {UserId}", userId);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _eventService.CreateEventAsync(userId, request);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return CreatedAtAction(nameof(GetEvent), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Updates an existing event
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
    {
        if (CurrentUserId == null)
            return Unauthorized();
        var userId = CurrentUserId.Value;

        _logger.LogInformation("PUT /api/events/{EventId} - Updating event by user {UserId}", id, userId);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _eventService.UpdateEventAsync(id, userId, request);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage!.Contains("not found"))
                return NotFound(new { error = result.ErrorMessage });
            if (result.ErrorMessage.Contains("Only the organizer"))
                return Forbid();
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Cancels an event
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CancelEvent(int id)
    {
        if (CurrentUserId == null)
            return Unauthorized();
        var userId = CurrentUserId.Value;

        _logger.LogInformation("POST /api/events/{EventId}/cancel - Cancelling event by user {UserId}", id, userId);

        var result = await _eventService.CancelEventAsync(id, userId);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage!.Contains("not found"))
                return NotFound(new { error = result.ErrorMessage });
            if (result.ErrorMessage.Contains("Only the organizer"))
                return Forbid();
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { message = "Event cancelled successfully" });
    }

    /// <summary>
    /// Deletes an event
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteEvent(int id)
    {
        if (CurrentUserId == null)
            return Unauthorized();
        var userId = CurrentUserId.Value;

        _logger.LogInformation("DELETE /api/events/{EventId} - Deleting event by user {UserId}", id, userId);

        var result = await _eventService.DeleteEventAsync(id, userId);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage!.Contains("not found"))
                return NotFound(new { error = result.ErrorMessage });
            if (result.ErrorMessage.Contains("Only the organizer"))
                return Forbid();
            return BadRequest(new { error = result.ErrorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Registers the current user for an event (increments attendee count)
    /// </summary>
    [HttpPost("{id}/register")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RegisterForEvent(int id)
    {
        if (CurrentUserId == null)
            return Unauthorized();
        var userId = CurrentUserId.Value;

        _logger.LogInformation("POST /api/events/{EventId}/register - User {UserId} registering for event", id, userId);

        var result = await _eventService.IncrementAttendeeCountAsync(id);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage!.Contains("not found"))
                return NotFound(new { error = result.ErrorMessage });
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { message = "Successfully registered for event" });
    }

    /// <summary>
    /// Unregisters the current user from an event (decrements attendee count)
    /// </summary>
    [HttpPost("{id}/unregister")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UnregisterFromEvent(int id)
    {
        if (CurrentUserId == null)
            return Unauthorized();
        var userId = CurrentUserId.Value;

        _logger.LogInformation("POST /api/events/{EventId}/unregister - User {UserId} unregistering from event", id, userId);

        var result = await _eventService.DecrementAttendeeCountAsync(id);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage!.Contains("not found"))
                return NotFound(new { error = result.ErrorMessage });
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { message = "Successfully unregistered from event" });
    }
}
