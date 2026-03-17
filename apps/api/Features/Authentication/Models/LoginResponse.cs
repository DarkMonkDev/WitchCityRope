namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// Login response containing JWT token and user data.
/// Used by both user login and service-to-service authentication endpoints.
/// Refresh token fields are [JsonIgnore] because they are set as httpOnly cookies
/// by the endpoint, never exposed in JSON response bodies.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token string for API authentication.
    /// Set as httpOnly cookie by the endpoint.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When the JWT access token expires (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Authenticated user information.
    /// </summary>
    public AuthUserResponse User { get; set; } = new();

    /// <summary>
    /// Validated return URL to redirect to after successful login.
    /// Null if no return URL was provided or validation failed (client should default to /dashboard).
    /// Guaranteed to be safe (OWASP-compliant validation applied).
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Refresh token string for setting the refresh-token httpOnly cookie.
    /// INTERNAL USE ONLY - this value is set on the cookie by the endpoint,
    /// never included in the JSON response body sent to the client.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// When the refresh token expires. Used to set the cookie Expires attribute
    /// for "remember me" sessions (persistent cookie).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// Whether this login used "remember me" - determines if the refresh token
    /// cookie should be persistent (with Expires) or session-only.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool RememberMe { get; set; }
}
