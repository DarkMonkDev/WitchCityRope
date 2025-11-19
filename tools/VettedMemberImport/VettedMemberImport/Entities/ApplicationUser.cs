using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace VettedMemberImport.Entities;

/// <summary>
/// Application user entity for import tool
/// Subset of full ApplicationUser from API - only fields needed for import
/// </summary>
[Table("Users")]
public class ApplicationUser : IdentityUser<Guid>
{
    public string SceneName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? DiscordName { get; set; }
    public string? FetLifeName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public override Guid Id { get; set; } = Guid.NewGuid();
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
    public int VettingStatus { get; set; } = 0;
    public bool HasVettingApplication { get; set; } = false;
    public bool TermsOfServiceAccepted { get; set; } = false;
    public DateTime? TermsOfServiceAcceptedAt { get; set; }
}
