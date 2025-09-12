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
                .ToListAsync(cancellationToken);

            // Map to DTO after database query to avoid EF Core translation issues
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
                CurrentAttendees = e.GetCurrentAttendeeCount(),
                CurrentRSVPs = e.GetCurrentRSVPCount(),
                CurrentTickets = e.GetCurrentTicketCount()
            }).ToList();

            _logger.LogInformation("Retrieved {EventCount} published events from database", eventDtos.Count);
            return (true, eventDtos, string.Empty);
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
                EndDate = eventEntity.EndDate,
                Location = eventEntity.Location,
                EventType = eventEntity.EventType,
                Capacity = eventEntity.Capacity,
                CurrentAttendees = eventEntity.GetCurrentAttendeeCount(),
                CurrentRSVPs = eventEntity.GetCurrentRSVPCount(),
                CurrentTickets = eventEntity.GetCurrentTicketCount()
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

    /// <summary>
    /// Update an existing event with business rule validation
    /// Supports partial updates - only non-null fields will be updated
    /// </summary>
    public async Task<(bool Success, EventDto? Response, string Error)> UpdateEventAsync(
        string eventId,
        UpdateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(eventId, out var parsedId))
            {
                _logger.LogWarning("Invalid event ID format for update: {EventId}", eventId);
                return (false, null, "Invalid event ID format");
            }

            if (request == null)
            {
                _logger.LogWarning("Update request is null for event: {EventId}", eventId);
                return (false, null, "Update request cannot be null");
            }

            // Find the existing event (with tracking for update)
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == parsedId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogInformation("Event not found for update: {EventId}", eventId);
                return (false, null, "Event not found");
            }

            // Business rule: Cannot update past events
            if (eventEntity.StartDate <= DateTime.UtcNow)
            {
                _logger.LogWarning("Attempted to update past event: {EventId} (StartDate: {StartDate})", 
                    eventId, eventEntity.StartDate);
                return (false, null, "Cannot update past events");
            }

            // Validate capacity changes
            if (request.Capacity.HasValue)
            {
                var currentAttendees = eventEntity.GetCurrentAttendeeCount();
                if (request.Capacity.Value < currentAttendees)
                {
                    _logger.LogWarning("Cannot reduce capacity below current attendance. Event: {EventId}, " +
                        "Requested Capacity: {RequestedCapacity}, Current Attendees: {CurrentAttendees}",
                        eventId, request.Capacity.Value, currentAttendees);
                    return (false, null, $"Cannot reduce capacity to {request.Capacity.Value}. " +
                        $"Current attendance is {currentAttendees}");
                }
            }

            // Validate date range if either date is provided
            var startDate = request.StartDate?.ToUniversalTime() ?? eventEntity.StartDate;
            var endDate = request.EndDate?.ToUniversalTime() ?? eventEntity.EndDate;
            
            if (startDate >= endDate)
            {
                _logger.LogWarning("Invalid date range for event update: {EventId}, " +
                    "StartDate: {StartDate}, EndDate: {EndDate}", eventId, startDate, endDate);
                return (false, null, "Start date must be before end date");
            }

            // Apply updates only for non-null fields (partial update)
            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                eventEntity.Title = request.Title.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                eventEntity.Description = request.Description.Trim();
            }

            if (request.StartDate.HasValue)
            {
                eventEntity.StartDate = startDate;
            }

            if (request.EndDate.HasValue)
            {
                eventEntity.EndDate = endDate;
            }

            if (!string.IsNullOrWhiteSpace(request.Location))
            {
                eventEntity.Location = request.Location.Trim();
            }

            if (request.Capacity.HasValue)
            {
                eventEntity.Capacity = request.Capacity.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.PricingTiers))
            {
                eventEntity.PricingTiers = request.PricingTiers;
            }

            if (request.IsPublished.HasValue)
            {
                eventEntity.IsPublished = request.IsPublished.Value;
            }

            // Update the UpdatedAt timestamp
            eventEntity.UpdatedAt = DateTime.UtcNow;

            // Save changes to database
            await _context.SaveChangesAsync(cancellationToken);

            // Return updated event as DTO
            var updatedEventDto = new EventDto
            {
                Id = eventEntity.Id.ToString(),
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                Location = eventEntity.Location,
                EventType = eventEntity.EventType,
                Capacity = eventEntity.Capacity,
                CurrentAttendees = eventEntity.GetCurrentAttendeeCount(),
                CurrentRSVPs = eventEntity.GetCurrentRSVPCount(),
                CurrentTickets = eventEntity.GetCurrentTicketCount()
            };

            _logger.LogInformation("Event updated successfully: {EventId} ({Title})", 
                eventId, eventEntity.Title);
            return (true, updatedEventDto, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update event: {EventId}", eventId);
            return (false, null, "Failed to update event");
        }
    }
}