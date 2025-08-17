using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Services;

namespace WitchCityRope.Api.Controllers;

/// <summary>
/// Events API Controller - Step 3: Database Integration
/// Proof-of-concept for React ↔ API ↔ Database communication
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/events
    /// Returns events from PostgreSQL database with hardcoded fallback
    /// Step 3: Database integration with progressive reliability
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("GET /api/events called - attempting database query");

            // Try to get events from database first
            var events = await _eventService.GetPublishedEventsAsync(cancellationToken);
            var eventList = events.ToList();

            if (eventList.Count > 0)
            {
                _logger.LogInformation("Returning {EventCount} events from database", eventList.Count);
                return Ok(eventList);
            }

            // Fallback to hardcoded data if database is empty
            _logger.LogWarning("No events found in database, falling back to hardcoded data");
            var fallbackEvents = GetFallbackEvents();

            _logger.LogInformation("Returning {EventCount} fallback events", fallbackEvents.Length);
            return Ok(fallbackEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database query failed, falling back to hardcoded data");

            // Fallback to hardcoded data if database fails
            try
            {
                var fallbackEvents = GetFallbackEvents();
                _logger.LogInformation("Returning {EventCount} fallback events due to database error", fallbackEvents.Length);
                return Ok(fallbackEvents);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback data generation also failed");
                return StatusCode(500, new
                {
                    message = "Failed to retrieve events from database and fallback",
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }

    /// <summary>
    /// Hardcoded fallback events for reliability
    /// Used when database is unavailable or empty
    /// </summary>
    private static EventDto[] GetFallbackEvents()
    {
        return new[]
        {
            new EventDto
            {
                Id = "550e8400-e29b-41d4-a716-446655440000",
                Title = "Rope Basics Workshop (Fallback)",
                Description = "Learn the fundamentals of rope bondage in a safe, educational environment. Perfect for beginners who want to explore shibari and kinbaku basics.",
                StartDate = new DateTime(2025, 8, 25, 14, 0, 0, DateTimeKind.Utc),
                Location = "Salem Community Center"
            },
            new EventDto
            {
                Id = "550e8400-e29b-41d4-a716-446655440001",
                Title = "Advanced Suspension Techniques (Fallback)",
                Description = "Advanced workshop covering suspension safety, rigging points, and dynamic movements. Prerequisites: completion of intermediate rope workshops.",
                StartDate = new DateTime(2025, 8, 30, 19, 0, 0, DateTimeKind.Utc),
                Location = "Studio Space Downtown"
            },
            new EventDto
            {
                Id = "550e8400-e29b-41d4-a716-446655440002",
                Title = "Community Social & Practice (Fallback)",
                Description = "Open practice session for all skill levels. Bring your rope and practice with others in a supportive community environment.",
                StartDate = new DateTime(2025, 9, 5, 18, 30, 0, DateTimeKind.Utc),
                Location = "Salem Arts Collective"
            }
        };
    }
}