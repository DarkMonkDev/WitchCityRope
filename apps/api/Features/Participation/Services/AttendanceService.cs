using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Features.Events.Interfaces;
using WitchCityRope.Api.Features.Events;
using WitchCityRope.Api.Features.Payments.ValueObjects;
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

            // Get event details for capacity calculation
            var eventEntity = await _context.Events
                .AsNoTracking()
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

            // Build enhanced DTO with nested structure
            var dto = new EnhancedParticipationStatusDto
            {
                HasRSVP = rsvpAttendance != null,
                HasTicket = ticketAttendance != null,
                CanRSVP = rsvpAttendance == null && activeAttendancesCount < eventEntity.Capacity,
                CanPurchaseTicket = ticketAttendance == null && activeAttendancesCount < eventEntity.Capacity,
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
                // Extract purchase amount from notes JSON (payment details stored in Notes field)
                decimal? amount = null;
                if (!string.IsNullOrWhiteSpace(ticketAttendance.Notes))
                {
                    try
                    {
                        var notesData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(ticketAttendance.Notes);
                        if (notesData != null && notesData.TryGetValue("purchaseAmount", out var amountElement))
                        {
                            amount = amountElement.GetDecimal();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse notes for attendance {AttendanceId}", ticketAttendance.Id);
                    }
                }

                dto.Ticket = new TicketDetailsDto
                {
                    Id = ticketAttendance.Id,
                    Status = ticketAttendance.Status.ToString(),
                    Amount = amount,
                    PaymentStatus = ticketAttendance.Status == AttendanceStatus.Active ? "Completed" :
                                   ticketAttendance.Status == AttendanceStatus.Refunded ? "Refunded" : "Unknown",
                    CreatedAt = ticketAttendance.CreatedAt,
                    CanceledAt = ticketAttendance.CancelledAt,
                    CancelReason = ticketAttendance.CancellationReason,
                    Notes = ticketAttendance.Notes
                };
            }

            _logger.LogInformation(
                "Attendance status for user {UserId} in event {EventId}: HasRSVP={HasRSVP}, HasTicket={HasTicket}, CanRSVP={CanRSVP}, Capacity={Current}/{Total}",
                userId, eventId, dto.HasRSVP, dto.HasTicket, dto.CanRSVP, dto.Capacity.Current, dto.Capacity.Total);

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

            if (eventEntity.EventType != EventType.Social)
            {
                return Result<ParticipationStatusDto>.Failure("RSVPs are only allowed for social events");
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
            else if (existingAttendee.RegistrationStatus == "cancelled")
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
    /// Purchase a ticket for a class event (any authenticated user)
    /// </summary>
    public async Task<Result<ParticipationStatusDto>> CreateTicketPurchaseAsync(
        CreateTicketPurchaseRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating ticket purchase for user {UserId} in event {EventId}", userId, request.EventId);

            // Check if event exists FIRST (need event for timing check)
            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result<ParticipationStatusDto>.Failure("Event not found");
            }

            // Social events support optional ticket purchases in addition to free RSVPs
            // Class events require ticket purchases
            // Both event types can have tickets

            // TIMING VALIDATION FIRST - Check before all other business rules
            // This ensures users get timing errors BEFORE other validation errors
            var isAllowed = await _timeZoneService.IsActionAllowedAsync(eventEntity, EventActionType.GetTicket, cancellationToken);
            if (!isAllowed)
            {
                _logger.LogWarning("Ticket purchase attempt for event {EventId} outside allowed timing window", request.EventId);
                return Result<ParticipationStatusDto>.Failure("Ticket purchase window is not currently open for this event");
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

            // Check if user already has a TICKET for this event
            // Allow ticket purchase even if user has RSVP'd (social events support both)
            var existingTicket = await _context.EventAttendances
                .FirstOrDefaultAsync(ea =>
                    ea.EventId == request.EventId &&
                    ea.UserId == userId &&
                    ea.Status == AttendanceStatus.Active &&
                    ea.AttendanceType == AttendanceType.Ticket,
                    cancellationToken);

            if (existingTicket != null)
            {
                return Result<ParticipationStatusDto>.Failure("User already has a ticket for this event");
            }

            // Check event capacity
            var currentAttendanceCount = await _context.EventAttendances
                .CountAsync(ea => ea.EventId == request.EventId && ea.Status == AttendanceStatus.Active, cancellationToken);

            if (currentAttendanceCount >= eventEntity.Capacity)
            {
                return Result<ParticipationStatusDto>.Failure("Event is at full capacity");
            }

            // Create the ticket purchase with Event Waiver acceptance
            var attendance = new EventAttendance(request.EventId, userId, AttendanceType.Ticket)
            {
                Notes = request.Notes,
                EventWaiverAccepted = true,
                EventWaiverAcceptedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            _context.EventAttendances.Add(attendance);

            // Create or update EventAttendee record so user appears in check-in system
            var ticketNumber = $"TKT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var existingAttendee = await _context.EventAttendees
                .FirstOrDefaultAsync(ea => ea.EventId == request.EventId && ea.UserId == userId, cancellationToken);

            if (existingAttendee == null)
            {
                // Create new attendee record with ticket
                var attendee = new CheckIn.Entities.EventAttendee
                {
                    Id = Guid.NewGuid(),
                    EventId = request.EventId,
                    UserId = userId,
                    TicketNumber = ticketNumber,
                    RegistrationStatus = "confirmed",
                    HasCompletedWaiver = true, // Online ticket purchases imply waiver acceptance through checkout flow
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                _context.EventAttendees.Add(attendee);
            }
            else
            {
                // Update existing attendee with ticket number
                existingAttendee.TicketNumber = ticketNumber;

                // Re-activate if cancelled
                if (existingAttendee.RegistrationStatus == "cancelled")
                {
                    _logger.LogInformation(
                        "Re-activating EventAttendee {AttendeeId} from 'cancelled' to 'confirmed' for user {UserId} purchasing ticket for event {EventId}",
                        existingAttendee.Id, userId, request.EventId);
                    existingAttendee.RegistrationStatus = "confirmed";
                    existingAttendee.HasCompletedWaiver = true; // Re-confirm waiver on ticket purchase
                }

                existingAttendee.UpdatedAt = DateTime.UtcNow;
                _context.EventAttendees.Update(existingAttendee);
            }

            // Create audit history for ticket
            var history = new AttendanceHistory(attendance.Id, "Created")
            {
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    EventId = attendance.EventId,
                    UserId = attendance.UserId,
                    AttendanceType = attendance.AttendanceType,
                    Notes = attendance.Notes,
                    PaymentMethodId = request.PaymentMethodId
                }),
                ChangedBy = userId,
                ChangeReason = "Ticket purchased by user"
            };

            _context.AttendanceHistory.Add(history);

            // ============================================================================
            // BUSINESS RULE: Auto-RSVP for social events when purchasing a ticket
            // ============================================================================
            //
            // CRITICAL: Ticket purchase creates TWO EventAttendances records:
            // 1. Ticket record (already created above)
            // 2. RSVP record (created here for social events)
            //
            // WHY: Users need to be on the attendance roster for check-in, capacity tracking
            // RESULT: User has BOTH active Ticket AND active RSVP simultaneously
            //
            // NOTE: If user already has an RSVP, we don't create another one (no duplicates)
            if (eventEntity.EventType == EventType.Social)
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

                    // Create audit history for auto-RSVP
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

            // CRITICAL: Save changes to persist ticket purchase to database
            await _context.SaveChangesAsync(cancellationToken);

            // Verify persistence (defensive check)
            var savedAttendance = await _context.EventAttendances
                .AsNoTracking()
                .FirstOrDefaultAsync(ea => ea.Id == attendance.Id, cancellationToken);

            if (savedAttendance == null)
            {
                _logger.LogError("CRITICAL: Ticket purchase {AttendanceId} for user {UserId} in event {EventId} failed to persist to database",
                    attendance.Id, userId, request.EventId);
                return Result<ParticipationStatusDto>.Failure("Failed to save ticket purchase to database");
            }

            _logger.LogInformation("Successfully created and verified ticket purchase {AttendanceId} for user {UserId} in event {EventId} (Status: {Status})",
                savedAttendance.Id, userId, request.EventId, savedAttendance.Status);

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
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result.Failure("Event not found");
            }

            // Determine action type based on attendance type (RSVP or Ticket)
            var actionType = attendance.AttendanceType == AttendanceType.Ticket
                ? EventActionType.CancelTicket
                : EventActionType.CancelRsvp;

            var isAllowed = await _timeZoneService.IsActionAllowedAsync(eventEntity, actionType, cancellationToken);
            if (!isAllowed)
            {
                _logger.LogWarning("Cancellation attempt for event {EventId} outside allowed timing window (action: {ActionType})", eventId, actionType);
                return Result.Failure("Cancellation window is not currently open for this event");
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

            // Join with EventAttendees to get check-in status
            // An attendee has checked in if they have a record with RegistrationStatus = 'checked-in' or 'confirmed'
            var attendances = await _context.EventAttendances
                .AsNoTracking()
                .Include(ea => ea.User)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp.TicketType)
                        .ThenInclude(tt => tt.Session)
                .Where(ea => ea.EventId == eventId)
                .GroupJoin(
                    _context.EventAttendees.Where(ea => ea.EventId == eventId),
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
                    // Check-in status: true ONLY if EventAttendees record exists with 'checked-in' status
                    // NOTE: 'confirmed' means they bought tickets/registered, NOT that they checked in
                    HasCheckedIn = x.Attendee != null && x.Attendee.RegistrationStatus == "checked-in",
                    // Check-in time from UpdatedAt when status changed to checked-in
                    CheckInTime = x.Attendee != null && x.Attendee.RegistrationStatus == "checked-in"
                                  ? x.Attendee.UpdatedAt
                                  : (DateTime?)null,
                    // Ticket type name from TicketPurchase navigation (null for RSVPs)
                    TicketTypeName = x.Attendance.TicketPurchase != null
                                     ? x.Attendance.TicketPurchase.TicketType.Name
                                     : null,
                    // Session name from TicketType.Session if available, otherwise "All Sessions"
                    SessionNames = x.Attendance.TicketPurchase != null && x.Attendance.TicketPurchase.TicketType.Session != null
                                   ? x.Attendance.TicketPurchase.TicketType.Session.Name
                                   : "All Sessions",
                    // Amount paid from TicketPurchase.TotalPrice (null for free RSVPs without TicketPurchase)
                    AmountPaid = x.Attendance.TicketPurchase != null
                                 ? x.Attendance.TicketPurchase.TotalPrice
                                 : (decimal?)null,
                    // TicketPurchase ID for refund processing (null for free RSVPs without TicketPurchase)
                    TicketId = x.Attendance.TicketPurchaseId,
                    // Payment method (generic for paid tickets, null for free RSVPs)
                    PaymentMethod = x.Attendance.TicketPurchase != null && x.Attendance.TicketPurchase.TotalPrice > 0
                                    ? "PayPal/Venmo/Cash"
                                    : null
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
                PaymentId = ticketPurchase.Id, // Now references TicketPurchase
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
}
