using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Features.Events.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
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

    #region RSVP Operations

    /// <summary>
    /// Creates a free RSVP for a social event
    /// Business Rules:
    /// - Only available for Social Events (EventType.Social)
    /// - User cannot already have an RSVP for this event
    /// - Event must have available capacity
    /// </summary>
    public async Task<(bool Success, RSVPDto? Response, string Error)> CreateRSVPAsync(
        Guid eventId,
        Guid userId,
        RSVPRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating RSVP for user {UserId} and event {EventId}", userId, eventId);

            // Get event with RSVPs to check capacity and business rules
            var eventEntity = await _context.Events
                .Include(e => e.RSVPs)
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Event not found: eventId={EventId}", eventId);
                return (false, null, "Event not found");
            }

            // Business rule: Only Social events allow RSVP
            if (!eventEntity.AllowsRSVP)
            {
                _logger.LogWarning("RSVP attempted for non-social event: eventId={EventId}, eventType={EventType}", 
                    eventId, eventEntity.EventType);
                return (false, null, "This event does not allow RSVP. Please purchase a ticket.");
            }

            // Check if user already has an active RSVP
            var existingRSVP = await _context.RSVPs
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId && r.Status != RSVPStatus.Cancelled, 
                    cancellationToken);

            if (existingRSVP != null)
            {
                _logger.LogWarning("User {UserId} already has RSVP for event {EventId}", userId, eventId);
                return (false, null, "You have already RSVP'd for this event");
            }

            // Get user for RSVP creation
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found: userId={UserId}", userId);
                return (false, null, "User not found");
            }

            // Create RSVP (this will validate capacity and business rules)
            var rsvp = new RSVP(user, eventEntity, request.DietaryRestrictions);

            _context.RSVPs.Add(rsvp);
            await _context.SaveChangesAsync(cancellationToken);

            var rsvpDto = new RSVPDto
            {
                Id = rsvp.Id,
                EventId = rsvp.EventId,
                EventTitle = eventEntity.Title,
                EventDate = eventEntity.StartDate,
                ConfirmationCode = rsvp.ConfirmationCode,
                Status = rsvp.Status,
                DietaryRestrictions = rsvp.DietaryRestrictions,
                CreatedAt = rsvp.CreatedAt,
                HasLinkedTicket = rsvp.TicketId.HasValue
            };

            _logger.LogInformation("Successfully created RSVP {RSVPId} for user {UserId} and event {EventId}", 
                rsvp.Id, userId, eventId);

            return (true, rsvpDto, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RSVP for user {UserId} and event {EventId}", userId, eventId);
            return (false, null, "Failed to create RSVP");
        }
    }

    /// <summary>
    /// Gets the attendance status for a user and event
    /// Returns information about RSVP, ticket status, and what actions are available
    /// </summary>
    public async Task<(bool Success, AttendanceStatusDto? Response, string Error)> GetAttendanceStatusAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting attendance status for user {UserId} and event {EventId}", userId, eventId);

            var eventEntity = await _context.Events
                .Include(e => e.RSVPs.Where(r => r.UserId == userId))
                .Include(e => e.Registrations.Where(r => r.UserId == userId))
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Event not found: eventId={EventId}", eventId);
                return (false, null, "Event not found");
            }

            var userRSVP = eventEntity.RSVPs.FirstOrDefault(r => r.Status != RSVPStatus.Cancelled);
            var userRegistration = eventEntity.Registrations.FirstOrDefault(r => r.Status != RegistrationStatus.Cancelled);

            var attendanceStatus = new AttendanceStatusDto
            {
                EventId = eventId,
                HasRSVP = userRSVP != null,
                HasTicket = userRegistration != null,
                CanPurchaseTicket = eventEntity.HasAvailableCapacity(), // Can buy ticket if capacity available
                CanRSVP = eventEntity.AllowsRSVP && userRSVP == null && eventEntity.HasAvailableCapacity()
            };

            // Add RSVP details if exists
            if (userRSVP != null)
            {
                attendanceStatus.RSVP = new RSVPDto
                {
                    Id = userRSVP.Id,
                    EventId = userRSVP.EventId,
                    EventTitle = eventEntity.Title,
                    EventDate = eventEntity.StartDate,
                    ConfirmationCode = userRSVP.ConfirmationCode,
                    Status = userRSVP.Status,
                    DietaryRestrictions = userRSVP.DietaryRestrictions,
                    CreatedAt = userRSVP.CreatedAt,
                    CancelledAt = userRSVP.CancelledAt,
                    CancellationReason = userRSVP.CancellationReason,
                    HasLinkedTicket = userRSVP.TicketId.HasValue
                };
            }

            // Add ticket details if exists
            if (userRegistration != null)
            {
                attendanceStatus.Ticket = new TicketDto
                {
                    Id = userRegistration.Id,
                    EventId = userRegistration.EventId,
                    EventTitle = eventEntity.Title,
                    EventDate = eventEntity.StartDate,
                    ConfirmationCode = userRegistration.ConfirmationCode,
                    PricePaid = userRegistration.SelectedPrice.Amount,
                    Currency = userRegistration.SelectedPrice.Currency,
                    Status = userRegistration.Status,
                    RegisteredAt = userRegistration.RegisteredAt,
                    ConfirmedAt = userRegistration.ConfirmedAt
                };
            }

            // Set constraint reason if can't RSVP
            if (!attendanceStatus.CanRSVP)
            {
                if (!eventEntity.AllowsRSVP)
                {
                    attendanceStatus.RSVPConstraintReason = "This event type does not allow RSVP";
                }
                else if (userRSVP != null)
                {
                    attendanceStatus.RSVPConstraintReason = "You already have an RSVP for this event";
                }
                else if (!eventEntity.HasAvailableCapacity())
                {
                    attendanceStatus.RSVPConstraintReason = "Event is at full capacity";
                }
            }

            return (true, attendanceStatus, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance status for user {UserId} and event {EventId}", userId, eventId);
            return (false, null, "Failed to get attendance status");
        }
    }

    /// <summary>
    /// Cancels an RSVP for a user
    /// </summary>
    public async Task<(bool Success, string Error)> CancelRSVPAsync(
        Guid eventId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling RSVP for user {UserId} and event {EventId}", userId, eventId);

            var rsvp = await _context.RSVPs
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId && r.Status != RSVPStatus.Cancelled,
                    cancellationToken);

            if (rsvp == null)
            {
                _logger.LogWarning("No active RSVP found for user {UserId} and event {EventId}", userId, eventId);
                return (false, "No active RSVP found for this event");
            }

            rsvp.Cancel(reason);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully cancelled RSVP {RSVPId} for user {UserId} and event {EventId}", 
                rsvp.Id, userId, eventId);

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling RSVP for user {UserId} and event {EventId}", userId, eventId);
            return (false, "Failed to cancel RSVP");
        }
    }

    #endregion

    #region Event CRUD Operations

    /// <summary>
    /// Updates an existing event with new details
    /// Business Rules:
    /// - Only event organizers or administrators can update events
    /// - Cannot update past events
    /// - Cannot reduce capacity below current registrations
    /// - Published events have restricted fields that can be changed
    /// </summary>
    public async Task<(bool Success, EventDetailsDto? Response, string Error)> UpdateEventAsync(
        Guid eventId,
        WitchCityRope.Api.Models.UpdateEventRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating event {EventId} by user {UserId}", eventId, userId);

            // Get event with organizers and sessions for validation
            var eventEntity = await _context.Events
                .Include(e => e.Organizers)
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.TicketTypeSessions)
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Event not found: eventId={EventId}", eventId);
                return (false, null, "Event not found");
            }

            // Get user for authorization
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found: userId={UserId}", userId);
                return (false, null, "User not found");
            }

            // Authorization check: user must be organizer or admin
            if (!eventEntity.Organizers.Any(o => o.Id == userId) && user.Role != UserRole.Administrator)
            {
                _logger.LogWarning("User {UserId} not authorized to update event {EventId}", userId, eventId);
                return (false, null, "Not authorized to update this event");
            }

            // Business rule: Cannot update past events
            if (eventEntity.StartDate < DateTime.UtcNow)
            {
                _logger.LogWarning("Attempted to update past event {EventId}", eventId);
                return (false, null, "Cannot update past events");
            }

            // Business rule: Cannot reduce capacity below current attendance (only if capacity is being updated)
            var currentAttendance = eventEntity.GetCurrentAttendeeCount();
            if (request.Capacity.HasValue && request.Capacity.Value < currentAttendance)
            {
                _logger.LogWarning("Attempted to reduce capacity below current attendance for event {EventId}. Current: {Current}, Requested: {Requested}", 
                    eventId, currentAttendance, request.Capacity.Value);
                return (false, null, $"Cannot reduce capacity to {request.Capacity}. Current attendance is {currentAttendance}");
            }

            // Update event properties using available methods - only update non-null values
            if (!string.IsNullOrWhiteSpace(request.Title) || !string.IsNullOrWhiteSpace(request.Description) || !string.IsNullOrWhiteSpace(request.Location))
            {
                var title = !string.IsNullOrWhiteSpace(request.Title) ? request.Title : eventEntity.Title;
                var description = !string.IsNullOrWhiteSpace(request.Description) ? request.Description : eventEntity.Description;
                var location = !string.IsNullOrWhiteSpace(request.Location) ? request.Location : eventEntity.Location;
                
                eventEntity.UpdateDetails(title, description, location);
            }
            
            if (request.StartDate.HasValue || request.EndDate.HasValue)
            {
                var startDate = request.StartDate?.ToUniversalTime() ?? eventEntity.StartDate;
                var endDate = request.EndDate?.ToUniversalTime() ?? eventEntity.EndDate;
                
                eventEntity.UpdateDates(startDate, endDate);
            }
            
            if (request.Capacity.HasValue)
            {
                eventEntity.UpdateCapacity(request.Capacity.Value);
            }
            
            // TODO: EventType property has private setter - need to add UpdateEventType method to Event entity
            // For now, EventType updates are not supported in this MVP version

            // Update published status if changed
            if (request.IsPublished.HasValue && request.IsPublished.Value != eventEntity.IsPublished)
            {
                if (request.IsPublished.Value)
                {
                    eventEntity.Publish();
                }
                else
                {
                    eventEntity.Unpublish();
                }
            }

            // Update pricing if needed (basic price for events without ticket types)
            if (!eventEntity.TicketTypes.Any(tt => tt.IsActive) && request.Price.HasValue && request.Price.Value > 0)
            {
                eventEntity.UpdatePricingTiers(new[] { Money.Create(request.Price.Value) });
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated event {EventId}", eventId);

            // Return updated event details
            var updatedDetails = await GetEventDetailsAsync(eventId, cancellationToken);
            return updatedDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId} by user {UserId}", eventId, userId);
            return (false, null, "Failed to update event");
        }
    }

    /// <summary>
    /// Deletes an event
    /// Business Rules:
    /// - Only event organizers or administrators can delete events
    /// - Cannot delete past events
    /// - Cannot delete events with confirmed registrations (must be cancelled first)
    /// - Published events should be unpublished before deletion
    /// </summary>
    public async Task<(bool Success, string Error)> DeleteEventAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting event {EventId} by user {UserId}", eventId, userId);

            // Get event with organizers and registrations for validation
            var eventEntity = await _context.Events
                .Include(e => e.Organizers)
                .Include(e => e.Registrations)
                .Include(e => e.RSVPs)
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Event not found: eventId={EventId}", eventId);
                return (false, "Event not found");
            }

            // Get user for authorization
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found: userId={UserId}", userId);
                return (false, "User not found");
            }

            // Authorization check: user must be organizer or admin
            if (!eventEntity.Organizers.Any(o => o.Id == userId) && user.Role != UserRole.Administrator)
            {
                _logger.LogWarning("User {UserId} not authorized to delete event {EventId}", userId, eventId);
                return (false, "Not authorized to delete this event");
            }

            // Business rule: Cannot delete past events
            if (eventEntity.StartDate < DateTime.UtcNow)
            {
                _logger.LogWarning("Attempted to delete past event {EventId}", eventId);
                return (false, "Cannot delete past events");
            }

            // Business rule: Cannot delete events with active registrations or RSVPs
            var activeRegistrations = eventEntity.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
            var activeRSVPs = eventEntity.RSVPs.Count(r => r.Status == RSVPStatus.Confirmed);

            if (activeRegistrations > 0 || activeRSVPs > 0)
            {
                _logger.LogWarning("Attempted to delete event {EventId} with active attendance. Registrations: {Registrations}, RSVPs: {RSVPs}", 
                    eventId, activeRegistrations, activeRSVPs);
                return (false, $"Cannot delete event with active attendance. Please cancel all registrations ({activeRegistrations}) and RSVPs ({activeRSVPs}) first.");
            }

            // Unpublish if published
            if (eventEntity.IsPublished)
            {
                eventEntity.Unpublish();
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Unpublished event {EventId} before deletion", eventId);
            }

            // Delete related entities (cascade should handle this, but being explicit)
            _context.EventSessions.RemoveRange(eventEntity.Sessions);
            _context.EventTicketTypes.RemoveRange(eventEntity.TicketTypes);
            
            // Remove cancelled registrations and RSVPs
            var cancelledRegistrations = eventEntity.Registrations.Where(r => r.Status == RegistrationStatus.Cancelled);
            var cancelledRSVPs = eventEntity.RSVPs.Where(r => r.Status == RSVPStatus.Cancelled);
            
            _context.Registrations.RemoveRange(cancelledRegistrations);
            _context.RSVPs.RemoveRange(cancelledRSVPs);

            // Delete the event
            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted event {EventId} by user {UserId}", eventId, userId);

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId} by user {UserId}", eventId, userId);
            return (false, "Failed to delete event");
        }
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