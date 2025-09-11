using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Events.Models;

namespace WitchCityRope.Api.Features.Events.Services;

/// <summary>
/// Events service using direct Entity Framework access
/// Example of the simplified vertical slice architecture pattern - NO MediatR complexity
/// </summary>
public class EventService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventService> _logger;

    public EventService(
        ApplicationDbContext context,
        ILogger<EventService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all published events - Simple Entity Framework service - NO MediatR complexity
    /// </summary>
    public async Task<(bool Success, List<EventDto> Response, string Error)> GetPublishedEventsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Querying published events from PostgreSQL database");

            // Direct Entity Framework query with AsNoTracking for read performance
            var events = await _context.Events
                .AsNoTracking() // Read-only for better performance
                .Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow) // Filter published and future events
                .OrderBy(e => e.StartDate) // Sort by date
                .Take(50) // Reasonable limit for performance
                .Select(e => new EventDto // Project to DTO in database
                {
                    Id = e.Id.ToString(),
                    Title = e.Title,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    Location = e.Location,
                    EventType = e.EventType.ToString()
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {EventCount} published events from database", events.Count);
            return (true, events, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve published events from database");
            return (false, new List<EventDto>(), "Failed to retrieve events");
        }
    }

    /// <summary>
    /// Get single event by ID - Direct Entity Framework access
    /// </summary>
    public async Task<(bool Success, EventDto? Response, string Error)> GetEventAsync(
        string eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(eventId, out var parsedId))
            {
                _logger.LogWarning("Invalid event ID format: {EventId}", eventId);
                return (false, null, "Invalid event ID format");
            }

            // Direct Entity Framework query for single event
            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == parsedId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogInformation("Event not found: {EventId}", eventId);
                return (false, null, "Event not found");
            }

            var eventDto = new EventDto
            {
                Id = eventEntity.Id.ToString(),
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                StartDate = eventEntity.StartDate,
                Location = eventEntity.Location,
                EventType = eventEntity.EventType.ToString()
            };

            _logger.LogDebug("Event retrieved successfully: {EventId} ({Title})", eventId, eventEntity.Title);
            return (true, eventDto, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve event: {EventId}", eventId);
            return (false, null, "Failed to retrieve event");
        }
    }
}