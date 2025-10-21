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
        return await GetEventsAsync(includeUnpublished: false, cancellationToken);
    }

    /// <summary>
    /// Get all events with optional filter for admin access - Simple Entity Framework service - NO MediatR complexity
    /// </summary>
    public async Task<(bool Success, List<EventDto> Response, string Error)> GetEventsAsync(
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventTypeFilter = includeUnpublished ? "all events" : "published events";
            _logger.LogInformation("Querying {EventTypeFilter} from PostgreSQL database", eventTypeFilter);

            // Direct Entity Framework query with AsNoTracking for read performance
            var query = _context.Events
                .Include(e => e.Sessions) // Include related sessions
                .Include(e => e.TicketTypes) // Include related ticket types
                    .ThenInclude(tt => tt.Session) // Include session info for ticket types
                .Include(e => e.VolunteerPositions) // Include related volunteer positions
                .Include(e => e.Organizers) // Include organizers/teachers
                .Include(e => e.EventParticipations) // Include participations for RSVP/ticket counts
                .AsNoTracking(); // Read-only for better performance

            // Apply filters based on admin vs public access
            if (includeUnpublished)
            {
                // Admin access: Show all events (both published and draft), including future and past
                query = query.Where(e => e.StartDate > DateTime.UtcNow.AddDays(-30)); // Show events from last 30 days
            }
            else
            {
                // Public access: Only published future events
                query = query.Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow);
            }

            var events = await query
                .OrderBy(e => e.StartDate) // Sort by date
                .Take(50) // Reasonable limit for performance
                .ToListAsync(cancellationToken);

            // Map to DTO after database query to avoid EF Core translation issues
            var eventDtos = events.Select(e => new EventDto
            {
                Id = e.Id.ToString(),
                Title = e.Title,
                ShortDescription = e.ShortDescription,
                Description = e.Description,
                Policies = e.Policies,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                EventType = e.EventType,
                Capacity = e.Capacity,
                IsPublished = e.IsPublished,
                RegistrationCount = e.GetCurrentAttendeeCount(),
                CurrentRSVPs = e.GetCurrentRSVPCount(),
                CurrentTickets = e.GetCurrentTicketCount(),
                Sessions = e.Sessions.Select(s => new SessionDto(s)).ToList(),
                TicketTypes = e.TicketTypes.Select(tt => new TicketTypeDto(tt)).ToList(),
                VolunteerPositions = e.VolunteerPositions.Select(vp => new VolunteerPositionDto(vp)).ToList(),
                TeacherIds = e.Organizers.Select(o => o.Id.ToString()).ToList()
            }).ToList();

            _logger.LogInformation("Retrieved {EventCount} {EventTypeFilter} from database", eventDtos.Count, eventTypeFilter);
            return (true, eventDtos, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve events from database");
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
                .Include(e => e.VolunteerPositions) // Include related volunteer positions
                .Include(e => e.Organizers) // Include organizers/teachers
                .Include(e => e.EventParticipations) // Include participations for RSVP/ticket counts
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
                ShortDescription = eventEntity.ShortDescription,
                Description = eventEntity.Description,
                Policies = eventEntity.Policies,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                Location = eventEntity.Location,
                EventType = eventEntity.EventType,
                Capacity = eventEntity.Capacity,
                IsPublished = eventEntity.IsPublished,
                RegistrationCount = eventEntity.GetCurrentAttendeeCount(),
                CurrentRSVPs = eventEntity.GetCurrentRSVPCount(),
                CurrentTickets = eventEntity.GetCurrentTicketCount(),
                Sessions = eventEntity.Sessions.Select(s => new SessionDto(s)).ToList(),
                TicketTypes = eventEntity.TicketTypes.Select(tt => new TicketTypeDto(tt)).ToList(),
                VolunteerPositions = eventEntity.VolunteerPositions.Select(vp => new VolunteerPositionDto(vp)).ToList(),
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

            // Log what we received in the request
            _logger.LogInformation("Update request for event {EventId}: Title={Title}, Sessions={SessionCount}, TicketTypes={TicketTypeCount}, TeacherIds={TeacherIdCount}",
                eventId,
                request.Title ?? "null",
                request.Sessions?.Count ?? 0,
                request.TicketTypes?.Count ?? 0,
                request.TeacherIds?.Count ?? 0);

            if (request.TeacherIds != null)
            {
                _logger.LogInformation("TeacherIds in request: [{TeacherIds}]", string.Join(", ", request.TeacherIds));
            }

            // Find the existing event (with tracking for update) and include related data
            var eventEntity = await _context.Events
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                .Include(e => e.VolunteerPositions)
                .Include(e => e.Organizers)
                .Include(e => e.EventParticipations) // Include participations for capacity validation
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

            if (request.ShortDescription != null)
            {
                eventEntity.ShortDescription = string.IsNullOrWhiteSpace(request.ShortDescription)
                    ? null
                    : request.ShortDescription.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                eventEntity.Description = request.Description.Trim();
            }

            if (request.Policies != null)
            {
                _logger.LogInformation("🔍 Policies field received: IsNull={IsNull}, IsEmpty={IsEmpty}, Length={Length}, Value=[{Value}]",
                    request.Policies == null,
                    string.IsNullOrWhiteSpace(request.Policies),
                    request.Policies?.Length ?? 0,
                    request.Policies);

                eventEntity.Policies = string.IsNullOrWhiteSpace(request.Policies)
                    ? null
                    : request.Policies.Trim();

                _logger.LogInformation("🔍 Policies after processing: [{ProcessedValue}]", eventEntity.Policies);
            }
            else
            {
                _logger.LogWarning("⚠️ Policies field was NULL in request - not included in update");
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

            // Handle volunteer positions updates if provided
            if (request.VolunteerPositions != null)
            {
                await UpdateEventVolunteerPositionsAsync(eventEntity, request.VolunteerPositions, cancellationToken);
            }

            // Update the UpdatedAt timestamp
            eventEntity.UpdatedAt = DateTime.UtcNow;

            // Log what we're about to save
            _logger.LogInformation("🔍 About to save event. ShortDescription=[{Short}], Policies=[{Policies}]",
                eventEntity.ShortDescription,
                eventEntity.Policies);

            // CRITICAL: Explicitly mark entity as modified to ensure EF Core tracks the change
            // This is required when modifying properties directly (not through navigation properties)
            // Similar to ticket cancellation fix - see backend-developer-lessons-learned-2.md lines 1211-1320
            _context.Events.Update(eventEntity);

            // Save changes to database
            await _context.SaveChangesAsync(cancellationToken);

            // Return updated event as DTO
            var updatedEventDto = new EventDto
            {
                Id = eventEntity.Id.ToString(),
                Title = eventEntity.Title,
                ShortDescription = eventEntity.ShortDescription,
                Description = eventEntity.Description,
                Policies = eventEntity.Policies,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                Location = eventEntity.Location,
                EventType = eventEntity.EventType,
                Capacity = eventEntity.Capacity,
                IsPublished = eventEntity.IsPublished,
                RegistrationCount = eventEntity.GetCurrentAttendeeCount(),
                CurrentRSVPs = eventEntity.GetCurrentRSVPCount(),
                CurrentTickets = eventEntity.GetCurrentTicketCount(),
                Sessions = eventEntity.Sessions.Select(s => new SessionDto(s)).ToList(),
                TicketTypes = eventEntity.TicketTypes.Select(tt => new TicketTypeDto(tt)).ToList(),
                VolunteerPositions = eventEntity.VolunteerPositions.Select(vp => new VolunteerPositionDto(vp)).ToList(),
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
    private Task UpdateEventSessionsAsync(
        WitchCityRope.Api.Models.Event eventEntity,
        List<SessionDto> newSessions,
        CancellationToken cancellationToken)
    {
        // Get current sessions mapped by ID for efficient lookups
        var currentSessions = eventEntity.Sessions.ToDictionary(s => s.Id);
        var processedSessionIds = new HashSet<Guid>();

        foreach (var sessionDto in newSessions)
        {
            // Only treat as existing if ID is valid AND exists in current sessions
            if (!string.IsNullOrEmpty(sessionDto.Id) &&
                Guid.TryParse(sessionDto.Id, out var sessionId) &&
                sessionId != Guid.Empty &&
                currentSessions.TryGetValue(sessionId, out var existingSession))
            {
                // Update existing session
                existingSession.SessionCode = sessionDto.SessionIdentifier;
                existingSession.Name = sessionDto.Name;
                existingSession.StartTime = sessionDto.StartTime.ToUniversalTime();
                existingSession.EndTime = sessionDto.EndTime.ToUniversalTime();
                existingSession.Capacity = sessionDto.Capacity;
                existingSession.CurrentAttendees = sessionDto.RegistrationCount;

                processedSessionIds.Add(sessionId);
            }
            else
            {
                // Add new session - DO NOT set ID, let EF generate it
                // This includes sessions with client-generated IDs that don't exist in DB
                var newSession = new WitchCityRope.Api.Models.Session
                {
                    // Do NOT set Id - let EF generate it
                    EventId = eventEntity.Id,
                    SessionCode = sessionDto.SessionIdentifier,
                    Name = sessionDto.Name,
                    StartTime = sessionDto.StartTime.ToUniversalTime(),
                    EndTime = sessionDto.EndTime.ToUniversalTime(),
                    Capacity = sessionDto.Capacity,
                    CurrentAttendees = sessionDto.RegistrationCount
                };

                // Let Entity Framework generate the ID for new sessions
                // The ID from frontend is just a temporary client-side ID
                eventEntity.Sessions.Add(newSession);

                // Track that this is a new session (won't be in processedSessionIds)
                // This ensures it won't be deleted in the removal logic
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

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the ticket types for an event with proper EF Core change tracking
    /// Handles updates, additions, and deletions correctly
    /// </summary>
    private Task UpdateEventTicketTypesAsync(
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
                // Add new ticket type - DO NOT set ID, let EF generate it
                // This includes ticket types with client-generated IDs that don't exist in DB
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

                // Let Entity Framework generate the ID for new ticket types
                // The ID from frontend is just a temporary client-side ID

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

        return Task.CompletedTask;
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
        _logger.LogInformation("Updating organizers for event {EventId}. Received {Count} teacher IDs: [{TeacherIds}]",
            eventEntity.Id, newTeacherIds.Count, string.Join(", ", newTeacherIds));

        // Get current organizers mapped by ID for efficient lookups
        var currentOrganizerIds = eventEntity.Organizers.Select(o => o.Id).ToHashSet();
        var newOrganizerIds = new HashSet<Guid>();

        _logger.LogInformation("Current organizers for event {EventId}: [{CurrentOrganizers}]",
            eventEntity.Id, string.Join(", ", currentOrganizerIds));

        // Parse and validate new teacher IDs
        foreach (var teacherIdString in newTeacherIds)
        {
            if (Guid.TryParse(teacherIdString, out var teacherId))
            {
                newOrganizerIds.Add(teacherId);
                _logger.LogDebug("Successfully parsed teacher ID: {TeacherId}", teacherId);
            }
            else
            {
                _logger.LogWarning("Invalid teacher ID format: {TeacherId}", teacherIdString);
            }
        }

        // Add new organizers that aren't already associated
        var organizersToAdd = newOrganizerIds.Except(currentOrganizerIds).ToList();
        _logger.LogInformation("Adding {Count} new organizers: [{OrganizersToAdd}]",
            organizersToAdd.Count, string.Join(", ", organizersToAdd));

        foreach (var teacherId in organizersToAdd)
        {
            var user = await _context.Users.FindAsync(teacherId);
            if (user != null)
            {
                eventEntity.Organizers.Add(user);
                _logger.LogInformation("Added organizer {TeacherId} ({UserEmail}) to event {EventId}",
                    teacherId, user.Email, eventEntity.Id);
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

        _logger.LogInformation("Removing {Count} organizers: [{OrganizersToRemove}]",
            organizersToRemove.Count, string.Join(", ", organizersToRemove.Select(o => o.Id)));

        foreach (var organizerToRemove in organizersToRemove)
        {
            eventEntity.Organizers.Remove(organizerToRemove);
            _logger.LogInformation("Removed organizer {TeacherId} ({UserEmail}) from event {EventId}",
                organizerToRemove.Id, organizerToRemove.Email, eventEntity.Id);
        }

        _logger.LogInformation("Completed organizer update for event {EventId}. Final organizer count: {Count}",
            eventEntity.Id, eventEntity.Organizers.Count);
    }

    /// <summary>
    /// Updates the volunteer positions for an event with proper EF Core change tracking
    /// Handles updates, additions, and deletions correctly
    /// </summary>
    private Task UpdateEventVolunteerPositionsAsync(
        WitchCityRope.Api.Models.Event eventEntity,
        List<VolunteerPositionDto> newPositions,
        CancellationToken cancellationToken)
    {
        // Get current volunteer positions mapped by ID for efficient lookups
        var currentPositions = eventEntity.VolunteerPositions.ToDictionary(vp => vp.Id);
        var processedPositionIds = new HashSet<Guid>();

        foreach (var positionDto in newPositions)
        {
            // Only treat as existing if ID is valid AND exists in current positions
            if (!string.IsNullOrEmpty(positionDto.Id) &&
                Guid.TryParse(positionDto.Id, out var positionId) &&
                positionId != Guid.Empty &&
                currentPositions.TryGetValue(positionId, out var existingPosition))
            {
                // Update existing volunteer position
                existingPosition.Title = positionDto.Title;
                existingPosition.Description = positionDto.Description;
                existingPosition.SlotsNeeded = positionDto.SlotsNeeded;
                existingPosition.SlotsFilled = positionDto.SlotsFilled;

                // Update session linkage if provided
                if (!string.IsNullOrEmpty(positionDto.SessionId) && Guid.TryParse(positionDto.SessionId, out var sessionId))
                {
                    existingPosition.SessionId = sessionId;
                }
                else
                {
                    existingPosition.SessionId = null;
                }

                processedPositionIds.Add(positionId);
            }
            else
            {
                // Add new volunteer position - DO NOT set ID, let EF generate it
                // This includes positions with client-generated IDs that don't exist in DB
                var newPosition = new WitchCityRope.Api.Models.VolunteerPosition
                {
                    EventId = eventEntity.Id,
                    Title = positionDto.Title,
                    Description = positionDto.Description,
                    SlotsNeeded = positionDto.SlotsNeeded,
                    SlotsFilled = positionDto.SlotsFilled
                };

                // Set session linkage if provided
                if (!string.IsNullOrEmpty(positionDto.SessionId) && Guid.TryParse(positionDto.SessionId, out var sessionId))
                {
                    newPosition.SessionId = sessionId;
                }

                // Let Entity Framework generate the ID for new positions
                // The ID from frontend is just a temporary client-side ID
                eventEntity.VolunteerPositions.Add(newPosition);
            }
        }

        // Remove volunteer positions that are no longer present
        var positionsToRemove = currentPositions.Values
            .Where(vp => !processedPositionIds.Contains(vp.Id))
            .ToList();

        foreach (var positionToRemove in positionsToRemove)
        {
            eventEntity.VolunteerPositions.Remove(positionToRemove);
        }

        return Task.CompletedTask;
    }
}