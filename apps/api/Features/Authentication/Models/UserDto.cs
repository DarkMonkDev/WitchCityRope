namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// Placeholder UserDto for test compilation
/// TODO: Implement full UserDto structure when Authentication feature is developed
/// </summary>
public class UserDto
{
    public string? Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? SceneName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; }
    public bool IsVetted { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public string? LastLoginAt { get; set; }
}