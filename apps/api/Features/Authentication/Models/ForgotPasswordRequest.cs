using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// Request model for initiating password reset - Phase 3: Password Reset
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// User's email address to send password reset link
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}
