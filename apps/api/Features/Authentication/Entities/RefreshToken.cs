using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Authentication.Entities;

/// <summary>
/// Database-backed refresh token for persistent login sessions.
/// Implements token rotation with reuse detection per OWASP guidelines.
///
/// SECURITY MODEL:
/// - Each login creates a new refresh token stored in the database
/// - On each refresh, the old token is revoked and a new one issued (rotation)
/// - If a revoked token is presented (reuse detection), ALL user tokens are revoked
///   because this indicates the token was stolen and used by an attacker
/// - Tokens are stored in httpOnly cookies scoped to /api/auth path
///
/// EXPIRATION:
/// - RememberMe ON: 14 days
/// - RememberMe OFF: 24 hours (session cookie, no browser-side persistence)
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to ApplicationUser. CASCADE delete ensures tokens are cleaned up
    /// when a user account is deleted.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Cryptographically secure random token (64 bytes, base64-encoded, ~88 chars).
    /// Generated using RandomNumberGenerator for CSPRNG security.
    /// This value is stored in the refresh-token httpOnly cookie.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When this token was created (UTC). Used for audit trail.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this token expires (UTC). After this time, the token cannot be used
    /// for refresh even if it hasn't been revoked.
    /// 24 hours for session tokens, 14 days for "remember me" tokens.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether this token has been revoked. Tokens are revoked when:
    /// 1. Used for refresh (rotation - replaced by new token)
    /// 2. User logs out (all tokens revoked)
    /// 3. Reuse detected (all tokens revoked - security measure)
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// When this token was revoked (UTC). Null if still active.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// The token that replaced this one during rotation.
    /// Used to trace the rotation chain for security auditing.
    /// If a revoked token is presented, we can trace forward to find
    /// and revoke the entire chain.
    /// </summary>
    [MaxLength(128)]
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// Truncated User-Agent string from the request that created this token.
    /// Used for audit trail and "active sessions" display.
    /// Truncated to 256 chars to prevent storage bloat.
    /// </summary>
    [MaxLength(256)]
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Last time this token was used for a refresh operation (UTC).
    /// Updated on each successful refresh before the token is rotated.
    /// Useful for monitoring active sessions.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    // Navigation property
    public ApplicationUser User { get; set; } = null!;

    public RefreshToken()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsRevoked = false;
    }
}
