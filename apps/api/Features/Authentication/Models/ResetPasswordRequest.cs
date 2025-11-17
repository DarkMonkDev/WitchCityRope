using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// Request model for resetting password with token - Phase 3: Password Reset
/// Uses userId instead of email for security and stability
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// User's unique identifier (GUID)
    /// </summary>
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Password reset token from email link
    /// </summary>
    [Required(ErrorMessage = "Reset token is required")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// New password to set
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    public string NewPassword { get; set; } = string.Empty;
}
