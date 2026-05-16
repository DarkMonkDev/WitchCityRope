using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.AuthorizedContacts.Services;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Events.Interfaces;
using WitchCityRope.Api.Features.Events;
using IEventEmailService = WitchCityRope.Api.Features.EmailTemplates.Services.IEventEmailService;
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
//    See: CreateTicketPurchaseAsync (auto-RSVP creation)
//
// 2. TICKET CANCELLATION cancels BOTH records:
//    - Ticket record → Status=Cancelled
//    - Associated RSVP → Status=Cancelled
//    See: CancelTicketPurchasesAsync (associated RSVP cancellation)
//
// 3. MANUAL RSVP creates standalone record:
//    - User CAN RSVP after cancelling ticket
//    - Creates NEW EventAttendances (AttendanceType=RSVP, Status=Active)
//    - Cancelled RSVPs do NOT prevent new RSVPs
//    See: CreateRSVPAsync (only checks ACTIVE RSVPs)
//
// 4. QUERIES must filter by AttendanceType:
//    - Check for existing RSVP: Filter by AttendanceType=RSVP AND Status=Active
//    - Check for existing Ticket: Filter by AttendanceType=Ticket AND Status=Active
//    - User can have BOTH active Ticket and active RSVP simultaneously
//    See: GetParticipationStatusAsync (separate queries for each type)
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
    private readonly IEventEmailService _eventEmailService;
    private readonly IAttendanceCountService _countService;
    private readonly IAuthorizedContactService _authorizedContactService;
    // Used to decrypt the stored Authorize.net refund transaction id for the admin
    // roster view (RefundHistoryDto.AuthNetRefundTransactionId).
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        ApplicationDbContext context,
        IVolunteerAssignmentService volunteerAssignmentService,
        ITimeZoneService timeZoneService,
        IRefundService refundService,
        IEventEmailService eventEmailService,
        IAttendanceCountService countService,
        IAuthorizedContactService authorizedContactService,
        IEncryptionService encryptionService,
        ILogger<AttendanceService> logger)
    {
        _context = context;
        _volunteerAssignmentService = volunteerAssignmentService;
        _timeZoneService = timeZoneService;
        _refundService = refundService;
        _eventEmailService = eventEmailService;
        _countService = countService;
        _authorizedContactService = authorizedContactService;
        _encryptionService = encryptionService;
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

            // Get event details with sessions and attendances for capacity calculation.
            // EventAttendances chain is needed for Session.CurrentAttendees (used by
            // Event.GetAvailableSpotsDisplay()) — same chain as EventService queries.
            var eventEntity = await _context.Events
                .AsNoTracking()
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.Sessions)
                .Include(e => e.EventAttendances)
                    .ThenInclude(ea => ea.TicketPurchase)
                        .ThenInclude(tp => tp.TicketType)
                            .ThenInclude(tt => tt.Sessions)
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Event {EventId} not found when fetching attendance status", eventId);
                return Result<EnhancedParticipationStatusDto?>.NotFound("Event not found");
            }

            // Display count: Active only, type-appropriate — matches events list page logic.
            // This is what users see as "X RSVPs" or "X sold" on event cards and detail pages.
            var displayCount = await _countService.GetDisplayCountAsync(eventId, cancellationToken);

            // Reserved count: Active + PendingPayment, all types — prevents overselling during checkout.
            // Used for capacity business logic (CanRSVP check).
            var reservedCount = await _countService.GetReservedCountAsync(eventId, cancellationToken);

            // Compute per-person participation count for DefaultMaxTicketOrRsvpPerPerson enforcement.
            // This counts: the user's own participation (RSVP or ticket = 1) + proxy RSVPs they've created.
            int? remainingPerPerson = null;
            if (eventEntity.DefaultMaxTicketOrRsvpPerPerson.HasValue)
            {
                var perPersonLimit = eventEntity.DefaultMaxTicketOrRsvpPerPerson.Value;

                // Check if user has any own participation (RSVP or Ticket, Active or PendingAcceptance)
                var hasOwnParticipation = await _context.EventAttendances
                    .AsNoTracking()
                    .AnyAsync(ea =>
                        ea.EventId == eventId
                        && ea.UserId == userId
                        && (ea.AttendanceType == AttendanceType.RSVP
                            || ea.AttendanceType == AttendanceType.Ticket)
                        && (ea.Status == AttendanceStatus.Active
                            || ea.Status == AttendanceStatus.PendingAcceptance),
                        cancellationToken);

                // Count proxy RSVPs created by this user for this event
                var proxyRsvpCount = await _context.EventAttendances
                    .AsNoTracking()
                    .CountAsync(ea =>
                        ea.EventId == eventId
                        && ea.AssignedByUserId == userId
                        && ea.AttendanceType == AttendanceType.RSVP
                        && (ea.Status == AttendanceStatus.Active
                            || ea.Status == AttendanceStatus.PendingAcceptance),
                        cancellationToken);

                var totalUserCount = (hasOwnParticipation ? 1 : 0) + proxyRsvpCount;
                remainingPerPerson = Math.Max(0, perPersonLimit - totalUserCount);
            }

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
                    ea.AssignedByUserId,
                    TicketTypeName = ea.TicketPurchase != null && ea.TicketPurchase.TicketType != null
                        ? ea.TicketPurchase.TicketType.Name
                        : null,
                    TotalPrice = ea.TicketPurchase != null ? ea.TicketPurchase.TotalPrice : 0m,
                    // Track whether the ticket was purchased by someone else (assigned to current user)
                    PurchaserUserId = ea.TicketPurchase != null ? ea.TicketPurchase.UserId : (Guid?)null
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

            // Resolve AssignedByUserId -> SceneName for tickets assigned to the current user.
            // This allows the frontend to show "From: SceneName" on received tickets.
            var assignedByUserIds = userTicketAttendanceData
                .Where(x => x.AssignedByUserId.HasValue)
                .Select(x => x.AssignedByUserId!.Value)
                .Distinct()
                .ToList();

            var assignedBySceneNames = assignedByUserIds.Count > 0
                ? await _context.Users.AsNoTracking()
                    .Where(u => assignedByUserIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.SceneName ?? string.Empty, cancellationToken)
                : new Dictionary<Guid, string>();

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

                // Check if this ticket was assigned to the current user by someone else.
                // If so, resolve the assigner's scene name for the "From:" badge.
                var attendanceForPurchase = userTicketAttendanceData
                    .FirstOrDefault(x => x.TicketPurchaseId == ticketPurchaseEntity.Id);

                string? assignedByName = null;
                var isReceivedTicket = attendanceForPurchase?.PurchaserUserId != null
                    && attendanceForPurchase.PurchaserUserId != userId;

                if (isReceivedTicket && attendanceForPurchase?.AssignedByUserId != null)
                {
                    assignedBySceneNames.TryGetValue(attendanceForPurchase.AssignedByUserId.Value, out assignedByName);
                }

                // Received tickets (assigned to current user by someone else) can be returned.
                // "Return" reverts the ticket to the original purchaser — no refund needed.
                if (isReceivedTicket && !canCancelThisPurchase)
                {
                    canCancelThisPurchase = true;
                    cancellationMessage = null;
                }

                ticketPurchases[ticketPurchaseEntity.Id] = new TicketPurchaseInfoDto
                {
                    TicketTypeName = ticketTypeName,
                    SessionIds = sessionIds,
                    TotalPrice = ticketPurchaseEntity.TotalPrice,
                    CanCancel = canCancelThisPurchase,
                    CancellationMessage = cancellationMessage,
                    AssignedBySceneName = assignedByName
                };
            }

            // ============================================================================
            // TICKETS PURCHASED FOR OTHERS
            // Include in the same ticketPurchases dict with IsForOther=true so the
            // ParticipationCard can show them alongside the user's own tickets.
            // ============================================================================
            var ticketsForOthers = await _context.TicketPurchases
                .AsNoTracking()
                .Include(tp => tp.TicketType)
                    .ThenInclude(tt => tt!.Sessions)
                .Where(tp => tp.UserId == userId
                    // Inline IsPaymentCompleted check — computed properties can't be
                    // translated to SQL by EF Core (causes InvalidOperationException)
                    && (tp.PaymentStatus == TicketPurchasePaymentStatus.Completed
                        || tp.PaymentStatus == TicketPurchasePaymentStatus.Confirmed
                        || tp.PaymentStatus == TicketPurchasePaymentStatus.PartiallyRefunded)
                    && tp.TicketType != null
                    && tp.TicketType.EventId == eventId
                    // Exclude tickets already in the dict (user's own tickets)
                    && !ticketPurchaseIds.Contains(tp.Id))
                .ToListAsync(cancellationToken);

            if (ticketsForOthers.Count > 0)
            {
                // Load EventAttendance records for these purchases to determine assignment status
                var forOthersPurchaseIds = ticketsForOthers.Select(tp => tp.Id).ToList();
                var forOthersAttendances = await _context.EventAttendances
                    .AsNoTracking()
                    .Where(ea => ea.TicketPurchaseId.HasValue
                        && forOthersPurchaseIds.Contains(ea.TicketPurchaseId.Value))
                    .Select(ea => new { ea.TicketPurchaseId, ea.UserId, ea.Status, ea.AcceptedAt })
                    .ToListAsync(cancellationToken);

                // Batch-resolve assignee scene names
                var assigneeUserIds = forOthersAttendances
                    .Select(a => a.UserId)
                    .Distinct()
                    .Where(uid => uid != userId) // Exclude the purchaser themselves
                    .ToList();

                var assigneeSceneNames = assigneeUserIds.Count > 0
                    ? await _context.Users.AsNoTracking()
                        .Where(u => assigneeUserIds.Contains(u.Id))
                        .ToDictionaryAsync(u => u.Id, u => u.SceneName ?? string.Empty, cancellationToken)
                    : new Dictionary<Guid, string>();

                foreach (var tp in ticketsForOthers)
                {
                    // Find the most relevant attendance for this purchase.
                    // Prefer Active/PendingAcceptance over Cancelled (returned tickets).
                    // A multi-session ticket may have multiple attendances — find the first non-cancelled one.
                    var allAttendancesForPurchase = forOthersAttendances
                        .Where(a => a.TicketPurchaseId == tp.Id)
                        .ToList();

                    var attendance = allAttendancesForPurchase
                        .FirstOrDefault(a => a.Status == AttendanceStatus.Active
                            || a.Status == AttendanceStatus.PendingAcceptance)
                        ?? allAttendancesForPurchase.FirstOrDefault();

                    // Determine assignee status
                    string assigneeStatus;
                    string? assigneeSceneName = null;
                    bool canCancelForOther;
                    string? cancelMsg = null;

                    if (attendance == null
                        || attendance.Status == AttendanceStatus.Cancelled
                        || attendance.Status == AttendanceStatus.Refunded)
                    {
                        // No active attendance = unassigned / returned ticket, available for (re)assignment
                        assigneeStatus = "Unassigned";
                        canCancelForOther = true;
                    }
                    else if (attendance.Status == AttendanceStatus.PendingAcceptance)
                    {
                        // Assigned but not yet accepted
                        assigneeStatus = "PendingAcceptance";
                        assigneeSceneNames.TryGetValue(attendance.UserId, out assigneeSceneName);
                        canCancelForOther = true;
                    }
                    else if (attendance.Status == AttendanceStatus.Active)
                    {
                        // Accepted by assignee - cannot cancel
                        assigneeStatus = "Active";
                        assigneeSceneNames.TryGetValue(attendance.UserId, out assigneeSceneName);
                        canCancelForOther = false;
                        cancelMsg = $"Ticket accepted by {assigneeSceneName ?? "assignee"} — cannot cancel";
                    }
                    else
                    {
                        // Other statuses - treat as unassigned
                        assigneeStatus = "Unassigned";
                        canCancelForOther = true;
                    }

                    // Also check timing window for cancellable tickets
                    if (canCancelForOther && tp.TicketType != null)
                    {
                        var refSession = _timeZoneService.GetReferenceSessionForTicketType(
                            tp.TicketType, eventEntity.Sessions);
                        if (refSession == null)
                        {
                            canCancelForOther = false;
                            cancelMsg = "All sessions have passed";
                        }
                        else
                        {
                            var timingOk = _timeZoneService.IsActionAllowedForSession(
                                refSession, null, eventEntity.CancellationCloseHours);
                            if (!timingOk)
                            {
                                canCancelForOther = false;
                                cancelMsg = "Cancellation window has closed";
                            }
                        }
                    }

                    if (canCancelForOther)
                        hasAnyCancelableTicket = true;

                    var sessionIds = tp.TicketType?.Sessions.Select(s => s.Id).ToList() ?? new List<Guid>();

                    ticketPurchases[tp.Id] = new TicketPurchaseInfoDto
                    {
                        TicketTypeName = tp.TicketType?.Name ?? "Event Ticket",
                        SessionIds = sessionIds,
                        TotalPrice = tp.TotalPrice,
                        CanCancel = canCancelForOther,
                        CancellationMessage = cancelMsg,
                        IsForOther = true,
                        AssigneeSceneName = assigneeSceneName,
                        AssigneeStatus = assigneeStatus
                    };
                }
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

            // Include PendingPayment so UI correctly shows user has a ticket in progress
            var ticketAttendance = await _context.EventAttendances
                .AsNoTracking()
                .Where(ea => ea.EventId == eventId &&
                            ea.UserId == userId &&
                            (ea.Status == AttendanceStatus.Active || ea.Status == AttendanceStatus.PendingPayment) &&
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
                CanRSVP = rsvpAttendance == null && reservedCount < eventEntity.Capacity,
                CanPurchaseTicket = canPurchaseTicket,
                CanCancelRSVP = canCancelRSVP,
                CanCancelTicket = canCancelTicket,
                TicketPurchaseMessage = ticketPurchaseMessage,
                // Current uses display count (Active only, type-appropriate) — consistent with events list page.
                // Available uses Event.GetAvailableSpotsDisplay() — single source of truth for
                // per-session-aware availability. For multi-session events this returns max remaining
                // across future sessions; for single-session events it returns Capacity - attendee count.
                Capacity = new CapacityInfoDto
                {
                    Current = displayCount,
                    Total = eventEntity.Capacity,
                    Available = eventEntity.GetAvailableSpotsDisplay()
                },
                MaxPerPerson = eventEntity.DefaultMaxTicketOrRsvpPerPerson,
                RemainingPerPerson = remainingPerPerson,
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

            // Calculate CanBuyForOthers: user has a ticket AND there are purchasable ticket types
            // with capacity remaining. Unlike CanPurchaseTicket (for self) or CanPurchaseAdditionalSessions
            // (for sessions user doesn't own), this ignores what the user already owns because the
            // tickets would be for other people via the BuyForOthersOnly checkout flow.
            if (dto.HasTicket && dto.Capacity.Available > 0
                && (remainingPerPerson == null || remainingPerPerson > 0))
            {
                // Check if any ticket type has remaining capacity
                dto.CanBuyForOthers = dto.CanPurchaseTicket || dto.CanPurchaseAdditionalSessions
                    || eventEntity.TicketTypes.Any(tt => tt.Remaining > 0);
            }

            _logger.LogInformation(
                "Attendance status for user {UserId} in event {EventId}: HasRSVP={HasRSVP}, HasTicket={HasTicket}, CanRSVP={CanRSVP}, CanCancelRSVP={CanCancelRSVP}, CanCancelTicket={CanCancelTicket}, CanBuyForOthers={CanBuyForOthers}, Capacity={Current}/{Total}",
                userId, eventId, dto.HasRSVP, dto.HasTicket, dto.CanRSVP, dto.CanCancelRSVP, dto.CanCancelTicket, dto.CanBuyForOthers, dto.Capacity.Current, dto.Capacity.Total);

            return Result<EnhancedParticipationStatusDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance status for user {UserId} in event {EventId}", userId, eventId);
            return Result<EnhancedParticipationStatusDto?>.Infrastructure("Failed to get attendance status. See server logs for details.");
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
                return Result<ParticipationStatusDto>.NotFound("Event not found");
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
                    return Result<ParticipationStatusDto>.Forbidden("This event is limited to vetted members only");
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
                return Result<ParticipationStatusDto>.NotFound("User not found");
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
                return Result<ParticipationStatusDto>.Conflict("User already has an active RSVP for this event");
            }

            // Use reserved count (Active + PendingPayment) to prevent overselling during checkout windows
            var currentAttendanceCount = await _countService.GetReservedCountAsync(request.EventId, cancellationToken);

            if (currentAttendanceCount >= eventEntity.Capacity)
            {
                return Result<ParticipationStatusDto>.Conflict("Event is at full capacity");
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

                return Result<ParticipationStatusDto>.Infrastructure("Failed to save RSVP to database. See server logs for details.");
            }

            _logger.LogInformation(
                "DIAGNOSTIC: Verification successful - Found RSVP {AttendanceId} for user {UserId} in event {EventId} (Status: {Status}, Type: {Type}, CreatedAt: {CreatedAt})",
                savedAttendance.Id, userId, request.EventId, savedAttendance.Status, savedAttendance.AttendanceType, savedAttendance.CreatedAt);

            // Send RSVP confirmation email (fire-and-forget)
            // Only for manual RSVPs — auto-RSVPs from ticket purchase do NOT send this
            // (those users already get a ticket confirmation email)
            try
            {
                await _eventEmailService.SendRsvpConfirmationEmailAsync(
                    userId, request.EventId, cancellationToken);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx,
                    "Failed to send RSVP confirmation email for user {UserId} event {EventId} (non-fatal)",
                    userId, request.EventId);
            }

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
            return Result<ParticipationStatusDto>.Infrastructure("Failed to create RSVP. See server logs for details.");
        }
    }

    /// <summary>
    /// Purchase one or more tickets for a class event (any authenticated user)
    /// Supports both single ticket (backward compatible) and multi-ticket purchases with optional assignees.
    /// When TicketSelections is provided, it takes precedence over TicketTypeIds.
    /// Backward compatible: if TicketSelections is null/empty, falls back to TicketTypeIds (one per type).
    /// </summary>
    public async Task<Result<ParticipationStatusDto>> CreateTicketPurchaseAsync(
        CreateTicketPurchaseRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ============================================================================
            // NORMALIZE: Convert old format to new format for unified processing (backward compat)
            // ============================================================================
            var selections = request.TicketSelections?.Any() == true
                ? request.TicketSelections
                : request.TicketTypeIds.Select(id => new TicketSelectionItem
                {
                    TicketTypeId = id,
                    Quantity = 1,
                    Assignees = null
                }).ToList();

            var totalNewTickets = selections.Sum(s => s.Quantity);
            var allTicketTypeIds = selections.Select(s => s.TicketTypeId).Distinct().ToList();

            _logger.LogInformation(
                "Creating ticket purchase for user {UserId} in event {EventId} with {SelectionCount} selection(s), {TotalTickets} total ticket(s)",
                userId, request.EventId, selections.Count, totalNewTickets);

            // Check if event exists FIRST (need event for timing check)
            var eventEntity = await _context.Events
                .Include(e => e.Sessions)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result<ParticipationStatusDto>.NotFound("Event not found");
            }

            // Check if user exists
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return Result<ParticipationStatusDto>.NotFound("User not found");
            }

            // VETTING ENFORCEMENT: Check if event requires vetted members (purchaser)
            // Must run before waiver/timing checks so non-vetted users get clear denial message
            if (eventEntity.VettedMembersOnly && !user.IsVetted)
            {
                return Result<ParticipationStatusDto>.Forbidden("This event is limited to vetted members only");
            }

            // CRITICAL: Validate Event Waiver acceptance (for purchaser's own ticket)
            if (!request.EventWaiverAccepted)
            {
                return Result<ParticipationStatusDto>.Failure("You must accept the Event Waiver to purchase a ticket");
            }

            // Load all ticket types with their sessions
            var ticketTypesWithSessions = await _context.TicketTypes
                .Include(tt => tt.Sessions)
                .Where(tt => allTicketTypeIds.Contains(tt.Id))
                .ToListAsync(cancellationToken);

            // Validate all ticket types exist
            if (ticketTypesWithSessions.Count != allTicketTypeIds.Count)
            {
                var missingIds = allTicketTypeIds.Except(ticketTypesWithSessions.Select(tt => tt.Id)).ToList();
                _logger.LogWarning("Ticket type(s) not found: {MissingIds}", string.Join(", ", missingIds));
                return Result<ParticipationStatusDto>.Failure("One or more ticket types not found");
            }

            // ============================================================================
            // VALIDATE: MaxQuantityPerPurchase per selection (BR-010)
            // ============================================================================
            foreach (var selection in selections)
            {
                var ticketType = ticketTypesWithSessions.First(tt => tt.Id == selection.TicketTypeId);
                if (selection.Quantity > ticketType.MaxQuantityPerPurchase)
                {
                    return Result<ParticipationStatusDto>.Failure(
                        $"Maximum {ticketType.MaxQuantityPerPurchase} tickets allowed per purchase for '{ticketType.Name}'");
                }

                // Validate assignees list length.
                // Normal mode: first ticket is for the purchaser, so max assignees = Quantity - 1.
                // BuyForOthersOnly mode: ALL tickets are for assignees, so max = Quantity.
                var maxAssignees = request.BuyForOthersOnly ? selection.Quantity : selection.Quantity - 1;
                if (selection.Assignees != null && selection.Assignees.Count > maxAssignees)
                {
                    var detail = request.BuyForOthersOnly
                        ? $"Maximum {selection.Quantity} assignees for {selection.Quantity} tickets."
                        : $"Maximum {maxAssignees} assignees for {selection.Quantity} tickets (first ticket is for purchaser).";
                    return Result<ParticipationStatusDto>.Failure(
                        $"Too many assignees for '{ticketType.Name}'. {detail}");
                }
            }

            // Collect all session IDs across all ticket types for overlap detection
            var allRequestedSessionIds = ticketTypesWithSessions
                .SelectMany(tt => tt.Sessions.Select(s => s.Id))
                .Distinct()
                .ToList();

            // ============================================================================
            // VALIDATE: Cumulative per-person ticket limit
            // Counts ALL tickets a user holds for this event (not per-session),
            // against Event.DefaultMaxTicketOrRsvpPerPerson.
            // This is separate from MaxQuantityPerPurchase which is a per-transaction cap.
            // ============================================================================
            if (eventEntity.DefaultMaxTicketOrRsvpPerPerson.HasValue)
            {
                var cumulativeLimit = eventEntity.DefaultMaxTicketOrRsvpPerPerson.Value;

                // For the purchaser (skip if BuyForOthersOnly — purchaser isn't getting tickets)
                if (!request.BuyForOthersOnly)
                {
                    // Count ALL of the purchaser's existing Active/PendingPayment/PendingAcceptance
                    // ticket attendances for this event. Uses event-wide count (not per-session)
                    // to prevent circumventing the limit via separate purchases for different sessions.
                    var purchaserExistingCount = await _context.EventAttendances
                        .AsNoTracking()
                        .CountAsync(ea =>
                            ea.EventId == request.EventId
                            && ea.UserId == userId
                            && ea.AttendanceType == AttendanceType.Ticket
                            && (ea.Status == AttendanceStatus.Active
                                || ea.Status == AttendanceStatus.PendingPayment
                                || ea.Status == AttendanceStatus.PendingAcceptance),
                            cancellationToken);

                    // Total new tickets for the purchaser = sum of all selections minus assignee-only tickets
                    // In normal mode, purchaser gets 1 ticket per selection; assignees get the rest
                    var purchaserNewTickets = selections.Count; // purchaser gets 1 per ticket type selection

                    if (purchaserExistingCount + purchaserNewTickets > cumulativeLimit)
                    {
                        _logger.LogWarning(
                            "Cumulative ticket limit exceeded for user {UserId} in event {EventId}: " +
                            "existing={ExistingCount}, new={NewCount}, limit={Limit}",
                            userId, request.EventId, purchaserExistingCount, purchaserNewTickets, cumulativeLimit);
                        return Result<ParticipationStatusDto>.Failure(
                            $"You already have {purchaserExistingCount} ticket(s) for this event. " +
                            $"Maximum {cumulativeLimit} allowed per person.");
                    }
                }
            }

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

            // Check if purchaser already has a ticket for ANY of these sessions.
            // SKIP this check when BuyForOthersOnly=true because the purchaser is NOT getting
            // a ticket for themselves - they're buying exclusively for their authorized contacts.
            // Each assignee still gets their own overlap check in the assignee validation below.
            if (!request.BuyForOthersOnly)
            {
                var overlappingAttendance = await _context.EventAttendances
                    .AsNoTracking()
                    .Where(ea =>
                        ea.UserId == userId &&
                        (ea.Status == AttendanceStatus.Active ||
                         ea.Status == AttendanceStatus.PendingPayment ||
                         ea.Status == AttendanceStatus.PendingAcceptance) &&
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
            }
            else
            {
                _logger.LogInformation(
                    "BuyForOthersOnly=true: Skipping purchaser overlap check for user {UserId} in event {EventId}",
                    userId, request.EventId);
            }

            // ============================================================================
            // VALIDATE ASSIGNEES (BR-012, BR-020, BR-035 / AD-014)
            // ============================================================================
            var allAssigneeIds = selections
                .Where(s => s.Assignees != null)
                .SelectMany(s => s.Assignees!)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (allAssigneeIds.Count > 0)
            {
                // Load assignee users for vetting checks
                var assigneeUsers = await _context.Users
                    .AsNoTracking()
                    .Where(u => allAssigneeIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, cancellationToken);

                foreach (var assigneeId in allAssigneeIds)
                {
                    // Validate assignee exists
                    if (!assigneeUsers.ContainsKey(assigneeId))
                    {
                        return Result<ParticipationStatusDto>.Failure(
                            $"Assigned user not found: {assigneeId}");
                    }

                    // BR-020: Check authorization (purchaser must be authorized delegate for each assignee/principal)
                    var isAuthorized = await _authorizedContactService.IsAuthorizedDelegateAsync(
                        assigneeId, userId, cancellationToken);
                    if (!isAuthorized)
                    {
                        var assigneeUser = assigneeUsers[assigneeId];
                        return Result<ParticipationStatusDto>.Failure(
                            $"You are not authorized to purchase tickets for '{assigneeUser.SceneName}'. They must add you as an authorized contact first.");
                    }

                    // BR-035 / AD-014: Vetting check at assignment time for VettedMembersOnly events
                    if (eventEntity.VettedMembersOnly)
                    {
                        var assigneeUser = assigneeUsers[assigneeId];
                        if (!assigneeUser.IsVetted)
                        {
                            return Result<ParticipationStatusDto>.Failure(
                                $"'{assigneeUser.SceneName}' is not a vetted member. This event is limited to vetted members only.");
                        }
                    }

                    // BR-012: Check if assignee already has a ticket for overlapping sessions
                    var assigneeOverlap = await _context.EventAttendances
                        .AsNoTracking()
                        .Where(ea =>
                            ea.UserId == assigneeId &&
                            (ea.Status == AttendanceStatus.Active ||
                             ea.Status == AttendanceStatus.PendingPayment ||
                             ea.Status == AttendanceStatus.PendingAcceptance) &&
                            ea.AttendanceType == AttendanceType.Ticket &&
                            ea.SessionId.HasValue &&
                            allRequestedSessionIds.Contains(ea.SessionId.Value))
                        .FirstOrDefaultAsync(cancellationToken);

                    if (assigneeOverlap != null)
                    {
                        var assigneeUser = assigneeUsers[assigneeId];
                        return Result<ParticipationStatusDto>.Failure(
                            $"'{assigneeUser.SceneName}' already has a ticket for this event or overlapping session.");
                    }
                }
            }

            // ============================================================================
            // CAPACITY CHECK: Account for total quantity across all selections (BR-013)
            // ============================================================================
            var currentAttendanceCount = await _countService.GetReservedCountAsync(request.EventId, cancellationToken);

            if (currentAttendanceCount + totalNewTickets > eventEntity.Capacity)
            {
                return Result<ParticipationStatusDto>.Failure(
                    totalNewTickets > 1
                        ? $"Not enough capacity for {totalNewTickets} tickets. Only {eventEntity.Capacity - currentAttendanceCount} spot(s) remaining."
                        : "Event is at full capacity");
            }

            // ============================================================================
            // CALCULATE PER-TICKET PRICE
            // The checkout Amount is the TOTAL for the entire purchase.
            // Split evenly across all tickets (AD-012: uniform sliding scale).
            // ============================================================================
            var perTicketPrice = totalNewTickets > 0 && request.Amount.HasValue
                ? request.Amount.Value / totalNewTickets
                : 0m;

            // ============================================================================
            // PROCESS ALL TICKET SELECTIONS IN A SINGLE TRANSACTION
            // Creates N TicketPurchase records based on quantities, with assignment info.
            // ============================================================================
            var allAttendances = new List<EventAttendance>();
            var ticketPurchases = new List<TicketPurchase>();

            foreach (var selection in selections)
            {
                var ticketType = ticketTypesWithSessions.First(tt => tt.Id == selection.TicketTypeId);
                var sessionIds = ticketType.Sessions.Select(s => s.Id).ToList();

                for (var ticketIndex = 0; ticketIndex < selection.Quantity; ticketIndex++)
                {
                    // Determine who this ticket is for:
                    // Normal mode: Index 0 = purchaser's own ticket, Index 1+ = assignees
                    // BuyForOthersOnly mode: ALL indexes are for assignees (purchaser gets nothing)
                    var isForPurchaser = !request.BuyForOthersOnly && ticketIndex == 0;
                    Guid? assigneeId = null;

                    if (!isForPurchaser && selection.Assignees != null)
                    {
                        // In BuyForOthersOnly mode, assignee index matches ticket index directly
                        // In normal mode, assignee index is offset by 1 (ticket 0 = purchaser)
                        var assigneeIndex = request.BuyForOthersOnly ? ticketIndex : ticketIndex - 1;
                        if (assigneeIndex >= 0 && assigneeIndex < selection.Assignees.Count)
                        {
                            assigneeId = selection.Assignees[assigneeIndex];
                        }
                    }

                    // Determine the attendee UserId for the EventAttendance records
                    var attendeeUserId = assigneeId ?? userId;
                    var isAssigned = assigneeId.HasValue && assigneeId.Value != userId;

                    // Create TicketPurchase record for this individual ticket.
                    // Each ticket gets its own TicketPurchase so it can be independently
                    // managed (refunded, assigned, etc.)
                    var ticketPurchase = new TicketPurchase
                    {
                        Id = Guid.NewGuid(),
                        TicketTypeId = ticketType.Id,
                        UserId = userId, // Always the purchaser
                        PurchasedForUserId = isAssigned ? assigneeId : null,
                        Quantity = 1,
                        TotalPrice = perTicketPrice > 0 ? perTicketPrice : (ticketType.Price ?? 0m),
                        SlidingScalePercentage = request.SlidingScalePercentage,
                        PaymentStatus = TicketPurchasePaymentStatus.Pending,
                        PaymentMethod = request.PaymentMethodId ?? "Unknown",
                        PaymentReference = $"WCR-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                        Notes = request.Notes ?? $"Ticket purchase - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                        EventWaiverAccepted = isForPurchaser ? request.EventWaiverAccepted : false,
                        EventWaiverAcceptedAt = isForPurchaser ? DateTime.UtcNow : null,
                        PurchaseDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.TicketPurchases.Add(ticketPurchase);
                    ticketPurchases.Add(ticketPurchase);

                    // Create EventAttendance records for each session in this ticket type.
                    // IMPORTANT: "Assign later" tickets (unassigned extras) do NOT get EventAttendance
                    // records yet because the purchaser already has attendance for these sessions via
                    // their own ticket (index 0). EventAttendance is created when the ticket is
                    // assigned to someone else via the dashboard (UC-007 post-purchase assignment).
                    // The TicketPurchase record alone tracks the unassigned ticket's existence.
                    var isUnassignedExtra = !isForPurchaser && !isAssigned;

                    if (!isUnassignedExtra)
                    {
                        foreach (var session in ticketType.Sessions)
                        {
                            var attendance = new EventAttendance(request.EventId, attendeeUserId, AttendanceType.Ticket)
                            {
                                SessionId = session.Id,
                                TicketPurchaseId = ticketPurchase.Id,
                                Status = AttendanceStatus.PendingPayment,
                                Notes = request.Notes,
                                CreatedBy = userId
                            };

                            if (isForPurchaser)
                            {
                                // Purchaser's own ticket: waiver accepted at checkout (BR-033)
                                attendance.EventWaiverAccepted = true;
                                attendance.EventWaiverAcceptedAt = DateTime.UtcNow;
                            }
                            else
                            {
                                // Assigned ticket: waiver NOT accepted (BR-030, BR-033)
                                // Assignee must accept waiver themselves
                                attendance.EventWaiverAccepted = false;
                                attendance.EventWaiverAcceptedAt = null;
                                attendance.AssignedByUserId = userId;
                                attendance.AssignedAt = DateTime.UtcNow;
                            }

                            allAttendances.Add(attendance);
                            _context.EventAttendances.Add(attendance);
                        }
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Ticket index {TicketIndex} for '{TicketTypeName}' is unassigned (assign later). " +
                            "TicketPurchase {TicketPurchaseId} created but no EventAttendance yet - " +
                            "will be created when assigned via dashboard (UC-007).",
                            ticketIndex, ticketType.Name, ticketPurchase.Id);
                    }

                    // Create audit history.
                    // For unassigned extras, no EventAttendance exists yet (created on assignment).
                    // Skip the attendance-based audit for these — the TicketPurchase log above is sufficient.
                    if (isUnassignedExtra)
                    {
                        _logger.LogInformation(
                            "Prepared TicketPurchase {TicketPurchaseId} for unassigned '{TicketTypeName}' " +
                            "(ticket {TicketIndex}/{TotalQuantity}, will be assigned later)",
                            ticketPurchase.Id, ticketType.Name, ticketIndex + 1, selection.Quantity);
                        continue; // Skip to next ticket — no attendance to audit
                    }

                    var primaryAttendanceForTicket = allAttendances.LastOrDefault();
                    if (primaryAttendanceForTicket == null)
                    {
                        _logger.LogError(
                            "No attendance records created for ticket type {TicketTypeId} '{TicketTypeName}' " +
                            "ticket index {TicketIndex} for user {UserId} in event {EventId}. Sessions loaded: {SessionCount}",
                            ticketType.Id, ticketType.Name, ticketIndex, userId, request.EventId, ticketType.Sessions.Count);
                        return Result<ParticipationStatusDto>.Failure(
                            $"Failed to create attendance records for ticket '{ticketType.Name}'. " +
                            "The ticket may not be configured correctly. Please contact support.");
                    }

                    var changeReason = isAssigned
                        ? $"Ticket '{ticketType.Name}' purchased by user and assigned to {attendeeUserId}"
                        : isForPurchaser
                            ? $"Ticket '{ticketType.Name}' purchased by user"
                            : $"Extra ticket '{ticketType.Name}' purchased (unassigned)";

                    if (sessionIds.Count > 1)
                    {
                        changeReason = $"Multi-session {changeReason} ({sessionIds.Count} sessions)";
                    }

                    var history = new AttendanceHistory(primaryAttendanceForTicket.Id, "Created")
                    {
                        NewValues = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            EventId = primaryAttendanceForTicket.EventId,
                            UserId = primaryAttendanceForTicket.UserId,
                            AttendanceType = primaryAttendanceForTicket.AttendanceType,
                            TicketTypeName = ticketType.Name,
                            SessionIds = sessionIds,
                            SessionCount = sessionIds.Count,
                            Notes = primaryAttendanceForTicket.Notes,
                            PaymentMethodId = request.PaymentMethodId,
                            IsAssigned = isAssigned,
                            AssignedByUserId = isAssigned ? (Guid?)userId : null,
                            AssignedToUserId = isAssigned ? assigneeId : null,
                            TicketIndex = ticketIndex,
                            TotalInPurchase = selection.Quantity
                        }),
                        ChangedBy = userId,
                        ChangeReason = changeReason
                    };

                    _context.AttendanceHistory.Add(history);

                    _logger.LogInformation(
                        "Prepared TicketPurchase {TicketPurchaseId} for ticket type '{TicketTypeName}' " +
                        "(ticket {TicketIndex}/{TotalQuantity}, assigned={IsAssigned}, attendee={AttendeeUserId}, {SessionCount} sessions)",
                        ticketPurchase.Id, ticketType.Name, ticketIndex + 1, selection.Quantity,
                        isAssigned, attendeeUserId, sessionIds.Count);
                }
            }

            // EventAttendee (check-in system) creation is deferred to ActivateAttendanceForPurchasesAsync
            // which is called after payment is confirmed. This prevents unpaid users from appearing in check-in.

            // Auto-RSVP for events that allow RSVPs - ONLY for the purchaser's own tickets.
            // Assigned tickets get their auto-RSVP when the assignee accepts (handled by TicketAssignmentService).
            // BuyForOthersOnly: Skip auto-RSVP because the purchaser is NOT attending - they're buying for others.
            if (eventEntity.AllowRsvps && !request.BuyForOthersOnly)
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

            // Verify persistence.
            // When BuyForOthersOnly with all "assign later" tickets, allAttendances is empty
            // because EventAttendance records are created later during assignment.
            // In that case, TicketPurchase records are sufficient — skip the attendance check.
            var primaryAttendance = allAttendances.FirstOrDefault();
            if (primaryAttendance == null && ticketPurchases.Count == 0)
            {
                _logger.LogError(
                    "No attendance records AND no ticket purchases were created for user {UserId} in event {EventId} " +
                    "despite processing {SelectionCount} selection(s)",
                    userId, request.EventId, selections.Count);
                return Result<ParticipationStatusDto>.Failure(
                    "Failed to create ticket purchase records. Please try again or contact support.");
            }
            // Verify persistence — skip attendance verification for all-unassigned purchases
            // (no EventAttendance exists yet, only TicketPurchase records).
            if (primaryAttendance != null)
            {
                var savedAttendance = await _context.EventAttendances
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ea => ea.Id == primaryAttendance.Id, cancellationToken);

                if (savedAttendance == null)
                {
                    _logger.LogError("CRITICAL: Ticket purchase {AttendanceId} for user {UserId} in event {EventId} failed to persist to database",
                        primaryAttendance.Id, userId, request.EventId);
                    return Result<ParticipationStatusDto>.Infrastructure("Failed to save ticket purchase to database. See server logs for details.");
                }
            }

            _logger.LogInformation(
                "Successfully created and verified {TotalTickets} ticket purchase(s) for user {UserId} in event {EventId} ({AttendanceCount} attendance records, {PurchaseCount} ticket purchases)",
                totalNewTickets, userId, request.EventId, allAttendances.Count, ticketPurchases.Count);

            // Build response DTO. For all-unassigned purchases (no attendance), return
            // a minimal DTO with the event/user info since there's no attendance to reference.
            var dto = primaryAttendance != null
                ? new ParticipationStatusDto
                {
                    EventId = primaryAttendance.EventId,
                    UserId = primaryAttendance.UserId,
                    ParticipationType = primaryAttendance.AttendanceType,
                    Status = primaryAttendance.Status,
                    ParticipationDate = primaryAttendance.CreatedAt,
                    Notes = primaryAttendance.Notes,
                    CanCancel = primaryAttendance.CanBeCancelled(),
                    Metadata = primaryAttendance.Metadata
                }
                : new ParticipationStatusDto
                {
                    EventId = request.EventId,
                    UserId = userId,
                    ParticipationType = AttendanceType.Ticket,
                    Status = AttendanceStatus.PendingPayment,
                    ParticipationDate = DateTime.UtcNow,
                    CanCancel = false
                };

            return Result<ParticipationStatusDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket purchase for user {UserId} in event {EventId}", userId, request.EventId);
            return Result<ParticipationStatusDto>.Infrastructure("Failed to create ticket purchase. See server logs for details.");
        }
    }

    /// <summary>
    /// Cancel user's RSVP for an event (RSVP-only, no refunds or ticket concerns)
    /// </summary>
    public async Task<Result> CancelRsvpAsync(
        Guid eventId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling RSVP for user {UserId} in event {EventId}", userId, eventId);

            // Find the active RSVP attendance
            var attendance = await _context.EventAttendances
                .Where(ea => ea.EventId == eventId &&
                            ea.UserId == userId &&
                            ea.Status == AttendanceStatus.Active &&
                            ea.AttendanceType == AttendanceType.RSVP)
                .OrderByDescending(ea => ea.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (attendance == null)
            {
                return Result.Failure("No active RSVP found for this event");
            }

            if (!attendance.CanBeCancelled())
            {
                return Result.Failure("RSVP cannot be cancelled in its current status");
            }

            // Check if cancellation is still allowed based on event timing rules
            var eventEntity = await _context.Events
                .Include(e => e.Sessions)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result.NotFound("Event not found");
            }

            var isAllowed = await _timeZoneService.IsActionAllowedAsync(
                eventEntity, EventActionType.CancelRsvp, cancellationToken);

            if (!isAllowed)
            {
                _logger.LogWarning("RSVP cancellation attempt for event {EventId} outside allowed timing window", eventId);
                return Result.Failure("Cancellation window is not currently open for this event");
            }

            // Store old values for audit
            var oldValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                Status = attendance.Status,
                CancelledAt = attendance.CancelledAt,
                CancellationReason = attendance.CancellationReason
            });

            // Cancel the RSVP
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
                ChangeReason = reason ?? "RSVP cancelled by user"
            };

            _context.AttendanceHistory.Add(history);

            // Update EventAttendee record if no remaining active attendances
            var remainingActiveAttendances = await _context.EventAttendances
                .Where(ea => ea.EventId == eventId &&
                            ea.UserId == userId &&
                            ea.Status == AttendanceStatus.Active &&
                            ea.Id != attendance.Id)
                .AnyAsync(cancellationToken);

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
            }

            // Save all changes
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully cancelled RSVP {AttendanceId} for user {UserId} in event {EventId}",
                attendance.Id, userId, eventId);

            // Cancel volunteer signups (fire-and-forget - failure must not block cancellation)
            try
            {
                var cancellationResult = await _volunteerAssignmentService.CancelAllVolunteerSignupsForUserEventAsync(
                    userId,
                    eventId,
                    "RSVP cancelled, so automatically canceled volunteer spot",
                    cancellationToken);

                if (cancellationResult.success && cancellationResult.cancelledCount > 0)
                {
                    _logger.LogInformation(
                        "Auto-cancelled {Count} volunteer signups for user {UserId} at event {EventId} due to RSVP cancellation",
                        cancellationResult.cancelledCount, userId, eventId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error auto-cancelling volunteer signups for user {UserId} at event {EventId} (non-fatal)",
                    userId, eventId);
            }

            // Send RSVP cancellation email (fire-and-forget)
            try
            {
                await _eventEmailService.SendRsvpCancellationEmailAsync(
                    userId, eventId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending RSVP cancellation email for user {UserId} at event {EventId} (non-fatal)",
                    userId, eventId);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling RSVP for user {UserId} in event {EventId}", userId, eventId);
            return Result.Infrastructure("Failed to cancel RSVP. See server logs for details.");
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
                return Result.NotFound("Event not found");
            }

            // Find ALL cancellable attendances for these ticket purchases.
            // Includes BOTH the user's own tickets (Active, UserId=current user)
            // AND tickets purchased for others that haven't been accepted yet (PendingAcceptance).
            // CRITICAL: Must include ALL sessions per ticket, not just one attendance.
            var attendancesToCancel = await _context.EventAttendances
                .Where(ea =>
                    ea.EventId == eventId &&
                    ea.TicketPurchaseId.HasValue &&
                    ticketPurchaseIds.Contains(ea.TicketPurchaseId.Value) &&
                    (
                        // User's own active tickets (existing behavior)
                        (ea.UserId == userId && ea.Status == AttendanceStatus.Active) ||
                        // Tickets purchased for others that are pending acceptance (not yet accepted)
                        // The purchaser can cancel these because the assignee hasn't accepted yet.
                        (ea.Status == AttendanceStatus.PendingAcceptance)
                    ))
                .ToListAsync(cancellationToken);

            // Also check for "assign later" tickets: TicketPurchases with NO EventAttendance.
            // These are tickets the purchaser bought but hasn't assigned to anyone yet.
            var purchaseIdsWithAttendance = attendancesToCancel
                .Where(ea => ea.TicketPurchaseId.HasValue)
                .Select(ea => ea.TicketPurchaseId!.Value)
                .Distinct()
                .ToHashSet();

            var unassignedPurchaseIds = ticketPurchaseIds
                .Where(id => !purchaseIdsWithAttendance.Contains(id))
                .ToList();

            // For unassigned tickets, verify they actually have no attendance (truly unassigned)
            if (unassignedPurchaseIds.Count > 0)
            {
                var hasAttendance = await _context.EventAttendances
                    .Where(ea => ea.TicketPurchaseId.HasValue
                        && unassignedPurchaseIds.Contains(ea.TicketPurchaseId.Value)
                        && ea.Status == AttendanceStatus.Active)
                    .Select(ea => ea.TicketPurchaseId!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                // If any "unassigned" ticket actually has an Active attendance owned by someone else,
                // that means it was accepted - block cancellation
                if (hasAttendance.Count > 0)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to cancel accepted tickets: {Ids}",
                        userId, string.Join(", ", hasAttendance));
                    return Result.Failure(
                        "Cannot cancel tickets that have been accepted by the assignee");
                }
            }

            if (attendancesToCancel.Count == 0 && unassignedPurchaseIds.Count == 0)
            {
                return Result.Failure("No cancellable ticket purchases found");
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
                // Check if the user is the ASSIGNEE of these tickets (received from someone else).
                // Assignees can "return" tickets — this reverts ownership to the original purchaser
                // rather than triggering a refund. Like the decline flow but for accepted tickets.
                //
                // We use the already-fetched attendancesToCancel list rather than re-querying,
                // since that query already found active attendances for the user + these purchase IDs.
                var unauthorizedPurchaseIds = unauthorizedPurchases.Select(p => p.Id).ToHashSet();
                var assigneeAttendances = attendancesToCancel
                    .Where(ea =>
                        ea.UserId == userId
                        && ea.Status == AttendanceStatus.Active
                        && ea.AttendanceType == AttendanceType.Ticket
                        && ea.TicketPurchaseId.HasValue
                        && unauthorizedPurchaseIds.Contains(ea.TicketPurchaseId.Value))
                    .ToList();

                // Need to load TicketPurchase nav property for the revert (it wasn't Included in attendancesToCancel)
                if (assigneeAttendances.Count > 0)
                {
                    foreach (var ea in assigneeAttendances)
                    {
                        if (ea.TicketPurchase == null && ea.TicketPurchaseId.HasValue)
                        {
                            ea.TicketPurchase = ticketPurchases.FirstOrDefault(tp => tp.Id == ea.TicketPurchaseId.Value);
                        }
                    }
                }

                // Compare by distinct TicketPurchaseId, not raw attendance count.
                // Multi-session tickets have multiple EventAttendance records per purchase.
                var assigneePurchaseIds = assigneeAttendances
                    .Where(ea => ea.TicketPurchaseId.HasValue)
                    .Select(ea => ea.TicketPurchaseId!.Value)
                    .Distinct()
                    .ToHashSet();

                _logger.LogDebug(
                    "Assignee return check: UserId={UserId}, UnauthorizedPurchases={UnauthorizedCount}, " +
                    "AttendancesToCancel={AttendancesCount}, AssigneeMatchPurchases={AssigneePurchaseCount}",
                    userId, unauthorizedPurchases.Count, attendancesToCancel.Count, assigneePurchaseIds.Count);

                if (assigneePurchaseIds.Count == unauthorizedPurchases.Count)
                {
                    // All "unauthorized" purchases are actually assigned-to-user tickets.
                    // Revert them to the original purchaser instead of cancelling/refunding.
                    _logger.LogInformation(
                        "User {UserId} returning {Count} assigned ticket(s) to original purchaser(s) in event {EventId}",
                        userId, assigneeAttendances.Count, eventId);

                    foreach (var ea in assigneeAttendances)
                    {
                        var originalPurchaserId = ea.TicketPurchase!.UserId;

                        // Cancel the assignee's attendance rather than reverting UserId.
                        // Reverting UserId would violate the unique constraint if the purchaser
                        // already has their own active attendance for the same event+session.
                        // The TicketPurchase record remains owned by the purchaser, so they
                        // can reassign it via the dashboard (it becomes an "assign later" ticket).
                        ea.Status = AttendanceStatus.Cancelled;
                        ea.CancelledAt = DateTime.UtcNow;
                        ea.CancellationReason = reason ?? "Returned by assignee";
                        ea.DeclinedAt = DateTime.UtcNow;
                        ea.UpdatedAt = DateTime.UtcNow;
                        ea.UpdatedBy = userId;

                        // Audit trail
                        var returnHistory = new AttendanceHistory(ea.Id, "TicketDeclined")
                        {
                            ChangedBy = userId,
                            ChangeReason = reason ?? "Ticket returned by assignee after acceptance",
                            OldValues = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                UserId = userId,
                                Status = AttendanceStatus.Active.ToString()
                            }),
                            NewValues = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                Status = AttendanceStatus.Cancelled.ToString(),
                                CancelledAt = ea.CancelledAt,
                                ReturnedByAssignee = true,
                                OriginalPurchaserId = originalPurchaserId
                            })
                        };
                        _context.AttendanceHistory.Add(returnHistory);

                        _logger.LogInformation(
                            "Returned ticket: AttendanceId={AttendanceId}, ReturnedBy={UserId}, PurchaserId={PurchaserId} (ticket available for reassignment)",
                            ea.Id, userId, originalPurchaserId);
                    }

                    // Also cancel any auto-created RSVP for the returning user
                    var rsvpToCancel = await _context.EventAttendances
                        .FirstOrDefaultAsync(ea =>
                            ea.EventId == eventId
                            && ea.UserId == userId
                            && ea.AttendanceType == AttendanceType.RSVP
                            && ea.Status == AttendanceStatus.Active, cancellationToken);

                    if (rsvpToCancel != null)
                    {
                        rsvpToCancel.Status = AttendanceStatus.Cancelled;
                        rsvpToCancel.CancelledAt = DateTime.UtcNow;
                        rsvpToCancel.CancellationReason = "Auto-cancelled: assigned ticket returned";
                        rsvpToCancel.UpdatedAt = DateTime.UtcNow;
                        rsvpToCancel.UpdatedBy = userId;
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    return Result.Success();
                }

                // Some purchases are truly unauthorized (not assignee-owned)
                _logger.LogWarning(
                    "User {UserId} attempted to cancel ticket purchases belonging to other users: {Ids}",
                    userId, string.Join(", ", unauthorizedPurchases.Select(p => p.Id)));
                return Result.Forbidden("Unauthorized: Cannot cancel tickets that don't belong to you");
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
            // BUSINESS RULE: Only cancel RSVP if no active ticket attendances will remain
            // ============================================================================
            var cancellingAttendanceIds = attendancesToCancel.Select(a => a.Id).ToHashSet();
            var remainingTicketAttendances = await _context.EventAttendances
                .Where(ea =>
                    ea.EventId == eventId &&
                    ea.UserId == userId &&
                    ea.Status == AttendanceStatus.Active &&
                    ea.AttendanceType == AttendanceType.Ticket &&
                    !cancellingAttendanceIds.Contains(ea.Id))
                .CountAsync(cancellationToken);

            EventAttendance? associatedRsvp = null;
            if (remainingTicketAttendances == 0)
            {
                associatedRsvp = await _context.EventAttendances
                    .Where(ea =>
                        ea.EventId == eventId &&
                        ea.UserId == userId &&
                        ea.Status == AttendanceStatus.Active &&
                        ea.AttendanceType == AttendanceType.RSVP)
                    .FirstOrDefaultAsync(cancellationToken);

                if (associatedRsvp != null)
                {
                    _logger.LogInformation(
                        "No remaining ticket attendances after cancellation - will also cancel RSVP {RsvpId}",
                        associatedRsvp.Id);
                }
            }
            else
            {
                _logger.LogInformation(
                    "User {UserId} still has {RemainingCount} active ticket attendance(s) for event {EventId} - keeping RSVP",
                    userId, remainingTicketAttendances, eventId);
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
            // HANDLE UNASSIGNED TICKETS (no EventAttendance, just TicketPurchase)
            // These are "assign later" tickets - mark them as refunded/cancelled.
            // ============================================================================
            if (unassignedPurchaseIds.Count > 0)
            {
                var unassignedPurchases = ticketPurchases
                    .Where(tp => unassignedPurchaseIds.Contains(tp.Id))
                    .ToList();

                foreach (var tp in unassignedPurchases)
                {
                    _logger.LogInformation(
                        "Cancelling unassigned ticket purchase {TicketPurchaseId} (no EventAttendance) for user {UserId}",
                        tp.Id, userId);

                    // Process refund for unassigned tickets
                    if (!processedRefunds.Contains(tp.Id))
                    {
                        // For unassigned tickets, we pass a dummy attendanceId since there's no attendance
                        // The refund method uses the TicketPurchaseId internally
                        await ProcessAutomaticRefundAsync(
                            Guid.Empty, // No attendance
                            userId,
                            reason,
                            cancellationToken);
                        processedRefunds.Add(tp.Id);
                    }

                    // Mark TicketPurchase as refunded
                    tp.PaymentStatus = TicketPurchasePaymentStatus.Refunded;
                    tp.UpdatedAt = DateTime.UtcNow;
                    _context.TicketPurchases.Update(tp);
                }

                _logger.LogInformation(
                    "Cancelled {Count} unassigned ticket purchase(s) for user {UserId}",
                    unassignedPurchases.Count, userId);
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

            // ============================================================================
            // SEND CANCELLATION EMAIL
            // ============================================================================
            // Fire-and-forget: sends a single email listing all cancelled sessions
            // grouped by ticket type. Failure must never block the cancellation.
            try
            {
                await _eventEmailService.SendCancellationEmailAsync(
                    userId, eventId, ticketPurchaseIds, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending cancellation email for user {UserId} at event {EventId} (non-fatal)",
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

            // Join with EventAttendees to get check-in status from CheckIns table.
            // Include Active AND Cancelled/Refunded attendances so admins can see
            // cancelled tickets with their refund history. Exclude only PendingPayment
            // and Waitlisted which are transient states not useful in the admin table.
            var attendances = await _context.EventAttendances
                .AsNoTracking()
                .Include(ea => ea.User)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp.TicketType)
                        .ThenInclude(tt => tt.Sessions)
                .Where(ea => ea.EventId == eventId
                    && ea.Status != AttendanceStatus.PendingPayment
                    && ea.Status != AttendanceStatus.Waitlisted)
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
                    // Only active tickets/RSVPs can be cancelled or refunded
                    CanCancel = x.Attendance.Status == AttendanceStatus.Active,
                    Metadata = x.Attendance.Metadata,
                    HasCheckedIn = x.Attendee != null && x.Attendee.CheckIns.Any(),
                    CheckInTime = x.Attendee != null && x.Attendee.CheckIns.Any()
                                  ? x.Attendee.CheckIns.OrderByDescending(c => c.CheckInTime).First().CheckInTime
                                  : (DateTime?)null,
                    TicketTypeName = x.Attendance.TicketPurchase != null
                                     ? x.Attendance.TicketPurchase.TicketType.Name
                                     : null,
                    SessionNames = x.Attendance.TicketPurchase != null && x.Attendance.TicketPurchase.TicketType.Sessions.Any()
                                   ? string.Join(", ", x.Attendance.TicketPurchase.TicketType.Sessions.OrderBy(s => s.StartTime).Select(s => s.Name))
                                   : "No Sessions",
                    AmountPaid = x.Attendance.TicketPurchase != null
                                 ? x.Attendance.TicketPurchase.TotalPrice
                                 : (decimal?)null,
                    TicketId = x.Attendance.TicketPurchaseId,
                    // Return the actual payment method from the ticket purchase record
                    // Values: "PayPal", "authorize-net", "Cash", "Venmo", "Free", etc.
                    PaymentMethod = x.Attendance.TicketPurchase != null
                                    ? x.Attendance.TicketPurchase.PaymentMethod
                                    : null,
                    CheckedInSessions = x.Attendee != null
                                        ? x.Attendee.CheckIns
                                            .Where(c => c.Session != null)
                                            .Select(c => c.Session.Name)
                                            .ToList()
                                        : new List<string>(),
                    // True when the linked TicketPurchase is AwaitingManualRefund — a
                    // credit-card (Authorize.net) ticket was cancelled but no automatic
                    // refund could be issued, so an admin still owes the member a refund.
                    // The roster UI shows this so a cancelled-but-unrefunded ticket is not
                    // mistaken for a fully-settled cancellation. False when there is no
                    // linked purchase (RSVP) or the purchase is in any other status.
                    RefundOwed = x.Attendance.TicketPurchase != null
                                 && x.Attendance.TicketPurchase.PaymentStatus == TicketPurchasePaymentStatus.AwaitingManualRefund
                })
                .ToListAsync(cancellationToken);

            // Second pass: populate refund history for tickets that have a TicketPurchase.
            // Done in-memory to avoid complex nested subqueries in the main LINQ projection.
            var ticketPurchaseIds = attendances
                .Where(a => a.TicketId.HasValue)
                .Select(a => a.TicketId!.Value)
                .Distinct()
                .ToList();

            if (ticketPurchaseIds.Count > 0)
            {
                // Fetch all refunds for all ticket purchases in one query
                var refundsByTicketPurchase = await _context.PaymentRefunds
                    .AsNoTracking()
                    .Include(pr => pr.ProcessedByUser)
                    .Where(pr => ticketPurchaseIds.Contains(pr.TicketPurchaseId))
                    .OrderByDescending(pr => pr.ProcessedAt)
                    .ToListAsync(cancellationToken);

                // Group refunds by TicketPurchaseId for O(1) lookup
                var refundLookup = refundsByTicketPurchase
                    .GroupBy(r => r.TicketPurchaseId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var attendance in attendances)
                {
                    if (attendance.TicketId.HasValue)
                    {
                        if (refundLookup.TryGetValue(attendance.TicketId.Value, out var refunds))
                        {
                            // Build refund history. Done with an async foreach (not a LINQ
                            // .Select projection) because decrypting the Authorize.net refund
                            // transaction id requires an awaitable call into IEncryptionService.
                            var refundHistory = new List<Features.Payments.Models.RefundHistoryDto>();
                            foreach (var r in refunds)
                            {
                                // Decrypt the Authorize.net refund transaction id for the
                                // admin-facing reconciliation column. Null for PayPal/manual
                                // refunds and for historical rows created before the id was
                                // persisted. Admin-only DTO, so decryption here is acceptable.
                                string? authNetRefundTransactionId = null;
                                if (!string.IsNullOrEmpty(r.EncryptedAuthNetRefundTransactionId))
                                {
                                    try
                                    {
                                        authNetRefundTransactionId =
                                            await _encryptionService.DecryptAsync(r.EncryptedAuthNetRefundTransactionId);
                                    }
                                    catch (Exception ex)
                                    {
                                        // A decryption failure for one historical row must not
                                        // break the whole roster. Log and leave the field null.
                                        _logger.LogWarning(ex,
                                            "Failed to decrypt Authorize.net refund transaction id for refund {RefundId}",
                                            r.Id);
                                    }
                                }

                                refundHistory.Add(new Features.Payments.Models.RefundHistoryDto
                                {
                                    Id = r.Id,
                                    Amount = r.RefundAmountValue,
                                    Reason = r.RefundReason,
                                    Status = r.RefundStatus.ToString(),
                                    ProcessedAt = r.ProcessedAt,
                                    ProcessedByName = r.ProcessedByUser?.SceneName ?? r.ProcessedByUser?.Email ?? "System",
                                    AuthNetRefundTransactionId = authNetRefundTransactionId,
                                    WasVoided = r.WasVoided
                                });
                            }
                            attendance.RefundHistory = refundHistory;

                            var completedRefundTotal = refunds
                                .Where(r => r.RefundStatus == Features.Payments.Models.RefundStatus.Completed)
                                .Sum(r => r.RefundAmountValue);

                            attendance.TotalRefunded = completedRefundTotal;
                            attendance.RemainingRefundable = (attendance.AmountPaid ?? 0) - completedRefundTotal;
                        }
                        else
                        {
                            // No refunds yet — full amount is still refundable
                            attendance.RemainingRefundable = attendance.AmountPaid ?? 0;
                        }
                    }
                }
            }

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
    /// Activate attendance records for completed ticket purchases.
    /// Transitions PendingPayment to Active for purchaser's own tickets,
    /// and PendingPayment to PendingAcceptance for assigned tickets.
    /// Also creates/updates EventAttendee records for the check-in system
    /// (only for Active tickets, not PendingAcceptance).
    /// </summary>
    public async Task<Result> ActivateAttendanceForPurchasesAsync(
        List<Guid> ticketPurchaseIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var attendances = await _context.EventAttendances
                .Where(ea => ea.TicketPurchaseId.HasValue
                    && ticketPurchaseIds.Contains(ea.TicketPurchaseId.Value)
                    && ea.Status == AttendanceStatus.PendingPayment)
                .ToListAsync(cancellationToken);

            if (attendances.Count == 0)
            {
                _logger.LogWarning(
                    "No PendingPayment attendances found for ticket purchases [{TicketPurchaseIds}]",
                    string.Join(", ", ticketPurchaseIds));
                return Result.Success();
            }

            // Get all purchaser user IDs from the ticket purchases to determine ownership
            var purchaserLookup = await _context.TicketPurchases
                .AsNoTracking()
                .Where(tp => ticketPurchaseIds.Contains(tp.Id))
                .ToDictionaryAsync(tp => tp.Id, tp => tp.UserId, cancellationToken);

            var activatedCount = 0;
            var pendingAcceptanceCount = 0;

            foreach (var attendance in attendances)
            {
                var purchaserUserId = attendance.TicketPurchaseId.HasValue
                    && purchaserLookup.TryGetValue(attendance.TicketPurchaseId.Value, out var purchaser)
                    ? purchaser
                    : (Guid?)null;

                // Assigned tickets (AssignedByUserId set AND attendee is not the purchaser)
                // go to PendingAcceptance so the assignee can accept waiver + ToS
                if (attendance.AssignedByUserId != null
                    && purchaserUserId.HasValue
                    && attendance.UserId != purchaserUserId.Value)
                {
                    attendance.Status = AttendanceStatus.PendingAcceptance;
                    pendingAcceptanceCount++;
                }
                else
                {
                    attendance.Status = AttendanceStatus.Active;
                    activatedCount++;
                }

                attendance.UpdatedAt = DateTime.UtcNow;
            }

            // Create/update EventAttendee records for the check-in system
            // ONLY for Active tickets (purchaser's own). PendingAcceptance tickets
            // get their EventAttendee created when the assignee accepts.
            var activeAttendances = attendances
                .Where(a => a.Status == AttendanceStatus.Active)
                .ToList();

            var userEventPairs = activeAttendances
                .Select(a => new { a.UserId, a.EventId })
                .Distinct()
                .ToList();

            foreach (var pair in userEventPairs)
            {
                var existingAttendee = await _context.EventAttendees
                    .FirstOrDefaultAsync(ea => ea.EventId == pair.EventId && ea.UserId == pair.UserId, cancellationToken);

                if (existingAttendee == null)
                {
                    var ticketNumber = $"TKT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                    var attendee = new CheckIn.Entities.EventAttendee
                    {
                        Id = Guid.NewGuid(),
                        EventId = pair.EventId,
                        UserId = pair.UserId,
                        TicketNumber = ticketNumber,
                        RegistrationStatus = "confirmed",
                        HasCompletedWaiver = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedBy = pair.UserId
                    };
                    _context.EventAttendees.Add(attendee);
                }
                else if (string.Equals(existingAttendee.RegistrationStatus, "cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    existingAttendee.RegistrationStatus = "confirmed";
                    existingAttendee.HasCompletedWaiver = true;
                    existingAttendee.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Post-payment activation for ticket purchases [{TicketPurchaseIds}]: " +
                "{ActivatedCount} activated, {PendingAcceptanceCount} set to PendingAcceptance (assigned tickets)",
                string.Join(", ", ticketPurchaseIds), activatedCount, pendingAcceptanceCount);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error activating attendances for ticket purchases [{TicketPurchaseIds}]",
                string.Join(", ", ticketPurchaseIds));
            return Result.Failure("Failed to activate attendance records", ex.Message);
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

            // Only the PayPal refund path is automated. Authorize-net and other payment
            // methods cannot be refunded via the self-service cancellation flow (the admin
            // payments UI handles those) — so we flag the purchase as AwaitingManualRefund,
            // which surfaces in the admin dashboard's "Awaiting Manual Refund" filter.
            //
            // History: before 2026-04-12 (M2b, BE-12) this path silently returned and left
            // the purchase reading PaymentStatus = Completed forever. Production row
            // c0c34074-... ($30 owed to Cepheus) was produced exactly this way and sat
            // unaddressed for 5 days until the first health-check caught it.
            if (!ticketPurchase.PaymentMethod.Equals("PayPal", StringComparison.OrdinalIgnoreCase))
            {
                // Only flip the status if it's still in a "completed" family — we don't
                // overwrite an already-Refunded or already-AwaitingManualRefund purchase
                // (idempotent for re-entrant cancellation attempts).
                if (ticketPurchase.PaymentStatus == TicketPurchasePaymentStatus.Completed
                    || ticketPurchase.PaymentStatus == TicketPurchasePaymentStatus.Confirmed)
                {
                    ticketPurchase.PaymentStatus = TicketPurchasePaymentStatus.AwaitingManualRefund;
                    ticketPurchase.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogWarning(
                        "Ticket purchase {TicketId} flagged as AwaitingManualRefund: user cancelled but " +
                        "PaymentMethod={PaymentMethod} is not eligible for automatic refund. " +
                        "Admin must process refund manually via the Admin Payments UI. " +
                        "Amount owed: {Amount:F2} {Currency}",
                        ticketPurchase.Id, ticketPurchase.PaymentMethod,
                        ticketPurchase.TotalPrice, "USD");
                }
                else
                {
                    _logger.LogInformation(
                        "Ticket purchase {TicketId} not eligible for automatic refund (method: {PaymentMethod}, " +
                        "status: {PaymentStatus}) - no status change",
                        ticketPurchase.Id, ticketPurchase.PaymentMethod, ticketPurchase.PaymentStatus);
                }
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
