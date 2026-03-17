using WitchCityRope.Api.Features.Authentication.Entities;

namespace WitchCityRope.Api.Features.Authentication.Services;

/// <summary>
/// Service for managing database-backed refresh tokens with rotation and reuse detection.
/// Implements OWASP-recommended token lifecycle for persistent login sessions.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Generate a new refresh token for a user after successful login.
    /// Token expiration depends on rememberMe flag:
    /// - true: 14 days (configurable via RefreshToken:RememberMeExpirationDays)
    /// - false: 24 hours (configurable via RefreshToken:SessionExpirationHours)
    /// </summary>
    Task<RefreshToken> GenerateRefreshTokenAsync(
        Guid userId, bool rememberMe, string? deviceInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate and rotate a refresh token. Returns new token + user ID if valid.
    ///
    /// SECURITY: Implements reuse detection - if a revoked token is presented,
    /// ALL tokens for that user are immediately revoked (indicates token theft).
    ///
    /// Flow:
    /// 1. Look up token in database
    /// 2. If not found -> error
    /// 3. If revoked -> REUSE DETECTION: revoke ALL user tokens, return error
    /// 4. If expired -> return error
    /// 5. Otherwise -> rotate: revoke current, generate new, return new token + UserId
    /// </summary>
    Task<(RefreshToken? NewToken, Guid? UserId, string? Error)> RotateRefreshTokenAsync(
        string currentToken, string? deviceInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all refresh tokens for a user. Used on:
    /// - Logout (revoke all sessions)
    /// - Reuse detection (security measure)
    /// - Password change (force re-authentication)
    /// </summary>
    Task RevokeAllUserTokensAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired and revoked tokens older than retentionDays.
    /// Called by Hangfire scheduled job daily at 4 AM UTC.
    /// </summary>
    Task<int> CleanupExpiredTokensAsync(
        int retentionDays = 30,
        CancellationToken cancellationToken = default);
}
