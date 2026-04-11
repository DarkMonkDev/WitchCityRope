using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WitchCityRope.Api.Features.Vetting.Entities;

namespace WitchCityRope.Api.Models;

/// <summary>
/// Application user entity matching existing database schema in auth.Users
/// For authentication vertical slice test - throwaway implementation
///
/// This model matches the existing Users table structure which has been enhanced
/// with ASP.NET Core Identity fields through EF migration
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    // Core Identity fields are inherited from IdentityUser<Guid>

    /// <summary>
    /// Scene name for the rope bondage community
    /// Required field, may be duplicated (use email for unique identification)
    /// </summary>
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// User's first name (optional)
    /// </summary>
    [MaxLength(50)]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name (optional)
    /// </summary>
    [MaxLength(50)]
    public string? LastName { get; set; }

    /// <summary>
    /// User's bio/description (optional, max 2000 characters)
    /// </summary>
    [MaxLength(1000)]
    public string? Bio { get; set; }

    /// <summary>
    /// Discord username for community communication (optional)
    /// </summary>
    public string? DiscordName { get; set; }

    /// <summary>
    /// FetLife username/profile for community verification (optional)
    /// </summary>
    public string? FetLifeName { get; set; }

    /// <summary>
    /// When the user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user account was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the user logged in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Override Id to ensure it's initialized
    /// Prevents EF Core issues with Guid primary keys
    /// </summary>
    public override Guid Id { get; set; } = Guid.NewGuid();

    // Additional fields from existing schema that we'll ignore for this test
    // But need to be here for EF Core mapping
    public string EncryptedLegalName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// User's assigned role (e.g., "Administrator", "Teacher", "SafetyTeam", "EventOrganizer").
    /// Empty string means no special role - this is the default for new registrations.
    /// "Member" is NOT a valid role and should never be assigned.
    /// Valid roles are defined in UserRoleConstants.ValidRoles.
    /// </summary>
    public string Role { get; set; } = "";

    /// <summary>
    /// Indicates whether the user account is active.
    /// </summary>
    /// <remarks>
    /// Future Use: This field will be used for soft-delete functionality.
    /// When false, the user account should be:
    /// - Prevented from logging in
    /// - Hidden from most user listings
    /// - Retained in the database for historical/audit purposes
    /// Note: Soft-delete functionality is not yet implemented as of 2025-11-09.
    /// </remarks>
    public bool IsActive { get; set; } = true;

    public string PronouncedName { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;

    /// <summary>
    /// Other names the member goes by (aliases, former scene names, etc.)
    /// Originated from vetting application, editable in profile
    /// </summary>
    [StringLength(500, ErrorMessage = "Other names cannot exceed 500 characters")]
    public string? OtherNames { get; set; }

    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedOutUntil { get; set; }
    public DateTime? LastPasswordChangeAt { get; set; }

    /// <summary>
    /// Email verification token (GUID-based)
    /// </summary>
    [MaxLength(50)]
    public string EmailVerificationToken { get; set; } = string.Empty;

    public DateTime? EmailVerificationTokenCreatedAt { get; set; }

    /// <summary>
    /// Current vetting status for permission/access control (SOURCE OF TRUTH).
    /// This is the authoritative status used for checking user permissions.
    /// Updated when VettingApplication reaches terminal states (Approved, Denied, etc.)
    ///
    /// Phase 3b-2: Type changed from int to the VettingStatus enum. EF Core
    /// stores enums as their underlying int type by default, so the database
    /// column remains `integer` — no data migration is needed. The generated
    /// migration adds a CHECK constraint (VettingStatus BETWEEN 0 AND 6) to
    /// defend against invalid enum values being written at the DB layer.
    /// </summary>
    public VettingStatus VettingStatus { get; set; } = VettingStatus.UnderReview;

    /// <summary>
    /// Tracks whether the user has submitted a vetting application
    /// Prevents expensive LEFT JOIN queries when displaying member lists
    /// Set to true when a vetting application is created, remains true even if deleted
    /// </summary>
    public bool HasVettingApplication { get; set; } = false;

    /// <summary>
    /// Computed property — user is vetted when VettingStatus is Approved.
    /// This property is not stored in the database and is computed from VettingStatus.
    /// During migration period, still allows setting for backward compatibility,
    /// but the set value is ignored (the property is derived, not stored).
    /// </summary>
    [NotMapped]
    public bool IsVetted
    {
        get => VettingStatus == VettingStatus.Approved;
        set { /* Ignore setter - kept for backward compatibility during migration */ }
    }

    /// <summary>
    /// Indicates whether the user has accepted the Terms of Service
    /// Required for legal compliance
    /// </summary>
    public bool TermsOfServiceAccepted { get; set; } = false;

    /// <summary>
    /// When the user accepted the Terms of Service (UTC)
    /// NULL if not yet accepted
    /// CRITICAL: UTC for PostgreSQL timestamptz compatibility
    /// </summary>
    public DateTime? TermsOfServiceAcceptedAt { get; set; }

    /// <summary>
    /// Override Email property to add explicit MaxLength constraint
    /// </summary>
    [MaxLength(100)]
    public override string? Email { get; set; }

    /// <summary>
    /// Override PhoneNumber property to add explicit MaxLength constraint
    /// </summary>
    [MaxLength(20)]
    public override string? PhoneNumber { get; set; }
}
