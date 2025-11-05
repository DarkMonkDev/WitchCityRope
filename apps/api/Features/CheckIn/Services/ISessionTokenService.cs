using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.CheckIn.Services;

/// <summary>
/// Service for managing kiosk mode session tokens
/// Tokens provide scoped access to specific event check-in operations
///
/// CRITICAL: This is NOT user authentication
/// Pattern: Admin generates token → Opens on kiosk device → Volunteers use without login
/// </summary>
public interface ISessionTokenService
{
    /// <summary>
    /// Generate a new check-in session token for an event
    /// Only accessible by administrators via authenticated endpoints
    /// </summary>
    /// <param name="eventId">Event to grant access to</param>
    /// <param name="adminUserId">Admin user generating the token</param>
    /// <param name="expirationHours">Token validity duration (default: 12 hours, supports fractional values for testing)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session token response with check-in URL</returns>
    Task<Result<SessionTokenResponse>> GenerateTokenAsync(
        Guid eventId,
        Guid adminUserId,
        double expirationHours = 12.0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a session token and return associated event ID and staff ID if valid
    /// Called on every check-in API request to verify kiosk access
    /// </summary>
    /// <param name="token">Token string from request header</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>TokenValidationResult with event ID and staff ID if valid, error if invalid/expired/revoked</returns>
    Task<Result<TokenValidationResult>> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke an active session token
    /// Used for emergency revocation if device is lost/compromised
    /// </summary>
    /// <param name="token">Token string to revoke</param>
    /// <param name="adminUserId">Admin user performing revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure result</returns>
    Task<Result> RevokeTokenAsync(
        string token,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active tokens for an event
    /// Admin monitoring of kiosk stations
    /// </summary>
    /// <param name="eventId">Event to get tokens for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active session tokens</returns>
    Task<Result<List<SessionTokenResponse>>> GetActiveTokensForEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}
