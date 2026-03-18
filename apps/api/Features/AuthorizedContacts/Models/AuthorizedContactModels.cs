using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.AuthorizedContacts.Models;

/// <summary>
/// Response DTO for a single authorized contact relationship.
/// The UserId and SceneName fields represent the OTHER party in the relationship,
/// with the Direction field indicating whether they are a "delegate" or "principal"
/// relative to the requesting user.
/// </summary>
public class AuthorizedContactDto
{
    /// <summary>
    /// Unique identifier for the authorization record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The other party's user ID (delegate or principal, depending on Direction)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The other party's scene name for display purposes (AD-009: scene name only)
    /// </summary>
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// Relationship direction relative to the requesting user:
    /// "delegate" = this person can act on my behalf (I am the principal)
    /// "principal" = I can act on this person's behalf (I am the delegate)
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// When the authorization was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response for GET /api/authorized-contacts listing all active relationships.
/// Separates contacts into two lists based on the user's role in each relationship.
/// </summary>
public class AuthorizedContactsListDto
{
    /// <summary>
    /// People who can act on my behalf (I authorized them as delegates)
    /// </summary>
    public List<AuthorizedContactDto> Delegates { get; set; } = new();

    /// <summary>
    /// People I can act for (they authorized me as their delegate)
    /// </summary>
    public List<AuthorizedContactDto> Principals { get; set; } = new();
}

/// <summary>
/// Request body for POST /api/authorized-contacts to add a new authorized contact.
/// The caller (authenticated user) becomes the Principal, and the specified user
/// becomes the Delegate (BR-001).
/// </summary>
public class AddAuthorizedContactRequest
{
    /// <summary>
    /// The user ID of the person being authorized to act on the caller's behalf
    /// </summary>
    [Required]
    public Guid DelegateUserId { get; set; }
}

/// <summary>
/// Search result DTO for user lookup when adding authorized contacts.
/// Returns only UserId and SceneName per AD-009 privacy requirements.
/// </summary>
public class UserSearchResultDto
{
    /// <summary>
    /// The user's unique identifier
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user's scene name (only identifying info exposed per AD-009)
    /// </summary>
    public string SceneName { get; set; } = string.Empty;
}

/// <summary>
/// DTO for checkout/RSVP dropdowns listing people who authorized the current user.
/// Used when a delegate is selecting which principal to act for.
/// Includes vetting status for VettedMembersOnly event filtering (BR-035).
/// </summary>
public class PrincipalContactDto
{
    /// <summary>
    /// The principal's user ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The principal's scene name for display
    /// </summary>
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the principal has approved vetting status (VettingStatus == 3).
    /// Used by the frontend to filter/indicate eligibility for VettedMembersOnly events.
    /// </summary>
    public bool IsVetted { get; set; }
}
