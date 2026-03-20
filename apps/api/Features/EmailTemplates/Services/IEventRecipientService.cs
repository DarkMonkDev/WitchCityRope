using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.EmailTemplates.Services;

public interface IEventRecipientService
{
    Task<List<RecipientInfo>> GetRecipientsAsync(
        EventRecipientGroup group, Guid sessionId, CancellationToken ct);
}

/// <summary>
/// Represents a single volunteer assignment (role + shift) for a recipient.
/// A volunteer may have multiple assignments for the same session.
/// </summary>
public record VolunteerAssignment(
    string Role,
    string? ShiftStart,
    string? ShiftEnd);

/// <summary>
/// Additional data for PendingAssignmentHolders recipients.
/// Carries per-attendance assignment details needed by
/// TicketAcceptanceReminder and RsvpAcceptanceReminder templates.
/// Populated by GetPendingAssignmentHoldersAsync in EventRecipientService.
/// </summary>
public record AssignmentInfo(
    string DelegateSceneName,
    string? TicketTypeName,
    AttendanceType AttendanceType,
    Guid AttendanceId,
    Guid EventId);

/// <summary>
/// Represents a recipient for an email template.
/// VolunteerAssignments is populated only when RecipientGroup is SessionVolunteers,
/// and contains ALL volunteer positions the user holds for the session (not just one).
/// Assignment is populated only when RecipientGroup is PendingAssignmentHolders,
/// and contains delegate/ticket info needed for acceptance reminder templates.
/// </summary>
public record RecipientInfo(
    Guid UserId,
    string Email,
    string DisplayName,
    List<VolunteerAssignment>? VolunteerAssignments = null,
    AssignmentInfo? Assignment = null);
