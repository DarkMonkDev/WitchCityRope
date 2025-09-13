using WitchCityRope.Api.Models;
using WitchCityRope.Api.Models.Auth;

namespace WitchCityRope.Api.Services;

/// <summary>
/// JWT token generation and validation service
/// For authentication vertical slice test
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate JWT token for authenticated user
    /// </summary>
    /// <param name="user">User to generate token for</param>
    /// <returns>JWT token with expiration</returns>
    JwtToken GenerateToken(ApplicationUser user);

    /// <summary>
    /// Validate JWT token (for future use)
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <returns>True if valid</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// Check if token is near expiry for refresh purposes
    /// </summary>
    /// <param name="token">Token to check</param>
    /// <returns>True if token expires within 30 minutes</returns>
    bool IsTokenNearExpiry(string token);

    /// <summary>
    /// Validate token structure without checking expiry (for refresh scenarios)
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <returns>True if token structure is valid</returns>
    bool ValidateTokenStructure(string token);
}