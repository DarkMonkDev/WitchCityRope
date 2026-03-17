using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Authentication.Entities;

namespace WitchCityRope.Api.Features.Authentication.Services;

/// <summary>
/// Manages database-backed refresh tokens with rotation and reuse detection.
/// Uses EF Core 7+ bulk operations (ExecuteUpdateAsync/ExecuteDeleteAsync) for efficiency.
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RefreshToken> GenerateRefreshTokenAsync(
        Guid userId, bool rememberMe, string? deviceInfo,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = CreateRefreshTokenEntity(userId, rememberMe, deviceInfo);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Generated refresh token for user {UserId}, expires at {ExpiresAt}, rememberMe: {RememberMe}",
            userId, refreshToken.ExpiresAt, rememberMe);

        return refreshToken;
    }

    /// <inheritdoc />
    public async Task<(RefreshToken? NewToken, Guid? UserId, string? Error)> RotateRefreshTokenAsync(
        string currentToken, string? deviceInfo,
        CancellationToken cancellationToken = default)
    {
        var existingToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == currentToken, cancellationToken);

        if (existingToken is null)
        {
            return (null, null, "Invalid refresh token");
        }

        // REUSE DETECTION: If token is already revoked, someone is replaying a stolen token.
        // This can also happen legitimately when concurrent requests both try to refresh
        // the same token (e.g., 401 interceptor + visibilitychange fire simultaneously).
        // Either way, the safe response is the same: reject and clear cookies.
        if (existingToken.IsRevoked)
        {
            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}. Revoking all tokens. This may indicate token theft or a concurrent refresh race condition.",
                existingToken.UserId);

            await RevokeAllUserTokensAsync(existingToken.UserId, cancellationToken);

            return (null, null, "Token has been revoked. All sessions have been terminated for security.");
        }

        if (existingToken.ExpiresAt < DateTime.UtcNow)
        {
            return (null, null, "Refresh token has expired");
        }

        // ROTATION: Revoke current token and generate new one in a single save.
        // Using a single SaveChangesAsync prevents the race condition where concurrent
        // requests both load the same token before either saves the revocation.
        try
        {
            existingToken.IsRevoked = true;
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.LastUsedAt = DateTime.UtcNow;

            // Determine if this was a "remember me" token based on expiration window
            // Session tokens are <= 24 hours, remember me tokens are longer
            var wasRememberMe = (existingToken.ExpiresAt - existingToken.CreatedAt).TotalHours > 25;

            // Create new token entity without saving yet (single atomic save below)
            var newToken = CreateRefreshTokenEntity(existingToken.UserId, wasRememberMe, deviceInfo);
            _context.RefreshTokens.Add(newToken);

            existingToken.ReplacedByToken = newToken.Token;

            // Single atomic save: revoke old token + create new token
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Rotated refresh token for user {UserId}, rememberMe: {RememberMe}",
                existingToken.UserId, wasRememberMe);

            return (newToken, existingToken.UserId, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another concurrent request already rotated this token.
            // This is expected when multiple API calls fail with 401 simultaneously
            // and both trigger refresh. The first one wins; this one should fail gracefully.
            _logger.LogWarning(
                "Concurrent refresh token rotation detected for user {UserId}. Another request already rotated this token.",
                existingToken.UserId);

            return (null, null, "Token was already refreshed by another request. Please retry.");
        }
    }

    /// <summary>
    /// Creates a RefreshToken entity without saving to the database.
    /// Used by both GenerateRefreshTokenAsync (standalone) and RotateRefreshTokenAsync (atomic save).
    /// </summary>
    private RefreshToken CreateRefreshTokenEntity(Guid userId, bool rememberMe, string? deviceInfo)
    {
        var randomBytes = new byte[64];
        RandomNumberGenerator.Fill(randomBytes);
        var tokenString = Convert.ToBase64String(randomBytes);

        var expiresAt = rememberMe
            ? DateTime.UtcNow.AddDays(
                _configuration.GetValue<int>("RefreshToken:RememberMeExpirationDays", 14))
            : DateTime.UtcNow.AddHours(
                _configuration.GetValue<int>("RefreshToken:SessionExpirationHours", 24));

        return new RefreshToken
        {
            UserId = userId,
            Token = tokenString,
            ExpiresAt = expiresAt,
            DeviceInfo = deviceInfo?.Length > 256 ? deviceInfo[..256] : deviceInfo
        };
    }

    /// <inheritdoc />
    public async Task RevokeAllUserTokensAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var revokedCount = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.IsRevoked, true)
                    .SetProperty(t => t.RevokedAt, DateTime.UtcNow),
                cancellationToken);

        _logger.LogInformation(
            "Revoked {Count} refresh tokens for user {UserId}",
            revokedCount, userId);
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredTokensAsync(
        int retentionDays = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var deletedCount = await _context.RefreshTokens
            .Where(t => t.ExpiresAt < cutoffDate && t.IsRevoked)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation(
            "Cleaned up {Count} expired refresh tokens older than {RetentionDays} days",
            deletedCount, retentionDays);

        return deletedCount;
    }
}
