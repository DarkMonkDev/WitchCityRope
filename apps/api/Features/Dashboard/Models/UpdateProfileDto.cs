using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Dashboard.Models;

/// <summary>
/// Request model for updating user profile
/// </summary>
public class UpdateProfileDto
{
    /// <summary>
    /// Scene name (required, 3-50 characters)
    /// </summary>
    [Required(ErrorMessage = "Scene name is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Scene name must be between 3 and 50 characters")]
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// First name (optional)
    /// </summary>
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name (optional)
    /// </summary>
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public string? LastName { get; set; }

    /// <summary>
    /// Email address (required, must be valid email format)
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's pronouns (optional)
    /// </summary>
    [StringLength(50, ErrorMessage = "Pronouns cannot exceed 50 characters")]
    public string? Pronouns { get; set; }

    /// <summary>
    /// User's bio (optional, max 2000 characters)
    /// </summary>
    [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
    public string? Bio { get; set; }

    /// <summary>
    /// Discord username (optional)
    /// </summary>
    [StringLength(100, ErrorMessage = "Discord name cannot exceed 100 characters")]
    public string? DiscordName { get; set; }

    /// <summary>
    /// FetLife username/profile (optional)
    /// </summary>
    [StringLength(100, ErrorMessage = "FetLife name cannot exceed 100 characters")]
    public string? FetLifeName { get; set; }

    /// <summary>
    /// Phone number (optional)
    /// </summary>
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? PhoneNumber { get; set; }
}
