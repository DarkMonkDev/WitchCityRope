namespace WitchCityRope.Api.Features.Users.Models;

/// <summary>
/// Simple DTO for user dropdown options (e.g., teacher selection)
/// </summary>
public class UserOptionDto
{
    /// <summary>
    /// User ID (for form values)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name (scene name or email)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// User email (for additional context)
    /// </summary>
    public string Email { get; set; } = string.Empty;
}