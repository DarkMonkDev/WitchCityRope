namespace WitchCityRope.Api.Models.Auth;

/// <summary>
/// Login response containing JWT token and user data
/// For service-to-service authentication
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
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