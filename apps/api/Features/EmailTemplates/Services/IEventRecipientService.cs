using WitchCityRope.Api.Features.EmailTemplates.Entities;

namespace WitchCityRope.Api.Features.EmailTemplates.Services;

public interface IEventRecipientService
{
    Task<List<RecipientInfo>> GetRecipientsAsync(
        EventRecipientGroup group, Guid sessionId, CancellationToken ct);
}

/// <summary>
/// Represents a recipient for an email template.
/// Volunteer-specific fields are populated only when RecipientGroup is SessionVolunteers.
/// </summary>
public record RecipientInfo(
    Guid UserId,
    string Email,
    string DisplayName,
    string? VolunteerRole = null,
    string? ShiftStart = null,
    string? ShiftEnd = null);
