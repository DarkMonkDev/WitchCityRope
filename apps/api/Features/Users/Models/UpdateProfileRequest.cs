namespace WitchCityRope.Api.Features.Users.Models;

/// <summary>
/// Request model for updating user profile
/// Follows the simplified vertical slice architecture pattern
/// </summary>
public class UpdateProfileRequest
{
    public string SceneName { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;
}