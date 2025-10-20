namespace WitchCityRope.Api.Models.Auth;

/// <summary>
/// User response DTO for API responses
/// For authentication vertical slice test
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string Role { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; }

    /// <summary>
    /// Constructor to create DTO from ApplicationUser
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
        CreatedAt = user.CreatedAt;
        LastLoginAt = user.LastLoginAt;
        Role = user.Role;
        Roles = new[] { user.Role }; // Frontend expects roles array, provide single role as array
        IsActive = user.IsActive;
    }
}