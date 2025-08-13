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
    [Authorize(Roles = "Organizer,Administrator")]
    public async Task<ActionResult<CreateEventResponse>> CreateEvent([FromBody] CreateEventRequest request)
    {
        // TODO: Get current user ID from claims
        var organizerId = request.OrganizerId; // Temporary - should get from authenticated user
        
        // Convert API model to Core DTO
        var coreRequest = new Core.DTOs.CreateEventRequest
        {
            Name = request.Title,
            Description = request.Description,
            Location = request.Location,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            MaxAttendees = request.MaxAttendees,
            Price = request.Price,
            RequiresVetting = request.RequiresVetting
        };
        
        var response = await _eventService.CreateEventAsync(coreRequest, organizerId);
        
        // Convert Core DTO response back to API response
        var apiResponse = new CreateEventResponse(
            response.EventId,
            coreRequest.Name,
            "event-slug", // TODO: Generate or get actual slug
            DateTime.UtcNow
        );
        
        return Created($"/api/events/{response.EventId}", apiResponse);
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
    /// Get featured events for homepage
    /// </summary>
    [HttpGet("featured")]
    public async Task<ActionResult<GetFeaturedEventsResponse>> GetFeaturedEvents([FromQuery] GetFeaturedEventsRequest request)
    {
        var response = await _eventService.GetFeaturedEventsAsync(request);
        return Ok(response);
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
}