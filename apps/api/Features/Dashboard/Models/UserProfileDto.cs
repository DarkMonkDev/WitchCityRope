using WitchCityRope.Api.Features.Vetting.Entities;

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
    /// Other names the member goes by (aliases, former scene names, etc.)
    /// </summary>
    public string? OtherNames { get; set; }

    /// <summary>
    /// Current vetting status enum value
    /// Only meaningful if HasVettingApplication is true
    /// </summary>
    public VettingStatus VettingStatus { get; set; } = VettingStatus.UnderReview;

    /// <summary>
    /// Indicates whether the user has submitted a vetting application
    /// If false, VettingStatus should not be displayed
    /// </summary>
    public bool HasVettingApplication { get; set; } = false;
}
