namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// Service token request for service-to-service authentication.
/// Used by the /api/auth/service-token endpoint to generate JWT tokens
/// for a given user without requiring their password.
/// Authentication is via X-Service-Secret header instead.
/// </summary>
public class ServiceTokenRequest
{
    /// <summary>
    /// User ID for token generation.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address for validation.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
