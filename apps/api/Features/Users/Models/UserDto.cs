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
    public string Role { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsVetted { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int VettingStatus { get; set; }

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
        Role = user.Role;
        Pronouns = user.Pronouns;
        IsActive = user.IsActive;
        IsVetted = user.IsVetted;
        EmailConfirmed = user.EmailConfirmed;
        CreatedAt = user.CreatedAt;
        LastLoginAt = user.LastLoginAt;
        VettingStatus = user.VettingStatus;
    }
}