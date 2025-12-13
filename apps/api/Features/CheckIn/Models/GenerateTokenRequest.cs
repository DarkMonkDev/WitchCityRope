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
    /// Session ID to generate token for (backwards compatibility)
    /// If provided and SessionIds is empty, generates single-session token
    /// If SessionIds is provided, this field is ignored
    /// </summary>
    public Guid? SessionId { get; init; }

    /// <summary>
    /// Multiple session IDs for multi-session tokens (preferred)
    /// If provided, SessionId is ignored
    /// Allows one token to grant access to multiple sessions (e.g., morning + afternoon)
    /// </summary>
    public List<Guid>? SessionIds { get; init; }

    /// <summary>
    /// Token expiration time in hours (optional, defaults to 12 if not provided)
    /// Supports fractional hours for testing (e.g., 0.001 = 3.6 seconds)
    /// </summary>
    public double? ExpirationHours { get; init; }
}
