namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// Placeholder RegisterUserRequest for test compilation
/// TODO: Implement full request structure when Authentication feature is developed
/// </summary>
public class RegisterUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}