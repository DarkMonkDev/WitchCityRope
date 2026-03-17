namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// JWT token model returned by IJwtService.GenerateToken().
/// Contains the token string and its expiration time.
/// </summary>
public class JwtToken
{
    /// <summary>
    /// JWT token string (signed, base64-encoded).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When the token expires (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
