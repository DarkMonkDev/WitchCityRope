using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// Login request DTO for user authentication
/// Example of simple request model for vertical slice authentication feature
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address or scene name
    /// </summary>
    [Required(ErrorMessage = "Email or Scene Name is required")]
    public string EmailOrSceneName { get; set; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional return URL to redirect to after successful login
    /// Will be validated against OWASP security standards to prevent open redirect attacks
    /// If not provided or validation fails, defaults to /dashboard
    /// </summary>
    public string? ReturnUrl { get; set; }
}