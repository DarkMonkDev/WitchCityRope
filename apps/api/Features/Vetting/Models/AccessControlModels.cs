using WitchCityRope.Api.Features.Vetting.Entities;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Result of an access control check for RSVP or ticket purchase
/// Indicates whether access is allowed and provides user-friendly messaging for denials
/// </summary>
public record AccessControlResult(
    bool IsAllowed,
    string? DenialReason,
    VettingStatus? VettingStatus,
    string? UserMessage
);

/// <summary>
/// Information about a user's vetting status
/// Used for caching and access control decisions
/// </summary>
public class VettingStatusInfo
{
    public bool HasApplication { get; set; }
    public VettingStatus? Status { get; set; }
    public Guid? ApplicationId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
}
