using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Models.Auth;

/// <summary>
/// Registration request DTO for new user accounts
/// For authentication vertical slice test
/// </summary>
public class RegisterDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Valid email address is required")]
    [StringLength(254, ErrorMessage = "Email must not exceed 254 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Scene name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Scene name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Scene name can only contain letters, numbers, underscores, and hyphens")]
    public string SceneName { get; set; } = string.Empty;
}