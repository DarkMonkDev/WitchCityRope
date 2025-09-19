namespace WitchCityRope.Api.Services;

/// <summary>
/// Service for managing blacklisted JWT tokens to handle logout
/// This ensures tokens are invalidated server-side when users log out
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Add a token to the blacklist by its JTI (JWT ID)
    /// </summary>
    /// <param name="jti">The JWT ID to blacklist</param>
    /// <param name="expirationTime">When the token would naturally expire</param>
    void BlacklistToken(string jti, DateTime expirationTime);

    /// <summary>
    /// Check if a token is blacklisted by its JTI
    /// </summary>
    /// <param name="jti">The JWT ID to check</param>
    /// <returns>True if the token is blacklisted</returns>
    bool IsTokenBlacklisted(string jti);

    /// <summary>
    /// Clean up expired tokens from the blacklist
    /// </summary>
    void CleanupExpiredTokens();
}