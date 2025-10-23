using WitchCityRope.Api.Features.Authentication.Models;

namespace WitchCityRope.Api.Models.Auth;

/// <summary>
/// Login response containing JWT token and user data
/// For service-to-service authentication
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AuthUserResponse User { get; set; } = new();

    /// <summary>
    /// Validated return URL to redirect to after successful login
    /// Null if no return URL was provided or validation failed (client should default to /dashboard)
    /// Guaranteed to be safe (OWASP-compliant validation applied)
    /// </summary>
    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Service token request for service-to-service authentication
/// </summary>
public class ServiceTokenRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// JWT token model
/// </summary>
public class JwtToken
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}