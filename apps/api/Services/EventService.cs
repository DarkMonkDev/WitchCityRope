using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services;

/// <summary>
/// Event service implementation with PostgreSQL database access
/// Step 3 of vertical slice - database integration
/// </summary>
public class EventService : IEventService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventService> _logger;

    public EventService(ApplicationDbContext context, ILogger<EventService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve published events from PostgreSQL database
    /// Optimized query with projections and filtering
    /// </summary>
    public async Task<IEnumerable<EventDto>> GetPublishedEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Querying published events from PostgreSQL database");

            // Query database first (EF Core can translate this to SQL)
            var events = await _context.Events
                .AsNoTracking() // Read-only for better performance
                .Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow) // Filter published and future events
                .OrderBy(e => e.StartDate) // Sort by date
                .Take(10) // Limit for POC performance
                .ToListAsync(cancellationToken);

            // Map to DTO in memory (can call custom methods now)
            var eventDtos = events.Select(e => new EventDto
            {
                Id = e.Id.ToString(),
                Title = e.Title,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                EventType = e.EventType,
                Capacity = e.Capacity,
                IsPublished = e.IsPublished,
                CurrentAttendees = e.GetCurrentAttendeeCount(),
                CurrentRSVPs = e.GetCurrentRSVPCount(),
                CurrentTickets = e.GetCurrentTicketCount()
            }).ToList();

            _logger.LogInformation("Retrieved {EventCount} published events from database", eventDtos.Count);
            return eventDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve published events from database");
            throw; // Re-throw to let controller handle HTTP response
        }
    }
}