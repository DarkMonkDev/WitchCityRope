using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using WitchCityRope.Web.Services;
using WitchCityRope.Core.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace WitchCityRope.Web.Controllers;

[ApiController]
[Route("api/events")]
public class EventsProxyController : ControllerBase
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<EventsProxyController> _logger;

    public EventsProxyController(ApiClient apiClient, ILogger<EventsProxyController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcomingEvents()
    {
        try
        {
            // Get events from the API service
            var events = await _apiClient.GetEventsAsync(page: 1, pageSize: 20);
            
            // Filter for upcoming events
            var upcomingEvents = events
                .Where(e => e.StartDate > DateTime.UtcNow)
                .OrderBy(e => e.StartDate)
                .Take(5) // Return top 5 upcoming events
                .ToList();

            return Ok(new { events = upcomingEvents });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching upcoming events");
            return StatusCode(500, new { error = "Failed to fetch events" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var events = await _apiClient.GetEventsAsync(page, pageSize);
            return Ok(new { events = events });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching events");
            return StatusCode(500, new { error = "Failed to fetch events" });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            var response = await _apiClient.PostAsync<CreateEventRequest, CreateEventResponse>("events", request);
            return CreatedAtAction(nameof(GetEventById), new { id = response.EventId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            return StatusCode(500, new { error = "Failed to create event" });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEventById(Guid id)
    {
        try
        {
            var eventDto = await _apiClient.GetEventByIdAsync(id);
            if (eventDto == null)
            {
                return NotFound(new { error = "Event not found" });
            }
            return Ok(eventDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event {EventId}", id);
            return StatusCode(500, new { error = "Failed to fetch event" });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEventById(int id)
    {
        try
        {
            // For backward compatibility, try to get event details using the int-based method
            var eventDetails = await _apiClient.GetEventDetailsAsync(id);
            if (eventDetails == null)
            {
                return NotFound(new { error = "Event not found" });
            }
            return Ok(eventDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event {EventId}", id);
            return StatusCode(500, new { error = "Failed to fetch event" });
        }
    }

    [HttpPost("{id:guid}/rsvp")]
    [Authorize]
    public async Task<IActionResult> RsvpToEvent(Guid id, [FromBody] RsvpRequest? request = null)
    {
        try
        {
            // If no request body provided, create empty request
            request ??= new RsvpRequest();
            
            var response = await _apiClient.PostAsync<RsvpRequest, RsvpResponse>($"events/{id}/rsvp", request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RSVP for event {EventId}", id);
            return StatusCode(500, new { error = "Failed to RSVP to event" });
        }
    }

    [HttpPost("{id:int}/rsvp")]
    [Authorize]
    public async Task<IActionResult> RsvpToEvent(int id, [FromBody] RsvpRequest? request = null)
    {
        try
        {
            // For backward compatibility with int-based event IDs
            var result = await _apiClient.RegisterForEventAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RSVP for event {EventId}", id);
            return StatusCode(500, new { error = "Failed to RSVP to event" });
        }
    }
}