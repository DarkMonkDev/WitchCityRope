using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// User response DTO for authentication endpoints
/// Example of user model for vertical slice authentication feature
/// </summary>
public class AuthUserResponse
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's scene name
    /// </summary>
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// When the user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the user last logged in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// User's role in the system
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// User's roles as an array (for frontend compatibility)
    /// </summary>
    public string[] Roles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public AuthUserResponse() { }

    /// <summary>
    /// Constructor to create DTO from ApplicationUser entity
    /// </summary>
    public AuthUserResponse(ApplicationUser user)
    {
        Id = user.Id;
        Email = user.Email ?? string.Empty;
        SceneName = user.SceneName;
        CreatedAt = user.CreatedAt;
        LastLoginAt = user.LastLoginAt;
        Role = user.Role;
        Roles = new[] { user.Role }; // Frontend expects roles array, provide single role as array
        IsActive = user.IsActive;
    }
}