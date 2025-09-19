using System.Collections.Concurrent;

namespace WitchCityRope.Api.Services;

/// <summary>
/// In-memory token blacklist service for handling logout
/// This ensures JWT tokens are invalidated server-side when users log out
///
/// NOTE: This is an in-memory implementation suitable for single-instance deployments.
/// For production with multiple instances, consider Redis or database storage.
/// </summary>
public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens;
    private readonly ILogger<TokenBlacklistService> _logger;
    private readonly Timer _cleanupTimer;

    public TokenBlacklistService(ILogger<TokenBlacklistService> logger)
    {
        _blacklistedTokens = new ConcurrentDictionary<string, DateTime>();
        _logger = logger;

        // Setup a timer to clean up expired tokens every 30 minutes
        _cleanupTimer = new Timer(CleanupExpiredTokensCallback, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
    }

    /// <summary>
    /// Add a token to the blacklist by its JTI (JWT ID)
    /// </summary>
    public void BlacklistToken(string jti, DateTime expirationTime)
    {
        if (string.IsNullOrEmpty(jti))
        {
            _logger.LogWarning("Attempted to blacklist token with empty JTI");
            return;
        }

        _blacklistedTokens.TryAdd(jti, expirationTime);
        _logger.LogInformation("🔐 BLACKLIST DEBUG: Token with JTI {Jti} added to blacklist, expires at {ExpirationTime}. Total blacklisted: {Count}", jti, expirationTime, _blacklistedTokens.Count);
    }

    /// <summary>
    /// Check if a token is blacklisted by its JTI
    /// </summary>
    public bool IsTokenBlacklisted(string jti)
    {
        if (string.IsNullOrEmpty(jti))
        {
            return false;
        }

        var isBlacklisted = _blacklistedTokens.ContainsKey(jti);

        _logger.LogInformation("🔐 BLACKLIST DEBUG: Checking JTI {Jti} - Blacklisted: {IsBlacklisted}. Total in blacklist: {Count}", jti, isBlacklisted, _blacklistedTokens.Count);

        return isBlacklisted;
    }

    /// <summary>
    /// Clean up expired tokens from the blacklist
    /// </summary>
    public void CleanupExpiredTokens()
    {
        var now = DateTime.UtcNow;
        var expiredTokens = _blacklistedTokens
            .Where(kvp => kvp.Value <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var jti in expiredTokens)
        {
            _blacklistedTokens.TryRemove(jti, out _);
        }

        if (expiredTokens.Count > 0)
        {
            _logger.LogInformation("🔐 BLACKLIST DEBUG: Cleaned up {Count} expired tokens from blacklist. Remaining: {Remaining}", expiredTokens.Count, _blacklistedTokens.Count);
        }
    }

    private void CleanupExpiredTokensCallback(object? state)
    {
        try
        {
            CleanupExpiredTokens();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token blacklist cleanup");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}