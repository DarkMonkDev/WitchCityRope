using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Interfaces;

namespace WitchCityRope.Api.Features.Events;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Create a new event
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult<CreateEventResponse>> CreateEvent([FromBody] CreateEventRequest request)
    {
        // TODO: Get current user ID from claims
        var organizerId = request.OrganizerId; // Temporary - should get from authenticated user
        var response = await _eventService.CreateEventAsync(request, organizerId);
        return Created($"/api/events/{response.EventId}", response);
    }

    /// <summary>
    /// List events with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ListEventsResponse>> ListEvents([FromQuery] ListEventsRequest request)
    {
        var response = await _eventService.ListEventsAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Get a specific event by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<WitchCityRope.Api.Models.EventDto>> GetEvent(Guid id)
    {
        var eventDetails = await _eventService.GetEventByIdAsync(id);
        
        if (eventDetails == null)
        {
            return NotFound($"Event with ID {id} not found");
        }
        
        return Ok(eventDetails);
    }

    /// <summary>
    /// Get featured events for homepage
    /// </summary>
    [HttpGet("featured")]
    public async Task<ActionResult<GetFeaturedEventsResponse>> GetFeaturedEvents([FromQuery] GetFeaturedEventsRequest request)
    {
        var response = await _eventService.GetFeaturedEventsAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Get a specific event by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<WitchCityRope.Core.DTOs.EventDto>> GetEvent(Guid id)
    {
        var eventDetails = await _eventService.GetEventByIdAsync(id);
        
        if (eventDetails == null)
        {
            return NotFound($"Event with ID {id} not found");
        }
        
        return Ok(eventDetails);
    }

    /// <summary>
    /// Register for an event
    /// </summary>
    [HttpPost("{eventId}/register")]
    [Authorize]
    public async Task<ActionResult<RegisterForEventResponse>> RegisterForEvent(
        Guid eventId,
        [FromBody] RegisterForEventRequest request)
    {
        // Ensure the eventId in the URL matches the request
        if (request.EventId != eventId)
        {
            return BadRequest("Event ID mismatch");
        }

        var response = await _eventService.RegisterForEventAsync(request);
        return Ok(response);
    }

    #region Event Session Management

    /// <summary>
    /// Create a new session for an event
    /// </summary>
    [HttpPost("{eventId}/sessions")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult<EventSessionDto>> CreateEventSession(
        Guid eventId,
        [FromBody] CreateEventSessionRequest request)
    {
        var result = await _eventService.CreateEventSessionAsync(eventId, request);
        
        if (!result.Success)
            return BadRequest(result.Message);

        return Created($"/api/events/{eventId}/sessions/{result.Session!.Id}", result.Session);
    }

    /// <summary>
    /// Update an existing event session
    /// </summary>
    [HttpPut("sessions/{sessionId}")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult<EventSessionDto>> UpdateEventSession(
        Guid sessionId,
        [FromBody] UpdateEventSessionRequest request)
    {
        var result = await _eventService.UpdateEventSessionAsync(sessionId, request);
        
        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Session);
    }

    /// <summary>
    /// Delete an event session
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult> DeleteEventSession(Guid sessionId)
    {
        var result = await _eventService.DeleteEventSessionAsync(sessionId);
        
        if (!result.Success)
            return BadRequest(result.Message);

        return NoContent();
    }

    /// <summary>
    /// Get all sessions for an event
    /// </summary>
    [HttpGet("{eventId}/sessions")]
    public async Task<ActionResult<ICollection<EventSessionDto>>> GetEventSessions(Guid eventId)
    {
        var sessions = await _eventService.GetEventSessionsAsync(eventId);
        return Ok(sessions);
    }

    #endregion

    #region Event Ticket Type Management

    /// <summary>
    /// Create a new ticket type for an event
    /// </summary>
    [HttpPost("{eventId}/ticket-types")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult<EventTicketTypeDto>> CreateEventTicketType(
        Guid eventId,
        [FromBody] CreateEventTicketTypeRequest request)
    {
        var result = await _eventService.CreateEventTicketTypeAsync(eventId, request);
        
        if (!result.Success)
            return BadRequest(result.Message);

        return Created($"/api/events/{eventId}/ticket-types/{result.TicketType!.Id}", result.TicketType);
    }

    /// <summary>
    /// Update an existing event ticket type
    /// </summary>
    [HttpPut("ticket-types/{ticketTypeId}")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult<EventTicketTypeDto>> UpdateEventTicketType(
        Guid ticketTypeId,
        [FromBody] UpdateEventTicketTypeRequest request)
    {
        var result = await _eventService.UpdateEventTicketTypeAsync(ticketTypeId, request);
        
        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.TicketType);
    }

    /// <summary>
    /// Delete an event ticket type
    /// </summary>
    [HttpDelete("ticket-types/{ticketTypeId}")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<ActionResult> DeleteEventTicketType(Guid ticketTypeId)
    {
        var result = await _eventService.DeleteEventTicketTypeAsync(ticketTypeId);
        
        if (!result.Success)
            return BadRequest(result.Message);

        return NoContent();
    }

    /// <summary>
    /// Get all ticket types for an event
    /// </summary>
    [HttpGet("{eventId}/ticket-types")]
    public async Task<ActionResult<ICollection<EventTicketTypeDto>>> GetEventTicketTypes(Guid eventId)
    {
        var ticketTypes = await _eventService.GetEventTicketTypesAsync(eventId);
        return Ok(ticketTypes);
    }

    #endregion
}