using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.AuthorizedContacts.Services;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Features.ProxyRsvp.Models;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Vetting.Entities;

namespace WitchCityRope.Api.Features.ProxyRsvp.Services;

/// <summary>
/// Implementation of <see cref="IProxyRsvpService"/> for managing proxy RSVP operations.
/// Uses ApplicationDbContext for data access, IAuthorizedContactService for delegation checks,
/// and follows the Result pattern for consistent error handling.
///
/// Business rules implemented:
/// - BR-050: Same authorization as tickets (Authorized Contacts)
/// - BR-051: PendingAcceptance flow with waiver requirement
/// - BR-053: Delegate cannot accept waiver for principal
/// - BR-054: Capacity check includes PendingAcceptance RSVPs
/// - BR-035: Vetting checked at creation time
/// - BR-036: Vetting re-checked at acceptance time
/// </summary>
public class ProxyRsvpService : IProxyRsvpService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthorizedContactService _authorizedContactService;
    private readonly IAttendanceCountService _countService;
    // Fully qualified to avoid Result<T> ambiguity with EmailTemplates.Services namespace
    private readonly WitchCityRope.Api.Features.EmailTemplates.Services.IEventEmailService _eventEmailService;
    private readonly ILogger<ProxyRsvpService> _logger;

    public ProxyRsvpService(
        ApplicationDbContext context,
        IAuthorizedContactService authorizedContactService,
        IAttendanceCountService countService,
        WitchCityRope.Api.Features.EmailTemplates.Services.IEventEmailService eventEmailService,
        ILogger<ProxyRsvpService> logger)
    {
        _context = context;
        _authorizedContactService = authorizedContactService;
        _countService = countService;
        _eventEmailService = eventEmailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ProxyRsvpDto>> CreateProxyRsvpAsync(
        Guid eventId,
        Guid delegateUserId,
        Guid principalUserId,
        string? notes,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Creating proxy RSVP: Delegate {DelegateUserId} for Principal {PrincipalUserId} at Event {EventId}",
                delegateUserId, principalUserId, eventId);

            // Begin a RepeatableRead transaction to prevent race conditions on the
            // per-person limit check and principal duplicate check. PostgreSQL's
            // RepeatableRead ensures a consistent snapshot from the first query,
            // so concurrent requests that both pass the checks will have one fail
            // at commit with a serialization error rather than both succeeding.
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.RepeatableRead, ct);

            // 1. Load event and check it exists with RSVPs enabled
            var eventEntity = await _context.Events
                .AsNoTracking()
                .Where(e => e.Id == eventId)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.AllowRsvps,
                    e.VettedMembersOnly,
                    e.Capacity,
                    e.DefaultMaxTicketOrRsvpPerPerson
                })
                .FirstOrDefaultAsync(ct);

            if (eventEntity == null)
            {
                _logger.LogWarning("Proxy RSVP attempt for non-existent event {EventId}", eventId);
                return Result<ProxyRsvpDto>.NotFound(
                    "Event not found",
                    "The specified event does not exist");
            }

            if (!eventEntity.AllowRsvps)
            {
                _logger.LogWarning(
                    "Proxy RSVP attempt for event {EventId} that does not allow RSVPs",
                    eventId);
                return Result<ProxyRsvpDto>.Forbidden(
                    "RSVPs not allowed",
                    "This event does not allow RSVPs");
            }

            // 2. Check authorized delegate relationship (BR-050)
            var isAuthorized = await _authorizedContactService.IsAuthorizedDelegateAsync(
                principalUserId, delegateUserId, ct);

            if (!isAuthorized)
            {
                _logger.LogWarning(
                    "Unauthorized proxy RSVP attempt: Delegate {DelegateUserId} not authorized by Principal {PrincipalUserId} (BR-050)",
                    delegateUserId, principalUserId);
                return Result<ProxyRsvpDto>.Forbidden(
                    "Not authorized",
                    "You are not authorized to RSVP on behalf of this user (BR-050)");
            }

            // 3. Check principal's vetting status for VettedMembersOnly events (BR-035)
            if (eventEntity.VettedMembersOnly)
            {
                var principalUser = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == principalUserId)
                    .Select(u => new { u.Id, u.VettingStatus })
                    .FirstOrDefaultAsync(ct);

                if (principalUser == null)
                {
                    return Result<ProxyRsvpDto>.NotFound(
                        "User not found",
                        "The specified principal user does not exist");
                }

                // IsVetted is computed as VettingStatus == Approved
                if (principalUser.VettingStatus != VettingStatus.Approved)
                {
                    _logger.LogWarning(
                        "Proxy RSVP blocked: Principal {PrincipalUserId} not vetted for VettedMembersOnly event {EventId} (BR-035)",
                        principalUserId, eventId);
                    return Result<ProxyRsvpDto>.Forbidden(
                        "Vetting required",
                        "This event requires vetted membership. The person you are RSVPing for is not vetted (BR-035)");
                }
            }

            // 4. Check capacity using single source of truth (BR-054).
            // Uses IAttendanceCountService.GetReservedCountAsync which counts DISTINCT users
            // to prevent double-counting users with both RSVP and Ticket records.
            var reservedCount = await _countService.GetReservedCountAsync(eventId, ct);

            if (reservedCount >= eventEntity.Capacity)
            {
                _logger.LogWarning(
                    "Proxy RSVP blocked: Event {EventId} at capacity ({ReservedCount}/{Capacity}) (BR-054)",
                    eventId, reservedCount, eventEntity.Capacity);
                return Result<ProxyRsvpDto>.Conflict(
                    "Event at capacity",
                    "This event is at full capacity (BR-054)");
            }

            // 5. Check for duplicate: principal doesn't already have Active or PendingAcceptance
            // participation (RSVP or Ticket). Block proxy RSVP if they already have any
            // form of participation — ticket purchases auto-create RSVPs for social events,
            // but ticket-only events wouldn't have an RSVP record.
            var existingParticipation = await _context.EventAttendances
                .AsNoTracking()
                .AnyAsync(ea =>
                    ea.EventId == eventId
                    && ea.UserId == principalUserId
                    && (ea.AttendanceType == AttendanceType.RSVP
                        || ea.AttendanceType == AttendanceType.Ticket)
                    && (ea.Status == AttendanceStatus.Active
                        || ea.Status == AttendanceStatus.PendingAcceptance), ct);

            if (existingParticipation)
            {
                _logger.LogWarning(
                    "Principal {PrincipalUserId} already has Active/PendingAcceptance participation for event {EventId}",
                    principalUserId, eventId);
                return Result<ProxyRsvpDto>.Conflict(
                    "Already participating",
                    "This person already has an active or pending RSVP or ticket for this event");
            }

            // 6. Check delegate cumulative limit (if event has a per-person cap).
            // The delegate's own RSVP/ticket counts as 1, plus each proxy RSVP they've created.
            // The +1 accounts for the new proxy RSVP being created.
            if (eventEntity.DefaultMaxTicketOrRsvpPerPerson.HasValue)
            {
                var limit = eventEntity.DefaultMaxTicketOrRsvpPerPerson.Value;

                // Count delegate's own participation (any RSVP or Ticket = 1)
                var delegateHasOwnParticipation = await _context.EventAttendances
                    .AsNoTracking()
                    .AnyAsync(ea =>
                        ea.EventId == eventId
                        && ea.UserId == delegateUserId
                        && (ea.AttendanceType == AttendanceType.RSVP
                            || ea.AttendanceType == AttendanceType.Ticket)
                        && (ea.Status == AttendanceStatus.Active
                            || ea.Status == AttendanceStatus.PendingAcceptance), ct);

                // Count all attendances created by this delegate for others (proxy RSVPs + ticket assignments).
                // Includes both RSVP and Ticket types to cover both proxy RSVP and "Buy For Others" flows.
                var delegateAssignmentCount = await _context.EventAttendances
                    .AsNoTracking()
                    .CountAsync(ea =>
                        ea.EventId == eventId
                        && ea.AssignedByUserId == delegateUserId
                        && (ea.Status == AttendanceStatus.Active
                            || ea.Status == AttendanceStatus.PendingAcceptance), ct);

                // Total = own participation (0 or 1) + existing assignments + 1 (new proxy)
                var totalDelegateCount = (delegateHasOwnParticipation ? 1 : 0) + delegateAssignmentCount + 1;

                if (totalDelegateCount > limit)
                {
                    _logger.LogWarning(
                        "Proxy RSVP blocked: Delegate {DelegateUserId} would exceed per-person limit " +
                        "({TotalCount}/{Limit}) for event {EventId}",
                        delegateUserId, totalDelegateCount, limit, eventId);
                    return Result<ProxyRsvpDto>.Conflict(
                        "Per-person limit reached",
                        $"You have reached the maximum of {limit} RSVPs/tickets allowed per person for this event");
                }
            }

            // 7. Load delegate and principal scene names for the response DTO
            var delegateUser = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == delegateUserId)
                .Select(u => new { u.Id, u.SceneName })
                .FirstOrDefaultAsync(ct);

            var principalSceneName = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == principalUserId)
                .Select(u => u.SceneName)
                .FirstOrDefaultAsync(ct);

            // 8. Create EventAttendance record for the proxy RSVP
            var attendance = new EventAttendance(eventId, principalUserId, AttendanceType.RSVP)
            {
                Status = AttendanceStatus.PendingAcceptance,
                AssignedByUserId = delegateUserId,
                AssignedAt = DateTime.UtcNow,
                CreatedBy = delegateUserId,
                Notes = notes,
                EventWaiverAccepted = false // Principal must accept personally (BR-053)
            };

            _context.EventAttendances.Add(attendance);

            // 9. Create AttendanceHistory audit record
            var history = new AttendanceHistory(attendance.Id, "ProxyRSVPCreated")
            {
                NewValues = JsonSerializer.Serialize(new
                {
                    EventId = eventId,
                    PrincipalUserId = principalUserId,
                    DelegateUserId = delegateUserId,
                    AttendanceType = AttendanceType.RSVP.ToString(),
                    Status = AttendanceStatus.PendingAcceptance.ToString(),
                    Notes = notes
                }),
                ChangedBy = delegateUserId,
                ChangeReason = $"Proxy RSVP created by delegate for principal"
            };

            _context.AttendanceHistory.Add(history);

            // 10. Save changes and commit transaction
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Proxy RSVP created: AttendanceId {AttendanceId}, Event {EventId}, Principal {PrincipalUserId}, Delegate {DelegateUserId}",
                attendance.Id, eventId, principalUserId, delegateUserId);

            // 11. Send RSVP assignment notification email to principal (fire-and-forget, never throws)
            await _eventEmailService.SendRsvpAssignmentNotificationAsync(
                principalUserId, delegateUserId, eventId, ct);

            // 12. Return ProxyRsvpDto
            return Result<ProxyRsvpDto>.Success(new ProxyRsvpDto
            {
                AttendanceId = attendance.Id,
                EventId = eventId,
                EventTitle = eventEntity.Title,
                Status = AttendanceStatus.PendingAcceptance.ToString(),
                RsvpForSceneName = principalSceneName ?? string.Empty,
                RsvpForUserId = principalUserId,
                CreatedBySceneName = delegateUser?.SceneName ?? string.Empty,
                CreatedByUserId = delegateUserId,
                CreatedAt = attendance.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating proxy RSVP: Delegate {DelegateUserId}, Principal {PrincipalUserId}, Event {EventId}",
                delegateUserId, principalUserId, eventId);
            return Result<ProxyRsvpDto>.Infrastructure(
                "Failed to create proxy RSVP",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ProxyRsvpDto>> AcceptProxyRsvpAsync(
        Guid attendanceId,
        Guid callerUserId,
        AcceptProxyRsvpRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Accepting proxy RSVP: AttendanceId {AttendanceId} by User {CallerUserId}",
                attendanceId, callerUserId);

            // 1. Find EventAttendance with PendingAcceptance status and RSVP type
            var attendance = await _context.EventAttendances
                .Include(ea => ea.Event)
                .Include(ea => ea.User)
                .Include(ea => ea.AssignedByUser)
                .Where(ea =>
                    ea.Id == attendanceId
                    && ea.Status == AttendanceStatus.PendingAcceptance
                    && ea.AttendanceType == AttendanceType.RSVP)
                .FirstOrDefaultAsync(ct);

            if (attendance == null)
            {
                _logger.LogWarning(
                    "Accept proxy RSVP failed: Attendance {AttendanceId} not found or not in PendingAcceptance RSVP state",
                    attendanceId);
                return Result<ProxyRsvpDto>.NotFound(
                    "RSVP not found",
                    "The specified RSVP does not exist or is not pending acceptance");
            }

            // 2. Verify caller is the principal (only the person the RSVP was created for can accept)
            if (attendance.UserId != callerUserId)
            {
                _logger.LogWarning(
                    "Accept proxy RSVP denied: Caller {CallerUserId} is not the principal {PrincipalUserId} for attendance {AttendanceId}",
                    callerUserId, attendance.UserId, attendanceId);
                return Result<ProxyRsvpDto>.Forbidden(
                    "Not authorized",
                    "Only the person this RSVP was created for can accept it");
            }

            // 2b. TIMING CHECK: Acceptance allowed up to 4 hours after the LAST session starts.
            // For RSVPs, uses the event's last session start time.
            // Grace period: 4 hours (people may accept at the door).
            var acceptanceCutoff = await GetAcceptanceCutoffForEventAsync(attendance.EventId, ct);
            if (acceptanceCutoff.HasValue && DateTime.UtcNow > acceptanceCutoff.Value)
            {
                _logger.LogWarning(
                    "Accept proxy RSVP denied: Attendance {AttendanceId} acceptance window expired. " +
                    "Cutoff was {Cutoff}, current time {Now}",
                    attendanceId, acceptanceCutoff.Value, DateTime.UtcNow);
                return Result<ProxyRsvpDto>.Failure(
                    "Acceptance window expired",
                    "The acceptance window for this RSVP has closed. RSVPs can be accepted up to 4 hours after the last session starts. Please contact an admin for assistance.");
            }

            // 3. Verify event waiver is accepted (AD-003, BR-030, BR-053)
            if (!request.EventWaiverAccepted)
            {
                return Result<ProxyRsvpDto>.Failure(
                    "Waiver required",
                    "You must accept the event waiver to confirm your RSVP (BR-030)");
            }

            // 4. Re-check vetting for VettedMembersOnly events (BR-036)
            if (attendance.Event.VettedMembersOnly)
            {
                var principalUser = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == callerUserId)
                    .Select(u => new { u.Id, u.VettingStatus })
                    .FirstOrDefaultAsync(ct);

                if (principalUser == null || principalUser.VettingStatus != VettingStatus.Approved)
                {
                    _logger.LogWarning(
                        "Accept proxy RSVP blocked: Principal {PrincipalUserId} vetting status changed for VettedMembersOnly event {EventId} (BR-036)",
                        callerUserId, attendance.EventId);
                    return Result<ProxyRsvpDto>.Forbidden(
                        "Vetting required",
                        "This event requires vetted membership. Your vetting status has changed since this RSVP was created (BR-036)");
                }
            }

            // 5. Update EventAttendance to Active with waiver timestamps
            attendance.Status = AttendanceStatus.Active;
            attendance.EventWaiverAccepted = true;
            attendance.EventWaiverAcceptedAt = DateTime.UtcNow;
            attendance.AcceptedAt = DateTime.UtcNow;
            attendance.UpdatedAt = DateTime.UtcNow;
            attendance.UpdatedBy = callerUserId;

            // 6. Handle Terms of Service acceptance if needed (BR-031)
            if (request.TermsOfServiceAccepted)
            {
                var appUser = await _context.Users
                    .Where(u => u.Id == callerUserId)
                    .FirstOrDefaultAsync(ct);

                if (appUser != null && !appUser.TermsOfServiceAccepted)
                {
                    appUser.TermsOfServiceAccepted = true;
                    appUser.TermsOfServiceAcceptedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Terms of Service accepted by user {UserId} during proxy RSVP acceptance (BR-031)",
                        callerUserId);
                }
            }

            // 7. Create or update EventAttendee record for the check-in system
            var existingAttendee = await _context.EventAttendees
                .FirstOrDefaultAsync(ea =>
                    ea.EventId == attendance.EventId
                    && ea.UserId == callerUserId, ct);

            if (existingAttendee == null)
            {
                var attendee = new EventAttendee
                {
                    Id = Guid.NewGuid(),
                    EventId = attendance.EventId,
                    UserId = callerUserId,
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

            // 8. Create AttendanceHistory audit record
            var history = new AttendanceHistory(attendanceId, "ProxyRSVPAccepted")
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
                    AcceptedAt = attendance.AcceptedAt
                }),
                ChangedBy = callerUserId,
                ChangeReason = "Proxy RSVP accepted by principal"
            };

            _context.AttendanceHistory.Add(history);

            // 9. Save changes
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Proxy RSVP accepted: AttendanceId {AttendanceId}, Event {EventId}, Principal {PrincipalUserId}",
                attendanceId, attendance.EventId, callerUserId);

            // 10. Return updated ProxyRsvpDto
            return Result<ProxyRsvpDto>.Success(new ProxyRsvpDto
            {
                AttendanceId = attendance.Id,
                EventId = attendance.EventId,
                EventTitle = attendance.Event.Title,
                Status = AttendanceStatus.Active.ToString(),
                RsvpForSceneName = attendance.User.SceneName ?? string.Empty,
                RsvpForUserId = attendance.UserId,
                CreatedBySceneName = attendance.AssignedByUser?.SceneName ?? string.Empty,
                CreatedByUserId = attendance.AssignedByUserId,
                CreatedAt = attendance.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error accepting proxy RSVP: AttendanceId {AttendanceId}, User {CallerUserId}",
                attendanceId, callerUserId);
            return Result<ProxyRsvpDto>.Infrastructure(
                "Failed to accept proxy RSVP",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeclineProxyRsvpAsync(
        Guid attendanceId,
        Guid callerUserId,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Declining proxy RSVP: AttendanceId {AttendanceId} by User {CallerUserId}",
                attendanceId, callerUserId);

            // 1. Find EventAttendance with PendingAcceptance status and RSVP type
            var attendance = await _context.EventAttendances
                .Where(ea =>
                    ea.Id == attendanceId
                    && ea.Status == AttendanceStatus.PendingAcceptance
                    && ea.AttendanceType == AttendanceType.RSVP)
                .FirstOrDefaultAsync(ct);

            if (attendance == null)
            {
                _logger.LogWarning(
                    "Decline proxy RSVP failed: Attendance {AttendanceId} not found or not in PendingAcceptance RSVP state",
                    attendanceId);
                return Result.NotFound(
                    "RSVP not found",
                    "The specified RSVP does not exist or is not pending acceptance");
            }

            // 2. Verify caller is the principal
            if (attendance.UserId != callerUserId)
            {
                _logger.LogWarning(
                    "Decline proxy RSVP denied: Caller {CallerUserId} is not the principal {PrincipalUserId} for attendance {AttendanceId}",
                    callerUserId, attendance.UserId, attendanceId);
                return Result.Forbidden(
                    "Not authorized",
                    "Only the person this RSVP was created for can decline it");
            }

            // 3. Update EventAttendance to Cancelled with decline details
            // Unlike ticket declines, declined RSVPs are simply cancelled (no reassignment)
            attendance.Status = AttendanceStatus.Cancelled;
            attendance.CancelledAt = DateTime.UtcNow;
            attendance.CancellationReason = "Declined by recipient";
            attendance.DeclinedAt = DateTime.UtcNow;
            attendance.UpdatedAt = DateTime.UtcNow;
            attendance.UpdatedBy = callerUserId;

            // 4. Create AttendanceHistory audit record
            var history = new AttendanceHistory(attendanceId, "ProxyRSVPDeclined")
            {
                OldValues = JsonSerializer.Serialize(new
                {
                    Status = AttendanceStatus.PendingAcceptance.ToString()
                }),
                NewValues = JsonSerializer.Serialize(new
                {
                    Status = AttendanceStatus.Cancelled.ToString(),
                    DeclinedAt = attendance.DeclinedAt,
                    CancellationReason = attendance.CancellationReason
                }),
                ChangedBy = callerUserId,
                ChangeReason = "Proxy RSVP declined by principal"
            };

            _context.AttendanceHistory.Add(history);

            // 5. Save changes
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Proxy RSVP declined: AttendanceId {AttendanceId}, Event {EventId}, Principal {CallerUserId}",
                attendanceId, attendance.EventId, callerUserId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error declining proxy RSVP: AttendanceId {AttendanceId}, User {CallerUserId}",
                attendanceId, callerUserId);
            return Result.Infrastructure(
                "Failed to decline proxy RSVP",
                ex.Message);
        }
    }

    /// <summary>
    /// Calculates the acceptance cutoff for an RSVP based on the event's last session.
    /// Acceptance is allowed up to 4 hours after the last session's start time.
    /// Returns null if no sessions found (acceptance is unrestricted).
    /// </summary>
    private async Task<DateTime?> GetAcceptanceCutoffForEventAsync(Guid eventId, CancellationToken ct)
    {
        const int GraceHoursAfterLastSession = 4;

        var latestSessionStart = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.EventId == eventId)
            .OrderByDescending(s => s.StartTime)
            .Select(s => (DateTime?)s.StartTime)
            .FirstOrDefaultAsync(ct);

        if (!latestSessionStart.HasValue)
        {
            return null;
        }

        return latestSessionStart.Value.AddHours(GraceHoursAfterLastSession);
    }
}
