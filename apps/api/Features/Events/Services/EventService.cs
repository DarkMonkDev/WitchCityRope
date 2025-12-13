using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Interfaces;
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
    private readonly ITimeZoneService _timeZoneService;

    public EventService(
        ApplicationDbContext context,
        ILogger<EventService> logger,
        ITimeZoneService timeZoneService)
    {
        _context = context;
        _logger = logger;
        _timeZoneService = timeZoneService;
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
                    .ThenInclude(tt => tt.Sessions)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.Purchases) // Include purchases for dynamic QuantitySold calculation
                        .ThenInclude(p => p.User) // Load user to match with EventParticipation
                .Include(e => e.VolunteerPositions)
                    .ThenInclude(vp => vp.Session) // Load Session for volunteer position start/end times
                .Include(e => e.Organizers)
                .Include(e => e.Venue) // Load venue for location name
                .Include(e => e.EventAttendances) // Load all attendances for the event
                    .ThenInclude(ea => ea.TicketPurchase) // CRITICAL: Load TicketPurchase for sold count calculation
                        .ThenInclude(tp => tp.TicketType) // CRITICAL: Load TicketType for session matching
                            .ThenInclude(tt => tt.Sessions); // CRITICAL: Load Sessions collection for Session.CurrentAttendees calculation (Session.cs lines 73-86)

            // Apply filters based on admin vs public access
            var now = DateTime.UtcNow;

            if (includeUnpublished)
            {
                // Admin access: Show all events (both published and draft), including future and past
                // Filter based on sessions: show events with sessions in last 365 days OR with future sessions
                query = query.Where(e =>
                    e.Sessions.Any(s => s.EndTime > now.AddDays(-365)) || // Has session that ended within last year
                    e.Sessions.Any(s => s.StartTime > now) || // Has upcoming session
                    e.StartDate > now.AddDays(-365)); // Fallback to StartDate for events without sessions
            }
            else
            {
                // Public access: Only published events
                if (includePastEvents)
                {
                    // Show published events including past ones (last 90 days)
                    // Filter based on sessions: show events with sessions in last 90 days OR with future sessions
                    query = query.Where(e => e.IsPublished && (
                        e.Sessions.Any(s => s.EndTime > now.AddDays(-90)) || // Has session that ended within last 90 days
                        e.Sessions.Any(s => s.StartTime > now) || // Has upcoming session
                        e.StartDate > now.AddDays(-90))); // Fallback to StartDate for events without sessions
                }
                else
                {
                    // Default: Only published future events
                    // An event is "future" if it has ANY session with StartTime > now
                    // An event is "past" only if ALL sessions have ended (EndTime < now)
                    query = query.Where(e => e.IsPublished && (
                        e.Sessions.Any(s => s.StartTime > now) || // Has at least one upcoming session
                        (!e.Sessions.Any() && e.StartDate > now))); // Fallback: no sessions but StartDate is future
                }
            }

            var events = await query
                .OrderBy(e => e.StartDate) // Sort by date
                .Take(50) // Reasonable limit for performance
                .ToListAsync(cancellationToken);

            // Map to DTO after database query (using DTO constructors for complex nested objects)
            // Note: Could optimize further with Select projection, but DTOs have constructors for this
            var eventDtos = events.Select(e =>
            {
                var ticketTypeDtos = e.TicketTypes.Select(tt => new TicketTypeDto(tt, e.EventAttendances)).ToList();

                // Populate session-based timing fields for ticket types
                PopulateTicketTypeTimingFields(ticketTypeDtos, e);

                return new EventDto
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
                    AllowRsvps = e.AllowRsvps,
                    RequireTicketPurchase = e.RequireTicketPurchase,
                    VettedMembersOnly = e.VettedMembersOnly,
                    Capacity = e.Capacity,
                    IsPublished = e.IsPublished,
                    RegistrationCount = e.GetCurrentAttendeeCount(),
                    CurrentRSVPs = e.GetCurrentRSVPCount(),
                    CurrentTickets = e.GetCurrentTicketCount(),
                    Sessions = e.Sessions.Select(s => new SessionDto(s)).ToList(),
                    TicketTypes = ticketTypeDtos,
                    VolunteerPositions = e.VolunteerPositions.Select(vp => new VolunteerPositionDto(vp)).ToList(),
                    TeacherIds = e.Organizers.Select(o => o.Id.ToString()).ToList(),
                    // Granular timing controls
                    RegistrationOpenHours = e.RegistrationOpenHours,
                    RegistrationCloseHours = e.RegistrationCloseHours,
                    CancellationCloseHours = e.CancellationCloseHours,
                    VolunteerRegistrationCloseHours = e.VolunteerRegistrationCloseHours,
                    VolunteerCancellationCloseHours = e.VolunteerCancellationCloseHours
                };
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
                    .ThenInclude(tt => tt.Sessions)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.Purchases)
                        .ThenInclude(p => p.User) // Load user to match with EventParticipation
                .Include(e => e.VolunteerPositions)
                    .ThenInclude(vp => vp.Session) // Load Session for volunteer position start/end times
                .Include(e => e.Organizers)
                .Include(e => e.Venue) // Load venue for location name
                .Include(e => e.EventAttendances) // Load all attendances for the event
                    .ThenInclude(ea => ea.TicketPurchase) // CRITICAL: Load TicketPurchase for sold count calculation
                        .ThenInclude(tp => tp.TicketType) // CRITICAL: Load TicketType for session matching
                            .ThenInclude(tt => tt.Sessions) // CRITICAL: Load Sessions collection for Session.CurrentAttendees calculation (Session.cs lines 73-86)
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
                // For tickets that include this session (via Sessions collection)
                // CRITICAL: Exclude cancelled/refunded tickets by checking EventAttendance.Status
                // Only count Active attendances (status = 1), exclude Cancelled (2), Refunded (3), Waitlisted (4)
                var ticketsSold = eventEntity.TicketTypes
                    .Where(tt => tt.Sessions.Any(s => s.Id == session.Id))
                    .SelectMany(tt => tt.Purchases)
                    .Where(p =>
                        p.IsPaymentCompleted &&
                        activeUserIds.Contains(p.UserId))
                    .Sum(p => p.Quantity);

                // DELETE: CurrentAttendees is now a calculated property, not a stored field
                // session.CurrentAttendees = ticketsSold;
            }

            var ticketTypeDtos = eventEntity.TicketTypes.Select(tt => new TicketTypeDto(tt, eventEntity.EventAttendances)).ToList();

            // Populate session-based timing fields for ticket types
            PopulateTicketTypeTimingFields(ticketTypeDtos, eventEntity);

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
                AllowRsvps = eventEntity.AllowRsvps,
                RequireTicketPurchase = eventEntity.RequireTicketPurchase,
                VettedMembersOnly = eventEntity.VettedMembersOnly,
                Capacity = eventEntity.Capacity,
                IsPublished = eventEntity.IsPublished,
                RegistrationCount = eventEntity.GetCurrentAttendeeCount(),
                CurrentRSVPs = eventEntity.GetCurrentRSVPCount(),
                CurrentTickets = eventEntity.GetCurrentTicketCount(),
                Sessions = eventEntity.Sessions.Select(s => new SessionDto(s)).ToList(),
                TicketTypes = ticketTypeDtos,
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
                    .ThenInclude(tt => tt.Sessions)  // Load TicketType->Session links for differential updates
                .Include(e => e.VolunteerPositions)
                    .ThenInclude(vp => vp.Session) // Load Session for volunteer position start/end times
                .Include(e => e.Organizers)
                .Include(e => e.Venue) // Load venue for location name
                .Include(e => e.EventAttendances) // Include attendances for capacity validation
                .FirstOrDefaultAsync(e => e.Id == parsedId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogInformation("Event not found for update: {EventId}", eventId);
                return (false, null, "Event not found");
            }

            // Business rule: Cannot update events where ALL sessions have ended more than 48 hours ago
            // For multi-session events, use the LATEST session's end time
            var latestSessionEndTime = eventEntity.Sessions.Any()
                ? eventEntity.Sessions.Max(s => s.EndTime)
                : eventEntity.EndDate;  // Fallback for events without sessions

            if (latestSessionEndTime <= DateTime.UtcNow.AddHours(-48))
            {
                _logger.LogWarning("Attempted to update past event outside grace period: {EventId} (LatestSessionEndTime: {LatestSessionEndTime}, Grace Period: 48 hours)",
                    eventId, latestSessionEndTime);
                return (false, null, "Cannot update events that ended more than 48 hours ago");
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

            if (request.AllowRsvps.HasValue)
            {
                eventEntity.AllowRsvps = request.AllowRsvps.Value;
            }

            if (request.RequireTicketPurchase.HasValue)
            {
                eventEntity.RequireTicketPurchase = request.RequireTicketPurchase.Value;
            }

            if (request.VettedMembersOnly.HasValue)
            {
                eventEntity.VettedMembersOnly = request.VettedMembersOnly.Value;
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
            var updatedTicketTypeDtos = eventEntity.TicketTypes.Select(tt => new TicketTypeDto(tt, eventEntity.EventAttendances)).ToList();

            // Populate session-based timing fields for ticket types
            PopulateTicketTypeTimingFields(updatedTicketTypeDtos, eventEntity);

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
                AllowRsvps = eventEntity.AllowRsvps,
                RequireTicketPurchase = eventEntity.RequireTicketPurchase,
                VettedMembersOnly = eventEntity.VettedMembersOnly,
                Capacity = eventEntity.Capacity,
                IsPublished = eventEntity.IsPublished,
                RegistrationCount = eventEntity.GetCurrentAttendeeCount(),
                CurrentRSVPs = eventEntity.GetCurrentRSVPCount(),
                CurrentTickets = eventEntity.GetCurrentTicketCount(),
                Sessions = eventEntity.Sessions.Select(s => new SessionDto(s)).ToList(),
                TicketTypes = updatedTicketTypeDtos,
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
    /// Populate computed session-based timing fields for ticket type DTOs
    /// </summary>
    private void PopulateTicketTypeTimingFields(
        List<TicketTypeDto> ticketTypeDtos,
        WitchCityRope.Api.Models.Event eventEntity)
    {
        foreach (var ticketTypeDto in ticketTypeDtos)
        {
            // Find the entity for this DTO
            var ticketTypeEntity = eventEntity.TicketTypes
                .FirstOrDefault(t => t.Id.ToString() == ticketTypeDto.Id);

            if (ticketTypeEntity != null)
            {
                var referenceSession = _timeZoneService.GetReferenceSessionForTicketType(
                    ticketTypeEntity, eventEntity.Sessions);

                ticketTypeDto.ReferenceSessionId = referenceSession?.Id.ToString();
                ticketTypeDto.ReferenceSessionName = referenceSession?.Name;

                if (referenceSession == null)
                {
                    ticketTypeDto.CanPurchase = false;
                    ticketTypeDto.CanCancel = false;
                    ticketTypeDto.AvailabilityMessage = "All sessions have passed";
                }
                else
                {
                    // Check if this is a multi-session ticket
                    var isMultiSession = ticketTypeEntity.Sessions != null && ticketTypeEntity.Sessions.Count > 1;

                    if (isMultiSession)
                    {
                        // For multi-session tickets, user can purchase if ANY session is still purchasable
                        // This fixes the bug where Session 1 is closed but Session 2 is still open
                        ticketTypeDto.CanPurchase = _timeZoneService.IsAnySessionPurchasable(
                            ticketTypeEntity.Sessions!, // ! is safe - null checked in isMultiSession condition
                            eventEntity.RegistrationOpenHours,
                            eventEntity.RegistrationCloseHours);

                        // Same logic for cancellation - allow if ANY session is still within cancel window
                        ticketTypeDto.CanCancel = _timeZoneService.IsAnySessionPurchasable(
                            ticketTypeEntity.Sessions!, // ! is safe - null checked in isMultiSession condition
                            null,
                            eventEntity.CancellationCloseHours);
                    }
                    else
                    {
                        // Single-session ticket - use existing logic
                        ticketTypeDto.CanPurchase = _timeZoneService.IsActionAllowedForSession(
                            referenceSession,
                            eventEntity.RegistrationOpenHours,
                            eventEntity.RegistrationCloseHours);

                        ticketTypeDto.CanCancel = _timeZoneService.IsActionAllowedForSession(
                            referenceSession,
                            null,
                            eventEntity.CancellationCloseHours);
                    }

                    ticketTypeDto.AvailabilityMessage = GetTicketAvailabilityMessage(
                        referenceSession,
                        ticketTypeDto.CanPurchase,
                        eventEntity);
                }
            }
        }
    }

    /// <summary>
    /// Get availability message for a ticket type based on reference session and current timing
    /// </summary>
    private string GetTicketAvailabilityMessage(
        WitchCityRope.Api.Models.Session? referenceSession,
        bool canPurchase,
        WitchCityRope.Api.Models.Event eventEntity)
    {
        if (referenceSession == null)
        {
            return "All sessions have passed";
        }

        if (canPurchase)
        {
            return "Available";
        }

        var now = DateTime.UtcNow;
        var hoursUntilSession = (referenceSession.StartTime - now).TotalHours;

        // Check if we're before the registration open window
        if (eventEntity.RegistrationOpenHours.HasValue &&
            hoursUntilSession > (double)eventEntity.RegistrationOpenHours.Value)
        {
            var opensAt = referenceSession.StartTime.AddHours(-(double)eventEntity.RegistrationOpenHours.Value);
            return $"Sales open {opensAt:MMM d}";
        }

        // Otherwise, we're past the registration close window
        return "Sales closed";
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
            // All sessions have passed - use LATEST session (most recent) for edit grace period calculation
            var latestSession = eventEntity.Sessions
                .OrderByDescending(s => s.StartTime)
                .First();
            eventEntity.StartDate = latestSession.StartTime;
        }
    }

    /// <summary>
    /// Recalculates the event's EndDate based on its sessions.
    /// EndDate = latest session end time across all sessions.
    /// </summary>
    private void RecalculateEventEndDate(WitchCityRope.Api.Models.Event eventEntity)
    {
        if (eventEntity.Sessions == null || !eventEntity.Sessions.Any())
        {
            // No sessions - keep existing EndDate
            return;
        }

        // Get latest session end time
        var latestSessionEndTime = eventEntity.Sessions
            .Max(s => s.EndTime);

        eventEntity.EndDate = latestSessionEndTime;
    }

    /// <summary>
    /// Recalculates both StartDate and EndDate based on sessions.
    /// Call this after any session changes.
    /// </summary>
    private void RecalculateEventDates(WitchCityRope.Api.Models.Event eventEntity)
    {
        RecalculateEventStartDate(eventEntity);
        RecalculateEventEndDate(eventEntity);
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
            // Validate session time range
            var sessionStartTime = sessionDto.StartTime.ToUniversalTime();
            var sessionEndTime = sessionDto.EndTime.ToUniversalTime();

            if (sessionEndTime <= sessionStartTime)
            {
                throw new InvalidOperationException(
                    $"Session '{sessionDto.Name}' end time must be after start time. " +
                    $"StartTime: {sessionStartTime}, EndTime: {sessionEndTime}");
            }

            // Only treat as existing if ID is valid AND exists in current sessions
            if (!string.IsNullOrEmpty(sessionDto.Id) &&
                Guid.TryParse(sessionDto.Id, out var sessionId) &&
                sessionId != Guid.Empty &&
                currentSessions.TryGetValue(sessionId, out var existingSession))
            {
                // Update existing session
                existingSession.SessionCode = sessionDto.SessionIdentifier;
                existingSession.Name = sessionDto.Name;
                existingSession.StartTime = sessionStartTime;
                existingSession.EndTime = sessionEndTime;
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
                    StartTime = sessionStartTime,
                    EndTime = sessionEndTime,
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

        // Recalculate event's StartDate and EndDate based on updated sessions
        RecalculateEventDates(eventEntity);

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

                // Update session linkage (many-to-many relationship)
                // Use differential update to avoid duplicate key violations
                var currentSessionCodes = existingTicketType.Sessions.Select(s => s.SessionCode).ToHashSet();
                var newSessionCodes = (ticketTypeDto.SessionIdentifiers ?? Enumerable.Empty<string>()).ToHashSet();

                // Remove sessions that are no longer linked
                var sessionsToRemove = existingTicketType.Sessions
                    .Where(s => !newSessionCodes.Contains(s.SessionCode))
                    .ToList();
                foreach (var session in sessionsToRemove)
                {
                    existingTicketType.Sessions.Remove(session);
                }

                // Add new sessions that aren't already linked
                var sessionsToAdd = eventEntity.Sessions
                    .Where(s => newSessionCodes.Contains(s.SessionCode) && !currentSessionCodes.Contains(s.SessionCode))
                    .ToList();
                foreach (var session in sessionsToAdd)
                {
                    existingTicketType.Sessions.Add(session);
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

                // Link sessions (many-to-many relationship)
                newTicketType.Sessions = new List<WitchCityRope.Api.Models.Session>();
                if (ticketTypeDto.SessionIdentifiers != null && ticketTypeDto.SessionIdentifiers.Any())
                {
                    var linkedSessions = eventEntity.Sessions
                        .Where(s => ticketTypeDto.SessionIdentifiers.Contains(s.SessionCode))
                        .ToList();
                    foreach (var session in linkedSessions)
                    {
                        newTicketType.Sessions.Add(session);
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
                    .ThenInclude(vp => vp.Session) // Load Session for volunteer position start/end times
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
                    AllowRsvps = sourceEvent.AllowRsvps,
                    RequireTicketPurchase = sourceEvent.RequireTicketPurchase,
                    VettedMembersOnly = sourceEvent.VettedMembersOnly,
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

                // Recalculate StartDate and EndDate based on copied session times
                RecalculateEventDates(copiedEvent);

                // 7. Deep copy TicketTypes with Sessions remapping (many-to-many)
                foreach (var sourceTicket in sourceEvent.TicketTypes)
                {
                    var copiedTicket = new TicketType
                    {
                        Id = Guid.NewGuid(),
                        EventId = copiedEvent.Id,

                        // REMAP Sessions to new sessions (many-to-many relationship)
                        Sessions = sourceTicket.Sessions
                            .Select(s => copiedEvent.Sessions.First(cs => cs.SessionCode == s.SessionCode))
                            .ToList(),

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
                        .ThenInclude(vp => vp.Session) // Load Session for volunteer position start/end times
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

                var copiedTicketTypeDtos = copiedEventWithNav.TicketTypes.Select(tt => new TicketTypeDto(tt, copiedEventWithNav.EventAttendances)).ToList();

                // Populate session-based timing fields for ticket types
                PopulateTicketTypeTimingFields(copiedTicketTypeDtos, copiedEventWithNav);

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
                    AllowRsvps = copiedEventWithNav.AllowRsvps,
                    RequireTicketPurchase = copiedEventWithNav.RequireTicketPurchase,
                    VettedMembersOnly = copiedEventWithNav.VettedMembersOnly,
                    Capacity = copiedEventWithNav.Capacity,
                    IsPublished = copiedEventWithNav.IsPublished,
                    RegistrationCount = copiedEventWithNav.GetCurrentAttendeeCount(),
                    CurrentRSVPs = copiedEventWithNav.GetCurrentRSVPCount(),
                    CurrentTickets = copiedEventWithNav.GetCurrentTicketCount(),
                    Sessions = copiedEventWithNav.Sessions.Select(s => new SessionDto(s)).ToList(),
                    TicketTypes = copiedTicketTypeDtos,
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

            _logger.LogInformation("Creating new event: {Title}, AllowRsvps: {AllowRsvps}, RequireTickets: {RequireTickets}, StartDate: {StartDate}",
                request.Title, request.AllowRsvps, request.RequireTicketPurchase, request.StartDate);

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
                    AllowRsvps = request.AllowRsvps,
                    RequireTicketPurchase = request.RequireTicketPurchase,
                    VettedMembersOnly = request.VettedMembersOnly,
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
                        // Validate session time range
                        var sessionStartTime = sessionDto.StartTime.ToUniversalTime();
                        var sessionEndTime = sessionDto.EndTime.ToUniversalTime();

                        if (sessionEndTime <= sessionStartTime)
                        {
                            _logger.LogWarning("Invalid session time range for session {SessionName}: " +
                                "StartTime: {StartTime}, EndTime: {EndTime}",
                                sessionDto.Name, sessionStartTime, sessionEndTime);
                            await transaction.RollbackAsync(cancellationToken);
                            return (false, null, $"Session '{sessionDto.Name}' end time must be after start time");
                        }

                        var newSession = new WitchCityRope.Api.Models.Session
                        {
                            // DO NOT set Id - let EF generate it
                            EventId = newEvent.Id,
                            SessionCode = sessionDto.SessionIdentifier,
                            Name = sessionDto.Name,
                            StartTime = sessionStartTime,
                            EndTime = sessionEndTime,
                            Capacity = sessionDto.Capacity,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        newEvent.Sessions.Add(newSession);
                    }

                    _logger.LogInformation("Added {SessionCount} sessions to new event", request.Sessions.Count);

                    // Recalculate StartDate and EndDate based on actual session times
                    RecalculateEventDates(newEvent);
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
                        // Link sessions to ticket type (many-to-many)
                        foreach (var sessionCode in ticketTypeDto.SessionIdentifiers)
                        {
                            var linkedSession = newEvent.Sessions.FirstOrDefault(s => s.SessionCode == sessionCode);
                            if (linkedSession != null)
                            {
                                newTicketType.Sessions.Add(linkedSession);
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
                        .ThenInclude(tt => tt.Sessions)
                    .Include(e => e.VolunteerPositions)
                        .ThenInclude(vp => vp.Session) // Load Session for volunteer position start/end times
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
                var createdTicketTypeDtos = createdEventWithNav.TicketTypes.Select(tt => new TicketTypeDto(tt, createdEventWithNav.EventAttendances)).ToList();

                // Populate session-based timing fields for ticket types
                PopulateTicketTypeTimingFields(createdTicketTypeDtos, createdEventWithNav);

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
                    AllowRsvps = createdEventWithNav.AllowRsvps,
                    RequireTicketPurchase = createdEventWithNav.RequireTicketPurchase,
                    VettedMembersOnly = createdEventWithNav.VettedMembersOnly,
                    Capacity = createdEventWithNav.Capacity,
                    IsPublished = createdEventWithNav.IsPublished,
                    RegistrationCount = createdEventWithNav.GetCurrentAttendeeCount(),
                    CurrentRSVPs = createdEventWithNav.GetCurrentRSVPCount(),
                    CurrentTickets = createdEventWithNav.GetCurrentTicketCount(),
                    Sessions = createdEventWithNav.Sessions.Select(s => new SessionDto(s)).ToList(),
                    TicketTypes = createdTicketTypeDtos,
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

    /// <summary>
    /// Check if a session can be deleted and return information about what would be affected
    /// </summary>
    public async Task<(bool Success, DeleteSessionCheckDto? Response, string Error)> CheckSessionDeletionAsync(
        string eventId,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(eventId, out var parsedEventId))
            {
                _logger.LogWarning("Invalid event ID format: {EventId}", eventId);
                return (false, null, "Invalid event ID format");
            }

            if (!Guid.TryParse(sessionId, out var parsedSessionId))
            {
                _logger.LogWarning("Invalid session ID format: {SessionId}", sessionId);
                return (false, null, "Invalid session ID format");
            }

            var session = await _context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == parsedSessionId && s.EventId == parsedEventId, cancellationToken);

            if (session == null)
            {
                return (false, null, "Session not found");
            }

            // Check if this is the only session in the event
            var sessionCount = await _context.Sessions
                .Where(s => s.EventId == parsedEventId)
                .CountAsync(cancellationToken);

            if (sessionCount == 1)
            {
                return (true, new DeleteSessionCheckDto
                {
                    CanDelete = false,
                    BlockReason = "onlySession",
                    RsvpCount = 0,
                    TicketsSoldCount = 0,
                    VolunteerShifts = new List<string>(),
                    AffectedTicketTypes = new List<AffectedTicketTypeDto>()
                }, string.Empty);
            }

            // Count tickets sold for this session (EventAttendances with AttendanceType = Ticket, Status = Active)
            var ticketsSold = await _context.Set<WitchCityRope.Api.Features.Participation.Entities.EventAttendance>()
                .Where(ea => ea.EventId == parsedEventId
                          && ea.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Active
                          && ea.AttendanceType == WitchCityRope.Api.Features.Participation.Entities.AttendanceType.Ticket
                          && ea.TicketPurchase != null
                          && ea.TicketPurchase.TicketType.Sessions.Any(s => s.Id == parsedSessionId))
                .CountAsync(cancellationToken);

            if (ticketsSold > 0)
            {
                return (true, new DeleteSessionCheckDto
                {
                    CanDelete = false,
                    BlockReason = "ticketsSold",
                    RsvpCount = 0,
                    TicketsSoldCount = ticketsSold,
                    VolunteerShifts = new List<string>(),
                    AffectedTicketTypes = new List<AffectedTicketTypeDto>()
                }, string.Empty);
            }

            // Check for multi-session ticket types that include this session with sales
            // If any ticket type includes this session and has sales, block deletion
            var multiSessionTicketTypes = await _context.TicketTypes
                .AsNoTracking()
                .Include(tt => tt.Sessions)
                .Include(tt => tt.Event)
                    .ThenInclude(e => e.EventAttendances)
                        .ThenInclude(ea => ea.TicketPurchase)
                .Where(tt => tt.EventId == parsedEventId && tt.Sessions.Count > 1 && tt.Sessions.Any(s => s.Id == parsedSessionId))
                .ToListAsync(cancellationToken);

            var affectedTicketTypeDtos = new List<AffectedTicketTypeDto>();
            foreach (var ticketType in multiSessionTicketTypes)
            {
                var sold = ticketType.Event?.EventAttendances
                    .Count(ea => ea.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Active
                              && ea.AttendanceType == WitchCityRope.Api.Features.Participation.Entities.AttendanceType.Ticket
                              && ea.TicketPurchase != null
                              && ea.TicketPurchase.TicketTypeId == ticketType.Id) ?? 0;

                if (sold > 0)
                {
                    affectedTicketTypeDtos.Add(new AffectedTicketTypeDto
                    {
                        Id = ticketType.Id,
                        Name = ticketType.Name,
                        TicketsSold = sold,
                        WillBeDeleted = false // Multi-session ticket types won't be deleted
                    });

                    return (true, new DeleteSessionCheckDto
                    {
                        CanDelete = false,
                        BlockReason = "cascadeBlocking",
                        RsvpCount = 0,
                        TicketsSoldCount = 0,
                        VolunteerShifts = new List<string>(),
                        AffectedTicketTypes = affectedTicketTypeDtos
                    }, string.Empty);
                }
            }

            // Find single-session ticket types that reference this session
            var singleSessionTicketTypes = await _context.TicketTypes
                .AsNoTracking()
                .Include(tt => tt.Sessions)
                .Include(tt => tt.Event)
                    .ThenInclude(e => e.EventAttendances)
                        .ThenInclude(ea => ea.TicketPurchase)
                .Where(tt => tt.Sessions.Count == 1 && tt.Sessions.Any(s => s.Id == parsedSessionId))
                .ToListAsync(cancellationToken);

            foreach (var ticketType in singleSessionTicketTypes)
            {
                var sold = ticketType.Event?.EventAttendances
                    .Count(ea => ea.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Active
                              && ea.AttendanceType == WitchCityRope.Api.Features.Participation.Entities.AttendanceType.Ticket
                              && ea.TicketPurchase != null
                              && ea.TicketPurchase.TicketTypeId == ticketType.Id) ?? 0;

                affectedTicketTypeDtos.Add(new AffectedTicketTypeDto
                {
                    Id = ticketType.Id,
                    Name = ticketType.Name,
                    TicketsSold = sold,
                    WillBeDeleted = true // Single-session ticket types will be deleted
                });

                // If any ticket type has sales, block deletion
                if (sold > 0)
                {
                    return (true, new DeleteSessionCheckDto
                    {
                        CanDelete = false,
                        BlockReason = "cascadeBlocking",
                        RsvpCount = 0,
                        TicketsSoldCount = 0,
                        VolunteerShifts = new List<string>(),
                        AffectedTicketTypes = affectedTicketTypeDtos
                    }, string.Empty);
                }
            }

            // Count RSVPs (EventAttendances with AttendanceType = RSVP, Status = Active or Waitlisted)
            var rsvpCount = await _context.Set<WitchCityRope.Api.Features.Participation.Entities.EventAttendance>()
                .Where(ea => ea.EventId == parsedEventId
                          && (ea.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Active
                           || ea.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Waitlisted)
                          && ea.AttendanceType == WitchCityRope.Api.Features.Participation.Entities.AttendanceType.RSVP)
                .CountAsync(cancellationToken);

            // Get volunteer shifts for this session (if VolunteerPosition entity has SessionId)
            // For now, return empty list - volunteer signups tracked separately
            var volunteerShifts = new List<string>();

            return (true, new DeleteSessionCheckDto
            {
                CanDelete = true,
                BlockReason = null,
                RsvpCount = rsvpCount,
                TicketsSoldCount = 0,
                VolunteerShifts = volunteerShifts,
                AffectedTicketTypes = affectedTicketTypeDtos
            }, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check session deletion for session {SessionId}", sessionId);
            return (false, null, "Failed to check session deletion");
        }
    }

    /// <summary>
    /// Delete a session and cascade to related entities
    /// </summary>
    public async Task<(bool Success, DeleteSessionResultDto? Response, string Error)> DeleteSessionAsync(
        string eventId,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!Guid.TryParse(eventId, out var parsedEventId))
            {
                _logger.LogWarning("Invalid event ID format: {EventId}", eventId);
                return (false, null, "Invalid event ID format");
            }

            if (!Guid.TryParse(sessionId, out var parsedSessionId))
            {
                _logger.LogWarning("Invalid session ID format: {SessionId}", sessionId);
                return (false, null, "Invalid session ID format");
            }

            // Re-validate deletion eligibility (prevent race condition)
            var checkResult = await CheckSessionDeletionAsync(eventId, sessionId, cancellationToken);
            if (!checkResult.Success || checkResult.Response == null)
            {
                return (false, null, checkResult.Error);
            }

            if (!checkResult.Response.CanDelete)
            {
                var reason = checkResult.Response.BlockReason switch
                {
                    "ticketsSold" => "Cannot delete session with sold tickets",
                    "onlySession" => "Cannot delete the only session in an event",
                    "cascadeBlocking" => "Cannot delete session - associated ticket types have sales",
                    _ => "Cannot delete session"
                };
                return (false, null, reason);
            }

            // Cancel all RSVPs for this session
            var rsvps = await _context.Set<WitchCityRope.Api.Features.Participation.Entities.EventAttendance>()
                .Where(ea => ea.EventId == parsedEventId
                          && (ea.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Active
                           || ea.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Waitlisted)
                          && ea.AttendanceType == WitchCityRope.Api.Features.Participation.Entities.AttendanceType.RSVP)
                .ToListAsync(cancellationToken);

            foreach (var rsvp in rsvps)
            {
                rsvp.Status = WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Cancelled;
                rsvp.CancelledAt = DateTime.UtcNow;
                rsvp.CancellationReason = "Session was canceled by administrator";
                rsvp.UpdatedAt = DateTime.UtcNow;
                _context.Update(rsvp);
            }

            var rsvpsCancelled = rsvps.Count;

            // TODO: Cancel volunteer signups for this session when volunteer system is implemented
            var volunteerSignupsCancelled = 0;

            // Delete single-session ticket types that reference only this session
            var ticketTypesToDelete = await _context.TicketTypes
                .Include(tt => tt.Sessions)
                .Where(tt => tt.Sessions.Count == 1 && tt.Sessions.Any(s => s.Id == parsedSessionId))
                .ToListAsync(cancellationToken);

            var deletedTicketTypeNames = ticketTypesToDelete.Select(tt => tt.Name).ToList();
            _context.TicketTypes.RemoveRange(ticketTypesToDelete);

            // Delete the session
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == parsedSessionId, cancellationToken);

            if (session == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, null, "Session not found");
            }

            _context.Sessions.Remove(session);

            // Save all changes
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Deleted session {SessionId}: cancelled {RsvpCount} RSVPs, {VolunteerCount} volunteer signups, deleted {TicketTypeCount} ticket types",
                sessionId, rsvpsCancelled, volunteerSignupsCancelled, deletedTicketTypeNames.Count);

            return (true, new DeleteSessionResultDto
            {
                Success = true,
                RsvpsCancelled = rsvpsCancelled,
                VolunteerSignupsCancelled = volunteerSignupsCancelled,
                DeletedTicketTypes = deletedTicketTypeNames
            }, string.Empty);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to delete session {SessionId}", sessionId);
            return (false, null, "Failed to delete session");
        }
    }

    /// <summary>
    /// Check if a ticket type can be deleted
    /// </summary>
    public async Task<(bool Success, DeleteTicketTypeCheckDto? Response, string Error)> CheckTicketTypeDeletionAsync(
        string eventId,
        string ticketTypeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(eventId, out var parsedEventId))
            {
                _logger.LogWarning("Invalid event ID format: {EventId}", eventId);
                return (false, null, "Invalid event ID format");
            }

            if (!Guid.TryParse(ticketTypeId, out var parsedTicketTypeId))
            {
                _logger.LogWarning("Invalid ticket type ID format: {TicketTypeId}", ticketTypeId);
                return (false, null, "Invalid ticket type ID format");
            }

            var ticketType = await _context.TicketTypes
                .AsNoTracking()
                .Include(tt => tt.Event)
                    .ThenInclude(e => e.EventAttendances)
                        .ThenInclude(ea => ea.TicketPurchase)
                .FirstOrDefaultAsync(tt => tt.Id == parsedTicketTypeId && tt.EventId == parsedEventId, cancellationToken);

            if (ticketType == null)
            {
                return (false, null, "Ticket type not found");
            }

            // Count tickets sold for this ticket type (via EventAttendance)
            var ticketsSold = ticketType.Event?.EventAttendances
                .Count(ea => ea.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Active
                          && ea.AttendanceType == WitchCityRope.Api.Features.Participation.Entities.AttendanceType.Ticket
                          && ea.TicketPurchase != null
                          && ea.TicketPurchase.TicketTypeId == ticketType.Id) ?? 0;

            if (ticketsSold > 0)
            {
                return (true, new DeleteTicketTypeCheckDto
                {
                    CanDelete = false,
                    BlockReason = "ticketsSold",
                    TicketsSoldCount = ticketsSold
                }, string.Empty);
            }

            return (true, new DeleteTicketTypeCheckDto
            {
                CanDelete = true,
                BlockReason = null,
                TicketsSoldCount = 0
            }, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check ticket type deletion for ticket type {TicketTypeId}", ticketTypeId);
            return (false, null, "Failed to check ticket type deletion");
        }
    }

    /// <summary>
    /// Delete a ticket type
    /// </summary>
    public async Task<(bool Success, DeleteTicketTypeResultDto? Response, string Error)> DeleteTicketTypeAsync(
        string eventId,
        string ticketTypeId,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!Guid.TryParse(eventId, out var parsedEventId))
            {
                _logger.LogWarning("Invalid event ID format: {EventId}", eventId);
                return (false, null, "Invalid event ID format");
            }

            if (!Guid.TryParse(ticketTypeId, out var parsedTicketTypeId))
            {
                _logger.LogWarning("Invalid ticket type ID format: {TicketTypeId}", ticketTypeId);
                return (false, null, "Invalid ticket type ID format");
            }

            // Re-validate deletion eligibility (prevent race condition)
            var checkResult = await CheckTicketTypeDeletionAsync(eventId, ticketTypeId, cancellationToken);
            if (!checkResult.Success || checkResult.Response == null)
            {
                return (false, null, checkResult.Error);
            }

            if (!checkResult.Response.CanDelete)
            {
                return (false, null, "Cannot delete ticket type with sold tickets");
            }

            // Delete the ticket type
            var ticketType = await _context.TicketTypes
                .FirstOrDefaultAsync(tt => tt.Id == parsedTicketTypeId && tt.EventId == parsedEventId, cancellationToken);

            if (ticketType == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, null, "Ticket type not found");
            }

            _context.TicketTypes.Remove(ticketType);

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Deleted ticket type {TicketTypeId} from event {EventId}", ticketTypeId, eventId);

            return (true, new DeleteTicketTypeResultDto
            {
                Success = true
            }, string.Empty);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to delete ticket type {TicketTypeId}", ticketTypeId);
            return (false, null, "Failed to delete ticket type");
        }
    }
}// Trigger recompile $(date)
