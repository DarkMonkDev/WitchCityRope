using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Events.Services;

/// <summary>
/// Events service using direct Entity Framework access
/// Example of the simplified vertical slice architecture pattern - NO MediatR complexity
/// </summary>
public class EventService : IEventService
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
        return await GetEventsAsync(includeUnpublished: false, includePastEvents: false, cancellationToken);
    }

    /// <summary>
    /// Get all events with optional filters for admin access and past events - Simple Entity Framework service - NO MediatR complexity
    /// </summary>
    public async Task<(bool Success, List<EventDto> Response, string Error)> GetEventsAsync(
        bool includeUnpublished = false,
        bool includePastEvents = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventTypeFilter = includeUnpublished ? "all events" : "published events";
            var timeFilter = includePastEvents ? " (including past events)" : "";
            _logger.LogInformation("Querying {EventTypeFilter}{TimeFilter} from PostgreSQL database", eventTypeFilter, timeFilter);

            // OPTIMIZATION: Add Include() for related collections to prevent N+1 queries
            // Before: Lazy loading triggers N+1 queries when accessing Sessions, TicketTypes, etc.
            // After: Single query with joins loads all related data
            // Impact: Reduces query count from 1+4N to 1 (80%+ reduction)
            IQueryable<WitchCityRope.Api.Models.Event> query = _context.Events
                .AsNoTracking() // Read-only for better performance
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.Session)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.Purchases) // Include purchases for dynamic QuantitySold calculation
                        .ThenInclude(p => p.User) // Load user to match with EventParticipation
                .Include(e => e.VolunteerPositions)
                .Include(e => e.Organizers)
                .Include(e => e.Venue) // Load venue for location name
                .Include(e => e.EventAttendances) // Load all attendances for the event
                    .ThenInclude(ea => ea.TicketPurchase) // CRITICAL: Load TicketPurchase for sold count calculation
                        .ThenInclude(tp => tp.TicketType); // CRITICAL: Load TicketType for session matching

            // Apply filters based on admin vs public access
            if (includeUnpublished)
            {
                // Admin access: Show all events (both published and draft), including future and past
                query = query.Where(e => e.StartDate > DateTime.UtcNow.AddDays(-30)); // Show events from last 30 days
            }
            else
            {
                // Public access: Only published events
                if (includePastEvents)
                {
                    // Show published events including past ones (last 90 days)
                    query = query.Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow.AddDays(-90));
                }
                else
                {
                    // Default: Only published future events
                    query = query.Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow);
                }
            }

            var events = await query
                .OrderBy(e => e.StartDate) // Sort by date
                .Take(50) // Reasonable limit for performance
                .ToListAsync(cancellationToken);

            // Map to DTO after database query (using DTO constructors for complex nested objects)
            // Note: Could optimize further with Select projection, but DTOs have constructors for this
            var eventDtos = events.Select(e => new EventDto
            {
                Id = e.Id.ToString(),
                Title = e.Title,
                ShortDescription = e.ShortDescription,
                Description = e.Description,
                Policies = e.Policies,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                VenueId = e.VenueId,
                VenueLocation = e.Venue?.Location,
                EventType = e.EventType.ToString(),
                Capacity = e.Capacity,
                IsPublished = e.IsPublished,
                RegistrationCount = e.GetCurrentAttendeeCount(),
                CurrentRSVPs = e.GetCurrentRSVPCount(),
                CurrentTickets = e.GetCurrentTicketCount(),
                Sessions = e.Sessions.Select(s => new SessionDto(s)).ToList(),
                TicketTypes = e.TicketTypes.Select(tt => new TicketTypeDto(tt, e.EventAttendances)).ToList(),
                VolunteerPositions = e.VolunteerPositions.Select(vp => new VolunteerPositionDto(vp)).ToList(),
                TeacherIds = e.Organizers.Select(o => o.Id.ToString()).ToList(),
                // Granular timing controls
                RegistrationOpenHours = e.RegistrationOpenHours,
                RegistrationCloseHours = e.RegistrationCloseHours,
                CancellationCloseHours = e.CancellationCloseHours,
                VolunteerRegistrationCloseHours = e.VolunteerRegistrationCloseHours,
                VolunteerCancellationCloseHours = e.VolunteerCancellationCloseHours
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

            // OPTIMIZATION: Include related collections to prevent N+1 queries
            // Before: Accessing Sessions, TicketTypes, etc. triggers separate queries
            // After: Single query loads event with all related data
            // Impact: Reduces from 5 queries to 1 (80% reduction)
            var eventEntity = await _context.Events
                .AsNoTracking()
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.Session)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.Purchases)
                        .ThenInclude(p => p.User) // Load user to match with EventParticipation
                .Include(e => e.VolunteerPositions)
                .Include(e => e.Organizers)
                .Include(e => e.Venue) // Load venue for location name
                .Include(e => e.EventAttendances) // Load all attendances for the event
                    .ThenInclude(ea => ea.TicketPurchase) // CRITICAL: Load TicketPurchase for sold count calculation
                        .ThenInclude(tp => tp.TicketType) // CRITICAL: Load TicketType for session matching
                .FirstOrDefaultAsync(e => e.Id == parsedId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogInformation("Event not found: {EventId}", eventId);
                return (false, null, "Event not found");
            }

            // Calculate CurrentAttendees for each session from actual ticket purchases
            // Create a lookup of active user IDs from event attendances
            var activeUserIds = eventEntity.EventAttendances
                .Where(ea => ea.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Active)
                .Select(ep => ep.UserId)
                .ToHashSet();

            foreach (var session in eventEntity.Sessions)
            {
                // Count completed ticket purchases for this session
                // For single-session tickets: TicketType.SessionId == session.Id
                // For multi-session tickets: Would need additional logic (not implemented yet)
                // CRITICAL: Exclude cancelled/refunded tickets by checking EventAttendance.Status
                // Only count Active attendances (status = 1), exclude Cancelled (2), Refunded (3), Waitlisted (4)
                var ticketsSold = eventEntity.TicketTypes
                    .Where(tt => tt.SessionId == session.Id)
                    .SelectMany(tt => tt.Purchases)
                    .Where(p =>
                        p.IsPaymentCompleted &&
                        activeUserIds.Contains(p.UserId))
                    .Sum(p => p.Quantity);

                // DELETE: CurrentAttendees is now a calculated property, not a stored field
                // session.CurrentAttendees = ticketsSold;
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
                VenueId = eventEntity.VenueId,
                VenueLocation = eventEntity.Venue?.Location,
                EventType = eventEntity.EventType.ToString(),
                Capacity = eventEntity.Capacity,
                IsPublished = eventEntity.IsPublished,
                RegistrationCount = eventEntity.GetCurrentAttendeeCount(),
                CurrentRSVPs = eventEntity.GetCurrentRSVPCount(),
                CurrentTickets = eventEntity.GetCurrentTicketCount(),
                Sessions = eventEntity.Sessions.Select(s => new SessionDto(s)).ToList(),
                TicketTypes = eventEntity.TicketTypes.Select(tt => new TicketTypeDto(tt, eventEntity.EventAttendances)).ToList(),
                VolunteerPositions = eventEntity.VolunteerPositions.Select(vp => new VolunteerPositionDto(vp)).ToList(),
                TeacherIds = eventEntity.Organizers.Select(o => o.Id.ToString()).ToList(),
                // Granular timing controls
                RegistrationOpenHours = eventEntity.RegistrationOpenHours,
                RegistrationCloseHours = eventEntity.RegistrationCloseHours,
                CancellationCloseHours = eventEntity.CancellationCloseHours,
                VolunteerRegistrationCloseHours = eventEntity.VolunteerRegistrationCloseHours,
                VolunteerCancellationCloseHours = eventEntity.VolunteerCancellationCloseHours
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
                .Include(e => e.Venue) // Load venue for location name
                .Include(e => e.EventAttendances) // Include attendances for capacity validation
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

            if (request.VenueId.HasValue)
            {
                eventEntity.VenueId = request.VenueId.Value;
            }

            if (request.Capacity.HasValue)
            {
                eventEntity.Capacity = request.Capacity.Value;
            }

            if (request.IsPublished.HasValue)
            {
                eventEntity.IsPublished = request.IsPublished.Value;
            }

            // CRITICAL: Update timing control fields - NULLABLE DECIMAL UPDATE PATTERN
            // These fields control when registration/cancellation windows open/close
            //
            // IMPORTANT CHANGE (2025-11-22): Fixed null value persistence for timing fields
            //
            // PROBLEM: Previous code used `if (request.Field.HasValue) { entity.Field = request.Field.Value; }`
            // This ONLY updated when HasValue=true, preventing null values from being saved to database.
            // When users cleared a field, the frontend sent null, but the backend skipped the update,
            // leaving the old value in the database.
            //
            // SOLUTION: Detect timing-only updates by checking if other major fields are null.
            // Frontend sends timing fields in well-defined groups:
            // - RSVP timing: All 4 fields together (handleSaveRsvpTiming in EventForm.tsx lines 1203-1209)
            // - Volunteer timing: Both fields together (handleSaveVolunteerTiming lines 1247-1249)
            //
            // We check if this is a timing-only update by verifying that all major event fields are null.
            // If so, we update ALL timing fields in the group (allowing null values to be persisted).
            // If ANY field in the group has a value, we also update the group (handles mixed values).
            //
            // This allows:
            // - Clearing all timing fields in a group (all null values persist)
            // - Setting some fields and clearing others in a group (mixed values persist)
            // - Partial updates of other fields don't affect timing fields

            // Detect if this is a timing-only update (no other major fields being updated)
            bool isTimingOnlyUpdate =
                request.Title == null &&
                request.Description == null &&
                request.ShortDescription == null &&
                request.Policies == null &&
                request.StartDate == null &&
                request.EndDate == null &&
                request.VenueId == null &&
                request.Capacity == null &&
                request.IsPublished == null &&
                request.Sessions == null &&
                request.TicketTypes == null &&
                request.TeacherIds == null &&
                request.VolunteerPositions == null;

            // RSVP/Registration timing fields (frontend sends all 4 together as a group)
            bool hasRsvpTimingFields =
                request.RegistrationOpenHours.HasValue ||
                request.RegistrationCloseHours.HasValue ||
                request.CancellationCloseHours.HasValue;

            if (hasRsvpTimingFields || isTimingOnlyUpdate)
            {
                // Update ALL RSVP timing fields (including null ones)
                // This handles both cases:
                // 1. At least one field has a value (mixed update)
                // 2. All fields are null but this is a timing-only update (clear all)
                eventEntity.RegistrationOpenHours = request.RegistrationOpenHours;
                eventEntity.RegistrationCloseHours = request.RegistrationCloseHours;
                eventEntity.CancellationCloseHours = request.CancellationCloseHours;

                _logger.LogDebug("Updated RSVP timing: RegOpen={RegOpen}, RegClose={RegClose}, " +
                    "CancelClose={CancelClose}",
                    request.RegistrationOpenHours, request.RegistrationCloseHours,
                    request.CancellationCloseHours);
            }

            // Volunteer timing fields (frontend sends both together as a group)
            bool hasVolunteerTimingFields =
                request.VolunteerRegistrationCloseHours.HasValue ||
                request.VolunteerCancellationCloseHours.HasValue;

            if (hasVolunteerTimingFields || isTimingOnlyUpdate)
            {
                // Update ALL volunteer timing fields (including null ones)
                // This handles both cases:
                // 1. At least one field has a value (mixed update)
                // 2. All fields are null but this is a timing-only update (clear all)
                eventEntity.VolunteerRegistrationCloseHours = request.VolunteerRegistrationCloseHours;
                eventEntity.VolunteerCancellationCloseHours = request.VolunteerCancellationCloseHours;

                _logger.LogDebug("Updated Volunteer timing: RegClose={VolRegClose}, CancelClose={VolCancelClose}",
                    request.VolunteerRegistrationCloseHours, request.VolunteerCancellationCloseHours);
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
                VenueId = eventEntity.VenueId,
                VenueLocation = eventEntity.Venue?.Location,
                EventType = eventEntity.EventType.ToString(),
                Capacity = eventEntity.Capacity,
                IsPublished = eventEntity.IsPublished,
                RegistrationCount = eventEntity.GetCurrentAttendeeCount(),
                CurrentRSVPs = eventEntity.GetCurrentRSVPCount(),
                CurrentTickets = eventEntity.GetCurrentTicketCount(),
                Sessions = eventEntity.Sessions.Select(s => new SessionDto(s)).ToList(),
                TicketTypes = eventEntity.TicketTypes.Select(tt => new TicketTypeDto(tt, eventEntity.EventAttendances)).ToList(),
                VolunteerPositions = eventEntity.VolunteerPositions.Select(vp => new VolunteerPositionDto(vp)).ToList(),
                TeacherIds = eventEntity.Organizers.Select(o => o.Id.ToString()).ToList(),
                // Granular timing controls
                RegistrationOpenHours = eventEntity.RegistrationOpenHours,
                RegistrationCloseHours = eventEntity.RegistrationCloseHours,
                CancellationCloseHours = eventEntity.CancellationCloseHours,
                VolunteerRegistrationCloseHours = eventEntity.VolunteerRegistrationCloseHours,
                VolunteerCancellationCloseHours = eventEntity.VolunteerCancellationCloseHours
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
    /// Recalculates the event's StartDate based on its sessions.
    /// StartDate = earliest session that hasn't passed yet.
    /// If all sessions have passed, StartDate = earliest session overall.
    /// </summary>
    private void RecalculateEventStartDate(WitchCityRope.Api.Models.Event eventEntity)
    {
        if (eventEntity.Sessions == null || !eventEntity.Sessions.Any())
        {
            // No sessions - keep existing StartDate
            return;
        }

        var now = DateTime.UtcNow;

        // Get earliest future session
        var earliestFutureSession = eventEntity.Sessions
            .Where(s => s.StartTime > now)
            .OrderBy(s => s.StartTime)
            .FirstOrDefault();

        if (earliestFutureSession != null)
        {
            // Use earliest future session's start time
            eventEntity.StartDate = earliestFutureSession.StartTime;
        }
        else
        {
            // All sessions have passed - use earliest session overall
            var earliestSession = eventEntity.Sessions
                .OrderBy(s => s.StartTime)
                .First();
            eventEntity.StartDate = earliestSession.StartTime;
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
                // CurrentAttendees is calculated from actual ticket purchases/participations, not user input

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
                    Capacity = sessionDto.Capacity
                    // CurrentAttendees is now a calculated property, no need to set it
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

        // Recalculate event's StartDate based on updated sessions
        RecalculateEventStartDate(eventEntity);

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
                existingTicketType.Description = $"{ticketTypeDto.PricingType} ticket";
                existingTicketType.PricingType = ticketTypeDto.PricingType;
                existingTicketType.Available = ticketTypeDto.QuantityAvailable;

                // Set pricing fields based on pricing type
                if (ticketTypeDto.PricingType == WitchCityRope.Models.PricingType.Fixed)
                {
                    existingTicketType.Price = ticketTypeDto.Price;
                    existingTicketType.MinPrice = null;
                    existingTicketType.MaxPrice = null;
                    existingTicketType.DefaultPrice = null;
                }
                else // SlidingScale
                {
                    existingTicketType.Price = null;
                    existingTicketType.MinPrice = ticketTypeDto.MinPrice;
                    existingTicketType.MaxPrice = ticketTypeDto.MaxPrice;
                    existingTicketType.DefaultPrice = ticketTypeDto.DefaultPrice;
                }

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
                    Description = $"{ticketTypeDto.PricingType} ticket",
                    Available = ticketTypeDto.QuantityAvailable,
                    // DELETE: Sold is now a calculated property, not a stored field
                    PricingType = ticketTypeDto.PricingType
                };

                // Set pricing fields based on pricing type
                if (ticketTypeDto.PricingType == WitchCityRope.Models.PricingType.Fixed)
                {
                    newTicketType.Price = ticketTypeDto.Price;
                    newTicketType.MinPrice = null;
                    newTicketType.MaxPrice = null;
                    newTicketType.DefaultPrice = null;
                }
                else // SlidingScale
                {
                    newTicketType.Price = null;
                    newTicketType.MinPrice = ticketTypeDto.MinPrice;
                    newTicketType.MaxPrice = ticketTypeDto.MaxPrice;
                    newTicketType.DefaultPrice = ticketTypeDto.DefaultPrice;
                }

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

        // OPTIMIZATION: Batch load all users to add in single query instead of N individual queries
        // Before: N queries (1 per organizer)
        // After: 1 query (all organizers)
        // Impact: 90% reduction for N=10 organizers
        if (organizersToAdd.Any())
        {
            var usersToAdd = await _context.Users
                .Where(u => organizersToAdd.Contains(u.Id))
                .ToListAsync(cancellationToken);

            foreach (var user in usersToAdd)
            {
                eventEntity.Organizers.Add(user);
                _logger.LogInformation("Added organizer {TeacherId} ({UserEmail}) to event {EventId}",
                    user.Id, user.Email, eventEntity.Id);
            }

            // Log any missing users
            var foundUserIds = usersToAdd.Select(u => u.Id).ToHashSet();
            var missingUserIds = organizersToAdd.Except(foundUserIds);
            foreach (var missingId in missingUserIds)
            {
                _logger.LogWarning("Teacher/organizer not found: {TeacherId}", missingId);
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

    /// <summary>
    /// Creates a deep copy of an event with all related entities
    /// Implements atomic transaction for data integrity
    /// </summary>
    public async Task<(bool Success, EventDto? Response, string? Error)> CopyEventAsync(
        string eventId,
        CopyEventRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validation - Parse event ID
            if (!Guid.TryParse(eventId, out var parsedEventId))
            {
                _logger.LogWarning("Invalid event ID format for copy: {EventId}", eventId);
                return (false, null, "Invalid event ID format");
            }

            // 2. Load source event with ALL related entities
            // CRITICAL: Use AsNoTracking to prevent EF tracking issues during copy
            var sourceEvent = await _context.Events
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                .Include(e => e.VolunteerPositions)
                .Include(e => e.Organizers)
                .Include(e => e.Venue)
                .Include(e => e.EventAttendances) // Needed for computed properties in DTO
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == parsedEventId, cancellationToken);

            if (sourceEvent == null)
            {
                _logger.LogWarning("Source event not found for copy: {EventId}", eventId);
                return (false, null, "Event not found");
            }

            _logger.LogInformation("Starting copy of event {EventId} ({Title}) with new title '{NewTitle}' and start date {NewStartDate}",
                eventId, sourceEvent.Title, request.NewTitle, request.NewStartDate);

            // 3. Begin transaction for atomic operation
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 4. Calculate date offset for session time adjustments
                var dateOffset = request.NewStartDate - sourceEvent.StartDate;

                // 5. Create new event with copied properties
                var copiedEvent = new WitchCityRope.Api.Models.Event
                {
                    // NEW values
                    Id = Guid.NewGuid(),
                    Title = request.NewTitle,
                    StartDate = request.NewStartDate.DateTime,
                    EndDate = sourceEvent.EndDate.Add(dateOffset),
                    IsPublished = false, // Always create as draft
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,

                    // COPY AS-IS
                    ShortDescription = sourceEvent.ShortDescription,
                    Description = sourceEvent.Description,
                    Policies = sourceEvent.Policies,
                    Capacity = sourceEvent.Capacity,
                    EventType = sourceEvent.EventType,
                    VenueId = sourceEvent.VenueId,

                    // COPY timing controls (5 fields)
                    RegistrationOpenHours = sourceEvent.RegistrationOpenHours,
                    RegistrationCloseHours = sourceEvent.RegistrationCloseHours,
                    CancellationCloseHours = sourceEvent.CancellationCloseHours,
                    VolunteerRegistrationCloseHours = sourceEvent.VolunteerRegistrationCloseHours,
                    VolunteerCancellationCloseHours = sourceEvent.VolunteerCancellationCloseHours
                };

                _context.Events.Add(copiedEvent);

                // 6. Deep copy Sessions with date offset
                // Track old SessionId -> new SessionId mapping for remapping
                var sessionIdMap = new Dictionary<Guid, Guid>();

                foreach (var sourceSession in sourceEvent.Sessions)
                {
                    var newSessionId = Guid.NewGuid();
                    sessionIdMap[sourceSession.Id] = newSessionId;

                    var copiedSession = new Session
                    {
                        Id = newSessionId,
                        EventId = copiedEvent.Id,
                        SessionCode = sourceSession.SessionCode,
                        Name = sourceSession.Name,
                        StartTime = sourceSession.StartTime.Add(dateOffset),
                        EndTime = sourceSession.EndTime.Add(dateOffset),
                        Capacity = sourceSession.Capacity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    copiedEvent.Sessions.Add(copiedSession);
                }

                _logger.LogInformation("Copied {SessionCount} sessions for event {NewEventId}",
                    sourceEvent.Sessions.Count, copiedEvent.Id);

                // Recalculate StartDate based on copied session times
                RecalculateEventStartDate(copiedEvent);

                // 7. Deep copy TicketTypes with SessionId remapping
                foreach (var sourceTicket in sourceEvent.TicketTypes)
                {
                    var copiedTicket = new TicketType
                    {
                        Id = Guid.NewGuid(),
                        EventId = copiedEvent.Id,

                        // REMAP SessionId to new session (if session-specific)
                        SessionId = sourceTicket.SessionId.HasValue
                            ? sessionIdMap[sourceTicket.SessionId.Value]
                            : null,

                        // COPY properties
                        Name = sourceTicket.Name,
                        Description = sourceTicket.Description,
                        PricingType = sourceTicket.PricingType,
                        Price = sourceTicket.Price,
                        MinPrice = sourceTicket.MinPrice,
                        MaxPrice = sourceTicket.MaxPrice,
                        DefaultPrice = sourceTicket.DefaultPrice,
                        Available = sourceTicket.Available
                        // NOTE: Sold and Purchases are NOT copied (computed/excluded)
                    };

                    copiedEvent.TicketTypes.Add(copiedTicket);
                }

                _logger.LogInformation("Copied {TicketTypeCount} ticket types for event {NewEventId}",
                    sourceEvent.TicketTypes.Count, copiedEvent.Id);

                // 8. Deep copy VolunteerPositions with SessionId remapping, reset SlotsFilled
                foreach (var sourcePosition in sourceEvent.VolunteerPositions)
                {
                    var copiedPosition = new VolunteerPosition
                    {
                        Id = Guid.NewGuid(),
                        EventId = copiedEvent.Id,

                        // REMAP SessionId to new session (if session-specific)
                        SessionId = sourcePosition.SessionId.HasValue
                            ? sessionIdMap[sourcePosition.SessionId.Value]
                            : null,

                        // COPY properties
                        Title = sourcePosition.Title,
                        Description = sourcePosition.Description,
                        SlotsNeeded = sourcePosition.SlotsNeeded,
                        IsPublicFacing = sourcePosition.IsPublicFacing,

                        // RESET filled count
                        SlotsFilled = 0,

                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    copiedEvent.VolunteerPositions.Add(copiedPosition);
                }

                _logger.LogInformation("Copied {PositionCount} volunteer positions for event {NewEventId}",
                    sourceEvent.VolunteerPositions.Count, copiedEvent.Id);

                // 9. Copy Organizers (many-to-many relationship)
                // Same organizers/teachers for the copied event
                copiedEvent.Organizers = sourceEvent.Organizers.ToList();

                _logger.LogInformation("Copied {OrganizerCount} organizers for event {NewEventId}",
                    sourceEvent.Organizers.Count, copiedEvent.Id);

                // 10. Deep copy EventEmailTemplates (custom templates only)
                // Load custom email templates for source event
                var sourceTemplates = await _context.Set<WitchCityRope.Api.Features.EmailTemplates.Entities.EventEmailTemplate>()
                    .Where(t => t.EventId == sourceEvent.Id)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                foreach (var sourceTemplate in sourceTemplates)
                {
                    var copiedTemplate = new WitchCityRope.Api.Features.EmailTemplates.Entities.EventEmailTemplate
                    {
                        Id = Guid.NewGuid(),
                        EventId = copiedEvent.Id, // Associate with new event
                        GlobalTemplateId = sourceTemplate.GlobalTemplateId, // Preserve reference
                        TemplateType = sourceTemplate.TemplateType,
                        Subject = sourceTemplate.Subject,
                        HtmlBody = sourceTemplate.HtmlBody,
                        PlainTextBody = sourceTemplate.PlainTextBody,
                        TargetSessions = sourceTemplate.TargetSessions,
                        RecipientGroup = sourceTemplate.RecipientGroup,
                        IsCustomized = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = sourceTemplate.UpdatedBy // Keep original creator reference
                    };

                    _context.Set<WitchCityRope.Api.Features.EmailTemplates.Entities.EventEmailTemplate>().Add(copiedTemplate);
                }

                _logger.LogInformation("Copied {TemplateCount} custom email templates for event {NewEventId}",
                    sourceTemplates.Count, copiedEvent.Id);

                // 11. Save all changes in transaction
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Successfully copied event {SourceEventId} to {NewEventId} ({NewTitle})",
                    eventId, copiedEvent.Id, copiedEvent.Title);

                // 12. Map to DTO and return
                // Need to reload with all navigation properties for DTO mapping
                var copiedEventWithNav = await _context.Events
                    .Include(e => e.Sessions)
                    .Include(e => e.TicketTypes)
                    .Include(e => e.VolunteerPositions)
                    .Include(e => e.Organizers)
                    .Include(e => e.Venue)
                    .Include(e => e.EventAttendances)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == copiedEvent.Id, cancellationToken);

                if (copiedEventWithNav == null)
                {
                    _logger.LogError("CRITICAL: Copied event {EventId} not found after save", copiedEvent.Id);
                    return (false, null, "Failed to retrieve copied event");
                }

                var eventDto = new EventDto
                {
                    Id = copiedEventWithNav.Id.ToString(),
                    Title = copiedEventWithNav.Title,
                    ShortDescription = copiedEventWithNav.ShortDescription,
                    Description = copiedEventWithNav.Description,
                    Policies = copiedEventWithNav.Policies,
                    StartDate = copiedEventWithNav.StartDate,
                    EndDate = copiedEventWithNav.EndDate,
                    VenueId = copiedEventWithNav.VenueId,
                    VenueLocation = copiedEventWithNav.Venue?.Location,
                    EventType = copiedEventWithNav.EventType.ToString(),
                    Capacity = copiedEventWithNav.Capacity,
                    IsPublished = copiedEventWithNav.IsPublished,
                    RegistrationCount = copiedEventWithNav.GetCurrentAttendeeCount(),
                    CurrentRSVPs = copiedEventWithNav.GetCurrentRSVPCount(),
                    CurrentTickets = copiedEventWithNav.GetCurrentTicketCount(),
                    Sessions = copiedEventWithNav.Sessions.Select(s => new SessionDto(s)).ToList(),
                    TicketTypes = copiedEventWithNav.TicketTypes.Select(tt => new TicketTypeDto(tt, copiedEventWithNav.EventAttendances)).ToList(),
                    VolunteerPositions = copiedEventWithNav.VolunteerPositions.Select(vp => new VolunteerPositionDto(vp)).ToList(),
                    TeacherIds = copiedEventWithNav.Organizers.Select(o => o.Id.ToString()).ToList(),
                    RegistrationOpenHours = copiedEventWithNav.RegistrationOpenHours,
                    RegistrationCloseHours = copiedEventWithNav.RegistrationCloseHours,
                    CancellationCloseHours = copiedEventWithNav.CancellationCloseHours,
                    VolunteerRegistrationCloseHours = copiedEventWithNav.VolunteerRegistrationCloseHours,
                    VolunteerCancellationCloseHours = copiedEventWithNav.VolunteerCancellationCloseHours
                };

                return (true, eventDto, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Transaction failed while copying event {EventId}", eventId);
                throw; // Re-throw to outer catch
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy event {EventId}", eventId);
            return (false, null, "Failed to copy event. Please try again.");
        }
    }

    /// <summary>
    /// Create a new event with all related entities
    /// Implements atomic transaction for data integrity
    /// </summary>
    public async Task<(bool Success, EventDto? Response, string Error)> CreateEventAsync(
        CreateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validation
            if (request == null)
            {
                _logger.LogWarning("Create request is null");
                return (false, null, "Create request cannot be null");
            }

            // Validate date range
            var startDate = request.StartDate.ToUniversalTime();
            var endDate = request.EndDate.ToUniversalTime();

            if (startDate >= endDate)
            {
                _logger.LogWarning("Invalid date range for event creation: StartDate: {StartDate}, EndDate: {EndDate}",
                    startDate, endDate);
                return (false, null, "Start date must be before end date");
            }

            // Parse and validate EventType enum
            if (!Enum.TryParse<WitchCityRope.Api.Enums.EventType>(request.EventType, out var eventType))
            {
                _logger.LogWarning("Invalid event type: {EventType}", request.EventType);
                return (false, null, $"Invalid event type: {request.EventType}");
            }

            _logger.LogInformation("Creating new event: {Title}, Type: {EventType}, StartDate: {StartDate}",
                request.Title, request.EventType, request.StartDate);

            // 2. Begin transaction for atomic operation
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 3. Create new event entity
                // CRITICAL: DO NOT initialize Id in property - let EF Core generate it
                var newEvent = new WitchCityRope.Api.Models.Event
                {
                    // Basic properties
                    Title = request.Title.Trim(),
                    ShortDescription = string.IsNullOrWhiteSpace(request.ShortDescription)
                        ? null
                        : request.ShortDescription.Trim(),
                    Description = request.Description.Trim(),
                    Policies = string.IsNullOrWhiteSpace(request.Policies)
                        ? null
                        : request.Policies.Trim(),
                    StartDate = startDate,
                    EndDate = endDate,
                    VenueId = request.VenueId,
                    EventType = eventType,
                    Capacity = request.Capacity,
                    IsPublished = request.IsPublished,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,

                    // Timing controls (all optional)
                    RegistrationOpenHours = request.RegistrationOpenHours,
                    RegistrationCloseHours = request.RegistrationCloseHours,
                    CancellationCloseHours = request.CancellationCloseHours,
                    VolunteerRegistrationCloseHours = request.VolunteerRegistrationCloseHours,
                    VolunteerCancellationCloseHours = request.VolunteerCancellationCloseHours
                };

                _context.Events.Add(newEvent);

                // 4. Add sessions if provided
                if (request.Sessions != null && request.Sessions.Any())
                {
                    foreach (var sessionDto in request.Sessions)
                    {
                        var newSession = new WitchCityRope.Api.Models.Session
                        {
                            // DO NOT set Id - let EF generate it
                            EventId = newEvent.Id,
                            SessionCode = sessionDto.SessionIdentifier,
                            Name = sessionDto.Name,
                            StartTime = sessionDto.StartTime.ToUniversalTime(),
                            EndTime = sessionDto.EndTime.ToUniversalTime(),
                            Capacity = sessionDto.Capacity,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        newEvent.Sessions.Add(newSession);
                    }

                    _logger.LogInformation("Added {SessionCount} sessions to new event", request.Sessions.Count);

                    // Recalculate StartDate based on actual session times
                    RecalculateEventStartDate(newEvent);
                }

                // 5. Add ticket types if provided
                if (request.TicketTypes != null && request.TicketTypes.Any())
                {
                    foreach (var ticketTypeDto in request.TicketTypes)
                    {
                        var newTicketType = new WitchCityRope.Api.Models.TicketType
                        {
                            EventId = newEvent.Id,
                            Name = ticketTypeDto.Name,
                            Description = $"{ticketTypeDto.PricingType} ticket",
                            Available = ticketTypeDto.QuantityAvailable,
                            PricingType = ticketTypeDto.PricingType
                        };

                        // Set pricing fields based on pricing type
                        if (ticketTypeDto.PricingType == WitchCityRope.Models.PricingType.Fixed)
                        {
                            newTicketType.Price = ticketTypeDto.Price;
                            newTicketType.MinPrice = null;
                            newTicketType.MaxPrice = null;
                            newTicketType.DefaultPrice = null;
                        }
                        else // SlidingScale
                        {
                            newTicketType.Price = null;
                            newTicketType.MinPrice = ticketTypeDto.MinPrice;
                            newTicketType.MaxPrice = ticketTypeDto.MaxPrice;
                            newTicketType.DefaultPrice = ticketTypeDto.DefaultPrice;
                        }

                        // Link to session if specified
                        if (ticketTypeDto.SessionIdentifiers.Count == 1)
                        {
                            var sessionCode = ticketTypeDto.SessionIdentifiers.First();
                            var linkedSession = newEvent.Sessions.FirstOrDefault(s => s.SessionCode == sessionCode);
                            if (linkedSession != null)
                            {
                                newTicketType.SessionId = linkedSession.Id;
                            }
                        }

                        newEvent.TicketTypes.Add(newTicketType);
                    }

                    _logger.LogInformation("Added {TicketTypeCount} ticket types to new event", request.TicketTypes.Count);
                }

                // 6. Add volunteer positions if provided
                if (request.VolunteerPositions != null && request.VolunteerPositions.Any())
                {
                    foreach (var positionDto in request.VolunteerPositions)
                    {
                        var newPosition = new WitchCityRope.Api.Models.VolunteerPosition
                        {
                            EventId = newEvent.Id,
                            Title = positionDto.Title,
                            Description = positionDto.Description,
                            SlotsNeeded = positionDto.SlotsNeeded,
                            SlotsFilled = 0, // New event starts with no filled slots
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        // Link to session if specified
                        if (!string.IsNullOrEmpty(positionDto.SessionId) && Guid.TryParse(positionDto.SessionId, out var sessionId))
                        {
                            // For new events, match by session code since IDs aren't persisted yet
                            var linkedSession = newEvent.Sessions.FirstOrDefault(s => s.Id == sessionId);
                            if (linkedSession != null)
                            {
                                newPosition.SessionId = linkedSession.Id;
                            }
                        }

                        newEvent.VolunteerPositions.Add(newPosition);
                    }

                    _logger.LogInformation("Added {PositionCount} volunteer positions to new event", request.VolunteerPositions.Count);
                }

                // 7. Add organizers/teachers if provided
                if (request.TeacherIds != null && request.TeacherIds.Any())
                {
                    var teacherGuids = new List<Guid>();
                    foreach (var teacherIdString in request.TeacherIds)
                    {
                        if (Guid.TryParse(teacherIdString, out var teacherId))
                        {
                            teacherGuids.Add(teacherId);
                        }
                        else
                        {
                            _logger.LogWarning("Invalid teacher ID format during event creation: {TeacherId}", teacherIdString);
                        }
                    }

                    if (teacherGuids.Any())
                    {
                        // OPTIMIZATION: Batch load all organizers in single query
                        var organizers = await _context.Users
                            .Where(u => teacherGuids.Contains(u.Id))
                            .ToListAsync(cancellationToken);

                        foreach (var organizer in organizers)
                        {
                            newEvent.Organizers.Add(organizer);
                        }

                        _logger.LogInformation("Added {OrganizerCount} organizers to new event", organizers.Count);
                    }
                }

                // 8. Save all changes in transaction
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Successfully created event {EventId} ({Title})", newEvent.Id, newEvent.Title);

                // 9. Reload event with navigation properties for DTO mapping
                var createdEventWithNav = await _context.Events
                    .Include(e => e.Sessions)
                    .Include(e => e.TicketTypes)
                        .ThenInclude(tt => tt.Session)
                    .Include(e => e.VolunteerPositions)
                    .Include(e => e.Organizers)
                    .Include(e => e.Venue)
                    .Include(e => e.EventAttendances)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == newEvent.Id, cancellationToken);

                if (createdEventWithNav == null)
                {
                    _logger.LogError("CRITICAL: Created event {EventId} not found after save", newEvent.Id);
                    return (false, null, "Failed to retrieve created event");
                }

                // 10. Map to DTO and return
                var eventDto = new EventDto
                {
                    Id = createdEventWithNav.Id.ToString(),
                    Title = createdEventWithNav.Title,
                    ShortDescription = createdEventWithNav.ShortDescription,
                    Description = createdEventWithNav.Description,
                    Policies = createdEventWithNav.Policies,
                    StartDate = createdEventWithNav.StartDate,
                    EndDate = createdEventWithNav.EndDate,
                    VenueId = createdEventWithNav.VenueId,
                    VenueLocation = createdEventWithNav.Venue?.Location,
                    EventType = createdEventWithNav.EventType.ToString(),
                    Capacity = createdEventWithNav.Capacity,
                    IsPublished = createdEventWithNav.IsPublished,
                    RegistrationCount = createdEventWithNav.GetCurrentAttendeeCount(),
                    CurrentRSVPs = createdEventWithNav.GetCurrentRSVPCount(),
                    CurrentTickets = createdEventWithNav.GetCurrentTicketCount(),
                    Sessions = createdEventWithNav.Sessions.Select(s => new SessionDto(s)).ToList(),
                    TicketTypes = createdEventWithNav.TicketTypes.Select(tt => new TicketTypeDto(tt, createdEventWithNav.EventAttendances)).ToList(),
                    VolunteerPositions = createdEventWithNav.VolunteerPositions.Select(vp => new VolunteerPositionDto(vp)).ToList(),
                    TeacherIds = createdEventWithNav.Organizers.Select(o => o.Id.ToString()).ToList(),
                    RegistrationOpenHours = createdEventWithNav.RegistrationOpenHours,
                    RegistrationCloseHours = createdEventWithNav.RegistrationCloseHours,
                    CancellationCloseHours = createdEventWithNav.CancellationCloseHours,
                    VolunteerRegistrationCloseHours = createdEventWithNav.VolunteerRegistrationCloseHours,
                    VolunteerCancellationCloseHours = createdEventWithNav.VolunteerCancellationCloseHours
                };

                return (true, eventDto, string.Empty);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Transaction failed while creating event");
                throw; // Re-throw to outer catch
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create event: {Title}", request?.Title ?? "unknown");
            return (false, null, "Failed to create event. Please try again.");
        }
    }
}