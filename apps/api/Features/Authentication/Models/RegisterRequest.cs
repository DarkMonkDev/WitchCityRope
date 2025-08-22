using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// User registration request DTO for vertical slice architecture
/// Example of simple request model with validation attributes for NSwag generation
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's email address (must be unique)
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(256, ErrorMessage = "Email must not exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password for the account
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// User's scene name (display name in community)
    /// </summary>
    [Required(ErrorMessage = "Scene name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Scene name must be between 2 and 50 characters")]
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// User's date of birth for age verification
    /// </summary>
    [Required(ErrorMessage = "Date of birth is required")]
    public DateTime DateOfBirth { get; set; }
}

/// <summary>
/// User login request DTO
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Remember login for extended session
    /// </summary>
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Service token request for service-to-service authentication
/// </summary>
public class ServiceTokenRequest
{
    /// <summary>
    /// User ID for token generation
    /// </summary>
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email for validation
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}