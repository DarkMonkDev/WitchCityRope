using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// Request model for email verification - Phase 2: Email Verification
/// Uses userId instead of email for security and stability (email addresses can change)
/// </summary>
public class VerifyEmailRequest
{
    /// <summary>
    /// User's unique identifier (GUID)
    /// </summary>
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email verification token
    /// </summary>
    [Required(ErrorMessage = "Verification token is required")]
    public string Token { get; set; } = string.Empty;
}
