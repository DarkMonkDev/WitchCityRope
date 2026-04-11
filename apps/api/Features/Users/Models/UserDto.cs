using WitchCityRope.Api.Features.Users.Constants;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Users.Models;

/// <summary>
/// User DTO for user management endpoints
/// Follows the simplified vertical slice architecture pattern
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string? DiscordName { get; set; }
    public string? Bio { get; set; }
    public string Role { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>(); // Frontend expects roles array
    public string Pronouns { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Vetting status for the user.
    ///
    /// Phase 3b-1: Type changed from int to the VettingStatus enum. Because
    /// JsonStringEnumConverter is registered globally (Program.cs), this
    /// field now ships as a JSON string ("UnderReview", "Approved", etc.)
    /// instead of a raw integer. Frontend admin code (AdminDashboardPage,
    /// MembersList) has been migrated to consume the string directly.
    ///
    /// The ApplicationUser entity still stores this as an int; the cast
    /// happens in this DTO's constructor. Phase 3b-2 will normalize the
    /// entity too, at which point the cast in the constructor can be
    /// removed.
    /// </summary>
    public VettingStatus VettingStatus { get; set; }

    public bool HasVettingApplication { get; set; } // True if user has submitted a vetting application
    public bool IsVetted { get; set; } // Computed from VettingStatus == Approved

    /// <summary>
    /// Default constructor
    /// </summary>
    public UserDto() { }

    /// <summary>
    /// Constructor to create DTO from ApplicationUser entity
    /// </summary>
    public UserDto(ApplicationUser user)
    {
        Id = user.Id;
        Email = user.Email ?? string.Empty;
        SceneName = user.SceneName;
        DiscordName = user.DiscordName;
        Bio = user.Bio;
        Role = user.Role;
        Roles = UserRoleConstants.ParseRoles(user.Role); // Multi-role support: parse CSV roles into array
        Pronouns = user.Pronouns;
        IsActive = user.IsActive;
        EmailConfirmed = user.EmailConfirmed;
        CreatedAt = user.CreatedAt;
        LastLoginAt = user.LastLoginAt;
        // Phase 3b-1 cast: the entity still stores VettingStatus as int.
        // Phase 3b-2 will change the entity to the enum type, removing this cast.
        VettingStatus = (VettingStatus)user.VettingStatus;
        HasVettingApplication = user.HasVettingApplication;
        IsVetted = user.IsVetted; // Computed property from ApplicationUser (VettingStatus == Approved)
    }
}