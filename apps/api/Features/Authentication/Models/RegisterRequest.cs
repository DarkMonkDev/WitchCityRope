using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// Registration request DTO for new user accounts
/// Example of registration model for vertical slice authentication feature
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Valid email address is required")]
    [StringLength(254, ErrorMessage = "Email must not exceed 254 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User's scene name
    /// </summary>
    [Required(ErrorMessage = "Scene name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Scene name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Scene name can only contain letters, numbers, underscores, and hyphens")]
    public string SceneName { get; set; } = string.Empty;
}