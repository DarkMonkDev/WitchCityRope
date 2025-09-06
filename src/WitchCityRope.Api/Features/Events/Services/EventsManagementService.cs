using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Features.Events.DTOs;
using WitchCityRope.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WitchCityRope.Api.Features.Events.Services;

/// <summary>
/// Events Management Service implementing the Event Session Matrix API requirements
/// Following vertical slice architecture patterns from lessons learned
/// </summary>
public class EventsManagementService
{
    private readonly WitchCityRopeDbContext _context;
    private readonly ILogger<EventsManagementService> _logger;

    public EventsManagementService(
        WitchCityRopeDbContext context,
        ILogger<EventsManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets list of published events with basic information (GET /api/events)
    /// Following tuple return pattern from lessons learned
    /// </summary>
    public async Task<(bool Success, List<EventSummaryDto>? Response, string Error)> GetPublishedEventsAsync(
        string? eventType = null,
        bool showPast = false,
        Guid? organizerId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting published events with filters: eventType={EventType}, showPast={ShowPast}, organizerId={OrganizerId}",
                eventType, showPast, organizerId);

            var query = _context.Events
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                .Include(e => e.Organizers)
                .AsNoTracking() // Performance optimization from lessons learned
                .Where(e => e.IsPublished);

            // Apply filters
            if (!showPast)
            {
                query = query.Where(e => e.StartDate > DateTime.UtcNow);
            }

            if (!string.IsNullOrEmpty(eventType))
            {
                if (Enum.TryParse<WitchCityRope.Core.Enums.EventType>(eventType, out var parsedEventType))
                {
                    query = query.Where(e => e.EventType == parsedEventType);
                }
            }

            if (organizerId.HasValue)
            {
                query = query.Where(e => e.Organizers.Any(o => o.Id == organizerId.Value));
            }

            // Order by start date
            query = query.OrderBy(e => e.StartDate);

            // Take max 50 events for performance
            var events = await query.Take(50).ToListAsync(cancellationToken);

            var eventSummaries = events.Select(e => new EventSummaryDto
            {
                EventId = e.Id,
                Title = e.Title,
                ShortDescription = TruncateDescription(e.Description, 200),
                EventType = e.EventType.ToString(),
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Venue = e.Location, // Using Location as venue for now
                TotalCapacity = e.HasSessions() ? e.GetTotalSessionCapacity() : e.Capacity,
                AvailableSpots = e.HasSessions() ? CalculateMinAvailableAcrossSessions(e) : e.GetAvailableSpots(),
                HasMultipleSessions = e.HasSessions(),
                SessionCount = e.Sessions.Count,
                LowestPrice = CalculateLowestPrice(e),
                HighestPrice = CalculateHighestPrice(e),
                IsPublished = e.IsPublished,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToList();

            _logger.LogInformation("Retrieved {Count} published events", eventSummaries.Count);
            return (true, eventSummaries, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting published events");
            return (false, null, "Failed to retrieve events");
        }
    }

    /// <summary>
    /// Gets complete event details including sessions and ticket types (GET /api/events/{eventId})
    /// Following tuple return pattern from lessons learned
    /// </summary>
    public async Task<(bool Success, EventDetailsDto? Response, string Error)> GetEventDetailsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting event details for eventId={EventId}", eventId);

            var eventEntity = await _context.Events
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.TicketTypeSessions)
                .Include(e => e.Organizers)
                .AsNoTracking() // Performance optimization
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Event not found: eventId={EventId}", eventId);
                return (false, null, "Event not found");
            }

            // Build session DTOs
            var sessionDtos = eventEntity.Sessions
                .OrderBy(s => s.SessionIdentifier)
                .Select(s => new EventSessionDto
                {
                    Id = s.Id,
                    SessionIdentifier = s.SessionIdentifier,
                    Name = s.Name,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Capacity = s.Capacity,
                    RegisteredCount = s.RegisteredCount,
                    AvailableSpots = s.GetAvailableSpots(),
                    IsRequired = s.IsRequired,
                    HasAvailableCapacity = s.HasAvailableCapacity()
                })
                .ToList();

            // Build ticket type DTOs
            var ticketTypeDtos = eventEntity.TicketTypes
                .Where(tt => tt.IsActive)
                .OrderBy(tt => tt.Name)
                .Select(tt => new TicketTypeDto
                {
                    TicketTypeId = tt.Id,
                    Name = tt.Name,
                    Description = tt.Description ?? string.Empty,
                    IncludedSessions = tt.GetIncludedSessionIdentifiers().OrderBy(x => x).ToList(),
                    Price = tt.MaxPrice, // Using MaxPrice as regular price
                    MemberPrice = tt.MinPrice != tt.MaxPrice ? tt.MinPrice : null,
                    MaxQuantity = tt.QuantityAvailable ?? int.MaxValue, // Unlimited if null
                    AvailableQuantity = eventEntity.CalculateTicketTypeAvailability(tt),
                    SalesEndDate = tt.SalesEndDate?.ToUniversalTime() ?? eventEntity.StartDate.AddHours(-2), // Default to 2 hours before event
                    IsAvailable = tt.IsCurrentlyOnSale() && eventEntity.CalculateTicketTypeAvailability(tt) > 0,
                    ConstraintReason = DetermineConstraintReason(tt, eventEntity)
                })
                .ToList();

            var eventDetails = new EventDetailsDto
            {
                EventId = eventEntity.Id,
                Title = eventEntity.Title,
                ShortDescription = TruncateDescription(eventEntity.Description, 500),
                FullDescription = eventEntity.Description, // Full description for details view
                EventType = eventEntity.EventType.ToString(),
                PoliciesProcedures = "", // TODO: Add to Event entity
                Venue = new VenueDto
                {
                    Name = eventEntity.Location,
                    Address = eventEntity.Location, // Using location as address for now
                    Capacity = eventEntity.Capacity
                },
                Organizers = eventEntity.Organizers.Select(o => new EventOrganizerDto
                {
                    UserId = o.Id,
                    DisplayName = o.SceneName?.Value ?? "Unknown",
                    Role = "Primary" // TODO: Add role information to Event-Organizer relationship
                }).ToList(),
                Sessions = sessionDtos,
                TicketTypes = ticketTypeDtos,
                IsPublished = eventEntity.IsPublished,
                CreatedAt = eventEntity.CreatedAt,
                UpdatedAt = eventEntity.UpdatedAt
            };

            _logger.LogInformation("Retrieved event details for eventId={EventId} with {SessionCount} sessions and {TicketTypeCount} ticket types",
                eventId, sessionDtos.Count, ticketTypeDtos.Count);

            return (true, eventDetails, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event details for eventId={EventId}", eventId);
            return (false, null, "Failed to retrieve event details");
        }
    }

    /// <summary>
    /// Gets real-time availability for an event (GET /api/events/{eventId}/availability)
    /// </summary>
    public async Task<(bool Success, AvailabilityDto? Response, string Error)> GetEventAvailabilityAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting availability for eventId={EventId}", eventId);

            var eventEntity = await _context.Events
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.TicketTypeSessions)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                return (false, null, "Event not found");
            }

            // Calculate session availability
            var sessionAvailabilities = eventEntity.Sessions
                .Select(s => new SessionAvailabilityDto
                {
                    SessionId = s.SessionIdentifier,
                    Capacity = s.Capacity,
                    Sold = s.RegisteredCount,
                    Available = s.GetAvailableSpots(),
                    HasWaitlist = false, // TODO: Implement waitlist functionality
                    WaitlistCount = 0
                })
                .ToList();

            // Calculate ticket type availability
            var ticketTypeAvailabilities = eventEntity.TicketTypes
                .Where(tt => tt.IsActive)
                .Select(tt => new TicketTypeAvailabilityDto
                {
                    TicketTypeId = tt.Id,
                    IsAvailable = tt.IsCurrentlyOnSale() && eventEntity.CalculateTicketTypeAvailability(tt) > 0,
                    MaxPurchasable = Math.Min(10, eventEntity.CalculateTicketTypeAvailability(tt)), // Max 10 per purchase
                    ConstraintReason = DetermineConstraintReason(tt, eventEntity) ?? string.Empty,
                    LimitingSessionIds = GetLimitingSessionIds(tt, eventEntity)
                })
                .ToList();

            var availability = new AvailabilityDto
            {
                EventId = eventId,
                Sessions = sessionAvailabilities,
                TicketTypes = ticketTypeAvailabilities,
                LastCalculated = DateTime.UtcNow
            };

            return (true, availability, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting availability for eventId={EventId}", eventId);
            return (false, null, "Failed to calculate availability");
        }
    }

    #region Private Helper Methods

    private static string TruncateDescription(string description, int maxLength)
    {
        if (string.IsNullOrEmpty(description) || description.Length <= maxLength)
            return description;
        
        return description.Substring(0, maxLength - 3) + "...";
    }

    private static int CalculateMinAvailableAcrossSessions(WitchCityRope.Core.Entities.Event eventEntity)
    {
        if (!eventEntity.Sessions.Any())
            return eventEntity.GetAvailableSpots();
        
        return eventEntity.Sessions.Min(s => s.GetAvailableSpots());
    }

    private static decimal CalculateLowestPrice(WitchCityRope.Core.Entities.Event eventEntity)
    {
        if (eventEntity.TicketTypes.Any(tt => tt.IsActive))
        {
            return eventEntity.TicketTypes.Where(tt => tt.IsActive).Min(tt => tt.MinPrice);
        }
        
        return eventEntity.PricingTiers.Any() ? eventEntity.PricingTiers.Min(p => p.Amount) : 0;
    }

    private static decimal CalculateHighestPrice(WitchCityRope.Core.Entities.Event eventEntity)
    {
        if (eventEntity.TicketTypes.Any(tt => tt.IsActive))
        {
            return eventEntity.TicketTypes.Where(tt => tt.IsActive).Max(tt => tt.MaxPrice);
        }
        
        return eventEntity.PricingTiers.Any() ? eventEntity.PricingTiers.Max(p => p.Amount) : 0;
    }

    private static string? DetermineConstraintReason(WitchCityRope.Core.Entities.EventTicketType ticketType, WitchCityRope.Core.Entities.Event eventEntity)
    {
        if (!ticketType.IsActive)
            return "Ticket type is inactive";

        if (!ticketType.IsCurrentlyOnSale())
            return "Sales period has ended";

        var availableQuantity = eventEntity.CalculateTicketTypeAvailability(ticketType);
        if (availableQuantity <= 0)
        {
            var limitingSessions = GetLimitingSessionIds(ticketType, eventEntity);
            if (limitingSessions.Any())
            {
                return $"No capacity available in sessions: {string.Join(", ", limitingSessions)}";
            }
            return "No capacity available";
        }

        return null;
    }

    private static List<string> GetLimitingSessionIds(WitchCityRope.Core.Entities.EventTicketType ticketType, WitchCityRope.Core.Entities.Event eventEntity)
    {
        var includedSessionIds = ticketType.GetIncludedSessionIdentifiers();
        var limitingSessions = new List<string>();

        foreach (var sessionId in includedSessionIds)
        {
            var session = eventEntity.GetSession(sessionId);
            if (session != null && !session.HasAvailableCapacity())
            {
                limitingSessions.Add(sessionId);
            }
        }

        return limitingSessions;
    }

    #endregion
}

// Additional DTOs that match the requirements but weren't in existing files

/// <summary>
/// DTO for availability information
/// </summary>
public class AvailabilityDto
{
    public Guid EventId { get; set; }
    public List<SessionAvailabilityDto> Sessions { get; set; } = new();
    public List<TicketTypeAvailabilityDto> TicketTypes { get; set; } = new();
    public DateTime LastCalculated { get; set; }
}

/// <summary>
/// DTO for session availability information
/// </summary>
public class SessionAvailabilityDto
{
    public string SessionId { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int Sold { get; set; }
    public int Available { get; set; }
    public bool HasWaitlist { get; set; }
    public int WaitlistCount { get; set; }
}

/// <summary>
/// DTO for ticket type availability information
/// </summary>
public class TicketTypeAvailabilityDto
{
    public Guid TicketTypeId { get; set; }
    public bool IsAvailable { get; set; }
    public int MaxPurchasable { get; set; }
    public string ConstraintReason { get; set; } = string.Empty;
    public List<string> LimitingSessionIds { get; set; } = new();
}