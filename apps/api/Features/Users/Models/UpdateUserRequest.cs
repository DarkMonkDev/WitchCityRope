namespace WitchCityRope.Api.Features.Users.Models;

/// <summary>
/// Request model for admin user updates
/// Follows the simplified vertical slice architecture pattern
/// </summary>
public class UpdateUserRequest
{
    public string? SceneName { get; set; }
    public string? Role { get; set; }
    public string? Pronouns { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsVetted { get; set; }
    public bool? EmailConfirmed { get; set; }
    public int? VettingStatus { get; set; }
}