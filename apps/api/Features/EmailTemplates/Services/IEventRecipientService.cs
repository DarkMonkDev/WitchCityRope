using WitchCityRope.Api.Features.EmailTemplates.Entities;

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
/// Represents a recipient for an email template.
/// VolunteerAssignments is populated only when RecipientGroup is SessionVolunteers,
/// and contains ALL volunteer positions the user holds for the session (not just one).
/// </summary>
public record RecipientInfo(
    Guid UserId,
    string Email,
    string DisplayName,
    List<VolunteerAssignment>? VolunteerAssignments = null);
