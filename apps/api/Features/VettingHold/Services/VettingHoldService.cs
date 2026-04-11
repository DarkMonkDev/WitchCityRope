using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Data.Entities;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.VettingHold.Models;

namespace WitchCityRope.Api.Features.VettingHold.Services;

/// <summary>
/// Service for membership hold and reinstatement operations
/// Handles voluntary membership holds and reinstatement requests
/// </summary>
public class VettingHoldService : IVettingHoldService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VettingHoldService> _logger;
    private readonly IConfiguration _configuration;

    // VettingStatus enum values (from database schema)
    private const int VettingStatus_FinalReview = 2;
    private const int VettingStatus_Approved = 3;
    private const int VettingStatus_OnHold = 5;

    public VettingHoldService(
        ApplicationDbContext context,
        ILogger<VettingHoldService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Place user's membership on hold
    /// Changes User.VettingStatus to OnHold (5)
    /// Updates VettingApplication.WorkflowStatus to OnHold (5)
    /// Cancels all future social event RSVPs
    /// Creates UserNote and VettingAuditLog entries
    /// Sends email notification to admin
    /// </summary>
    public async Task<Result<MembershipHoldResponse>> PlaceMembershipOnHoldAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("User {UserId} requesting to place membership on hold. Reason: {Reason}",
                userId, reason);

            // Validate reason is provided
            if (string.IsNullOrWhiteSpace(reason))
            {
                return Result<MembershipHoldResponse>.Failure(
                    "Invalid request",
                    "A reason must be provided to place membership on hold");
            }

            // Begin transaction for atomicity
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 1. Get user and validate current status
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

                if (user == null)
                {
                    return Result<MembershipHoldResponse>.Failure(
                        "User not found",
                        $"User {userId} does not exist");
                }

                // Verify user is currently Approved
                if (user.VettingStatus != VettingStatus_Approved)
                {
                    var currentStatusName = GetStatusName(user.VettingStatus);
                    return Result<MembershipHoldResponse>.Failure(
                        "Invalid status",
                        $"Only approved members can place their membership on hold. Current status: {currentStatusName}");
                }

                // 2. Update User.VettingStatus to OnHold
                user.VettingStatus = VettingStatus_OnHold;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);

                // 3. Update VettingApplication.WorkflowStatus if application exists
                // Exclude soft-deleted applications — they should not be modified during hold
                var application = await _context.VettingApplications
                    .Where(va => !va.IsDeleted)
                    .FirstOrDefaultAsync(va => va.UserId == userId, cancellationToken);

                if (application != null)
                {
                    application.WorkflowStatus = VettingStatus.OnHold;
                    _context.VettingApplications.Update(application);
                }

                var changedAt = DateTime.UtcNow;

                // 4. Create VettingAuditLog entries (if application exists)
                if (application != null)
                {
                    // Create status change entry with null Notes to get badge
                    var statusChangeLog = new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = userId,
                        PerformedAt = changedAt,
                        OldValue = "Approved",
                        NewValue = "OnHold",
                        Notes = null // Null to trigger simplified description with badge
                    };
                    _context.VettingAuditLogs.Add(statusChangeLog);

                    // Create separate note entry with user's reason
                    var reasonLog = new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Note Added",
                        PerformedBy = userId,
                        PerformedAt = changedAt,
                        OldValue = null,
                        NewValue = null,
                        Notes = reason
                    };
                    _context.VettingAuditLogs.Add(reasonLog);
                }

                // 6. Cancel all future RSVPs for vetted-only events
                var now = DateTime.UtcNow;
                var futureRsvps = await _context.EventAttendances
                    .Include(ea => ea.Event)
                    .Where(ea => ea.UserId == userId &&
                                ea.Status == AttendanceStatus.Active &&
                                ea.AttendanceType == AttendanceType.RSVP &&
                                ea.Event!.VettedMembersOnly &&
                                ea.Event.StartDate > now)
                    .ToListAsync(cancellationToken);

                foreach (var rsvp in futureRsvps)
                {
                    rsvp.Cancel("Auto-cancelled due to membership placed on hold");
                    rsvp.UpdatedBy = userId;
                    _context.EventAttendances.Update(rsvp);

                    _logger.LogInformation(
                        "Cancelled RSVP {RsvpId} for event {EventId} due to membership hold for user {UserId}",
                        rsvp.Id, rsvp.EventId, userId);
                }

                if (futureRsvps.Any())
                {
                    _logger.LogInformation(
                        "Cancelled {Count} future social event RSVPs for user {UserId}",
                        futureRsvps.Count, userId);
                }

                // Save all changes
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // 7. Send email notification to admin (non-blocking, log errors)
                try
                {
                    await SendAdminNotificationAsync(
                        subject: $"Membership On Hold: {user.SceneName}",
                        body: $"User {user.SceneName} ({user.Email}) has placed their membership on hold.\n\n" +
                              $"Reason: {reason}\n\n" +
                              $"Changed at: {changedAt:yyyy-MM-dd HH:mm:ss} UTC\n" +
                              $"Future RSVPs cancelled: {futureRsvps.Count}",
                        cancellationToken);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx,
                        "Failed to send admin notification for membership hold by user {UserId}",
                        userId);
                    // Don't fail the operation if email fails
                }

                _logger.LogInformation(
                    "Successfully placed membership on hold for user {UserId}. Status: {OldStatus} -> {NewStatus}",
                    userId, "Approved", "OnHold");

                return Result<MembershipHoldResponse>.Success(new MembershipHoldResponse(
                    // Phase 3b-1: MembershipHoldResponse.NewStatus is now
                    // the VettingStatus enum (was int). Using the enum
                    // literal directly instead of the old VettingStatus_OnHold
                    // integer constant.
                    NewStatus: VettingStatus.OnHold,
                    StatusName: "OnHold",
                    ChangedAt: changedAt
                ));
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing membership on hold for user {UserId}", userId);
            return Result<MembershipHoldResponse>.Failure(
                "Failed to place membership on hold",
                ex.Message);
        }
    }

    /// <summary>
    /// Request reinstatement of membership (moves to Final Review)
    /// Changes User.VettingStatus to FinalReview (2)
    /// Updates VettingApplication.WorkflowStatus to FinalReview (2)
    /// Creates UserNote and VettingAuditLog entries
    /// Sends email notification to admin
    /// </summary>
    public async Task<Result<MembershipHoldResponse>> RequestReinstatementAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("User {UserId} requesting membership reinstatement. Reason: {Reason}",
                userId, reason);

            // Validate reason is provided
            if (string.IsNullOrWhiteSpace(reason))
            {
                return Result<MembershipHoldResponse>.Failure(
                    "Invalid request",
                    "A reason must be provided to request reinstatement");
            }

            // Begin transaction for atomicity
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 1. Get user and validate current status
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

                if (user == null)
                {
                    return Result<MembershipHoldResponse>.Failure(
                        "User not found",
                        $"User {userId} does not exist");
                }

                // Verify user is currently OnHold
                if (user.VettingStatus != VettingStatus_OnHold)
                {
                    var currentStatusName = GetStatusName(user.VettingStatus);
                    return Result<MembershipHoldResponse>.Failure(
                        "Invalid status",
                        $"Only members on hold can request reinstatement. Current status: {currentStatusName}");
                }

                // 2. Update User.VettingStatus to FinalReview
                user.VettingStatus = VettingStatus_FinalReview;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);

                // 3. Update VettingApplication.WorkflowStatus
                // Exclude soft-deleted applications — they should not be modified during reinstatement
                var application = await _context.VettingApplications
                    .Where(va => !va.IsDeleted)
                    .FirstOrDefaultAsync(va => va.UserId == userId, cancellationToken);

                if (application != null)
                {
                    application.WorkflowStatus = VettingStatus.FinalReview;
                    _context.VettingApplications.Update(application);
                }

                var changedAt = DateTime.UtcNow;

                // 4. Create VettingAuditLog entries (if application exists)
                if (application != null)
                {
                    // Create status change entry with null Notes to get badge
                    var statusChangeLog = new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Status Changed",
                        PerformedBy = userId,
                        PerformedAt = changedAt,
                        OldValue = "OnHold",
                        NewValue = "FinalReview",
                        Notes = null // Null to trigger simplified description with badge
                    };
                    _context.VettingAuditLogs.Add(statusChangeLog);

                    // Create separate note entry with user's reason
                    var reasonLog = new VettingAuditLog
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        Action = "Note Added",
                        PerformedBy = userId,
                        PerformedAt = changedAt,
                        OldValue = null,
                        NewValue = null,
                        Notes = reason
                    };
                    _context.VettingAuditLogs.Add(reasonLog);
                }

                // Save all changes
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // 6. Send email notification to admin (non-blocking, log errors)
                try
                {
                    await SendAdminNotificationAsync(
                        subject: $"Reinstatement Request: {user.SceneName}",
                        body: $"User {user.SceneName} ({user.Email}) has requested membership reinstatement.\n\n" +
                              $"Reason: {reason}\n\n" +
                              $"Changed at: {changedAt:yyyy-MM-dd HH:mm:ss} UTC\n\n" +
                              $"The user's status has been changed to Final Review and requires admin approval.",
                        cancellationToken);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx,
                        "Failed to send admin notification for reinstatement request by user {UserId}",
                        userId);
                    // Don't fail the operation if email fails
                }

                _logger.LogInformation(
                    "Successfully requested reinstatement for user {UserId}. Status: {OldStatus} -> {NewStatus}",
                    userId, "OnHold", "FinalReview");

                return Result<MembershipHoldResponse>.Success(new MembershipHoldResponse(
                    NewStatus: VettingStatus.FinalReview,
                    StatusName: "FinalReview",
                    ChangedAt: changedAt
                ));
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting reinstatement for user {UserId}", userId);
            return Result<MembershipHoldResponse>.Failure(
                "Failed to request reinstatement",
                ex.Message);
        }
    }

    /// <summary>
    /// Get current hold/reinstatement status for user
    /// </summary>
    public async Task<Result<VettingHoldStatusResponse>> GetHoldStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return Result<VettingHoldStatusResponse>.Failure(
                    "User not found",
                    $"User {userId} does not exist");
            }

            // Get last status change date from UserNotes
            var lastStatusChange = await _context.UserNotes
                .Where(n => n.UserId == userId && n.NoteType == "StatusChange")
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => n.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var statusName = GetStatusName(user.VettingStatus);
            var canPlaceOnHold = user.VettingStatus == VettingStatus_Approved;
            var canRequestReinstatement = user.VettingStatus == VettingStatus_OnHold;

            return Result<VettingHoldStatusResponse>.Success(new VettingHoldStatusResponse(
                // Phase 3b-1 cast: response field is the enum; entity is still int.
                // Phase 3b-2 will align the entity type and remove this cast.
                VettingStatus: (VettingStatus)user.VettingStatus,
                StatusName: statusName,
                CanPlaceOnHold: canPlaceOnHold,
                CanRequestReinstatement: canRequestReinstatement,
                LastStatusChangeDate: lastStatusChange == default ? null : lastStatusChange
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hold status for user {UserId}", userId);
            return Result<VettingHoldStatusResponse>.Failure(
                "Failed to get hold status",
                ex.Message);
        }
    }

    /// <summary>
    /// Send email notification to admin email address
    /// Uses simple console logging in development (no actual email service integration required)
    /// </summary>
    private async Task SendAdminNotificationAsync(
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        // Get admin email from configuration (default to console log if not configured)
        var adminEmail = _configuration["AdminEmail"] ?? "admin@witchcityrope.com";

        _logger.LogInformation(
            "ADMIN EMAIL NOTIFICATION:\n" +
            "To: {AdminEmail}\n" +
            "Subject: {Subject}\n" +
            "Body:\n{Body}",
            adminEmail, subject, body);

        // In production, this would integrate with an email service
        // For now, just log the notification
        await Task.CompletedTask;
    }

    /// <summary>
    /// Get human-readable status name from vetting status integer
    /// </summary>
    private static string GetStatusName(int vettingStatus)
    {
        return vettingStatus switch
        {
            0 => "NotStarted",
            1 => "Submitted",
            2 => "FinalReview",
            3 => "Approved",
            4 => "Denied",
            5 => "OnHold",
            6 => "Withdrawn",
            7 => "InterviewScheduled",
            8 => "InterviewCompleted",
            _ => "Unknown"
        };
    }
}
