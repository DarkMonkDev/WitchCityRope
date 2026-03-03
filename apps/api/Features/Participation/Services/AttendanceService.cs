using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Features.Events.Interfaces;
using WitchCityRope.Api.Features.Events;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Models;
using PaymentModels = WitchCityRope.Api.Features.Payments.Models;
using IRefundService = WitchCityRope.Api.Features.Payments.Services.IRefundService;
using ProcessRefundRequest = WitchCityRope.Api.Features.Payments.Services.ProcessRefundRequest;

namespace WitchCityRope.Api.Features.Participation.Services;

// ============================================================================
// BUSINESS LOGIC: Tickets and RSVPs - CRITICAL UNDERSTANDING
// ============================================================================
//
// IMPORTANT: Tickets and RSVPs are SEPARATE EventAttendances records
//
// 1. TICKET PURCHASE creates TWO records (for social events):
//    - EventAttendances (AttendanceType=Ticket, Status=Active)
//    - EventAttendances (AttendanceType=RSVP, Status=Active)
//    See: CreateTicketPurchaseAsync lines 515-556 (auto-RSVP creation)
//
// 2. TICKET CANCELLATION cancels BOTH records:
//    - Ticket record → Status=Cancelled
//    - Associated RSVP → Status=Cancelled
//    See: CancelParticipationAsync lines 653-730 (associated RSVP cancellation)
//
// 3. MANUAL RSVP creates standalone record:
//    - User CAN RSVP after cancelling ticket
//    - Creates NEW EventAttendances (AttendanceType=RSVP, Status=Active)
//    - Cancelled RSVPs do NOT prevent new RSVPs
//    See: CreateRSVPAsync lines 217-229 (only checks ACTIVE RSVPs)
//
// 4. QUERIES must filter by AttendanceType:
//    - Check for existing RSVP: Filter by AttendanceType=RSVP AND Status=Active
//    - Check for existing Ticket: Filter by AttendanceType=Ticket AND Status=Active
//    - User can have BOTH active Ticket and active RSVP simultaneously
//    See: GetParticipationStatusAsync lines 66-82 (separate queries for each type)
//
// WHY THIS MATTERS:
// - Users can hold both a ticket AND an RSVP for the same event
// - Cancelling a ticket removes the RSVP to prevent orphaned RSVPs
// - Users can re-RSVP after cancelling (new record, not reactivation)
// - Always filter by AttendanceType when checking for existing attendance
// ============================================================================

/// <summary>
/// Service for managing event attendance (RSVPs and tickets)
/// Follows vertical slice architecture with direct EF access
/// Enforces registration/cancellation cutoff times based on event start time and configured buffer
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _context;
    private readonly IVolunteerAssignmentService _volunteerAssignmentService;
    private readonly ITimeZoneService _timeZoneService;
    private readonly IRefundService _refundService;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        ApplicationDbContext context,
        IVolunteerAssignmentService volunteerAssignmentService,
        ITimeZoneService timeZoneService,
        IRefundService refundService,
        ILogger<AttendanceService> logger)
    {
        _context = context;
        _volunteerAssignmentService = volunteerAssignmentService;
        _timeZoneService = timeZoneService;
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// Get user's attendance status for a specific event
    /// Returns enhanced DTO with hasRSVP/hasTicket flags and nested details
    /// Matches frontend ParticipationCard component expectations
    /// </summary>
    public async Task<Result<EnhancedParticipationStatusDto?>> GetParticipationStatusAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting enhanced attendance status for user {UserId} in event {EventId}", userId, eventId);

            // Get event details with sessions for capacity calculation
            var eventEntity = await _context.Events
                .AsNoTracking()
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.Sessions)
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Event {EventId} not found when fetching attendance status", eventId);
                return Result<EnhancedParticipationStatusDto?>.Failure("Event not found");
            }

            // Get all ACTIVE attendances for this event (for capacity calculation)
            var activeAttendancesCount = await _context.EventAttendances
                .Where(ea => ea.EventId == eventId && ea.Status == AttendanceStatus.Active)
                .CountAsync(cancellationToken);

            // Get user's owned session IDs and ticket purchase mappings (sessions they have tickets for)
            // Include TicketPurchase -> TicketType for ticket name and price
            var userTicketAttendanceData = await _context.EventAttendances
                .AsNoTracking()
                .Where(ea => ea.EventId == eventId &&
                            ea.UserId == userId &&
                            ea.Status == AttendanceStatus.Active &&
                            ea.AttendanceType == AttendanceType.Ticket &&
                            ea.SessionId.HasValue)
                .Select(ea => new {
                    ea.SessionId,
                    ea.TicketPurchaseId,
                    TicketTypeName = ea.TicketPurchase != null && ea.TicketPurchase.TicketType != null
                        ? ea.TicketPurchase.TicketType.Name
                        : null,
                    TotalPrice = ea.TicketPurchase != null ? ea.TicketPurchase.TotalPrice : 0m
                })
                .ToListAsync(cancellationToken);

            // Extract flat list of owned session IDs (for backward compatibility)
            var userTicketAttendances = userTicketAttendanceData
                .Where(x => x.SessionId.HasValue)
                .Select(x => x.SessionId!.Value)
                .ToList();

            // Build TicketPurchaseSessionMap: maps TicketPurchaseId -> List of SessionIds
            var ticketPurchaseSessionMap = userTicketAttendanceData
                .Where(x => x.TicketPurchaseId.HasValue && x.SessionId.HasValue)
                .GroupBy(x => x.TicketPurchaseId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.SessionId!.Value).ToList()
                );

            // Build TicketPurchases with per-purchase cancellation eligibility
            // Need to load TicketPurchase entities to get TicketType for reference session calculation
            var ticketPurchaseIds = userTicketAttendanceData
                .Where(x => x.TicketPurchaseId.HasValue)
                .Select(x => x.TicketPurchaseId!.Value)
                .Distinct()
                .ToList();

            var ticketPurchaseEntities = ticketPurchaseIds.Count > 0
                ? await _context.TicketPurchases
                    .AsNoTracking()
                    .Include(tp => tp.TicketType)
                        .ThenInclude(tt => tt.Sessions)
                    .Where(tp => ticketPurchaseIds.Contains(tp.Id))
                    .ToListAsync(cancellationToken)
                : new List<TicketPurchase>();

            var ticketPurchases = new Dictionary<Guid, TicketPurchaseInfoDto>();
            var hasAnyCancelableTicket = false;

            foreach (var ticketPurchaseEntity in ticketPurchaseEntities)
            {
                var sessionIds = userTicketAttendanceData
                    .Where(x => x.TicketPurchaseId == ticketPurchaseEntity.Id && x.SessionId.HasValue)
                    .Select(x => x.SessionId!.Value)
                    .ToList();

                // Calculate cancellation eligibility for this specific ticket purchase
                var canCancelThisPurchase = false;
                string? cancellationMessage = null;

                if (ticketPurchaseEntity.TicketType != null)
                {
                    var referenceSession = _timeZoneService.GetReferenceSessionForTicketType(
                        ticketPurchaseEntity.TicketType,
                        eventEntity.Sessions);

                    if (referenceSession == null)
                    {
                        // All sessions have passed
                        canCancelThisPurchase = false;
                        cancellationMessage = "All sessions for this ticket have passed";
                    }
                    else
                    {
                        canCancelThisPurchase = _timeZoneService.IsActionAllowedForSession(
                            referenceSession,
                            null, // No open restriction for cancellation
                            eventEntity.CancellationCloseHours);

                        if (!canCancelThisPurchase)
                        {
                            cancellationMessage = "Cancellation window has closed for this ticket";
                        }
                    }
                }
                else
                {
                    // TicketType not found - allow cancellation (defensive)
                    canCancelThisPurchase = true;
                }

                if (canCancelThisPurchase)
                {
                    hasAnyCancelableTicket = true;
                }

                var ticketTypeName = userTicketAttendanceData
                    .Where(x => x.TicketPurchaseId == ticketPurchaseEntity.Id)
                    .Select(x => x.TicketTypeName)
                    .FirstOrDefault() ?? "Event Ticket";

                ticketPurchases[ticketPurchaseEntity.Id] = new TicketPurchaseInfoDto
                {
                    TicketTypeName = ticketTypeName,
                    SessionIds = sessionIds,
                    TotalPrice = ticketPurchaseEntity.TotalPrice,
                    CanCancel = canCancelThisPurchase,
                    CancellationMessage = cancellationMessage
                };
            }

            // Calculate per-session sold counts
            // This needs to handle two cases:
            // 1. Tickets with SessionId set → count directly
            // 2. Legacy tickets without SessionId → trace through TicketPurchase → TicketType → TicketTypeSessions

            // First, get counts from tickets WITH SessionId
            var directSessionCounts = await _context.EventAttendances
                .Where(ea => ea.EventId == eventId &&
                            ea.Status == AttendanceStatus.Active &&
                            ea.AttendanceType == AttendanceType.Ticket &&
                            ea.SessionId.HasValue)
                .GroupBy(ea => ea.SessionId!.Value)
                .Select(g => new { SessionId = g.Key, SoldCount = g.Count() })
                .ToDictionaryAsync(x => x.SessionId, x => x.SoldCount, cancellationToken);

            // Second, get counts from tickets WITHOUT SessionId by tracing through TicketType
            // For each ticket without SessionId, it counts toward ALL sessions its TicketType covers
            var legacyTicketCounts = await _context.EventAttendances
                .Where(ea => ea.EventId == eventId &&
                            ea.Status == AttendanceStatus.Active &&
                            ea.AttendanceType == AttendanceType.Ticket &&
                            !ea.SessionId.HasValue &&
                            ea.TicketPurchaseId.HasValue)
                .Join(_context.TicketPurchases,
                      ea => ea.TicketPurchaseId,
                      tp => tp.Id,
                      (ea, tp) => tp.TicketTypeId)
                .Join(_context.Set<TicketType>().SelectMany(tt => tt.Sessions, (tt, s) => new { tt.Id, SessionId = s.Id }),
                      ticketTypeId => ticketTypeId,
                      tts => tts.Id,
                      (ticketTypeId, tts) => tts.SessionId)
                .GroupBy(sessionId => sessionId)
                .Select(g => new { SessionId = g.Key, SoldCount = g.Count() })
                .ToDictionaryAsync(x => x.SessionId, x => x.SoldCount, cancellationToken);

            // Merge the counts
            var sessionSoldCounts = new Dictionary<Guid, int>(directSessionCounts);
            foreach (var kvp in legacyTicketCounts)
            {
                if (sessionSoldCounts.ContainsKey(kvp.Key))
                    sessionSoldCounts[kvp.Key] += kvp.Value;
                else
                    sessionSoldCounts[kvp.Key] = kvp.Value;
            }

            // BUSINESS RULE: Users can have BOTH RSVP and Ticket for social events
            // Query for both attendance types separately
            var rsvpAttendance = await _context.EventAttendances
                .AsNoTracking()
                .Where(ea => ea.EventId == eventId &&
                            ea.UserId == userId &&
                            ea.Status == AttendanceStatus.Active &&
                            ea.AttendanceType == AttendanceType.RSVP)
                .FirstOrDefaultAsync(cancellationToken);

            var ticketAttendance = await _context.EventAttendances
                .AsNoTracking()
                .Where(ea => ea.EventId == eventId &&
                            ea.UserId == userId &&
                            ea.Status == AttendanceStatus.Active &&
                            ea.AttendanceType == AttendanceType.Ticket)
                .FirstOrDefaultAsync(cancellationToken);

            // Calculate cancellation permissions based on event timing rules
            // Only check timing if user actually has the participation type
            var canCancelRSVP = false;
            if (rsvpAttendance != null)
            {
                canCancelRSVP = await _timeZoneService.IsActionAllowedAsync(
                    eventEntity,
                    EventActionType.CancelRsvp,
                    cancellationToken);
            }

            // canCancelTicket is now derived from per-purchase calculations above
            // True if ANY ticket purchase is cancelable (for showing/hiding the cancel button)
            var canCancelTicket = ticketAttendance != null && hasAnyCancelableTicket;

            // Check if ticket purchase is allowed based on timing rules
            // For multi-session events, use per-ticket-type session-based timing
            // User CAN purchase tickets for different sessions even if they already have a ticket for other sessions
            bool canPurchaseTicket = false;
            string? ticketPurchaseMessage = null;

            // Get sessions user already owns (to exclude from purchase options)
            var ownedSessionIds = userTicketAttendances.ToHashSet();

            // Check each ticket type to see if ANY are purchasable
            // A ticket type is purchasable if:
            // 1. It has availability (Available > 0)
            // 2. Its sessions are not all already owned by the user
            // 3. The timing window is open (based on reference session)
            foreach (var ticketType in eventEntity.TicketTypes)
            {
                if (ticketType.Available <= 0)
                    continue; // No availability

                // Check if user already owns ALL sessions in this ticket type
                var ticketTypeSessionIds = ticketType.Sessions.Select(s => s.Id).ToHashSet();
                if (ticketTypeSessionIds.Count > 0 && ticketTypeSessionIds.All(sid => ownedSessionIds.Contains(sid)))
                    continue; // User already owns all sessions in this ticket type

                // Check timing window using the reference session for this ticket type
                var referenceSession = _timeZoneService.GetReferenceSessionForTicketType(
                    ticketType,
                    eventEntity.Sessions);

                if (referenceSession != null)
                {
                    var isTimingAllowed = _timeZoneService.IsActionAllowedForSession(
                        referenceSession,
                        eventEntity.RegistrationOpenHours,
                        eventEntity.RegistrationCloseHours);

                    if (isTimingAllowed)
                    {
                        canPurchaseTicket = true;
                        ticketPurchaseMessage = null;
                        break; // At least one ticket type is purchasable
                    }
                }
            }

            // If no ticket types are purchasable, set appropriate message
            if (!canPurchaseTicket && eventEntity.TicketTypes.Any(tt => tt.Available > 0))
            {
                // There are ticket types with availability, but timing windows are closed
                ticketPurchaseMessage = GetTicketPurchaseTimingMessage(eventEntity);
            }

            // Build enhanced DTO with nested structure
            var dto = new EnhancedParticipationStatusDto
            {
                HasRSVP = rsvpAttendance != null,
                HasTicket = ticketAttendance != null,
                CanRSVP = rsvpAttendance == null && activeAttendancesCount < eventEntity.Capacity,
                CanPurchaseTicket = canPurchaseTicket,
                CanCancelRSVP = canCancelRSVP,
                CanCancelTicket = canCancelTicket,
                TicketPurchaseMessage = ticketPurchaseMessage,
                Capacity = new CapacityInfoDto
                {
                    Current = activeAttendancesCount,
                    Total = eventEntity.Capacity,
                    Available = Math.Max(0, eventEntity.Capacity - activeAttendancesCount)
                }
            };

            // Populate RSVP details if exists
            if (rsvpAttendance != null)
            {
                dto.Rsvp = new RsvpDetailsDto
                {
                    Id = rsvpAttendance.Id,
                    Status = rsvpAttendance.Status.ToString(),
                    CreatedAt = rsvpAttendance.CreatedAt,
                    CanceledAt = rsvpAttendance.CancelledAt,
                    CancelReason = rsvpAttendance.CancellationReason,
                    Notes = rsvpAttendance.Notes
                };
            }

            // Populate Ticket details if exists
            if (ticketAttendance != null)
            {
                // Calculate TOTAL amount from ALL ticket purchases (not just one)
                // This handles users who purchased multiple tickets for the same event
                var totalAmount = ticketPurchases.Values.Sum(tp => tp.TotalPrice);

                dto.Ticket = new TicketDetailsDto
                {
                    Id = ticketAttendance.Id,
                    Status = ticketAttendance.Status.ToString(),
                    Amount = totalAmount > 0 ? totalAmount : null, // Only show amount if there's a price
                    PaymentStatus = ticketAttendance.Status == AttendanceStatus.Active ? "Completed" :
                                   ticketAttendance.Status == AttendanceStatus.Refunded ? "Refunded" : "Unknown",
                    CreatedAt = ticketAttendance.CreatedAt,
                    CanceledAt = ticketAttendance.CancelledAt,
                    CancelReason = ticketAttendance.CancellationReason,
                    Notes = ticketAttendance.Notes
                };
            }

            // Populate session-based fields for multi-session events
            dto.OwnedSessionIds = userTicketAttendances;
            dto.TicketPurchaseSessionMap = ticketPurchaseSessionMap;
            dto.TicketPurchases = ticketPurchases;

            // Build session availability list for multi-session events
            if (eventEntity.Sessions.Count > 1)
            {
                foreach (var session in eventEntity.Sessions.OrderBy(s => s.StartTime))
                {
                    var soldCount = sessionSoldCounts.GetValueOrDefault(session.Id, 0);
                    dto.SessionAvailability.Add(new SessionAvailabilityDto
                    {
                        SessionId = session.Id,
                        SessionIdentifier = session.SessionCode ?? string.Empty,
                        SessionName = session.Name ?? string.Empty,
                        StartTime = session.StartTime,
                        EndTime = session.EndTime,
                        SoldCount = soldCount,
                        AvailableCount = Math.Max(0, session.Capacity - soldCount),
                        Capacity = session.Capacity
                    });
                }

                // Calculate CanPurchaseAdditionalSessions:
                // True if there are sessions user doesn't own AND there are purchasable ticket types for those sessions
                // Uses session-based timing (not event-level timing)
                var unownedSessionIds = eventEntity.Sessions
                    .Where(s => !userTicketAttendances.Contains(s.Id))
                    .Select(s => s.Id)
                    .ToHashSet();

                if (unownedSessionIds.Any())
                {
                    // Check each ticket type that covers unowned sessions
                    foreach (var ticketType in eventEntity.TicketTypes)
                    {
                        // Skip if no availability or doesn't cover any unowned sessions
                        if (ticketType.Available <= 0)
                            continue;
                        if (!ticketType.Sessions.Any(s => unownedSessionIds.Contains(s.Id)))
                            continue;

                        // Check timing window using session-based timing
                        var referenceSession = _timeZoneService.GetReferenceSessionForTicketType(
                            ticketType,
                            eventEntity.Sessions);

                        if (referenceSession != null)
                        {
                            var isTimingAllowed = _timeZoneService.IsActionAllowedForSession(
                                referenceSession,
                                eventEntity.RegistrationOpenHours,
                                eventEntity.RegistrationCloseHours);

                            if (isTimingAllowed)
                            {
                                dto.CanPurchaseAdditionalSessions = true;
                                break; // At least one ticket type is purchasable
                            }
                        }
                    }
                }
            }

            _logger.LogInformation(
                "Attendance status for user {UserId} in event {EventId}: HasRSVP={HasRSVP}, HasTicket={HasTicket}, CanRSVP={CanRSVP}, CanCancelRSVP={CanCancelRSVP}, CanCancelTicket={CanCancelTicket}, Capacity={Current}/{Total}",
                userId, eventId, dto.HasRSVP, dto.HasTicket, dto.CanRSVP, dto.CanCancelRSVP, dto.CanCancelTicket, dto.Capacity.Current, dto.Capacity.Total);

            return Result<EnhancedParticipationStatusDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance status for user {UserId} in event {EventId}", userId, eventId);
            return Result<EnhancedParticipationStatusDto?>.Failure("Failed to get attendance status", ex.Message);
        }
    }

    /// <summary>
    /// Create an RSVP for a social event (any authenticated user allowed)
    /// Business Rule: Social events are open to all authenticated users, regardless of vetting status
    /// </summary>
    public async Task<Result<ParticipationStatusDto>> CreateRSVPAsync(
        CreateRSVPRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating RSVP for user {UserId} in event {EventId}", userId, request.EventId);

            // Check if event exists and is a social event FIRST (need event for timing check)
            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result<ParticipationStatusDto>.Failure("Event not found");
            }

            if (!eventEntity.AllowRsvps)
            {
                return Result<ParticipationStatusDto>.Failure("RSVPs are not enabled for this event");
            }

            // VETTING ENFORCEMENT: Check if event requires vetted members
            if (eventEntity.VettedMembersOnly)
            {
                var userForVetting = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

                if (userForVetting != null && !userForVetting.IsVetted)
                {
                    return Result<ParticipationStatusDto>.Failure("This event is limited to vetted members only");
                }
            }

            // TIMING VALIDATION FIRST - Check before all other business rules
            // This ensures users get timing errors BEFORE other validation errors
            var isAllowed = await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.GetRsvp, cancellationToken);
            if (!isAllowed)
            {
                _logger.LogWarning("RSVP attempt for event {EventId} outside allowed timing window", request.EventId);
                return Result<ParticipationStatusDto>.Failure("RSVP registration window is not currently open for this event");
            }

            // CRITICAL: Validate Event Waiver acceptance
            if (!request.EventWaiverAccepted)
            {
                return Result<ParticipationStatusDto>.Failure("You must accept the Event Waiver to RSVP");
            }

            // Check if user exists (authentication verified by endpoint authorization)
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return Result<ParticipationStatusDto>.Failure("User not found");
            }

            // REMOVED: Vetting requirement - Social events are open to all authenticated users
            // Previous restrictive validation: if (!user.IsVetted) return Failure("Only vetted members...")
            // New business rule: Allow any authenticated user to RSVP for social events

            // Check if user already has an ACTIVE RSVP for this event
            //
            // BUSINESS RULE: We ONLY check for ACTIVE RSVPs, not Cancelled/Refunded
            // WHY: Users can re-RSVP after cancelling (creates NEW record)
            //
            // BUSINESS RULE: We filter by AttendanceType.RSVP specifically
            // WHY: Users can have BOTH an active Ticket AND active RSVP simultaneously
            //      (ticket purchases auto-create RSVP for social events)
            //
            // Note: Cancelled RSVPs do NOT block new RSVPs - this allows re-registration
            var existingRsvp = await _context.EventAttendances
                .FirstOrDefaultAsync(ea => ea.EventId == request.EventId
                    && ea.UserId == userId
                    && ea.Status == AttendanceStatus.Active
                    && ea.AttendanceType == AttendanceType.RSVP, cancellationToken);

            if (existingRsvp != null)
            {
                return Result<ParticipationStatusDto>.Failure("User already has an active RSVP for this event");
            }

            // Check event capacity
            var currentAttendanceCount = await _context.EventAttendances
                .CountAsync(ea => ea.EventId == request.EventId && ea.Status == AttendanceStatus.Active, cancellationToken);

            if (currentAttendanceCount >= eventEntity.Capacity)
            {
                return Result<ParticipationStatusDto>.Failure("Event is at full capacity");
            }

            // Create the RSVP with Event Waiver acceptance
            var attendance = new EventAttendance(request.EventId, userId, AttendanceType.RSVP)
            {
                Notes = request.Notes,
                EventWaiverAccepted = true,
                EventWaiverAcceptedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            _logger.LogInformation(
                "DIAGNOSTIC: Created EventAttendance object in memory - Id: {AttendanceId}, EventId: {EventId}, UserId: {UserId}, Type: {Type}, Status: {Status}",
                attendance.Id, attendance.EventId, attendance.UserId, attendance.AttendanceType, attendance.Status);

            _context.EventAttendances.Add(attendance);

            _logger.LogInformation(
                "DIAGNOSTIC: Added EventAttendance to DbContext - EntityState: {EntityState}, Id: {AttendanceId}",
                _context.Entry(attendance).State, attendance.Id);

            // Create or update EventAttendee record so user appears in check-in system
            var existingAttendee = await _context.EventAttendees
                .FirstOrDefaultAsync(ea => ea.EventId == request.EventId && ea.UserId == userId, cancellationToken);

            if (existingAttendee == null)
            {
                // Create new EventAttendee record
                var attendee = new CheckIn.Entities.EventAttendee
                {
                    Id = Guid.NewGuid(),
                    EventId = request.EventId,
                    UserId = userId,
                    RegistrationStatus = "confirmed",
                    HasCompletedWaiver = true, // Online RSVPs imply waiver acceptance through registration flow
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                _context.EventAttendees.Add(attendee);
            }
            // Case-insensitive status check ensures re-registration works regardless of stored casing
            else if (string.Equals(existingAttendee.RegistrationStatus, "cancelled", StringComparison.OrdinalIgnoreCase))
            {
                // Re-activate cancelled attendee when they RSVP again
                _logger.LogInformation(
                    "Re-activating EventAttendee {AttendeeId} from 'cancelled' to 'confirmed' for user {UserId} RSVPing to event {EventId}",
                    existingAttendee.Id, userId, request.EventId);

                existingAttendee.RegistrationStatus = "confirmed";
                existingAttendee.HasCompletedWaiver = true; // Re-confirm waiver on re-registration
                existingAttendee.UpdatedAt = DateTime.UtcNow;
                _context.EventAttendees.Update(existingAttendee);
            }

            // Create audit history
            var history = new AttendanceHistory(attendance.Id, "Created")
            {
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    EventId = attendance.EventId,
                    UserId = attendance.UserId,
                    AttendanceType = attendance.AttendanceType,
                    Notes = attendance.Notes
                }),
                ChangedBy = userId,
                ChangeReason = "RSVP created by user"
            };

            _context.AttendanceHistory.Add(history);

            _logger.LogInformation(
                "DIAGNOSTIC: About to call SaveChangesAsync - Entities tracked: {TrackedCount}, Id before save: {AttendanceId}",
                _context.ChangeTracker.Entries().Count(), attendance.Id);

            // CRITICAL: Save changes to persist RSVP to database
            var savedCount = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "DIAGNOSTIC: SaveChangesAsync completed - Rows affected: {SavedCount}, Id after save: {AttendanceId}, EntityState: {EntityState}",
                savedCount, attendance.Id, _context.Entry(attendance).State);

            // Verify persistence (defensive check)
            _logger.LogInformation(
                "DIAGNOSTIC: Querying database for verification with Id: {AttendanceId}",
                attendance.Id);

            var savedAttendance = await _context.EventAttendances
                .AsNoTracking()
                .FirstOrDefaultAsync(ea => ea.Id == attendance.Id, cancellationToken);

            if (savedAttendance == null)
            {
                // DIAGNOSTIC: Query all records for this user/event to see what actually exists
                var allRecords = await _context.EventAttendances
                    .AsNoTracking()
                    .Where(ea => ea.UserId == userId && ea.EventId == request.EventId)
                    .Select(ea => new { ea.Id, ea.Status, ea.AttendanceType, ea.CreatedAt })
                    .ToListAsync(cancellationToken);

                _logger.LogError(
                    "CRITICAL: RSVP {AttendanceId} for user {UserId} in event {EventId} failed to persist to database. " +
                    "Total records for this user/event: {RecordCount}. Records: {@Records}",
                    attendance.Id, userId, request.EventId, allRecords.Count, allRecords);

                return Result<ParticipationStatusDto>.Failure("Failed to save RSVP to database");
            }

            _logger.LogInformation(
                "DIAGNOSTIC: Verification successful - Found RSVP {AttendanceId} for user {UserId} in event {EventId} (Status: {Status}, Type: {Type}, CreatedAt: {CreatedAt})",
                savedAttendance.Id, userId, request.EventId, savedAttendance.Status, savedAttendance.AttendanceType, savedAttendance.CreatedAt);

            var dto = new ParticipationStatusDto
            {
                EventId = attendance.EventId,
                UserId = attendance.UserId,
                ParticipationType = attendance.AttendanceType,
                Status = attendance.Status,
                ParticipationDate = attendance.CreatedAt,
                Notes = attendance.Notes,
                CanCancel = attendance.CanBeCancelled(),
                Metadata = attendance.Metadata
            };

            return Result<ParticipationStatusDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RSVP for user {UserId} in event {EventId}", userId, request.EventId);
            return Result<ParticipationStatusDto>.Failure("Failed to create RSVP", ex.Message);
        }
    }

    /// <summary>
    /// Purchase one or more tickets for a class event (any authenticated user)
    /// Supports both single ticket (backward compatible) and batch ticket purchases
    /// </summary>
    public async Task<Result<ParticipationStatusDto>> CreateTicketPurchaseAsync(
        CreateTicketPurchaseRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating ticket purchase for user {UserId} in event {EventId} with {TicketCount} ticket type(s)",
                userId, request.EventId, request.TicketTypeIds.Count);

            // Check if event exists FIRST (need event for timing check)
            var eventEntity = await _context.Events
                .Include(e => e.Sessions)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result<ParticipationStatusDto>.Failure("Event not found");
            }

            // CRITICAL: Validate Event Waiver acceptance
            if (!request.EventWaiverAccepted)
            {
                return Result<ParticipationStatusDto>.Failure("You must accept the Event Waiver to purchase a ticket");
            }

            // Check if user exists
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return Result<ParticipationStatusDto>.Failure("User not found");
            }

            // Load all ticket types with their sessions
            var ticketTypesWithSessions = await _context.TicketTypes
                .Include(tt => tt.Sessions)
                .Where(tt => request.TicketTypeIds.Contains(tt.Id))
                .ToListAsync(cancellationToken);

            // Validate all ticket types exist
            if (ticketTypesWithSessions.Count != request.TicketTypeIds.Count)
            {
                var missingIds = request.TicketTypeIds.Except(ticketTypesWithSessions.Select(tt => tt.Id)).ToList();
                _logger.LogWarning("Ticket type(s) not found: {MissingIds}", string.Join(", ", missingIds));
                return Result<ParticipationStatusDto>.Failure("One or more ticket types not found");
            }

            // Collect all session IDs across all ticket types for overlap detection
            var allRequestedSessionIds = ticketTypesWithSessions
                .SelectMany(tt => tt.Sessions.Select(s => s.Id))
                .Distinct()
                .ToList();

            // Validate timing for each ticket type
            foreach (var ticketType in ticketTypesWithSessions)
            {
                var referenceSession = _timeZoneService.GetReferenceSessionForTicketType(
                    ticketType, eventEntity.Sessions);

                if (referenceSession == null)
                {
                    _logger.LogWarning("Ticket purchase attempt for event {EventId} ticket type {TicketTypeId} - all sessions have passed",
                        request.EventId, ticketType.Id);
                    return Result<ParticipationStatusDto>.Failure($"All sessions for ticket '{ticketType.Name}' have passed");
                }

                var isAllowed = _timeZoneService.IsActionAllowedForSession(
                    referenceSession,
                    eventEntity.RegistrationOpenHours,
                    eventEntity.RegistrationCloseHours);

                if (!isAllowed)
                {
                    _logger.LogWarning("Ticket purchase attempt for event {EventId} outside allowed timing window for ticket type {TicketTypeName}",
                        request.EventId, ticketType.Name);
                    return Result<ParticipationStatusDto>.Failure($"Ticket purchase window is not currently open for '{ticketType.Name}'");
                }
            }

            // Check if user already has a ticket for ANY of these sessions
            var overlappingAttendance = await _context.EventAttendances
                .AsNoTracking()
                .Where(ea =>
                    ea.UserId == userId &&
                    ea.Status == AttendanceStatus.Active &&
                    ea.AttendanceType == AttendanceType.Ticket &&
                    ea.SessionId.HasValue &&
                    allRequestedSessionIds.Contains(ea.SessionId.Value))
                .Include(ea => ea.Session)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp != null ? tp.TicketType : null)
                .FirstOrDefaultAsync(cancellationToken);

            if (overlappingAttendance != null)
            {
                var overlappingSessionName = overlappingAttendance.Session?.Name ?? "a session";
                var existingTicketName = overlappingAttendance.TicketPurchase?.TicketType?.Name ?? "an existing ticket";

                return Result<ParticipationStatusDto>.Failure(
                    $"You already have a ticket that includes the {overlappingSessionName} session ({existingTicketName})");
            }

            // Check event capacity (rough check - detailed per-session capacity not enforced here)
            var currentAttendanceCount = await _context.EventAttendances
                .CountAsync(ea => ea.EventId == request.EventId && ea.Status == AttendanceStatus.Active, cancellationToken);

            if (currentAttendanceCount >= eventEntity.Capacity)
            {
                return Result<ParticipationStatusDto>.Failure("Event is at full capacity");
            }

            // ============================================================================
            // PROCESS ALL TICKET TYPES IN A SINGLE TRANSACTION
            // ============================================================================
            var allAttendances = new List<EventAttendance>();
            var ticketPurchases = new List<TicketPurchase>();

            foreach (var ticketType in ticketTypesWithSessions)
            {
                // Create TicketPurchase record for this ticket type
                var ticketPurchase = new TicketPurchase
                {
                    Id = Guid.NewGuid(),
                    TicketTypeId = ticketType.Id,
                    UserId = userId,
                    Quantity = 1,
                    TotalPrice = ticketType.Price ?? 0m,
                    PaymentStatus = "Pending",
                    PaymentMethod = request.PaymentMethodId ?? "Unknown",
                    PaymentReference = $"WCR-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    Notes = request.Notes ?? $"Ticket purchase - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                    EventWaiverAccepted = request.EventWaiverAccepted,
                    EventWaiverAcceptedAt = DateTime.UtcNow,
                    PurchaseDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TicketPurchases.Add(ticketPurchase);
                ticketPurchases.Add(ticketPurchase);

                // Create EventAttendance records for each session in this ticket type
                var sessionIds = ticketType.Sessions.Select(s => s.Id).ToList();

                foreach (var session in ticketType.Sessions)
                {
                    var attendance = new EventAttendance(request.EventId, userId, AttendanceType.Ticket)
                    {
                        SessionId = session.Id,
                        TicketPurchaseId = ticketPurchase.Id,
                        Notes = request.Notes,
                        EventWaiverAccepted = true,
                        EventWaiverAcceptedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };

                    allAttendances.Add(attendance);
                    _context.EventAttendances.Add(attendance);
                }

                // Create audit history for this ticket type's purchase
                var primaryAttendanceForType = allAttendances.LastOrDefault();
                if (primaryAttendanceForType == null)
                {
                    _logger.LogError(
                        "No attendance records created for ticket type {TicketTypeId} '{TicketTypeName}' " +
                        "for user {UserId} in event {EventId}. Sessions loaded: {SessionCount}",
                        ticketType.Id, ticketType.Name, userId, request.EventId, ticketType.Sessions.Count);
                    return Result<ParticipationStatusDto>.Failure(
                        $"Failed to create attendance records for ticket '{ticketType.Name}'. " +
                        "The ticket may not be configured correctly. Please contact support.");
                }
                var history = new AttendanceHistory(primaryAttendanceForType.Id, "Created")
                {
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        EventId = primaryAttendanceForType.EventId,
                        UserId = primaryAttendanceForType.UserId,
                        AttendanceType = primaryAttendanceForType.AttendanceType,
                        TicketTypeName = ticketType.Name,
                        SessionIds = sessionIds,
                        SessionCount = sessionIds.Count,
                        Notes = primaryAttendanceForType.Notes,
                        PaymentMethodId = request.PaymentMethodId
                    }),
                    ChangedBy = userId,
                    ChangeReason = sessionIds.Count > 1
                        ? $"Multi-session ticket '{ticketType.Name}' purchased by user ({sessionIds.Count} sessions)"
                        : $"Ticket '{ticketType.Name}' purchased by user"
                };

                _context.AttendanceHistory.Add(history);

                _logger.LogInformation("Prepared TicketPurchase {TicketPurchaseId} for ticket type '{TicketTypeName}' ({SessionCount} sessions)",
                    ticketPurchase.Id, ticketType.Name, sessionIds.Count);
            }

            // Create or update EventAttendee record so user appears in check-in system
            var ticketNumber = $"TKT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var existingAttendee = await _context.EventAttendees
                .FirstOrDefaultAsync(ea => ea.EventId == request.EventId && ea.UserId == userId, cancellationToken);

            if (existingAttendee == null)
            {
                var attendee = new CheckIn.Entities.EventAttendee
                {
                    Id = Guid.NewGuid(),
                    EventId = request.EventId,
                    UserId = userId,
                    TicketNumber = ticketNumber,
                    RegistrationStatus = "confirmed",
                    HasCompletedWaiver = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                _context.EventAttendees.Add(attendee);
            }
            else
            {
                existingAttendee.TicketNumber = ticketNumber;

                // Case-insensitive status check ensures cancelled attendees are properly detected
                if (string.Equals(existingAttendee.RegistrationStatus, "cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "Re-activating EventAttendee {AttendeeId} from 'cancelled' to 'confirmed' for user {UserId} purchasing ticket for event {EventId}",
                        existingAttendee.Id, userId, request.EventId);
                    existingAttendee.RegistrationStatus = "confirmed";
                    existingAttendee.HasCompletedWaiver = true;
                }

                existingAttendee.UpdatedAt = DateTime.UtcNow;
                _context.EventAttendees.Update(existingAttendee);
            }

            // Auto-RSVP for events that allow RSVPs
            if (eventEntity.AllowRsvps)
            {
                var existingRsvp = await _context.EventAttendances
                    .FirstOrDefaultAsync(ea =>
                        ea.EventId == request.EventId &&
                        ea.UserId == userId &&
                        ea.Status == AttendanceStatus.Active &&
                        ea.AttendanceType == AttendanceType.RSVP,
                        cancellationToken);

                if (existingRsvp == null)
                {
                    _logger.LogInformation("Auto-creating RSVP for user {UserId} in social event {EventId} (ticket purchase)", userId, request.EventId);

                    var autoRsvp = new EventAttendance(request.EventId, userId, AttendanceType.RSVP)
                    {
                        Notes = "Auto-created RSVP from ticket purchase",
                        CreatedBy = userId
                    };

                    _context.EventAttendances.Add(autoRsvp);

                    var rsvpHistory = new AttendanceHistory(autoRsvp.Id, "Created")
                    {
                        NewValues = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            EventId = autoRsvp.EventId,
                            UserId = autoRsvp.UserId,
                            AttendanceType = autoRsvp.AttendanceType,
                            Notes = autoRsvp.Notes,
                            AutoCreated = true
                        }),
                        ChangedBy = userId,
                        ChangeReason = "Auto-created RSVP from ticket purchase"
                    };

                    _context.AttendanceHistory.Add(rsvpHistory);
                }
            }

            // CRITICAL: Save all changes in a single transaction
            await _context.SaveChangesAsync(cancellationToken);

            // Verify persistence
            var primaryAttendance = allAttendances.FirstOrDefault();
            if (primaryAttendance == null)
            {
                _logger.LogError(
                    "No attendance records were created for user {UserId} in event {EventId} " +
                    "despite processing {TicketTypeCount} ticket types",
                    userId, request.EventId, request.TicketTypeIds.Count);
                return Result<ParticipationStatusDto>.Failure(
                    "Failed to create ticket purchase records. Please try again or contact support.");
            }
            var savedAttendance = await _context.EventAttendances
                .AsNoTracking()
                .FirstOrDefaultAsync(ea => ea.Id == primaryAttendance.Id, cancellationToken);

            if (savedAttendance == null)
            {
                _logger.LogError("CRITICAL: Ticket purchase {AttendanceId} for user {UserId} in event {EventId} failed to persist to database",
                    primaryAttendance.Id, userId, request.EventId);
                return Result<ParticipationStatusDto>.Failure("Failed to save ticket purchase to database");
            }

            _logger.LogInformation(
                "Successfully created and verified {TicketTypeCount} ticket purchase(s) for user {UserId} in event {EventId} ({AttendanceCount} attendance records total)",
                request.TicketTypeIds.Count, userId, request.EventId, allAttendances.Count);

            var dto = new ParticipationStatusDto
            {
                EventId = primaryAttendance.EventId,
                UserId = primaryAttendance.UserId,
                ParticipationType = primaryAttendance.AttendanceType,
                Status = primaryAttendance.Status,
                ParticipationDate = primaryAttendance.CreatedAt,
                Notes = primaryAttendance.Notes,
                CanCancel = primaryAttendance.CanBeCancelled(),
                Metadata = primaryAttendance.Metadata
            };

            return Result<ParticipationStatusDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket purchase for user {UserId} in event {EventId}", userId, request.EventId);
            return Result<ParticipationStatusDto>.Failure("Failed to create ticket purchase", ex.Message);
        }
    }

    /// <summary>
    /// Cancel user's attendance in an event
    /// </summary>
    public async Task<Result> CancelParticipationAsync(
        Guid eventId,
        Guid userId,
        AttendanceType? attendanceType = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling attendance for user {UserId} in event {EventId}, Type: {Type}",
                userId, eventId, attendanceType?.ToString() ?? "Most Recent");

            // Find the ACTIVE attendance for cancellation
            // If attendanceType is specified, filter by that type; otherwise get most recent
            var query = _context.EventAttendances
                .Where(ea => ea.EventId == eventId && ea.UserId == userId && ea.Status == AttendanceStatus.Active);

            if (attendanceType.HasValue)
            {
                query = query.Where(ea => ea.AttendanceType == attendanceType.Value);
            }

            var attendance = await query
                .OrderByDescending(ea => ea.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (attendance == null)
            {
                return Result.Failure("No active attendance found for this event");
            }

            if (!attendance.CanBeCancelled())
            {
                return Result.Failure("Attendance cannot be cancelled in its current status");
            }

            // Check if cancellation is still allowed based on event start time and buffer
            var eventEntity = await _context.Events
                .Include(e => e.Sessions)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result.Failure("Event not found");
            }

            // For tickets, use session-based timing; for RSVPs, use event-based timing
            if (attendance.AttendanceType == AttendanceType.Ticket)
            {
                // Get the ticket type for this attendance to determine reference session
                var ticketPurchase = await _context.TicketPurchases
                    .Include(tp => tp.TicketType)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(tp => tp.Id == attendance.TicketPurchaseId, cancellationToken);

                if (ticketPurchase?.TicketType != null)
                {
                    var referenceSession = _timeZoneService.GetReferenceSessionForTicketType(
                        ticketPurchase.TicketType, eventEntity.Sessions);

                    if (referenceSession == null)
                    {
                        _logger.LogWarning("Ticket cancellation attempt for event {EventId} - all sessions for this ticket have passed",
                            eventId);
                        return Result.Failure("Cannot cancel - all sessions for this ticket have passed");
                    }

                    var canCancel = _timeZoneService.IsActionAllowedForSession(
                        referenceSession,
                        null, // No open restriction for cancellation
                        eventEntity.CancellationCloseHours);

                    if (!canCancel)
                    {
                        _logger.LogWarning("Ticket cancellation attempt for event {EventId} outside allowed timing window for session {SessionId}",
                            eventId, referenceSession.Id);
                        return Result.Failure("Cancellation window has closed for this session");
                    }
                }
                // If no ticket purchase found, fall through to allow cancellation (legacy data support)
            }
            else
            {
                // RSVP cancellation uses event-based timing (per specification - out of scope for session-based refactor)
                var isAllowed = await _timeZoneService.IsActionAllowedAsync(
                    eventEntity, EventActionType.CancelRsvp, cancellationToken);

                if (!isAllowed)
                {
                    _logger.LogWarning("RSVP cancellation attempt for event {EventId} outside allowed timing window", eventId);
                    return Result.Failure("Cancellation window is not currently open for this event");
                }
            }

            // ============================================================================
            // BUSINESS RULE: If cancelling a ticket, also cancel any associated RSVP
            // ============================================================================
            //
            // CRITICAL: Ticket cancellation cancels BOTH records:
            // 1. Ticket record (the one we're cancelling)
            // 2. Associated RSVP record (if exists)
            //
            // WHY: When user purchases ticket for social event, we auto-create RSVP.
            //      If they cancel ticket, we must also cancel the RSVP to prevent orphaned RSVPs.
            //
            // RESULT: User loses BOTH ticket AND RSVP
            // MANUAL RE-RSVP: User CAN manually RSVP again after cancelling (creates NEW record)
            EventAttendance? associatedRsvp = null;
            if (attendance.AttendanceType == AttendanceType.Ticket)
            {
                associatedRsvp = await _context.EventAttendances
                    .Where(ea => ea.EventId == eventId &&
                                ea.UserId == userId &&
                                ea.Status == AttendanceStatus.Active &&
                                ea.AttendanceType == AttendanceType.RSVP)
                    .FirstOrDefaultAsync(cancellationToken);

                if (associatedRsvp != null)
                {
                    _logger.LogInformation("Found associated RSVP {RsvpId} - will also cancel when cancelling ticket {TicketId}",
                        associatedRsvp.Id, attendance.Id);
                }

                // ============================================================================
                // AUTOMATIC REFUND PROCESSING: Process refund for paid tickets
                // ============================================================================
                //
                // BUSINESS RULE: When user cancels their own ticket, automatically process refund
                // if the ticket was paid for via PayPal.
                //
                // CRITICAL: Refund failures should NOT block cancellation
                // - Users should be able to cancel even if refund fails
                // - Failed refunds are logged for manual admin processing
                //
                // INTEGRATION: Uses existing RefundService - no duplicate refund logic
                await ProcessAutomaticRefundAsync(attendance.Id, userId, reason, cancellationToken);
            }

            // Store old values for audit
            var oldValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                Status = attendance.Status,
                CancelledAt = attendance.CancelledAt,
                CancellationReason = attendance.CancellationReason
            });

            // Cancel the attendance
            attendance.Cancel(reason);
            attendance.UpdatedBy = userId;

            // Explicitly mark entity as modified to ensure EF Core tracks the change
            _context.EventAttendances.Update(attendance);

            // Create audit history
            var history = new AttendanceHistory(attendance.Id, "Cancelled")
            {
                OldValues = oldValues,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Status = attendance.Status,
                    CancelledAt = attendance.CancelledAt,
                    CancellationReason = attendance.CancellationReason
                }),
                ChangedBy = userId,
                ChangeReason = reason ?? "Cancelled by user"
            };

            _context.AttendanceHistory.Add(history);

            // Cancel associated RSVP if exists
            if (associatedRsvp != null)
            {
                var rsvpOldValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Status = associatedRsvp.Status,
                    CancelledAt = associatedRsvp.CancelledAt,
                    CancellationReason = associatedRsvp.CancellationReason
                });

                associatedRsvp.Cancel("Auto-cancelled when ticket was cancelled");
                associatedRsvp.UpdatedBy = userId;
                _context.EventAttendances.Update(associatedRsvp);

                var rsvpHistory = new AttendanceHistory(associatedRsvp.Id, "Cancelled")
                {
                    OldValues = rsvpOldValues,
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Status = associatedRsvp.Status,
                        CancelledAt = associatedRsvp.CancelledAt,
                        CancellationReason = associatedRsvp.CancellationReason
                    }),
                    ChangedBy = userId,
                    ChangeReason = "Auto-cancelled when ticket was cancelled"
                };

                _context.AttendanceHistory.Add(rsvpHistory);
            }

            // Update EventAttendee record for check-in system integration
            // Check if user has any remaining ACTIVE attendances after this cancellation
            var remainingActiveAttendances = await _context.EventAttendances
                .Where(ea => ea.EventId == eventId &&
                            ea.UserId == userId &&
                            ea.Status == AttendanceStatus.Active &&
                            ea.Id != attendance.Id && // Exclude the one we're cancelling
                            (associatedRsvp == null || ea.Id != associatedRsvp.Id)) // Exclude associated RSVP if cancelling
                .AnyAsync(cancellationToken);

            // If no active attendances remain, update EventAttendee to "cancelled" status
            if (!remainingActiveAttendances)
            {
                var eventAttendee = await _context.EventAttendees
                    .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId, cancellationToken);

                if (eventAttendee != null)
                {
                    _logger.LogInformation(
                        "Updating EventAttendee {AttendeeId} status to 'cancelled' - no active attendances remain for user {UserId} in event {EventId}",
                        eventAttendee.Id, userId, eventId);

                    eventAttendee.RegistrationStatus = "cancelled";
                    eventAttendee.UpdatedAt = DateTime.UtcNow;
                    _context.EventAttendees.Update(eventAttendee);
                }
                else
                {
                    _logger.LogWarning(
                        "EventAttendee record not found for user {UserId} in event {EventId} during cancellation - check-in list may be out of sync",
                        userId, eventId);
                }
            }
            else
            {
                _logger.LogInformation(
                    "EventAttendee status NOT updated - user {UserId} still has active attendances in event {EventId}",
                    userId, eventId);
            }

            // CRITICAL: Save changes to persist cancellation to database
            await _context.SaveChangesAsync(cancellationToken);

            // Verify persistence (defensive check)
            var cancelledAttendance = await _context.EventAttendances
                .AsNoTracking()
                .FirstOrDefaultAsync(ea => ea.Id == attendance.Id, cancellationToken);

            if (cancelledAttendance == null)
            {
                _logger.LogError("CRITICAL: Attendance {AttendanceId} disappeared after cancellation for user {UserId} in event {EventId}",
                    attendance.Id, userId, eventId);
                return Result.Failure("Failed to verify cancellation in database");
            }

            if (cancelledAttendance.Status != AttendanceStatus.Cancelled)
            {
                _logger.LogError("CRITICAL: Attendance {AttendanceId} cancellation not persisted - Status is {Status} instead of Cancelled",
                    attendance.Id, cancelledAttendance.Status);
                return Result.Failure("Cancellation did not persist to database");
            }

            _logger.LogInformation("Successfully cancelled and verified attendance {AttendanceId} for user {UserId} in event {EventId} (Status: {Status}, CancelledAt: {CancelledAt})",
                cancelledAttendance.Id, userId, eventId, cancelledAttendance.Status, cancelledAttendance.CancelledAt);

            // Verify associated RSVP cancellation if it existed
            if (associatedRsvp != null)
            {
                var cancelledRsvp = await _context.EventAttendances
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ea => ea.Id == associatedRsvp.Id, cancellationToken);

                if (cancelledRsvp == null || cancelledRsvp.Status != AttendanceStatus.Cancelled)
                {
                    _logger.LogError("CRITICAL: Associated RSVP {RsvpId} cancellation not persisted properly",
                        associatedRsvp.Id);
                    return Result.Failure("Failed to cancel associated RSVP");
                }

                _logger.LogInformation("Successfully cancelled and verified associated RSVP {RsvpId} (Status: {Status}, CancelledAt: {CancelledAt})",
                    cancelledRsvp.Id, cancelledRsvp.Status, cancelledRsvp.CancelledAt);
            }

            // Auto-cancel volunteer signups when attendance is cancelled
            try
            {
                var cancellationResult = await _volunteerAssignmentService.CancelAllVolunteerSignupsForUserEventAsync(
                    userId,
                    eventId,
                    "Refunded Ticket, so automatically canceled volunteer spot",
                    cancellationToken);

                if (cancellationResult.success && cancellationResult.cancelledCount > 0)
                {
                    _logger.LogInformation(
                        "Auto-cancelled {Count} volunteer signups for user {UserId} at event {EventId} due to attendance cancellation",
                        cancellationResult.cancelledCount, userId, eventId);
                }
                else if (!cancellationResult.success)
                {
                    _logger.LogWarning(
                        "Failed to auto-cancel volunteer signups for user {UserId} at event {EventId}: {Error}",
                        userId, eventId, cancellationResult.error);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the cancellation if volunteer cancellation fails
                _logger.LogError(ex,
                    "Error auto-cancelling volunteer signups for user {UserId} at event {EventId}",
                    userId, eventId);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling attendance for user {UserId} in event {EventId}", userId, eventId);
            return Result.Failure("Failed to cancel attendance", ex.Message);
        }
    }

    /// <summary>
    /// Cancel specific ticket purchases (selective mode)
    /// </summary>
    /// <remarks>
    /// BUSINESS RULE: Cancel ALL EventAttendance records for the specified TicketPurchase IDs
    ///
    /// CRITICAL: Multi-session tickets create MULTIPLE EventAttendance records (one per session)
    /// - Each EventAttendance has the same TicketPurchaseId
    /// - Must cancel ALL attendances for each ticket purchase
    /// - Process refund for each unique TicketPurchase
    ///
    /// SECURITY: Verify all ticket purchases belong to the requesting user
    ///
    /// INTEGRATION:
    /// - Cancels associated RSVP if exists
    /// - Processes automatic refunds via RefundService
    /// - Auto-cancels volunteer signups
    /// - Updates EventAttendee records
    /// </remarks>
    public async Task<Result> CancelTicketPurchasesAsync(
        Guid eventId,
        Guid userId,
        List<Guid> ticketPurchaseIds,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Cancelling {Count} ticket purchase(s) for user {UserId} in event {EventId}",
                ticketPurchaseIds.Count, userId, eventId);

            // Validate event exists and get event details (needed for timing checks)
            var eventEntity = await _context.Events
                .Include(e => e.Sessions)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result.Failure("Event not found");
            }

            // Find ALL active attendances for these ticket purchases
            // CRITICAL: Must include ALL sessions, not just one attendance per ticket
            var attendancesToCancel = await _context.EventAttendances
                .Where(ea =>
                    ea.EventId == eventId &&
                    ea.UserId == userId &&
                    ea.Status == AttendanceStatus.Active &&
                    ea.TicketPurchaseId.HasValue &&
                    ticketPurchaseIds.Contains(ea.TicketPurchaseId.Value))
                .ToListAsync(cancellationToken);

            if (attendancesToCancel.Count == 0)
            {
                return Result.Failure("No active ticket attendances found for the specified ticket purchases");
            }

            // SECURITY: Verify all ticket purchases belong to this user
            // IMPORTANT: Must include TicketType.Sessions for GetReferenceSessionForTicketType to work correctly
            var ticketPurchases = await _context.TicketPurchases
                .Include(tp => tp.TicketType)
                    .ThenInclude(tt => tt.Sessions)
                .Where(tp => ticketPurchaseIds.Contains(tp.Id))
                .ToListAsync(cancellationToken);

            var unauthorizedPurchases = ticketPurchases.Where(tp => tp.UserId != userId).ToList();
            if (unauthorizedPurchases.Any())
            {
                _logger.LogWarning(
                    "User {UserId} attempted to cancel ticket purchases belonging to other users: {Ids}",
                    userId, string.Join(", ", unauthorizedPurchases.Select(p => p.Id)));
                return Result.Failure("Unauthorized: Cannot cancel tickets that don't belong to you");
            }

            // TIMING VALIDATION: Check if cancellation is still allowed for each ticket type
            foreach (var ticketPurchase in ticketPurchases)
            {
                if (ticketPurchase.TicketType == null)
                {
                    _logger.LogWarning(
                        "Ticket purchase {TicketPurchaseId} has no TicketType - allowing cancellation for legacy data",
                        ticketPurchase.Id);
                    continue;
                }

                var referenceSession = _timeZoneService.GetReferenceSessionForTicketType(
                    ticketPurchase.TicketType, eventEntity.Sessions);

                if (referenceSession == null)
                {
                    _logger.LogWarning(
                        "Ticket cancellation attempt for event {EventId} - all sessions for ticket {TicketId} have passed",
                        eventId, ticketPurchase.Id);
                    return Result.Failure($"Cannot cancel - all sessions for ticket '{ticketPurchase.TicketType.Name}' have passed");
                }

                var canCancel = _timeZoneService.IsActionAllowedForSession(
                    referenceSession,
                    null, // No open restriction for cancellation
                    eventEntity.CancellationCloseHours);

                if (!canCancel)
                {
                    _logger.LogWarning(
                        "Ticket cancellation attempt for event {EventId} outside allowed timing window for session {SessionId}",
                        eventId, referenceSession.Id);
                    return Result.Failure($"Cancellation window has closed for ticket '{ticketPurchase.TicketType.Name}'");
                }
            }

            // Check if each attendance can be cancelled
            foreach (var attendance in attendancesToCancel)
            {
                if (!attendance.CanBeCancelled())
                {
                    return Result.Failure($"Attendance {attendance.Id} cannot be cancelled in its current status");
                }
            }

            // ============================================================================
            // BUSINESS RULE: Also cancel associated RSVP if exists
            // ============================================================================
            var associatedRsvp = await _context.EventAttendances
                .Where(ea =>
                    ea.EventId == eventId &&
                    ea.UserId == userId &&
                    ea.Status == AttendanceStatus.Active &&
                    ea.AttendanceType == AttendanceType.RSVP)
                .FirstOrDefaultAsync(cancellationToken);

            if (associatedRsvp != null)
            {
                _logger.LogInformation(
                    "Found associated RSVP {RsvpId} - will also cancel when cancelling tickets",
                    associatedRsvp.Id);
            }

            // ============================================================================
            // PROCESS REFUNDS: One refund per TicketPurchase
            // ============================================================================
            var processedRefunds = new HashSet<Guid>();
            foreach (var attendance in attendancesToCancel)
            {
                if (attendance.TicketPurchaseId.HasValue && !processedRefunds.Contains(attendance.TicketPurchaseId.Value))
                {
                    // Process refund (once per ticket purchase, not per attendance)
                    await ProcessAutomaticRefundAsync(
                        attendance.Id,
                        userId,
                        reason,
                        cancellationToken);

                    processedRefunds.Add(attendance.TicketPurchaseId.Value);
                }
            }

            // ============================================================================
            // CANCEL ALL ATTENDANCES
            // ============================================================================
            foreach (var attendance in attendancesToCancel)
            {
                var oldValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Status = attendance.Status,
                    CancelledAt = attendance.CancelledAt,
                    CancellationReason = attendance.CancellationReason
                });

                attendance.Cancel(reason);
                attendance.UpdatedBy = userId;
                _context.EventAttendances.Update(attendance);

                // Create audit history
                var history = new AttendanceHistory(attendance.Id, "Cancelled")
                {
                    OldValues = oldValues,
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Status = attendance.Status,
                        CancelledAt = attendance.CancelledAt,
                        CancellationReason = attendance.CancellationReason
                    }),
                    ChangedBy = userId,
                    ChangeReason = reason ?? "User cancelled ticket purchase"
                };

                _context.AttendanceHistory.Add(history);

                _logger.LogInformation(
                    "Cancelled attendance {AttendanceId} for ticket purchase {TicketPurchaseId}",
                    attendance.Id, attendance.TicketPurchaseId);
            }

            // Cancel associated RSVP if exists
            if (associatedRsvp != null)
            {
                var rsvpOldValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Status = associatedRsvp.Status,
                    CancelledAt = associatedRsvp.CancelledAt,
                    CancellationReason = associatedRsvp.CancellationReason
                });

                associatedRsvp.Cancel("Auto-cancelled when ticket was cancelled");
                associatedRsvp.UpdatedBy = userId;
                _context.EventAttendances.Update(associatedRsvp);

                var rsvpHistory = new AttendanceHistory(associatedRsvp.Id, "Cancelled")
                {
                    OldValues = rsvpOldValues,
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Status = associatedRsvp.Status,
                        CancelledAt = associatedRsvp.CancelledAt,
                        CancellationReason = associatedRsvp.CancellationReason
                    }),
                    ChangedBy = userId,
                    ChangeReason = "Auto-cancelled when ticket was cancelled"
                };

                _context.AttendanceHistory.Add(rsvpHistory);
                _logger.LogInformation("Cancelled associated RSVP {RsvpId}", associatedRsvp.Id);
            }

            // ============================================================================
            // UPDATE EventAttendee RECORD
            // ============================================================================
            // Check if user has any remaining ACTIVE attendances after these cancellations
            var remainingActiveAttendances = await _context.EventAttendances
                .Where(ea =>
                    ea.EventId == eventId &&
                    ea.UserId == userId &&
                    ea.Status == AttendanceStatus.Active &&
                    !attendancesToCancel.Select(a => a.Id).Contains(ea.Id) &&
                    (associatedRsvp == null || ea.Id != associatedRsvp.Id))
                .AnyAsync(cancellationToken);

            if (!remainingActiveAttendances)
            {
                var eventAttendee = await _context.EventAttendees
                    .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId, cancellationToken);

                if (eventAttendee != null)
                {
                    _logger.LogInformation(
                        "Updating EventAttendee {AttendeeId} status to 'cancelled' - no active attendances remain",
                        eventAttendee.Id);

                    eventAttendee.RegistrationStatus = "cancelled";
                    eventAttendee.UpdatedAt = DateTime.UtcNow;
                    _context.EventAttendees.Update(eventAttendee);
                }
            }

            // ============================================================================
            // SAVE ALL CHANGES
            // ============================================================================
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully cancelled {Count} attendance(s) for {TicketCount} ticket purchase(s) for user {UserId} in event {EventId}",
                attendancesToCancel.Count, ticketPurchaseIds.Count, userId, eventId);

            // ============================================================================
            // AUTO-CANCEL VOLUNTEER SIGNUPS FOR CANCELLED SESSIONS ONLY
            // ============================================================================
            // Only cancel volunteer signups for sessions that were associated with the cancelled tickets
            // This preserves volunteer signups for sessions the user still has tickets for
            try
            {
                // Collect the session IDs from the cancelled attendances
                var cancelledSessionIds = attendancesToCancel
                    .Where(a => a.SessionId.HasValue)
                    .Select(a => a.SessionId!.Value)
                    .Distinct()
                    .ToList();

                if (cancelledSessionIds.Count > 0)
                {
                    var cancellationResult = await _volunteerAssignmentService.CancelVolunteerSignupsForSessionsAsync(
                        userId,
                        eventId,
                        cancelledSessionIds,
                        "Ticket cancelled, so automatically canceled volunteer spot for affected sessions",
                        cancellationToken);

                    if (cancellationResult.success && cancellationResult.cancelledCount > 0)
                    {
                        _logger.LogInformation(
                            "Auto-cancelled {Count} volunteer signups for user {UserId} at event {EventId} for {SessionCount} sessions",
                            cancellationResult.cancelledCount, userId, eventId, cancelledSessionIds.Count);
                    }
                    else if (!cancellationResult.success)
                    {
                        _logger.LogWarning(
                            "Failed to auto-cancel volunteer signups: {Error}",
                            cancellationResult.error);
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "No session-specific attendances found in cancelled tickets for user {UserId} at event {EventId}",
                        userId, eventId);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the cancellation
                _logger.LogError(ex,
                    "Error auto-cancelling volunteer signups for user {UserId} at event {EventId}",
                    userId, eventId);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error cancelling ticket purchases for user {UserId} in event {EventId}",
                userId, eventId);
            return Result.Failure("Failed to cancel ticket purchases", ex.Message);
        }
    }

    /// <summary>
    /// Get all of user's current attendances
    /// </summary>
    public async Task<Result<List<UserParticipationDto>>> GetUserParticipationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting attendances for user {UserId}", userId);

            var attendances = await _context.EventAttendances
                .AsNoTracking()
                .Include(ea => ea.Event)
                    .ThenInclude(e => e.Venue)
                .Where(ea => ea.UserId == userId)
                .OrderByDescending(ea => ea.CreatedAt)
                .Select(ea => new UserParticipationDto
                {
                    Id = ea.Id,
                    EventId = ea.EventId,
                    EventTitle = ea.Event.Title,
                    EventStartDate = ea.Event.StartDate,
                    EventEndDate = ea.Event.EndDate,
                    EventLocation = ea.Event.Venue != null ? ea.Event.Venue.Name : string.Empty,
                    ParticipationType = ea.AttendanceType,
                    Status = ea.Status,
                    ParticipationDate = ea.CreatedAt,
                    Notes = ea.Notes,
                    CanCancel = ea.Status == AttendanceStatus.Active
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} attendances for user {UserId}", attendances.Count, userId);

            return Result<List<UserParticipationDto>>.Success(attendances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendances for user {UserId}", userId);
            return Result<List<UserParticipationDto>>.Failure("Failed to get user attendances", ex.Message);
        }
    }

    /// <summary>
    /// Get all attendances for a specific event (admin only)
    /// Includes check-in status from EventAttendees table
    /// </summary>
    public async Task<Result<List<EventParticipationDto>>> GetEventParticipationsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting attendances for event {EventId}", eventId);

            // Join with EventAttendees to get check-in status from CheckIns table
            // FILTER: Only include Active attendances (exclude cancelled)
            var attendances = await _context.EventAttendances
                .AsNoTracking()
                .Include(ea => ea.User)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp.TicketType)
                        .ThenInclude(tt => tt.Sessions)
                .Where(ea => ea.EventId == eventId && ea.Status == AttendanceStatus.Active)
                .GroupJoin(
                    _context.EventAttendees
                        .Include(attendee => attendee.CheckIns)
                            .ThenInclude(checkin => checkin.Session)
                        .Where(ea => ea.EventId == eventId),
                    ea => ea.UserId,
                    ea => ea.UserId,
                    (ea, attendees) => new { Attendance = ea, Attendees = attendees })
                .SelectMany(
                    x => x.Attendees.DefaultIfEmpty(),
                    (x, attendee) => new { x.Attendance, Attendee = attendee })
                .OrderByDescending(x => x.Attendance.CreatedAt)
                .Select(x => new EventParticipationDto
                {
                    Id = x.Attendance.Id,
                    UserId = x.Attendance.UserId,
                    UserSceneName = x.Attendance.User.SceneName ?? x.Attendance.User.Email ?? "Unknown",
                    UserEmail = x.Attendance.User.Email ?? "",
                    ParticipationType = x.Attendance.AttendanceType,
                    Status = x.Attendance.Status,
                    ParticipationDate = x.Attendance.CreatedAt,
                    Notes = x.Attendance.Notes,
                    CanCancel = x.Attendance.Status == AttendanceStatus.Active,
                    Metadata = x.Attendance.Metadata,
                    // Check-in status: true if EventAttendee has ANY CheckIn records
                    HasCheckedIn = x.Attendee != null && x.Attendee.CheckIns.Any(),
                    // Check-in time from most recent CheckIn record
                    CheckInTime = x.Attendee != null && x.Attendee.CheckIns.Any()
                                  ? x.Attendee.CheckIns.OrderByDescending(c => c.CheckInTime).First().CheckInTime
                                  : (DateTime?)null,
                    // Ticket type name from TicketPurchase navigation (null for RSVPs)
                    TicketTypeName = x.Attendance.TicketPurchase != null
                                     ? x.Attendance.TicketPurchase.TicketType.Name
                                     : null,
                    // Session names from TicketType.Sessions (many-to-many)
                    SessionNames = x.Attendance.TicketPurchase != null && x.Attendance.TicketPurchase.TicketType.Sessions.Any()
                                   ? string.Join(", ", x.Attendance.TicketPurchase.TicketType.Sessions.OrderBy(s => s.StartTime).Select(s => s.Name))
                                   : "No Sessions",
                    // Amount paid from TicketPurchase.TotalPrice (null for free RSVPs without TicketPurchase)
                    AmountPaid = x.Attendance.TicketPurchase != null
                                 ? x.Attendance.TicketPurchase.TotalPrice
                                 : (decimal?)null,
                    // TicketPurchase ID for refund processing (null for free RSVPs without TicketPurchase)
                    TicketId = x.Attendance.TicketPurchaseId,
                    // Payment method (generic for paid tickets, null for free RSVPs)
                    PaymentMethod = x.Attendance.TicketPurchase != null && x.Attendance.TicketPurchase.TotalPrice > 0
                                    ? "PayPal/Venmo/Cash"
                                    : null,
                    // List of session names user has checked into
                    CheckedInSessions = x.Attendee != null
                                        ? x.Attendee.CheckIns
                                            .Where(c => c.Session != null)
                                            .Select(c => c.Session.Name)
                                            .ToList()
                                        : new List<string>()
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} attendances for event {EventId}", attendances.Count, eventId);

            return Result<List<EventParticipationDto>>.Success(attendances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendances for event {EventId}", eventId);
            return Result<List<EventParticipationDto>>.Failure("Failed to get event attendances", ex.Message);
        }
    }

    /// <summary>
    /// Process automatic refund for user-initiated ticket cancellation
    /// </summary>
    /// <remarks>
    /// BUSINESS RULE: When users cancel their own tickets, automatically process refunds for paid tickets.
    ///
    /// CRITICAL REQUIREMENTS:
    /// - Only refund PayPal payments that are Completed
    /// - Do NOT block cancellation if refund fails
    /// - Log all refund attempts for admin review
    /// - Include user's cancellation reason in refund reason
    ///
    /// INTEGRATION: Uses existing RefundService - no duplicate refund logic
    /// </remarks>
    private async Task ProcessAutomaticRefundAsync(
        Guid attendanceId,
        Guid userId,
        string? userCancellationReason,
        CancellationToken cancellationToken)
    {
        try
        {
            // ARCHITECTURE FIX: Find associated TicketPurchase (single source of truth)
            // Get the event attendance to find the TicketPurchaseId
            var attendance = await _context.EventAttendances
                .FirstOrDefaultAsync(ea => ea.Id == attendanceId, cancellationToken);

            if (attendance == null || !attendance.TicketPurchaseId.HasValue)
            {
                _logger.LogInformation(
                    "No ticket purchase found for attendance {AttendanceId} - skipping automatic refund",
                    attendanceId);
                return;
            }

            // Get the ticket purchase
            var ticketPurchase = await _context.TicketPurchases
                .FirstOrDefaultAsync(tp => tp.Id == attendance.TicketPurchaseId.Value, cancellationToken);

            // No ticket purchase found or not completed - skip refund
            if (ticketPurchase == null || !ticketPurchase.IsPaymentCompleted)
            {
                _logger.LogInformation(
                    "No completed ticket purchase found for attendance {AttendanceId} - skipping automatic refund",
                    attendanceId);
                return;
            }

            // Only process refunds for PayPal payments
            if (!ticketPurchase.PaymentMethod.Equals("PayPal", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Ticket purchase {TicketId} is not PayPal (method: {PaymentMethod}) - skipping automatic refund",
                    ticketPurchase.Id, ticketPurchase.PaymentMethod);
                return;
            }

            // Construct refund reason with user's cancellation reason
            var refundReason = string.IsNullOrWhiteSpace(userCancellationReason)
                ? "User-initiated ticket cancellation: No reason provided"
                : $"User-initiated ticket cancellation: {userCancellationReason}";

            _logger.LogInformation(
                "Processing automatic refund for ticket {TicketId} (user {UserId} cancelling ticket). Amount: {Amount}",
                ticketPurchase.Id, userId, ticketPurchase.TotalPrice);

            // Create refund request
            var refundRequest = new ProcessRefundRequest
            {
                TicketPurchaseId = ticketPurchase.Id,
                RefundAmount = Money.Create(ticketPurchase.TotalPrice, "USD"),
                RefundReason = refundReason,
                ProcessedByUserId = userId, // User cancelling their own ticket
                IpAddress = "user-initiated-cancellation", // Placeholder - no IP available in service layer
                UserAgent = "automatic-refund",
                Metadata = new Dictionary<string, object>
                {
                    ["automatic_refund"] = true,
                    ["triggered_by"] = "user_ticket_cancellation",
                    ["attendance_id"] = attendanceId.ToString()
                }
            };

            // Process refund via RefundService
            var refundResult = await _refundService.ProcessRefundAsync(refundRequest, cancellationToken);

            if (refundResult.IsSuccess)
            {
                _logger.LogInformation(
                    "Automatic refund processed successfully for ticket {TicketId}. Refund ID: {RefundId}",
                    ticketPurchase.Id, refundResult.Value?.Id);
            }
            else
            {
                // Log warning but DON'T fail the cancellation
                _logger.LogWarning(
                    "Automatic refund failed for ticket {TicketId} during user ticket cancellation. Error: {Error}. " +
                    "User's ticket will still be cancelled. Admin should manually process refund.",
                    ticketPurchase.Id, refundResult.ErrorMessage);

                // TODO: Send notification to admins about failed automatic refund
                // This would require email service integration - out of scope for this task
            }
        }
        catch (Exception ex)
        {
            // Log error but DON'T fail the cancellation
            _logger.LogError(ex,
                "Error processing automatic refund for attendance {AttendanceId} during user ticket cancellation. " +
                "User's ticket will still be cancelled. Admin should manually process refund.",
                attendanceId);

            // TODO: Send notification to admins about failed automatic refund
            // This would require email service integration - out of scope for this task
        }
    }

    /// <summary>
    /// Generate a helpful message explaining when ticket purchase will be available
    /// Based on event's RegistrationOpenHours and RegistrationCloseHours
    /// </summary>
    private string GetTicketPurchaseTimingMessage(WitchCityRope.Api.Models.Event eventEntity)
    {
        var now = DateTime.UtcNow;

        // For events with sessions, use the earliest session start time
        // For events without sessions, use the event start date
        DateTime referenceTime;
        if (eventEntity.Sessions != null && eventEntity.Sessions.Any())
        {
            referenceTime = eventEntity.Sessions.Min(s => s.StartTime);
        }
        else
        {
            referenceTime = eventEntity.StartDate;
        }

        var hoursUntilEvent = (referenceTime - now).TotalHours;

        // Check if we're before the registration open window
        if (eventEntity.RegistrationOpenHours.HasValue &&
            hoursUntilEvent > (double)eventEntity.RegistrationOpenHours.Value)
        {
            var opensAt = referenceTime.AddHours(-(double)eventEntity.RegistrationOpenHours.Value);
            // Convert UTC to Eastern Time for display
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            var opensAtLocal = TimeZoneInfo.ConvertTimeFromUtc(opensAt, easternZone);
            return $"Sales open {opensAtLocal:MMM d}";
        }

        // Check if we're past the registration close window
        if (eventEntity.RegistrationCloseHours.HasValue &&
            hoursUntilEvent < (double)eventEntity.RegistrationCloseHours.Value)
        {
            return "Sales closed";
        }

        // Otherwise, assume sales are closed (past the event)
        return "Sales closed";
    }
}
