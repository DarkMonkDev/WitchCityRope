using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// Represents a delegation authorization between two users.
///
/// BUSINESS PURPOSE:
/// The Principal (person being represented) grants the Delegate (person acting
/// on their behalf) the ability to purchase tickets and create RSVPs for them.
///
/// AUTHORIZATION DIRECTION:
/// Principal -> Delegate: "I authorize this person to act on my behalf"
///
/// LIFECYCLE:
/// - Created when Principal adds Delegate via Profile Settings > Authorized Contacts
/// - Active while RevokedAt is NULL
/// - Soft-deleted (RevokedAt set) when Principal removes the contact
/// - Revocation does NOT affect existing tickets/RSVPs (BR-005)
///
/// CONSTRAINTS:
/// - PrincipalId != DelegateId (self-authorization blocked, BR-003)
/// - Unique active relationship per PrincipalId + DelegateId pair
/// - Mutual authorization allowed (BR-004): A->B and B->A are separate records
/// </summary>
public class AuthorizedContact
{
    /// <summary>
    /// Unique identifier for the authorization record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user who grants authorization (the person being represented)
    /// </summary>
    [Required]
    public Guid PrincipalId { get; set; }

    /// <summary>
    /// The user who receives authorization (the person who can act on behalf)
    /// </summary>
    [Required]
    public Guid DelegateId { get; set; }

    /// <summary>
    /// When the authorization was created (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the authorization was last updated (UTC)
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When the authorization was revoked (UTC). NULL = active.
    /// Soft delete for audit trail preservation (UC-002).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Optional reason for revocation (e.g., "Removed by principal")
    /// </summary>
    public string? RevokedReason { get; set; }

    // Navigation Properties

    /// <summary>
    /// Navigation property to the Principal (person being represented)
    /// </summary>
    public ApplicationUser Principal { get; set; } = null!;

    /// <summary>
    /// Navigation property to the Delegate (person who can act on behalf)
    /// </summary>
    public ApplicationUser Delegate { get; set; } = null!;

    /// <summary>
    /// Whether this authorization is currently active
    /// </summary>
    public bool IsActive => RevokedAt == null;

    /// <summary>
    /// Constructor initializes required fields with proper UTC handling
    /// </summary>
    public AuthorizedContact()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for creating a new authorization
    /// </summary>
    public AuthorizedContact(Guid principalId, Guid delegateId) : this()
    {
        if (principalId == delegateId)
            throw new InvalidOperationException("Cannot authorize yourself as a contact (BR-003)");

        PrincipalId = principalId;
        DelegateId = delegateId;
    }

    /// <summary>
    /// Revokes this authorization
    /// </summary>
    public void Revoke(string? reason = null)
    {
        if (RevokedAt != null)
            throw new InvalidOperationException("Authorization is already revoked");

        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}
