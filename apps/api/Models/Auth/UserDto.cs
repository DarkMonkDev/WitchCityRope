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
    }
}