using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

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
    /// Required field, must be unique
    /// </summary>
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// User's first name (optional)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name (optional)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// User's bio/description (optional, max 2000 characters)
    /// </summary>
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
    public string Role { get; set; } = "Member";
    public bool IsActive { get; set; } = true;
    public string PronouncedName { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedOutUntil { get; set; }
    public DateTime? LastPasswordChangeAt { get; set; }
    public string EmailVerificationToken { get; set; } = string.Empty;
    public DateTime? EmailVerificationTokenCreatedAt { get; set; }

    /// <summary>
    /// Current vetting status for permission/access control (SOURCE OF TRUTH)
    /// This is the authoritative status used for checking user permissions.
    /// Updated when VettingApplication reaches terminal states (Approved, Denied, etc.)
    /// </summary>
    public int VettingStatus { get; set; } = 0;

    /// <summary>
    /// Tracks whether the user has submitted a vetting application
    /// Prevents expensive LEFT JOIN queries when displaying member lists
    /// Set to true when a vetting application is created, remains true even if deleted
    /// </summary>
    public bool HasVettingApplication { get; set; } = false;

    /// <summary>
    /// Computed property - User is vetted when VettingStatus is Approved (3)
    /// This property is not stored in the database and is computed from VettingStatus
    /// During migration period, still allows setting for backward compatibility, but value is ignored
    /// </summary>
    [NotMapped]
    public bool IsVetted
    {
        get => VettingStatus == 3;
        set { /* Ignore setter - kept for backward compatibility during migration */ }
    }
}
