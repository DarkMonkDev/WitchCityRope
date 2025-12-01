namespace WitchCityRope.Api.Features.CheckIn.Models;

/// <summary>
/// Request to generate a new check-in session token
/// Admin-only endpoint
/// </summary>
public record GenerateTokenRequest
{
    /// <summary>
    /// Event ID to generate token for
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Session ID to generate token for (REQUIRED)
    /// Tokens are scoped to a specific session for multi-session events
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// Token expiration time in hours (optional, defaults to 12 if not provided)
    /// Supports fractional hours for testing (e.g., 0.001 = 3.6 seconds)
    /// </summary>
    public double? ExpirationHours { get; init; }
}
