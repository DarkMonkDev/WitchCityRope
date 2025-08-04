using System;

namespace WitchCityRope.Core.DTOs;

/// <summary>
/// Response model for RSVP operations
/// </summary>
public class RsvpResponse
{
    /// <summary>
    /// The RSVP ID
    /// </summary>
    public Guid RsvpId { get; set; }

    /// <summary>
    /// The event ID
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// The user ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Current status of the RSVP
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Success or error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of guests included in the RSVP
    /// </summary>
    public int? GuestCount { get; set; }

    /// <summary>
    /// Timestamp when the RSVP was created or updated
    /// </summary>
    public DateTime Timestamp { get; set; }
}