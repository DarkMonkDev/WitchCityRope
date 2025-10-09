namespace WitchCityRope.Api.Features.Dashboard.Models;

/// <summary>
/// User profile information for settings page
/// </summary>
public class UserProfileDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Scene name
    /// </summary>
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// First name (optional)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name (optional)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's pronouns
    /// </summary>
    public string? Pronouns { get; set; }

    /// <summary>
    /// User's bio
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Discord username
    /// </summary>
    public string? DiscordName { get; set; }

    /// <summary>
    /// FetLife username/profile
    /// </summary>
    public string? FetLifeName { get; set; }

    /// <summary>
    /// Phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Current vetting status for display
    /// </summary>
    public string VettingStatus { get; set; } = string.Empty;
}
