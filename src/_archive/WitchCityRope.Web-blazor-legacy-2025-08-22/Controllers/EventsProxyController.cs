using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using WitchCityRope.Web.Services;

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
}