using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Models.Auth;

/// <summary>
/// Login request DTO for user authentication
/// For authentication vertical slice test
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Valid email address is required")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}