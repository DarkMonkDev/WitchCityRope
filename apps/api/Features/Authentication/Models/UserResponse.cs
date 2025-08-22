namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// User response DTO for API responses
/// Optimized for NSwag TypeScript generation
/// </summary>
public class UserResponse
{
    /// <summary>
    /// Unique user identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's scene name (display name)
    /// </summary>
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// User's role in the system
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// When the user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the user last logged in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the user's email is confirmed
    /// </summary>
    public bool EmailConfirmed { get; set; }
}

/// <summary>
/// Login response with user data and token
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT token for API authentication
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When the token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// User information
    /// </summary>
    public AuthUserResponse User { get; set; } = new();
}