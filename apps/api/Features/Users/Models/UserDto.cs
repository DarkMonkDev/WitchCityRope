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
    public string Role { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int VettingStatus { get; set; }
    public bool HasVettingApplication { get; set; } // True if user has submitted a vetting application

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
        Role = user.Role;
        Pronouns = user.Pronouns;
        IsActive = user.IsActive;
        EmailConfirmed = user.EmailConfirmed;
        CreatedAt = user.CreatedAt;
        LastLoginAt = user.LastLoginAt;
        VettingStatus = user.VettingStatus;
        HasVettingApplication = user.HasVettingApplication;
    }
}