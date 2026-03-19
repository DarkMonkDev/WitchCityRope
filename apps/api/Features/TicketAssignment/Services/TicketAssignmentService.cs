using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.AuthorizedContacts.Services;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.TicketAssignment.Models;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.TicketAssignment.Services;

/// <summary>
/// Implementation of ITicketAssignmentService for managing ticket assignment workflows.
/// Handles assign, accept, decline, and reassign operations with full business rule enforcement.
///
/// Dependencies:
/// - ApplicationDbContext: Data access for EventAttendance, TicketPurchase, Users
/// - IAuthorizedContactService: Validates delegation authorization (BR-020)
/// - IVettingAccessControlService: Checks vetting status for vetted-only events (BR-035, BR-036)
/// - ILogger: Structured logging with contextual properties
/// </summary>
public class TicketAssignmentService : ITicketAssignmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthorizedContactService _authorizedContactService;
    // VettingAccessControlService injected for future expanded vetting checks.
    // Currently, VettedMembersOnly events use direct IsVetted check (matching AttendanceService pattern).
    private readonly IVettingAccessControlService _vettingAccessControlService;
    private readonly ILogger<TicketAssignmentService> _logger;

    public TicketAssignmentService(
        ApplicationDbContext context,
        IAuthorizedContactService authorizedContactService,
        IVettingAccessControlService vettingAccessControlService,
        ILogger<TicketAssignmentService> logger)
    {
        _context = context;
        _authorizedContactService = authorizedContactService;
        _vettingAccessControlService = vettingAccessControlService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TicketAssignmentDto>> AssignTicketAsync(
        Guid attendanceId, Guid callerUserId, Guid assignToUserId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Assigning ticket: AttendanceId={AttendanceId}, CallerUserId={CallerUserId}, AssignToUserId={AssignToUserId}",
                attendanceId, callerUserId, assignToUserId);

            // 1. Find the EventAttendance with required includes
            var attendance = await _context.EventAttendances
                .Include(ea => ea.Event)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp!.TicketType)
                .FirstOrDefaultAsync(ea => ea.Id == attendanceId, ct);

            if (attendance == null)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Attendance not found",
                    "The specified attendance record does not exist");
            }

            // 2. Verify this is a Ticket type and Status is Active
            if (attendance.AttendanceType != AttendanceType.Ticket)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Invalid attendance type",
                    "Only ticket-type attendance records can be assigned");
            }

            if (attendance.Status != AttendanceStatus.Active)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Ticket not assignable",
                    "Only active tickets can be assigned");
            }

            // 3. Verify the caller owns this ticket
            var isOwner = attendance.UserId == callerUserId
                          || (attendance.TicketPurchase != null && attendance.TicketPurchase.UserId == callerUserId);

            if (!isOwner)
            {
                _logger.LogWarning(
                    "User {CallerUserId} attempted to assign ticket {AttendanceId} they don't own",
                    callerUserId, attendanceId);
                return Result<TicketAssignmentDto>.Failure(
                    "Not authorized to assign",
                    "You can only assign tickets you own");
            }

            // 4. Check authorized delegate: the assignee (principal) must have authorized the caller (delegate)
            // BR-020: Assignment requires authorization
            var isAuthorized = await _authorizedContactService.IsAuthorizedDelegateAsync(
                assignToUserId, callerUserId, ct);

            if (!isAuthorized)
            {
                _logger.LogWarning(
                    "Assignment denied: User {AssignToUserId} has not authorized {CallerUserId} as delegate (BR-020)",
                    assignToUserId, callerUserId);
                return Result<TicketAssignmentDto>.Failure(
                    "Not authorized by assignee",
                    "The assignee has not authorized you as a contact. They must add you as an authorized contact first (BR-020).");
            }

            // 5. If event is VettedMembersOnly: check assignee vetting (BR-035)
            if (attendance.Event.VettedMembersOnly)
            {
                var assigneeUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == assignToUserId, ct);

                if (assigneeUser == null)
                {
                    return Result<TicketAssignmentDto>.Failure(
                        "Assignee not found",
                        "The specified user does not exist");
                }

                if (!assigneeUser.IsVetted)
                {
                    _logger.LogWarning(
                        "Assignment denied: Assignee {AssignToUserId} is not vetted for vetted-only event {EventId} (BR-035)",
                        assignToUserId, attendance.EventId);
                    return Result<TicketAssignmentDto>.Failure(
                        "Assignee not vetted",
                        "This event is limited to vetted members only. The assignee is not currently vetted (BR-035).");
                }
            }

            // 6. Check assignee doesn't already have Active/PendingAcceptance for this event+session (BR-012)
            var hasDuplicate = await _context.EventAttendances
                .AsNoTracking()
                .AnyAsync(ea =>
                    ea.UserId == assignToUserId
                    && ea.EventId == attendance.EventId
                    && ea.SessionId == attendance.SessionId
                    && ea.AttendanceType == AttendanceType.Ticket
                    && (ea.Status == AttendanceStatus.Active || ea.Status == AttendanceStatus.PendingAcceptance)
                    && ea.Id != attendanceId, ct);

            if (hasDuplicate)
            {
                _logger.LogWarning(
                    "Assignment denied: Assignee {AssignToUserId} already has active/pending ticket for event {EventId} (BR-012)",
                    assignToUserId, attendance.EventId);
                return Result<TicketAssignmentDto>.Failure(
                    "Assignee already has ticket",
                    "The assignee already has an active or pending ticket for this event/session (BR-012).");
            }

            // 7. Update EventAttendance
            attendance.UserId = assignToUserId;
            attendance.Status = AttendanceStatus.PendingAcceptance;
            attendance.AssignedByUserId = callerUserId;
            attendance.AssignedAt = DateTime.UtcNow;
            attendance.EventWaiverAccepted = false;
            attendance.EventWaiverAcceptedAt = null;
            attendance.UpdatedAt = DateTime.UtcNow;
            attendance.UpdatedBy = callerUserId;

            // 8. Create AttendanceHistory record
            var history = new AttendanceHistory(attendance.Id, "TicketAssigned")
            {
                ChangedBy = callerUserId,
                ChangeReason = "Ticket assigned to authorized contact",
                NewValues = JsonSerializer.Serialize(new
                {
                    AssignedToUserId = assignToUserId,
                    AssignedByUserId = callerUserId,
                    Status = AttendanceStatus.PendingAcceptance.ToString(),
                    AssignedAt = attendance.AssignedAt
                })
            };
            _context.AttendanceHistory.Add(history);

            // 9. Save changes
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Ticket assigned successfully: AttendanceId={AttendanceId}, AssignedTo={AssignToUserId}, AssignedBy={CallerUserId}, EventId={EventId}",
                attendanceId, assignToUserId, callerUserId, attendance.EventId);

            // 10. Return DTO
            return Result<TicketAssignmentDto>.Success(await BuildAssignmentDtoAsync(attendance, ct));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error assigning ticket: AttendanceId={AttendanceId}, CallerUserId={CallerUserId}, AssignToUserId={AssignToUserId}",
                attendanceId, callerUserId, assignToUserId);
            return Result<TicketAssignmentDto>.Failure(
                "Failed to assign ticket",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TicketAssignmentDto>> AcceptAssignmentAsync(
        Guid attendanceId, Guid callerUserId, AcceptAssignmentRequest request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Accepting assignment: AttendanceId={AttendanceId}, CallerUserId={CallerUserId}",
                attendanceId, callerUserId);

            // 1. Find EventAttendance where Status == PendingAcceptance
            var attendance = await _context.EventAttendances
                .Include(ea => ea.Event)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp!.TicketType)
                .Include(ea => ea.Session)
                .FirstOrDefaultAsync(ea =>
                    ea.Id == attendanceId
                    && ea.Status == AttendanceStatus.PendingAcceptance, ct);

            if (attendance == null)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Assignment not found",
                    "The specified attendance record does not exist or is not pending acceptance");
            }

            // 2. Verify the caller is the assigned user
            if (attendance.UserId != callerUserId)
            {
                _logger.LogWarning(
                    "Accept denied: User {CallerUserId} is not the assignee of attendance {AttendanceId}",
                    callerUserId, attendanceId);
                return Result<TicketAssignmentDto>.Failure(
                    "Not the assigned user",
                    "Only the assigned user can accept this ticket");
            }

            // 2b. TIMING CHECK: Acceptance allowed up to 4 hours after the LAST session starts.
            // Session-based: uses the latest session the ticket covers (via TicketType.Sessions).
            // If no sessions found, falls back to Event.StartDate.
            // Grace period: 4 hours after session start (people may show up and accept at the door).
            var acceptanceCutoff = await GetAcceptanceCutoffAsync(attendance, ct);
            if (acceptanceCutoff.HasValue && DateTime.UtcNow > acceptanceCutoff.Value)
            {
                _logger.LogWarning(
                    "Accept denied: Attendance {AttendanceId} acceptance window expired. " +
                    "Cutoff was {Cutoff}, current time {Now}",
                    attendanceId, acceptanceCutoff.Value, DateTime.UtcNow);
                return Result<TicketAssignmentDto>.Failure(
                    "Acceptance window expired",
                    "The acceptance window for this ticket has closed. Tickets can be accepted up to 4 hours after the last session starts. Please contact an admin for assistance.");
            }

            // 3. Verify waiver acceptance (AD-003)
            if (!request.EventWaiverAccepted)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Waiver not accepted",
                    "You must accept the event waiver to activate this ticket (AD-003)");
            }

            // 4. If event is VettedMembersOnly: re-check vetting (BR-036, AD-014)
            if (attendance.Event.VettedMembersOnly)
            {
                var callerUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == callerUserId, ct);

                if (callerUser == null || !callerUser.IsVetted)
                {
                    _logger.LogWarning(
                        "Accept denied: User {CallerUserId} vetting revoked for vetted-only event {EventId} (BR-036)",
                        callerUserId, attendance.EventId);
                    return Result<TicketAssignmentDto>.Failure(
                        "Vetting required",
                        "This event requires vetted membership. Your vetting status has changed since the ticket was assigned (BR-036).");
                }
            }

            // 5. Update EventAttendance
            attendance.Status = AttendanceStatus.Active;
            attendance.EventWaiverAccepted = true;
            attendance.EventWaiverAcceptedAt = DateTime.UtcNow;
            attendance.AcceptedAt = DateTime.UtcNow;
            attendance.UpdatedAt = DateTime.UtcNow;
            attendance.UpdatedBy = callerUserId;

            // 6. If ToS accepted and user hasn't accepted platform ToS (BR-031)
            if (request.TermsOfServiceAccepted)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == callerUserId, ct);

                if (user != null && !user.TermsOfServiceAccepted)
                {
                    user.TermsOfServiceAccepted = true;
                    user.TermsOfServiceAcceptedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "ToS accepted by user {UserId} during ticket acceptance (BR-031)",
                        callerUserId);
                }
            }

            // 7. Auto-create or auto-activate RSVP for social events.
            // Only applies when accepting a TICKET (not when accepting an RSVP through this endpoint).
            // If a PendingAcceptance proxy RSVP already exists, activate it instead of creating a duplicate.
            // Same base pattern as AttendanceService.CreateTicketPurchaseAsync.
            if (attendance.AttendanceType == AttendanceType.Ticket && attendance.Event.AllowRsvps)
            {
                // Check for any existing RSVP — Active OR PendingAcceptance.
                // PendingAcceptance RSVPs come from proxy RSVP creation (ProxyRsvpService).
                var existingRsvp = await _context.EventAttendances
                    .FirstOrDefaultAsync(ea =>
                        ea.EventId == attendance.EventId
                        && ea.UserId == callerUserId
                        && (ea.Status == AttendanceStatus.Active || ea.Status == AttendanceStatus.PendingAcceptance)
                        && ea.AttendanceType == AttendanceType.RSVP, ct);

                if (existingRsvp == null)
                {
                    // No RSVP exists — create a new Active one (existing behavior)
                    _logger.LogInformation(
                        "Auto-creating RSVP for user {UserId} in social event {EventId} (ticket acceptance)",
                        callerUserId, attendance.EventId);

                    var autoRsvp = new EventAttendance(attendance.EventId, callerUserId, AttendanceType.RSVP)
                    {
                        Notes = "Auto-created RSVP from ticket acceptance",
                        CreatedBy = callerUserId
                    };

                    _context.EventAttendances.Add(autoRsvp);

                    var rsvpHistory = new AttendanceHistory(autoRsvp.Id, "Created")
                    {
                        NewValues = JsonSerializer.Serialize(new
                        {
                            EventId = autoRsvp.EventId,
                            UserId = autoRsvp.UserId,
                            AttendanceType = autoRsvp.AttendanceType.ToString(),
                            Notes = autoRsvp.Notes,
                            AutoCreated = true
                        }),
                        ChangedBy = callerUserId,
                        ChangeReason = "Auto-created RSVP from ticket acceptance"
                    };
                    _context.AttendanceHistory.Add(rsvpHistory);
                }
                else if (existingRsvp.Status == AttendanceStatus.PendingAcceptance)
                {
                    // A proxy RSVP exists in PendingAcceptance — auto-activate it.
                    // The ticket acceptance covers the waiver requirement, so the RSVP
                    // can be promoted to Active without separate acceptance.
                    _logger.LogInformation(
                        "Auto-activating PendingAcceptance RSVP {RsvpId} for user {UserId} in event {EventId} during ticket acceptance",
                        existingRsvp.Id, callerUserId, attendance.EventId);

                    existingRsvp.Status = AttendanceStatus.Active;
                    existingRsvp.EventWaiverAccepted = true;
                    existingRsvp.EventWaiverAcceptedAt = DateTime.UtcNow;
                    existingRsvp.AcceptedAt = DateTime.UtcNow;
                    existingRsvp.UpdatedAt = DateTime.UtcNow;
                    existingRsvp.UpdatedBy = callerUserId;

                    var rsvpHistory = new AttendanceHistory(existingRsvp.Id, "ProxyRSVPAccepted")
                    {
                        OldValues = JsonSerializer.Serialize(new
                        {
                            Status = AttendanceStatus.PendingAcceptance.ToString(),
                            EventWaiverAccepted = false
                        }),
                        NewValues = JsonSerializer.Serialize(new
                        {
                            Status = AttendanceStatus.Active.ToString(),
                            EventWaiverAccepted = true,
                            AcceptedAt = existingRsvp.AcceptedAt,
                            AutoAcceptedDuringTicketAcceptance = true
                        }),
                        ChangedBy = callerUserId,
                        ChangeReason = "Auto-accepted proxy RSVP during ticket acceptance"
                    };
                    _context.AttendanceHistory.Add(rsvpHistory);
                }
                // else: Active RSVP already exists — nothing to do
            }

            // 8. Create or update EventAttendee record (for check-in system)
            var existingAttendee = await _context.EventAttendees
                .FirstOrDefaultAsync(ea =>
                    ea.EventId == attendance.EventId
                    && ea.UserId == callerUserId, ct);

            if (existingAttendee == null)
            {
                var ticketNumber = $"TKT-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                var attendee = new CheckIn.Entities.EventAttendee
                {
                    Id = Guid.NewGuid(),
                    EventId = attendance.EventId,
                    UserId = callerUserId,
                    TicketNumber = ticketNumber,
                    RegistrationStatus = "confirmed",
                    HasCompletedWaiver = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = callerUserId
                };
                _context.EventAttendees.Add(attendee);
            }
            else if (string.Equals(existingAttendee.RegistrationStatus, "cancelled", StringComparison.OrdinalIgnoreCase))
            {
                // Re-activate cancelled attendee
                existingAttendee.RegistrationStatus = "confirmed";
                existingAttendee.HasCompletedWaiver = true;
                existingAttendee.UpdatedAt = DateTime.UtcNow;
                _context.EventAttendees.Update(existingAttendee);
            }

            // 9. Create AttendanceHistory "TicketAccepted"
            var history = new AttendanceHistory(attendance.Id, "TicketAccepted")
            {
                ChangedBy = callerUserId,
                ChangeReason = "Assigned ticket accepted by recipient",
                OldValues = JsonSerializer.Serialize(new
                {
                    Status = AttendanceStatus.PendingAcceptance.ToString()
                }),
                NewValues = JsonSerializer.Serialize(new
                {
                    Status = AttendanceStatus.Active.ToString(),
                    EventWaiverAccepted = true,
                    AcceptedAt = attendance.AcceptedAt
                })
            };
            _context.AttendanceHistory.Add(history);

            // 10. Save changes
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Assignment accepted: AttendanceId={AttendanceId}, AcceptedBy={CallerUserId}, EventId={EventId}",
                attendanceId, callerUserId, attendance.EventId);

            // 11. Return DTO
            return Result<TicketAssignmentDto>.Success(await BuildAssignmentDtoAsync(attendance, ct));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error accepting assignment: AttendanceId={AttendanceId}, CallerUserId={CallerUserId}",
                attendanceId, callerUserId);
            return Result<TicketAssignmentDto>.Failure(
                "Failed to accept assignment",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TicketAssignmentDto>> DeclineAssignmentAsync(
        Guid attendanceId, Guid callerUserId, string? reason, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Declining assignment: AttendanceId={AttendanceId}, CallerUserId={CallerUserId}",
                attendanceId, callerUserId);

            // 1. Find EventAttendance where Status == PendingAcceptance
            var attendance = await _context.EventAttendances
                .Include(ea => ea.Event)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp!.TicketType)
                .FirstOrDefaultAsync(ea =>
                    ea.Id == attendanceId
                    && ea.Status == AttendanceStatus.PendingAcceptance, ct);

            if (attendance == null)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Assignment not found",
                    "The specified attendance record does not exist or is not pending acceptance");
            }

            // 2. Verify the caller is the assigned user
            if (attendance.UserId != callerUserId)
            {
                _logger.LogWarning(
                    "Decline denied: User {CallerUserId} is not the assignee of attendance {AttendanceId}",
                    callerUserId, attendanceId);
                return Result<TicketAssignmentDto>.Failure(
                    "Not the assigned user",
                    "Only the assigned user can decline this ticket");
            }

            // 3. Type-specific decline handling.
            // RSVPs are cancelled when declined (matching ProxyRsvpService.DeclineProxyRsvpAsync).
            // Tickets are reverted to the original purchaser as Active (AD-015).
            if (attendance.AttendanceType == AttendanceType.RSVP)
            {
                // RSVP decline: cancel the RSVP (no "revert to purchaser" concept for RSVPs)
                attendance.Status = AttendanceStatus.Cancelled;
                attendance.CancelledAt = DateTime.UtcNow;
                attendance.CancellationReason = reason ?? "Declined by recipient";
                attendance.DeclinedAt = DateTime.UtcNow;
                attendance.UpdatedAt = DateTime.UtcNow;
                attendance.UpdatedBy = callerUserId;

                var rsvpDeclineHistory = new AttendanceHistory(attendance.Id, "ProxyRSVPDeclined")
                {
                    ChangedBy = callerUserId,
                    ChangeReason = reason ?? "Proxy RSVP declined by recipient",
                    OldValues = JsonSerializer.Serialize(new
                    {
                        UserId = attendance.UserId,
                        Status = AttendanceStatus.PendingAcceptance.ToString()
                    }),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        Status = AttendanceStatus.Cancelled.ToString(),
                        DeclinedAt = attendance.DeclinedAt,
                        CancellationReason = attendance.CancellationReason
                    })
                };
                _context.AttendanceHistory.Add(rsvpDeclineHistory);

                await _context.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "RSVP declined and cancelled: AttendanceId={AttendanceId}, DeclinedBy={CallerUserId}, EventId={EventId}",
                    attendanceId, callerUserId, attendance.EventId);

                return Result<TicketAssignmentDto>.Success(await BuildAssignmentDtoAsync(attendance, ct));
            }

            // Ticket decline: revert to original purchaser as Active (AD-015)
            var originalPurchaserId = attendance.TicketPurchase?.UserId ?? callerUserId;

            // Store old values for history
            var oldUserId = attendance.UserId;

            // 4. Update EventAttendance (AD-015: revert to purchaser as Active)
            attendance.UserId = originalPurchaserId;
            attendance.Status = AttendanceStatus.Active;
            attendance.DeclinedAt = DateTime.UtcNow;
            attendance.DeclinedReason = reason;
            // Keep AssignedByUserId and AssignedAt for audit trail
            attendance.UpdatedAt = DateTime.UtcNow;
            attendance.UpdatedBy = callerUserId;

            // 5. Also cancel any orphaned PendingAcceptance RSVP for the same event+user.
            // This prevents stale proxy RSVPs from lingering after the user declines the ticket.
            var orphanedRsvp = await _context.EventAttendances
                .FirstOrDefaultAsync(ea =>
                    ea.EventId == attendance.EventId
                    && ea.UserId == callerUserId
                    && ea.AttendanceType == AttendanceType.RSVP
                    && ea.Status == AttendanceStatus.PendingAcceptance, ct);

            if (orphanedRsvp != null)
            {
                orphanedRsvp.Status = AttendanceStatus.Cancelled;
                orphanedRsvp.CancelledAt = DateTime.UtcNow;
                orphanedRsvp.CancellationReason = "Auto-cancelled: associated ticket was declined";
                orphanedRsvp.UpdatedAt = DateTime.UtcNow;
                orphanedRsvp.UpdatedBy = callerUserId;

                var orphanHistory = new AttendanceHistory(orphanedRsvp.Id, "Cancelled")
                {
                    ChangedBy = callerUserId,
                    ChangeReason = "Auto-cancelled proxy RSVP when associated ticket was declined",
                    OldValues = JsonSerializer.Serialize(new
                    {
                        Status = AttendanceStatus.PendingAcceptance.ToString()
                    }),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        Status = AttendanceStatus.Cancelled.ToString(),
                        CancellationReason = orphanedRsvp.CancellationReason
                    })
                };
                _context.AttendanceHistory.Add(orphanHistory);

                _logger.LogInformation(
                    "Auto-cancelled orphaned RSVP {RsvpId} for user {UserId} in event {EventId} during ticket decline",
                    orphanedRsvp.Id, callerUserId, attendance.EventId);
            }

            // 6. Create AttendanceHistory "TicketDeclined"
            var history = new AttendanceHistory(attendance.Id, "TicketDeclined")
            {
                ChangedBy = callerUserId,
                ChangeReason = reason ?? "Ticket declined by assignee",
                OldValues = JsonSerializer.Serialize(new
                {
                    UserId = oldUserId,
                    Status = AttendanceStatus.PendingAcceptance.ToString()
                }),
                NewValues = JsonSerializer.Serialize(new
                {
                    UserId = originalPurchaserId,
                    Status = AttendanceStatus.Active.ToString(),
                    DeclinedAt = attendance.DeclinedAt,
                    DeclinedReason = reason
                })
            };
            _context.AttendanceHistory.Add(history);

            // 7. Save changes
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Assignment declined: AttendanceId={AttendanceId}, DeclinedBy={CallerUserId}, RevertedTo={OriginalPurchaserId}, EventId={EventId}",
                attendanceId, callerUserId, originalPurchaserId, attendance.EventId);

            // 8. Return DTO
            return Result<TicketAssignmentDto>.Success(await BuildAssignmentDtoAsync(attendance, ct));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error declining assignment: AttendanceId={AttendanceId}, CallerUserId={CallerUserId}",
                attendanceId, callerUserId);
            return Result<TicketAssignmentDto>.Failure(
                "Failed to decline assignment",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TicketAssignmentDto>> ReassignTicketAsync(
        Guid attendanceId, Guid callerUserId, Guid newAssigneeUserId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Reassigning ticket: AttendanceId={AttendanceId}, CallerUserId={CallerUserId}, NewAssigneeUserId={NewAssigneeUserId}",
                attendanceId, callerUserId, newAssigneeUserId);

            // 1. Find EventAttendance where Status=Active AND DeclinedAt IS NOT NULL
            var attendance = await _context.EventAttendances
                .Include(ea => ea.Event)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp!.TicketType)
                .FirstOrDefaultAsync(ea =>
                    ea.Id == attendanceId
                    && ea.Status == AttendanceStatus.Active
                    && ea.DeclinedAt != null, ct);

            if (attendance == null)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Ticket not eligible for reassignment",
                    "The ticket must be in Active status with a previous decline to be reassigned");
            }

            // 2. Verify caller is the original purchaser
            if (attendance.TicketPurchase == null || attendance.TicketPurchase.UserId != callerUserId)
            {
                _logger.LogWarning(
                    "Reassign denied: User {CallerUserId} is not the original purchaser of ticket {AttendanceId}",
                    callerUserId, attendanceId);
                return Result<TicketAssignmentDto>.Failure(
                    "Not the original purchaser",
                    "Only the original ticket purchaser can reassign a declined ticket");
            }

            // 3. Check authorized delegate (same as AssignTicketAsync)
            var isAuthorized = await _authorizedContactService.IsAuthorizedDelegateAsync(
                newAssigneeUserId, callerUserId, ct);

            if (!isAuthorized)
            {
                _logger.LogWarning(
                    "Reassignment denied: User {NewAssigneeUserId} has not authorized {CallerUserId} as delegate (BR-020)",
                    newAssigneeUserId, callerUserId);
                return Result<TicketAssignmentDto>.Failure(
                    "Not authorized by assignee",
                    "The new assignee has not authorized you as a contact (BR-020).");
            }

            // 4. If event is VettedMembersOnly: check new assignee vetting (BR-035)
            if (attendance.Event.VettedMembersOnly)
            {
                var assigneeUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == newAssigneeUserId, ct);

                if (assigneeUser == null)
                {
                    return Result<TicketAssignmentDto>.Failure(
                        "Assignee not found",
                        "The specified user does not exist");
                }

                if (!assigneeUser.IsVetted)
                {
                    _logger.LogWarning(
                        "Reassignment denied: Assignee {NewAssigneeUserId} is not vetted for vetted-only event {EventId} (BR-035)",
                        newAssigneeUserId, attendance.EventId);
                    return Result<TicketAssignmentDto>.Failure(
                        "Assignee not vetted",
                        "This event is limited to vetted members only. The assignee is not currently vetted (BR-035).");
                }
            }

            // 5. Check duplicate attendance (BR-012)
            var hasDuplicate = await _context.EventAttendances
                .AsNoTracking()
                .AnyAsync(ea =>
                    ea.UserId == newAssigneeUserId
                    && ea.EventId == attendance.EventId
                    && ea.SessionId == attendance.SessionId
                    && ea.AttendanceType == AttendanceType.Ticket
                    && (ea.Status == AttendanceStatus.Active || ea.Status == AttendanceStatus.PendingAcceptance)
                    && ea.Id != attendanceId, ct);

            if (hasDuplicate)
            {
                _logger.LogWarning(
                    "Reassignment denied: Assignee {NewAssigneeUserId} already has active/pending ticket for event {EventId} (BR-012)",
                    newAssigneeUserId, attendance.EventId);
                return Result<TicketAssignmentDto>.Failure(
                    "Assignee already has ticket",
                    "The assignee already has an active or pending ticket for this event/session (BR-012).");
            }

            // 6. Update EventAttendance
            attendance.UserId = newAssigneeUserId;
            attendance.Status = AttendanceStatus.PendingAcceptance;
            attendance.AssignedByUserId = callerUserId;
            attendance.AssignedAt = DateTime.UtcNow;
            attendance.DeclinedAt = null;
            attendance.DeclinedReason = null;
            attendance.EventWaiverAccepted = false;
            attendance.EventWaiverAcceptedAt = null;
            attendance.AcceptedAt = null;
            attendance.ReminderSentAt = null;
            attendance.UpdatedAt = DateTime.UtcNow;
            attendance.UpdatedBy = callerUserId;

            // 7. Create AttendanceHistory "TicketReassigned"
            var history = new AttendanceHistory(attendance.Id, "TicketReassigned")
            {
                ChangedBy = callerUserId,
                ChangeReason = "Ticket reassigned to new authorized contact after decline",
                NewValues = JsonSerializer.Serialize(new
                {
                    NewAssigneeUserId = newAssigneeUserId,
                    AssignedByUserId = callerUserId,
                    Status = AttendanceStatus.PendingAcceptance.ToString(),
                    AssignedAt = attendance.AssignedAt
                })
            };
            _context.AttendanceHistory.Add(history);

            // 8. Save changes
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Ticket reassigned: AttendanceId={AttendanceId}, NewAssignee={NewAssigneeUserId}, ReassignedBy={CallerUserId}, EventId={EventId}",
                attendanceId, newAssigneeUserId, callerUserId, attendance.EventId);

            // 9. Return DTO
            return Result<TicketAssignmentDto>.Success(await BuildAssignmentDtoAsync(attendance, ct));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error reassigning ticket: AttendanceId={AttendanceId}, CallerUserId={CallerUserId}, NewAssigneeUserId={NewAssigneeUserId}",
                attendanceId, callerUserId, newAssigneeUserId);
            return Result<TicketAssignmentDto>.Failure(
                "Failed to reassign ticket",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TicketAssignmentDto>> AssignUnassignedTicketAsync(
        Guid ticketPurchaseId, Guid callerUserId, Guid assignToUserId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Assigning unassigned ticket: TicketPurchaseId={TicketPurchaseId}, CallerUserId={CallerUserId}, AssignToUserId={AssignToUserId}",
                ticketPurchaseId, callerUserId, assignToUserId);

            // 1. Load the TicketPurchase with TicketType and Sessions
            var ticketPurchase = await _context.TicketPurchases
                .Include(tp => tp.TicketType)
                    .ThenInclude(tt => tt!.Sessions)
                .Include(tp => tp.TicketType)
                    .ThenInclude(tt => tt!.Event)
                .FirstOrDefaultAsync(tp => tp.Id == ticketPurchaseId, ct);

            if (ticketPurchase == null)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Ticket not found",
                    "The specified ticket purchase does not exist");
            }

            // 2. Verify the caller is the purchaser
            if (ticketPurchase.UserId != callerUserId)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Not authorized",
                    "You can only assign tickets you purchased");
            }

            // 3. Verify no EventAttendance exists yet (truly unassigned)
            var hasAttendance = await _context.EventAttendances
                .AnyAsync(ea => ea.TicketPurchaseId == ticketPurchaseId, ct);

            if (hasAttendance)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Ticket already assigned",
                    "This ticket already has an attendance record. Use the standard assign endpoint.");
            }

            // 4. Check authorized delegate (BR-020)
            var isAuthorized = await _authorizedContactService.IsAuthorizedDelegateAsync(
                assignToUserId, callerUserId, ct);

            if (!isAuthorized)
            {
                _logger.LogWarning(
                    "User {CallerUserId} attempted to assign ticket {TicketPurchaseId} to {AssignToUserId} without authorization",
                    callerUserId, ticketPurchaseId, assignToUserId);
                return Result<TicketAssignmentDto>.Failure(
                    "Not authorized by contact",
                    "The target user has not authorized you to assign tickets on their behalf");
            }

            // 5. Validate event and ticket type are loaded
            var eventEntity = ticketPurchase.TicketType?.Event;
            if (eventEntity == null || ticketPurchase.TicketType == null)
            {
                _logger.LogWarning(
                    "TicketPurchase {TicketPurchaseId} has null TicketType or Event - data integrity issue",
                    ticketPurchaseId);
                return Result<TicketAssignmentDto>.Failure(
                    "Ticket configuration error",
                    "Ticket type or event data is missing. Please contact support.");
            }

            // 6. Check vetting for VettedMembersOnly events (BR-035)
            // Load assignee user once - reused for vetting check and DTO (avoid duplicate query)
            var assigneeUser = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == assignToUserId, ct);

            if (assigneeUser == null)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Assignee not found",
                    "The target user does not exist");
            }

            if (eventEntity.VettedMembersOnly && !assigneeUser.IsVetted)
            {
                _logger.LogWarning(
                    "User {CallerUserId} attempted to assign ticket {TicketPurchaseId} to non-vetted user {AssignToUserId} for VettedMembersOnly event",
                    callerUserId, ticketPurchaseId, assignToUserId);
                return Result<TicketAssignmentDto>.Failure(
                    "Vetting required",
                    "This event is limited to vetted members only");
            }

            // 7. Check assignee doesn't already have tickets for overlapping sessions (BR-012)
            var sessionIds = ticketPurchase.TicketType.Sessions.Select(s => s.Id).ToList();
            if (sessionIds.Count > 0)
            {
                var assigneeOverlap = await _context.EventAttendances
                    .AsNoTracking()
                    .AnyAsync(ea =>
                        ea.UserId == assignToUserId
                        && (ea.Status == AttendanceStatus.Active || ea.Status == AttendanceStatus.PendingAcceptance)
                        && ea.AttendanceType == AttendanceType.Ticket
                        && ea.SessionId.HasValue
                        && sessionIds.Contains(ea.SessionId.Value), ct);

                if (assigneeOverlap)
                {
                    return Result<TicketAssignmentDto>.Failure(
                        "Duplicate ticket",
                        "The target user already has a ticket for overlapping sessions in this event");
                }
            }

            // 7. Create EventAttendance records for each session (PendingAcceptance)
            var eventId = ticketPurchase.TicketType?.Event?.Id ?? Guid.Empty;
            EventAttendance? primaryAttendance = null;

            foreach (var session in ticketPurchase.TicketType?.Sessions ?? Enumerable.Empty<Session>())
            {
                var attendance = new EventAttendance(eventId, assignToUserId, AttendanceType.Ticket)
                {
                    SessionId = session.Id,
                    TicketPurchaseId = ticketPurchase.Id,
                    Status = AttendanceStatus.PendingAcceptance,
                    AssignedByUserId = callerUserId,
                    AssignedAt = DateTime.UtcNow,
                    EventWaiverAccepted = false,
                    CreatedBy = callerUserId
                };

                _context.EventAttendances.Add(attendance);
                primaryAttendance ??= attendance;
            }

            // If ticket type has no sessions (event-level), create one event-level attendance
            if (primaryAttendance == null)
            {
                primaryAttendance = new EventAttendance(eventId, assignToUserId, AttendanceType.Ticket)
                {
                    TicketPurchaseId = ticketPurchase.Id,
                    Status = AttendanceStatus.PendingAcceptance,
                    AssignedByUserId = callerUserId,
                    AssignedAt = DateTime.UtcNow,
                    EventWaiverAccepted = false,
                    CreatedBy = callerUserId
                };
                _context.EventAttendances.Add(primaryAttendance);
            }

            // 8. Update TicketPurchase to record the intended recipient
            ticketPurchase.PurchasedForUserId = assignToUserId;
            ticketPurchase.UpdatedAt = DateTime.UtcNow;

            // 9. Create audit history
            var history = new AttendanceHistory(primaryAttendance.Id, "TicketAssigned")
            {
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    TicketPurchaseId = ticketPurchase.Id,
                    AssignedTo = assignToUserId,
                    AssignedBy = callerUserId,
                    FromUnassigned = true,
                    SessionCount = sessionIds.Count
                }),
                ChangedBy = callerUserId,
                ChangeReason = "Unassigned ticket assigned from dashboard (UC-007)"
            };
            _context.AttendanceHistory.Add(history);

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Unassigned ticket {TicketPurchaseId} assigned to {AssignToUserId} by {CallerUserId}. " +
                "Created {SessionCount} EventAttendance records in PendingAcceptance.",
                ticketPurchaseId, assignToUserId, callerUserId, sessionIds.Count);

            // Reload the attendance with navigation properties for BuildAssignmentDtoAsync
            var savedAttendance = await _context.EventAttendances
                .Include(ea => ea.Event)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp!.TicketType)
                .FirstOrDefaultAsync(ea => ea.Id == primaryAttendance.Id, ct);

            return Result<TicketAssignmentDto>.Success(
                await BuildAssignmentDtoAsync(savedAttendance ?? primaryAttendance, ct));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error assigning unassigned ticket {TicketPurchaseId} to {AssignToUserId}",
                ticketPurchaseId, assignToUserId);
            return Result<TicketAssignmentDto>.Failure(
                "Assignment failed",
                "An unexpected error occurred while assigning the ticket. Please try again.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<PendingAssignmentDto>>> GetPendingAssignmentsAsync(
        Guid userId, CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Getting pending assignments for user {UserId}", userId);

            var pendingAssignments = await _context.EventAttendances
                .AsNoTracking()
                .Include(ea => ea.Event)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp!.TicketType)
                .Include(ea => ea.Session)
                .Where(ea =>
                    ea.UserId == userId
                    && ea.Status == AttendanceStatus.PendingAcceptance)
                .OrderBy(ea => ea.Event.StartDate)
                .ToListAsync(ct);

            // Deduplicate: When both a Ticket and an RSVP are pending for the same event,
            // suppress the RSVP from the dashboard display. Accepting the ticket will
            // auto-activate the RSVP (see AcceptAssignmentAsync). This prevents users
            // from seeing two acceptance boxes for the same event.
            // Standalone proxy RSVPs (no ticket) still appear normally.
            var eventsWithPendingTickets = pendingAssignments
                .Where(ea => ea.AttendanceType == AttendanceType.Ticket)
                .Select(ea => ea.EventId)
                .ToHashSet();

            pendingAssignments = pendingAssignments
                .Where(ea => !(ea.AttendanceType == AttendanceType.RSVP
                                && eventsWithPendingTickets.Contains(ea.EventId)))
                .ToList();

            // Resolve AssignedByUserId -> SceneName via batch lookup
            var assignedByUserIds = pendingAssignments
                .Where(ea => ea.AssignedByUserId.HasValue)
                .Select(ea => ea.AssignedByUserId!.Value)
                .Distinct()
                .ToList();

            var assignedByUsers = await _context.Users
                .AsNoTracking()
                .Where(u => assignedByUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.SceneName })
                .ToDictionaryAsync(u => u.Id, u => u.SceneName ?? string.Empty, ct);

            var result = pendingAssignments.Select(ea => new PendingAssignmentDto
            {
                AttendanceId = ea.Id,
                EventId = ea.EventId,
                EventTitle = ea.Event.Title ?? string.Empty,
                EventDate = ea.Event.StartDate,
                AssignedBySceneName = ea.AssignedByUserId.HasValue
                    && assignedByUsers.TryGetValue(ea.AssignedByUserId.Value, out var sceneName)
                    ? sceneName
                    : string.Empty,
                AttendanceType = ea.AttendanceType == AttendanceType.Ticket ? "Ticket" : "RSVP",
                TicketTypeName = ea.TicketPurchase?.TicketType?.Name ?? string.Empty,
                SessionNames = ea.Session != null
                    ? new List<string> { ea.Session.Name ?? string.Empty }
                    : new List<string>(),
                AssignedAt = ea.AssignedAt ?? ea.CreatedAt
            }).ToList();

            _logger.LogDebug(
                "Retrieved {Count} pending assignments for user {UserId}",
                result.Count, userId);

            return Result<List<PendingAssignmentDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting pending assignments for user {UserId}", userId);
            return Result<List<PendingAssignmentDto>>.Failure(
                "Failed to retrieve pending assignments",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<AssignedTicketStatusDto>>> GetAssignedTicketsAsync(
        Guid userId, CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Getting assigned tickets for purchaser {UserId}", userId);

            var result = new List<AssignedTicketStatusDto>();

            // 1. Find EventAttendance records linked to TicketPurchases made by this user
            //    where the ticket has been assigned (AssignedByUserId IS NOT NULL)
            var assignedAttendances = await _context.EventAttendances
                .AsNoTracking()
                .Include(ea => ea.Event)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp!.TicketType)
                .Where(ea =>
                    ea.TicketPurchase != null
                    && ea.TicketPurchase.UserId == userId
                    && ea.AssignedByUserId != null)
                .OrderBy(ea => ea.Event.StartDate)
                .ToListAsync(ct);

            // Resolve assigned-to user scene names via batch lookup
            var assignedToUserIds = assignedAttendances
                .Select(ea => ea.UserId)
                .Distinct()
                .ToList();

            var assignedToUsers = await _context.Users
                .AsNoTracking()
                .Where(u => assignedToUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.SceneName })
                .ToDictionaryAsync(u => u.Id, u => u.SceneName ?? string.Empty, ct);

            result.AddRange(assignedAttendances.Select(ea => new AssignedTicketStatusDto
            {
                AttendanceId = ea.Id,
                TicketPurchaseId = ea.TicketPurchaseId ?? Guid.Empty,
                EventId = ea.EventId,
                EventTitle = ea.Event.Title ?? string.Empty,
                EventDate = ea.Event.StartDate,
                TicketTypeName = ea.TicketPurchase?.TicketType?.Name ?? string.Empty,
                Status = ea.Status.ToString(),
                AssignedToUserId = ea.UserId,
                AssignedToSceneName = assignedToUsers.TryGetValue(ea.UserId, out var sceneName)
                    ? sceneName
                    : null,
                // AD-015: Can reassign if Active + previously declined
                CanReassign = ea.Status == AttendanceStatus.Active && ea.DeclinedAt != null,
                IsUnassigned = false
            }));

            // 2. Find "assign later" tickets: TicketPurchases with NO EventAttendance records.
            //    These are extra tickets bought during multi-ticket checkout where the purchaser
            //    chose "Assign later". They only have a TicketPurchase record (no EventAttendance)
            //    until they are assigned to someone via the dashboard.
            var unassignedPurchases = await _context.TicketPurchases
                .AsNoTracking()
                .Include(tp => tp.TicketType)
                    .ThenInclude(tt => tt!.Event)
                .Where(tp =>
                    tp.UserId == userId
                    && tp.IsPaymentCompleted
                    && !_context.EventAttendances.Any(ea => ea.TicketPurchaseId == tp.Id))
                .OrderBy(tp => tp.TicketType!.Event!.StartDate)
                .ToListAsync(ct);

            result.AddRange(unassignedPurchases.Select(tp => new AssignedTicketStatusDto
            {
                AttendanceId = Guid.Empty, // No attendance yet - will be created on assignment
                TicketPurchaseId = tp.Id,
                EventId = tp.TicketType?.Event?.Id ?? Guid.Empty,
                EventTitle = tp.TicketType?.Event?.Title ?? string.Empty,
                EventDate = tp.TicketType?.Event?.StartDate ?? DateTime.MinValue,
                TicketTypeName = tp.TicketType?.Name ?? string.Empty,
                Status = "Unassigned",
                AssignedToUserId = null,
                AssignedToSceneName = null,
                CanReassign = false,
                IsUnassigned = true
            }));

            _logger.LogDebug(
                "Retrieved {AssignedCount} assigned + {UnassignedCount} unassigned tickets for purchaser {UserId}",
                assignedAttendances.Count, unassignedPurchases.Count, userId);

            return Result<List<AssignedTicketStatusDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting assigned tickets for purchaser {UserId}", userId);
            return Result<List<AssignedTicketStatusDto>>.Failure(
                "Failed to retrieve assigned tickets",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<TicketAssignmentDto>> AdminAssignTicketAsync(
        Guid eventId, Guid adminUserId, AdminAssignTicketRequest request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Admin assigning ticket: EventId={EventId}, AdminUserId={AdminUserId}, TargetUserId={TargetUserId}, TicketTypeId={TicketTypeId}",
                eventId, adminUserId, request.UserId, request.TicketTypeId);

            // 1. Load event by eventId, verify exists
            var eventEntity = await _context.Events
                .Include(e => e.EventAttendances)
                .FirstOrDefaultAsync(e => e.Id == eventId, ct);

            if (eventEntity == null)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Event not found",
                    "The specified event does not exist");
            }

            // 2. Load ticket type, verify it belongs to this event
            var ticketType = await _context.TicketTypes
                .Include(tt => tt.Sessions)
                .FirstOrDefaultAsync(tt => tt.Id == request.TicketTypeId && tt.EventId == eventId, ct);

            if (ticketType == null)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "Ticket type not found",
                    "The specified ticket type does not exist or does not belong to this event");
            }

            // 3. Load target user, verify exists
            var targetUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

            if (targetUser == null)
            {
                return Result<TicketAssignmentDto>.Failure(
                    "User not found",
                    "The specified user does not exist");
            }

            // 4. If event.VettedMembersOnly: check user.IsVetted (BR-041 still checks vetting)
            if (eventEntity.VettedMembersOnly && !targetUser.IsVetted)
            {
                _logger.LogWarning(
                    "Admin assignment denied: User {TargetUserId} is not vetted for vetted-only event {EventId} (BR-041)",
                    request.UserId, eventId);
                return Result<TicketAssignmentDto>.Failure(
                    "User not vetted",
                    "This event is limited to vetted members only. The target user is not currently vetted (BR-041).");
            }

            // 5. Check user doesn't already have Active/PendingAcceptance ticket for this event's sessions (BR-012)
            var sessionIds = ticketType.Sessions.Select(s => s.Id).ToList();

            // If ticket type has sessions, check per-session; otherwise check event-level
            bool hasDuplicate;
            if (sessionIds.Any())
            {
                hasDuplicate = await _context.EventAttendances
                    .AsNoTracking()
                    .AnyAsync(ea =>
                        ea.UserId == request.UserId
                        && ea.EventId == eventId
                        && ea.AttendanceType == AttendanceType.Ticket
                        && (ea.Status == AttendanceStatus.Active || ea.Status == AttendanceStatus.PendingAcceptance)
                        && ea.SessionId != null
                        && sessionIds.Contains(ea.SessionId.Value), ct);
            }
            else
            {
                hasDuplicate = await _context.EventAttendances
                    .AsNoTracking()
                    .AnyAsync(ea =>
                        ea.UserId == request.UserId
                        && ea.EventId == eventId
                        && ea.AttendanceType == AttendanceType.Ticket
                        && (ea.Status == AttendanceStatus.Active || ea.Status == AttendanceStatus.PendingAcceptance), ct);
            }

            if (hasDuplicate)
            {
                _logger.LogWarning(
                    "Admin assignment denied: User {TargetUserId} already has active/pending ticket for event {EventId} (BR-012)",
                    request.UserId, eventId);
                return Result<TicketAssignmentDto>.Failure(
                    "User already has ticket",
                    "The target user already has an active or pending ticket for this event/session (BR-012).");
            }

            // 6. Create a "comp" TicketPurchase
            var paymentReference = $"WCR-ADMIN-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            var ticketPurchase = new TicketPurchase
            {
                Id = Guid.NewGuid(),
                TicketTypeId = request.TicketTypeId,
                UserId = adminUserId, // Admin is the "purchaser"
                PurchasedForUserId = request.UserId,
                PurchaseDate = DateTime.UtcNow,
                Quantity = 1,
                TotalPrice = 0m,
                PaymentStatus = TicketPurchasePaymentStatus.Completed,
                PaymentMethod = "admin-comp",
                PaymentReference = paymentReference,
                EventWaiverAccepted = false, // Recipient must accept (BR-041)
                RecordedByStaffId = adminUserId,
                Notes = request.Notes ?? string.Empty,
                ProcessedAt = DateTime.UtcNow,
                IdempotencyKey = paymentReference,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.TicketPurchases.Add(ticketPurchase);

            // 7. Create EventAttendance for each session in the ticket type (or one event-level if no sessions)
            var now = DateTime.UtcNow;
            EventAttendance? firstAttendance = null;

            if (sessionIds.Any())
            {
                foreach (var sessionId in sessionIds)
                {
                    var attendance = new EventAttendance(eventId, request.UserId, AttendanceType.Ticket)
                    {
                        SessionId = sessionId,
                        Status = AttendanceStatus.PendingAcceptance,
                        TicketPurchaseId = ticketPurchase.Id,
                        AssignedByUserId = adminUserId,
                        AssignedAt = now,
                        EventWaiverAccepted = false,
                        CreatedBy = adminUserId,
                        Notes = $"Admin comp ticket assigned by admin"
                    };
                    _context.EventAttendances.Add(attendance);

                    // 8. Create AttendanceHistory
                    var history = new AttendanceHistory(attendance.Id, "TicketAssigned")
                    {
                        ChangedBy = adminUserId,
                        ChangeReason = "Admin comp ticket assignment (BR-040)",
                        NewValues = JsonSerializer.Serialize(new
                        {
                            AssignedToUserId = request.UserId,
                            AssignedByUserId = adminUserId,
                            Status = AttendanceStatus.PendingAcceptance.ToString(),
                            AssignedAt = now,
                            IsAdminAssignment = true,
                            TicketPurchaseId = ticketPurchase.Id,
                            PaymentReference = paymentReference,
                            Notes = request.Notes
                        })
                    };
                    _context.AttendanceHistory.Add(history);

                    firstAttendance ??= attendance;
                }
            }
            else
            {
                // Single event-level attendance (no sessions)
                var attendance = new EventAttendance(eventId, request.UserId, AttendanceType.Ticket)
                {
                    Status = AttendanceStatus.PendingAcceptance,
                    TicketPurchaseId = ticketPurchase.Id,
                    AssignedByUserId = adminUserId,
                    AssignedAt = now,
                    EventWaiverAccepted = false,
                    CreatedBy = adminUserId,
                    Notes = $"Admin comp ticket assigned by admin"
                };
                _context.EventAttendances.Add(attendance);

                var history = new AttendanceHistory(attendance.Id, "TicketAssigned")
                {
                    ChangedBy = adminUserId,
                    ChangeReason = "Admin comp ticket assignment (BR-040)",
                    NewValues = JsonSerializer.Serialize(new
                    {
                        AssignedToUserId = request.UserId,
                        AssignedByUserId = adminUserId,
                        Status = AttendanceStatus.PendingAcceptance.ToString(),
                        AssignedAt = now,
                        IsAdminAssignment = true,
                        TicketPurchaseId = ticketPurchase.Id,
                        PaymentReference = paymentReference,
                        Notes = request.Notes
                    })
                };
                _context.AttendanceHistory.Add(history);

                firstAttendance = attendance;
            }

            // 9. Save changes
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Admin ticket assigned successfully: EventId={EventId}, TargetUserId={TargetUserId}, AdminUserId={AdminUserId}, TicketTypeId={TicketTypeId}, PaymentRef={PaymentReference}",
                eventId, request.UserId, adminUserId, request.TicketTypeId, paymentReference);

            // 10. Return DTO (reload with includes for proper DTO building)
            var savedAttendance = await _context.EventAttendances
                .Include(ea => ea.Event)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp!.TicketType)
                .FirstAsync(ea => ea.Id == firstAttendance!.Id, ct);

            return Result<TicketAssignmentDto>.Success(await BuildAssignmentDtoAsync(savedAttendance, ct));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error in admin ticket assignment: EventId={EventId}, AdminUserId={AdminUserId}, TargetUserId={TargetUserId}",
                eventId, adminUserId, request.UserId);
            return Result<TicketAssignmentDto>.Failure(
                "Failed to assign ticket",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<AdminEventAssignmentDto>>> GetEventAssignmentsAsync(
        Guid eventId, CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Getting all assignments for event {EventId} (admin view)", eventId);

            // 1. Verify event exists
            var eventExists = await _context.Events.AnyAsync(e => e.Id == eventId, ct);
            if (!eventExists)
            {
                return Result<List<AdminEventAssignmentDto>>.Failure(
                    "Event not found",
                    "The specified event does not exist");
            }

            // 2. Query EventAttendance where EventId matches AND AssignedByUserId IS NOT NULL
            //    Include all statuses (Active, PendingAcceptance, Cancelled)
            var assignments = await _context.EventAttendances
                .AsNoTracking()
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp!.TicketType)
                .Where(ea =>
                    ea.EventId == eventId
                    && ea.AssignedByUserId != null)
                .OrderByDescending(ea => ea.AssignedAt)
                .ToListAsync(ct);

            // 3. Batch resolve user scene names for attendees and assigners
            var allUserIds = assignments
                .SelectMany(ea => new[] { ea.UserId, ea.AssignedByUserId!.Value })
                .Distinct()
                .ToList();

            var userSceneNames = await _context.Users
                .AsNoTracking()
                .Where(u => allUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.SceneName })
                .ToDictionaryAsync(u => u.Id, u => u.SceneName ?? string.Empty, ct);

            // 4. Map to AdminEventAssignmentDto
            var result = assignments.Select(ea => new AdminEventAssignmentDto
            {
                AttendanceId = ea.Id,
                EventId = ea.EventId,
                AttendeeName = userSceneNames.TryGetValue(ea.UserId, out var attendeeName)
                    ? attendeeName
                    : string.Empty,
                AttendeeUserId = ea.UserId,
                AssignedByName = ea.AssignedByUserId.HasValue
                    && userSceneNames.TryGetValue(ea.AssignedByUserId.Value, out var assignerName)
                    ? assignerName
                    : string.Empty,
                AssignedByUserId = ea.AssignedByUserId!.Value,
                TicketTypeName = ea.TicketPurchase?.TicketType?.Name ?? string.Empty,
                AttendanceType = ea.AttendanceType == AttendanceType.Ticket ? "Ticket" : "RSVP",
                Status = ea.Status.ToString(),
                AssignedAt = ea.AssignedAt ?? ea.CreatedAt,
                AcceptedAt = ea.AcceptedAt,
                DeclinedAt = ea.DeclinedAt,
                WaiverAccepted = ea.EventWaiverAccepted
            }).ToList();

            _logger.LogDebug(
                "Retrieved {Count} assignments for event {EventId} (admin view)",
                result.Count, eventId);

            return Result<List<AdminEventAssignmentDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting assignments for event {EventId} (admin view)", eventId);
            return Result<List<AdminEventAssignmentDto>>.Failure(
                "Failed to retrieve event assignments",
                ex.Message);
        }
    }

    // ============================================================================
    // Private Helper Methods
    // ============================================================================

    /// <summary>
    /// Builds a TicketAssignmentDto from an EventAttendance entity.
    /// Resolves user scene names for the assigned-to and assigned-by users.
    /// </summary>
    private async Task<TicketAssignmentDto> BuildAssignmentDtoAsync(
        EventAttendance attendance, CancellationToken ct)
    {
        // Resolve scene names for the assigned-to and assigned-by users
        var userIds = new List<Guid> { attendance.UserId };
        if (attendance.AssignedByUserId.HasValue)
        {
            userIds.Add(attendance.AssignedByUserId.Value);
        }

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.SceneName })
            .ToDictionaryAsync(u => u.Id, u => u.SceneName ?? string.Empty, ct);

        return new TicketAssignmentDto
        {
            AttendanceId = attendance.Id,
            EventId = attendance.EventId,
            EventTitle = attendance.Event?.Title ?? string.Empty,
            TicketTypeName = attendance.TicketPurchase?.TicketType?.Name ?? string.Empty,
            Status = attendance.Status.ToString(),
            AssignedToUserId = attendance.UserId,
            AssignedToSceneName = users.TryGetValue(attendance.UserId, out var toName) ? toName : null,
            AssignedByUserId = attendance.AssignedByUserId,
            AssignedBySceneName = attendance.AssignedByUserId.HasValue
                && users.TryGetValue(attendance.AssignedByUserId.Value, out var byName)
                    ? byName
                    : null,
            AssignedAt = attendance.AssignedAt,
            CreatedAt = attendance.CreatedAt
        };
    }

    /// <summary>
    /// Calculates the acceptance cutoff time for a ticket or RSVP.
    /// Acceptance is allowed up to 4 hours after the LAST session's start time
    /// that the ticket covers. For RSVPs (no ticket type), uses the event's last session.
    ///
    /// BUSINESS RULE: People may show up at the door and need to accept at check-in.
    /// The 4-hour grace period after last session start allows this.
    /// Session-based: uses the latest session the ticket covers, not the first.
    ///
    /// Returns null if no sessions found (acceptance is unrestricted).
    /// </summary>
    private async Task<DateTime?> GetAcceptanceCutoffAsync(
        EventAttendance attendance, CancellationToken ct)
    {
        const int GraceHoursAfterLastSession = 4;

        DateTime? latestSessionStart = null;

        // For tickets: find the latest session the ticket type covers
        if (attendance.TicketPurchase?.TicketType != null)
        {
            // Load sessions for this ticket type if not already loaded
            var ticketTypeSessions = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.TicketTypes.Any(tt => tt.Id == attendance.TicketPurchase.TicketTypeId))
                .OrderByDescending(s => s.StartTime)
                .Select(s => s.StartTime)
                .ToListAsync(ct);

            if (ticketTypeSessions.Count > 0)
            {
                latestSessionStart = ticketTypeSessions.First();
            }
        }

        // For RSVPs or if ticket type had no sessions: use the event's last session
        if (latestSessionStart == null)
        {
            var eventSessions = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.EventId == attendance.EventId)
                .OrderByDescending(s => s.StartTime)
                .Select(s => s.StartTime)
                .FirstOrDefaultAsync(ct);

            if (eventSessions != default)
            {
                latestSessionStart = eventSessions;
            }
        }

        // If no sessions found at all, no timing restriction
        if (!latestSessionStart.HasValue)
        {
            return null;
        }

        return latestSessionStart.Value.AddHours(GraceHoursAfterLastSession);
    }
}
