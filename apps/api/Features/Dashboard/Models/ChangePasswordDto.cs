using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Dashboard.Models;

/// <summary>
/// Request model for changing user password
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// Current password for verification
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password (minimum 8 characters, must contain uppercase, lowercase, and number)
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation (must match NewPassword)
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
