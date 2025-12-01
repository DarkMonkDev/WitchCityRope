using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.CheckIn.Services;

/// <summary>
/// Session token service for kiosk mode authentication
/// Implements cryptographically secure token generation and validation
///
/// CRITICAL: This is NOT user authentication - tokens provide scoped event access
/// Pattern: Admin generates → Kiosk uses → No volunteer login required
/// </summary>
public class SessionTokenService : ISessionTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SessionTokenService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Token configuration constants
    private const int TOKEN_LENGTH = 64; // 512 bits for cryptographic strength
    private const string TOKEN_CACHE_PREFIX = "checkin_token_";

    public SessionTokenService(
        ApplicationDbContext context,
        ILogger<SessionTokenService> logger,
        IMemoryCache cache,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public async Task<Result<SessionTokenResponse>> GenerateTokenAsync(
        Guid eventId,
        Guid sessionId,
        Guid adminUserId,
        double expirationHours = 12.0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate event exists
            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result<SessionTokenResponse>.Failure("Event not found");
            }

            // Validate session exists and belongs to this event
            var sessionEntity = await _context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.EventId == eventId, cancellationToken);

            if (sessionEntity == null)
            {
                return Result<SessionTokenResponse>.Failure("Session not found or does not belong to this event");
            }

            // Generate cryptographically secure token (512 bits, URL-safe base64)
            var tokenBytes = new byte[TOKEN_LENGTH];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }

            // Convert to URL-safe base64 (replace characters that cause URL issues)
            var token = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            // Calculate expiration
            var expiresAt = DateTime.UtcNow.AddHours(expirationHours);

            _logger.LogInformation(
                "Generating token with {ExpirationHours} hours expiration. Now: {Now}, ExpiresAt: {ExpiresAt}, Duration: {DurationSeconds}s",
                expirationHours, DateTime.UtcNow, expiresAt, (expiresAt - DateTime.UtcNow).TotalSeconds);

            // Store token in database
            var sessionToken = new CheckInSessionToken
            {
                Token = token,
                EventId = eventId,
                SessionId = sessionId,
                CreatedByUserId = adminUserId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IsRevoked = false
            };

            _context.CheckInSessionTokens.Add(sessionToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Cache token for fast validation (TTL matches expiration)
            // CRITICAL: Include SessionId in cache key for session-level scope
            var cacheKey = $"{TOKEN_CACHE_PREFIX}{token}_{sessionId}";
            var cacheDuration = TimeSpan.FromHours(expirationHours);
            _cache.Set(cacheKey, sessionToken, cacheDuration);

            _logger.LogInformation(
                "Generated check-in session token {TokenId} for event {EventId} session {SessionId} by admin {AdminId}, expires {ExpiresAt}",
                sessionToken.Id, eventId, sessionId, adminUserId, expiresAt);

            // Generate check-in URL for frontend (uses dynamically discovered frontend URL)
            var frontendUrl = GetFrontendUrl();
            var checkInUrl = $"{frontendUrl}/events/{eventId}/checkin?token={token}&session={sessionId}";

            return Result<SessionTokenResponse>.Success(new SessionTokenResponse
            {
                Token = token,
                EventId = eventId,
                EventTitle = eventEntity.Title,
                SessionId = sessionId,
                SessionName = sessionEntity.Name,
                CheckInUrl = checkInUrl,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating session token for event {EventId}", eventId);
            return Result<SessionTokenResponse>.Failure("Failed to generate session token");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<TokenValidationResult>> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Cache key is now token-only (SessionId comes from DB query)
            // We can't include SessionId in cache key because we don't have it yet
            var cacheKey = $"{TOKEN_CACHE_PREFIX}{token}";
            if (_cache.TryGetValue<CheckInSessionToken>(cacheKey, out var cachedToken))
            {
                // CRITICAL: Always check expiration even for cached tokens
                // Cache TTL doesn't guarantee immediate eviction
                if (cachedToken.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning(
                        "Cached token {Token} is EXPIRED - removing from cache. ExpiresAt: {ExpiresAt}, Now: {Now}",
                        token.Substring(0, 10), cachedToken.ExpiresAt, DateTime.UtcNow);
                    _cache.Remove(cacheKey);
                    return Result<TokenValidationResult>.Failure("Session token is invalid, revoked, or expired");
                }

                // Validate other rules (revoked, etc.)
                if (IsTokenValid(cachedToken))
                {
                    _logger.LogDebug("Token {Token} validated from cache for session {SessionId}", token.Substring(0, 10), cachedToken.SessionId);
                    return Result<TokenValidationResult>.Success(new TokenValidationResult
                    {
                        EventId = cachedToken.EventId,
                        SessionId = cachedToken.SessionId,
                        CreatedByStaffId = cachedToken.CreatedByUserId
                    });
                }
                else
                {
                    // Invalid for non-expiration reason - remove from cache
                    _cache.Remove(cacheKey);
                }
            }

            // Cache miss or invalid - check database (slow path)
            var sessionToken = await _context.CheckInSessionTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

            if (sessionToken == null)
            {
                _logger.LogWarning("Invalid check-in token attempted: {Token}", token.Substring(0, 10));
                return Result<TokenValidationResult>.Failure("Invalid session token");
            }

            // Validate token rules
            if (!IsTokenValid(sessionToken))
            {
                _logger.LogWarning(
                    "Token validation failed for {Token} - Revoked: {IsRevoked}, Expired: {Expired}",
                    token.Substring(0, 10), sessionToken.IsRevoked, sessionToken.ExpiresAt < DateTime.UtcNow);
                return Result<TokenValidationResult>.Failure("Session token is invalid, revoked, or expired");
            }

            // Update last used timestamp (don't await - fire and forget for performance)
            _ = Task.Run(async () =>
            {
                try
                {
                    var tokenToUpdate = await _context.CheckInSessionTokens
                        .FirstOrDefaultAsync(t => t.Token == token);

                    if (tokenToUpdate != null)
                    {
                        tokenToUpdate.LastUsedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update LastUsedAt for token {Token}", token.Substring(0, 10));
                }
            }, cancellationToken);

            // Refresh cache with updated token
            var remainingTime = sessionToken.ExpiresAt - DateTime.UtcNow;
            if (remainingTime > TimeSpan.Zero)
            {
                _cache.Set(cacheKey, sessionToken, remainingTime);
            }

            _logger.LogDebug("Token {Token} validated from database for session {SessionId}", token.Substring(0, 10), sessionToken.SessionId);
            return Result<TokenValidationResult>.Success(new TokenValidationResult
            {
                EventId = sessionToken.EventId,
                SessionId = sessionToken.SessionId,
                CreatedByStaffId = sessionToken.CreatedByUserId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session token");
            return Result<TokenValidationResult>.Failure("Token validation error");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> RevokeTokenAsync(
        string token,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionToken = await _context.CheckInSessionTokens
                .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

            if (sessionToken == null)
            {
                return Result.Failure("Token not found");
            }

            // Revoke token
            sessionToken.IsRevoked = true;
            sessionToken.RevokedAt = DateTime.UtcNow;
            sessionToken.RevokedByUserId = adminUserId;

            // Explicitly mark as modified (EF Core change tracking pattern)
            _context.CheckInSessionTokens.Update(sessionToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Remove from cache
            var cacheKey = $"{TOKEN_CACHE_PREFIX}{token}";
            _cache.Remove(cacheKey);

            _logger.LogInformation(
                "Session token {TokenId} revoked for event {EventId} by admin {AdminId}",
                sessionToken.Id, sessionToken.EventId, adminUserId);

            // Create audit log entry for revocation
            var auditLog = new CheckInAuditLog(
                sessionToken.EventId,
                "token-revoked",
                $"Check-in session token revoked by admin",
                adminUserId);

            _context.CheckInAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session token");
            return Result.Failure("Failed to revoke token");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<List<SessionTokenResponse>>> GetActiveTokensForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokens = await _context.CheckInSessionTokens
                .AsNoTracking()
                .Include(t => t.Event)
                .Include(t => t.Session)
                .Where(t => t.EventId == eventId
                         && !t.IsRevoked
                         && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            // Get frontend URL dynamically from request
            var frontendUrl = GetFrontendUrl();

            var responses = tokens.Select(t => new SessionTokenResponse
            {
                Token = t.Token,
                EventId = t.EventId,
                EventTitle = t.Event?.Title ?? "Unknown Event",
                SessionId = t.SessionId,
                SessionName = t.Session?.Name ?? "Unknown Session",
                CheckInUrl = $"{frontendUrl}/events/{t.EventId}/checkin?token={t.Token}&session={t.SessionId}",
                CreatedAt = t.CreatedAt,
                ExpiresAt = t.ExpiresAt
            }).ToList();

            _logger.LogInformation(
                "Retrieved {Count} active session tokens for event {EventId}",
                responses.Count, eventId);

            return Result<List<SessionTokenResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active tokens for event {EventId}", eventId);
            return Result<List<SessionTokenResponse>>.Failure("Failed to retrieve active tokens");
        }
    }

    /// <summary>
    /// Get frontend URL dynamically from HTTP request context
    /// Discovers the URL from Origin/Referer headers or request host
    /// Ensures check-in links work in any environment without hardcoded configuration
    /// </summary>
    private string GetFrontendUrl()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null, falling back to localhost");
            return "http://localhost:5173"; // Fallback for non-HTTP contexts (e.g., background jobs)
        }

        // Try Origin header first (most reliable for CORS requests)
        var origin = httpContext.Request.Headers["Origin"].ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            _logger.LogDebug("Frontend URL discovered from Origin header: {Url}", origin);
            return origin;
        }

        // Try Referer header (contains the page that made the request)
        var referer = httpContext.Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer))
        {
            var uri = new Uri(referer);
            var url = $"{uri.Scheme}://{uri.Authority}";
            _logger.LogDebug("Frontend URL discovered from Referer header: {Url}", url);
            return url;
        }

        // Fallback to request host (API host, but may not match frontend)
        var request = httpContext.Request;
        var fallbackUrl = $"{request.Scheme}://{request.Host}";
        _logger.LogDebug("Frontend URL fallback to request host: {Url}", fallbackUrl);
        return fallbackUrl;
    }

    /// <summary>
    /// Validate token rules (not revoked, not expired)
    /// Centralized validation logic used by both cache and database paths
    /// </summary>
    private bool IsTokenValid(CheckInSessionToken token)
    {
        // Rule 1: Token must not be revoked
        if (token.IsRevoked)
        {
            _logger.LogWarning("Token {TokenPrefix} is revoked", token.Token.Substring(0, 10));
            return false;
        }

        // Rule 2: Token must not be expired
        var now = DateTime.UtcNow;
        var isExpired = token.ExpiresAt < now;

        if (isExpired)
        {
            _logger.LogWarning(
                "Token {TokenPrefix} is EXPIRED - ExpiresAt: {ExpiresAt}, Now: {Now}, Diff: {Diff}ms",
                token.Token.Substring(0, 10), token.ExpiresAt, now, (now - token.ExpiresAt).TotalMilliseconds);
            return false;
        }

        _logger.LogDebug(
            "Token {TokenPrefix} is VALID - ExpiresAt: {ExpiresAt}, Now: {Now}, TimeLeft: {TimeLeft}ms",
            token.Token.Substring(0, 10), token.ExpiresAt, now, (token.ExpiresAt - now).TotalMilliseconds);

        return true;
    }
}
