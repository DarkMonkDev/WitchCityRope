using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.ProxyRsvp.Models;

/// <summary>
/// Request DTO for creating a proxy RSVP on behalf of a principal.
/// The caller (delegate) creates an RSVP for the specified user (principal)
/// who must then accept the event waiver before the RSVP becomes active.
/// See: BR-050 (same authorization), BR-051 (pending acceptance flow), BR-054 (capacity check).
/// </summary>
public class CreateProxyRsvpRequest
{
    /// <summary>
    /// The user ID of the principal to RSVP for.
    /// The principal must have authorized the caller as their delegate.
    /// </summary>
    [Required]
    public Guid RsvpForUserId { get; set; }

    /// <summary>
    /// Optional notes from the delegate about this proxy RSVP.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }
}

/// <summary>
/// Request DTO for the principal accepting a proxy RSVP.
/// The principal must personally accept the event waiver (BR-053, AD-003).
/// Optionally accepts platform Terms of Service if not previously accepted (BR-031).
/// </summary>
public class AcceptProxyRsvpRequest
{
    /// <summary>
    /// Whether the user accepts the event waiver. Must be true to accept.
    /// The waiver must be personally accepted by the actual attendee (BR-030, BR-053).
    /// </summary>
    [Required]
    public bool EventWaiverAccepted { get; set; }

    /// <summary>
    /// Whether the user accepts the platform Terms of Service.
    /// Only required if the user has not previously accepted the ToS (BR-031).
    /// </summary>
    public bool TermsOfServiceAccepted { get; set; }
}

/// <summary>
/// Response DTO for proxy RSVP operations.
/// Provides details about the proxy RSVP including the principal, delegate, and status.
/// Reuses TicketAssignmentDto structure from the API design (Section 1C) for consistency
/// across both ticket assignment and proxy RSVP responses.
/// </summary>
public class ProxyRsvpDto
{
    /// <summary>
    /// The EventAttendance record ID.
    /// </summary>
    public Guid AttendanceId { get; set; }

    /// <summary>
    /// The event this RSVP is for.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// The event title for display.
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the RSVP (e.g., "PendingAcceptance", "Active", "Cancelled").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Scene name of the principal (the person the RSVP is for).
    /// </summary>
    public string? RsvpForSceneName { get; set; }

    /// <summary>
    /// User ID of the principal.
    /// </summary>
    public Guid? RsvpForUserId { get; set; }

    /// <summary>
    /// Scene name of the delegate who created the proxy RSVP.
    /// </summary>
    public string? CreatedBySceneName { get; set; }

    /// <summary>
    /// User ID of the delegate who created the proxy RSVP.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// When the proxy RSVP was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
