namespace WitchCityRope.Api.Features.CheckIn.Models;

/// <summary>
/// Session token response DTO
/// Returned when admin generates a new check-in session token
/// </summary>
public class SessionTokenResponse
{
    /// <summary>
    /// The session token string (64 characters, URL-safe)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Event this token grants access to
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Event title for display
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Session this token grants check-in access to (backwards compatibility, single session)
    /// For multi-session tokens, check SessionIds instead
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Session name for display (backwards compatibility, single session)
    /// For multi-session tokens, check SessionNames instead
    /// </summary>
    public string? SessionName { get; set; }

    /// <summary>
    /// All session IDs this token grants access to (multi-session support)
    /// </summary>
    public List<Guid>? SessionIds { get; set; }

    /// <summary>
    /// All session names for display (multi-session support)
    /// </summary>
    public List<string>? SessionNames { get; set; }

    /// <summary>
    /// When the token was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the token expires (UTC)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Full check-in URL with token parameter
    /// Example: /checkin?token={token}&event={eventId}&session={sessionId}
    /// Admin can share this URL or QR code with volunteers
    /// </summary>
    public string CheckInUrl { get; set; } = string.Empty;
}
