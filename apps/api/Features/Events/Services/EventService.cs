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
                .Include(e => e.Sessions) // Include related sessions
                .Include(e => e.TicketTypes) // Include related ticket types
                    .ThenInclude(tt => tt.Session) // Include session info for ticket types
                .Include(e => e.Organizers) // Include organizers/teachers
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
                IsPublished = e.IsPublished,
                CurrentAttendees = e.GetCurrentAttendeeCount(),
                CurrentRSVPs = e.GetCurrentRSVPCount(),
                CurrentTickets = e.GetCurrentTicketCount(),
                Sessions = e.Sessions.Select(s => new SessionDto(s)).ToList(),
                TicketTypes = e.TicketTypes.Select(tt => new TicketTypeDto(tt)).ToList(),
                TeacherIds = e.Organizers.Select(o => o.Id.ToString()).ToList()
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

            // Direct Entity Framework query for single event with includes
            var eventEntity = await _context.Events
                .Include(e => e.Sessions) // Include related sessions
                .Include(e => e.TicketTypes) // Include related ticket types
                    .ThenInclude(tt => tt.Session) // Include session info for ticket types
                .Include(e => e.Organizers) // Include organizers/teachers
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
                IsPublished = eventEntity.IsPublished,
                CurrentAttendees = eventEntity.GetCurrentAttendeeCount(),
                CurrentRSVPs = eventEntity.GetCurrentRSVPCount(),
                CurrentTickets = eventEntity.GetCurrentTicketCount(),
                Sessions = eventEntity.Sessions.Select(s => new SessionDto(s)).ToList(),
                TicketTypes = eventEntity.TicketTypes.Select(tt => new TicketTypeDto(tt)).ToList(),
                TeacherIds = eventEntity.Organizers.Select(o => o.Id.ToString()).ToList()
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

            // Find the existing event (with tracking for update) and include related data
            var eventEntity = await _context.Events
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                .Include(e => e.Organizers)
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

            // Handle sessions updates if provided
            if (request.Sessions != null)
            {
                await UpdateEventSessionsAsync(eventEntity, request.Sessions, cancellationToken);
            }

            // Handle ticket types updates if provided
            if (request.TicketTypes != null)
            {
                await UpdateEventTicketTypesAsync(eventEntity, request.TicketTypes, cancellationToken);
            }

            // Handle organizers/teachers updates if provided
            if (request.TeacherIds != null)
            {
                await UpdateEventOrganizersAsync(eventEntity, request.TeacherIds, cancellationToken);
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
                IsPublished = eventEntity.IsPublished,
                CurrentAttendees = eventEntity.GetCurrentAttendeeCount(),
                CurrentRSVPs = eventEntity.GetCurrentRSVPCount(),
                CurrentTickets = eventEntity.GetCurrentTicketCount(),
                Sessions = eventEntity.Sessions.Select(s => new SessionDto(s)).ToList(),
                TicketTypes = eventEntity.TicketTypes.Select(tt => new TicketTypeDto(tt)).ToList(),
                TeacherIds = eventEntity.Organizers.Select(o => o.Id.ToString()).ToList()
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

    /// <summary>
    /// Updates the sessions for an event with proper EF Core change tracking
    /// Handles updates, additions, and deletions correctly
    /// </summary>
    private async Task UpdateEventSessionsAsync(
        WitchCityRope.Api.Models.Event eventEntity,
        List<SessionDto> newSessions,
        CancellationToken cancellationToken)
    {
        // Get current sessions mapped by ID for efficient lookups
        var currentSessions = eventEntity.Sessions.ToDictionary(s => s.Id);
        var processedSessionIds = new HashSet<Guid>();

        foreach (var sessionDto in newSessions)
        {
            if (Guid.TryParse(sessionDto.Id, out var sessionId) && currentSessions.TryGetValue(sessionId, out var existingSession))
            {
                // Update existing session
                existingSession.SessionCode = sessionDto.SessionIdentifier;
                existingSession.Name = sessionDto.Name;
                existingSession.StartTime = sessionDto.StartTime.ToUniversalTime();
                existingSession.EndTime = sessionDto.EndTime.ToUniversalTime();
                existingSession.Capacity = sessionDto.Capacity;
                existingSession.CurrentAttendees = sessionDto.RegisteredCount;

                processedSessionIds.Add(sessionId);
            }
            else
            {
                // Add new session
                var newSession = new WitchCityRope.Api.Models.Session
                {
                    EventId = eventEntity.Id,
                    SessionCode = sessionDto.SessionIdentifier,
                    Name = sessionDto.Name,
                    StartTime = sessionDto.StartTime.ToUniversalTime(),
                    EndTime = sessionDto.EndTime.ToUniversalTime(),
                    Capacity = sessionDto.Capacity,
                    CurrentAttendees = sessionDto.RegisteredCount
                };

                // Only set ID if it's a valid new GUID and not already in use
                if (Guid.TryParse(sessionDto.Id, out var newSessionId) && newSessionId != Guid.Empty)
                {
                    newSession.Id = newSessionId;
                    processedSessionIds.Add(newSessionId);
                }

                eventEntity.Sessions.Add(newSession);
            }
        }

        // Remove sessions that are no longer present
        var sessionsToRemove = currentSessions.Values
            .Where(s => !processedSessionIds.Contains(s.Id))
            .ToList();

        foreach (var sessionToRemove in sessionsToRemove)
        {
            eventEntity.Sessions.Remove(sessionToRemove);
        }
    }

    /// <summary>
    /// Updates the ticket types for an event with proper EF Core change tracking
    /// Handles updates, additions, and deletions correctly
    /// </summary>
    private async Task UpdateEventTicketTypesAsync(
        WitchCityRope.Api.Models.Event eventEntity,
        List<TicketTypeDto> newTicketTypes,
        CancellationToken cancellationToken)
    {
        // Get current ticket types mapped by ID for efficient lookups
        var currentTicketTypes = eventEntity.TicketTypes.ToDictionary(tt => tt.Id);
        var processedTicketTypeIds = new HashSet<Guid>();

        foreach (var ticketTypeDto in newTicketTypes)
        {
            if (Guid.TryParse(ticketTypeDto.Id, out var ticketTypeId) && currentTicketTypes.TryGetValue(ticketTypeId, out var existingTicketType))
            {
                // Update existing ticket type
                existingTicketType.Name = ticketTypeDto.Name;
                existingTicketType.Description = $"{ticketTypeDto.Type} ticket";
                existingTicketType.Price = ticketTypeDto.MinPrice;
                existingTicketType.Available = ticketTypeDto.QuantityAvailable;
                existingTicketType.IsRsvpMode = ticketTypeDto.Type == "rsvp";

                // Update session linkage
                if (ticketTypeDto.SessionIdentifiers.Count == 1)
                {
                    var sessionCode = ticketTypeDto.SessionIdentifiers.First();
                    var linkedSession = eventEntity.Sessions.FirstOrDefault(s => s.SessionCode == sessionCode);
                    existingTicketType.SessionId = linkedSession?.Id;
                }
                else
                {
                    existingTicketType.SessionId = null;
                }

                processedTicketTypeIds.Add(ticketTypeId);
            }
            else
            {
                // Add new ticket type
                var newTicketType = new WitchCityRope.Api.Models.TicketType
                {
                    EventId = eventEntity.Id,
                    Name = ticketTypeDto.Name,
                    Description = $"{ticketTypeDto.Type} ticket",
                    Price = ticketTypeDto.MinPrice,
                    Available = ticketTypeDto.QuantityAvailable,
                    Sold = 0, // Start with 0 sold for new ticket types
                    IsRsvpMode = ticketTypeDto.Type == "rsvp"
                };

                // Only set ID if it's a valid new GUID and not already in use
                if (Guid.TryParse(ticketTypeDto.Id, out var newTicketTypeId) && newTicketTypeId != Guid.Empty)
                {
                    newTicketType.Id = newTicketTypeId;
                    processedTicketTypeIds.Add(newTicketTypeId);
                }

                // If this ticket type is for a specific session, find and link it
                if (ticketTypeDto.SessionIdentifiers.Count == 1)
                {
                    var sessionCode = ticketTypeDto.SessionIdentifiers.First();
                    var linkedSession = eventEntity.Sessions.FirstOrDefault(s => s.SessionCode == sessionCode);
                    if (linkedSession != null)
                    {
                        newTicketType.SessionId = linkedSession.Id;
                    }
                }

                eventEntity.TicketTypes.Add(newTicketType);
            }
        }

        // Remove ticket types that are no longer present
        var ticketTypesToRemove = currentTicketTypes.Values
            .Where(tt => !processedTicketTypeIds.Contains(tt.Id))
            .ToList();

        foreach (var ticketTypeToRemove in ticketTypesToRemove)
        {
            eventEntity.TicketTypes.Remove(ticketTypeToRemove);
        }
    }

    /// <summary>
    /// Updates the organizers/teachers for an event with proper EF Core change tracking
    /// Handles additions and removals correctly
    /// </summary>
    private async Task UpdateEventOrganizersAsync(
        WitchCityRope.Api.Models.Event eventEntity,
        List<string> newTeacherIds,
        CancellationToken cancellationToken)
    {
        // Get current organizers mapped by ID for efficient lookups
        var currentOrganizerIds = eventEntity.Organizers.Select(o => o.Id).ToHashSet();
        var newOrganizerIds = new HashSet<Guid>();

        // Parse and validate new teacher IDs
        foreach (var teacherIdString in newTeacherIds)
        {
            if (Guid.TryParse(teacherIdString, out var teacherId))
            {
                newOrganizerIds.Add(teacherId);
            }
            else
            {
                _logger.LogWarning("Invalid teacher ID format: {TeacherId}", teacherIdString);
            }
        }

        // Add new organizers that aren't already associated
        var organizersToAdd = newOrganizerIds.Except(currentOrganizerIds).ToList();
        foreach (var teacherId in organizersToAdd)
        {
            var user = await _context.Users.FindAsync(teacherId);
            if (user != null)
            {
                eventEntity.Organizers.Add(user);
            }
            else
            {
                _logger.LogWarning("Teacher/organizer not found: {TeacherId}", teacherId);
            }
        }

        // Remove organizers that are no longer in the new list
        var organizersToRemove = eventEntity.Organizers
            .Where(o => !newOrganizerIds.Contains(o.Id))
            .ToList();

        foreach (var organizerToRemove in organizersToRemove)
        {
            eventEntity.Organizers.Remove(organizerToRemove);
        }
    }
}