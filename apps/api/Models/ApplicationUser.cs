using Microsoft.AspNetCore.Identity;

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
    public bool IsVetted { get; set; } = false;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedOutUntil { get; set; }
    public DateTime? LastPasswordChangeAt { get; set; }
    public string EmailVerificationToken { get; set; } = string.Empty;
    public DateTime? EmailVerificationTokenCreatedAt { get; set; }
    public int VettingStatus { get; set; } = 0;
}