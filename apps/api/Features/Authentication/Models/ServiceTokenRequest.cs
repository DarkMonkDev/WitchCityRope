namespace WitchCityRope.Api.Features.Authentication.Models;

/// <summary>
/// Service token request for service-to-service authentication
/// Example of service-to-service authentication model for vertical slice authentication feature
/// </summary>
public class ServiceTokenRequest
{
    /// <summary>
    /// User ID for token generation
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address for validation
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// JWT token model for authentication responses
/// Example of JWT token model for vertical slice authentication feature
/// </summary>
public class JwtToken
{
    /// <summary>
    /// JWT token string
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration timestamp
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}