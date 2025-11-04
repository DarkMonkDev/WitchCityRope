namespace WitchCityRope.Api.Features.CheckIn.Models;

/// <summary>
/// Request to revoke an active session token
/// Admin-only endpoint for security incidents
/// </summary>
public record RevokeTokenRequest
{
    /// <summary>
    /// Token string to revoke
    /// </summary>
    public string Token { get; init; } = string.Empty;
}
