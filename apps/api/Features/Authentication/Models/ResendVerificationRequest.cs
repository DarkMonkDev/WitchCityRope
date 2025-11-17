using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// Request model for resending email verification - Phase 2: Email Verification
/// </summary>
public class ResendVerificationRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}
