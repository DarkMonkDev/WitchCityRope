namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Request model for creating test events programmatically
/// ONLY available in Development/Test environments
/// </summary>
public class CreateTestEventRequest
{
    /// <summary>
    /// Event title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Brief summary for event cards (optional)
    /// </summary>
    public string? ShortDescription { get; set; }

    /// <summary>
    /// Full detailed event description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Event start date/time in UTC
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Event end date/time in UTC
    /// </summary>
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// Whether free RSVPs are enabled (default: false for test events)
    /// </summary>
    public bool AllowRsvps { get; set; } = false;

    /// <summary>
    /// Whether ticket purchase is mandatory (default: true for test events)
    /// </summary>
    public bool RequireTicketPurchase { get; set; } = true;

    /// <summary>
    /// Whether only vetted members can attend (default: false)
    /// </summary>
    public bool VettedMembersOnly { get; set; } = false;

    /// <summary>
    /// Event status (Draft = 0, Published = 1, Cancelled = 2)
    /// Default: Published
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// Whether event is published/visible
    /// Default: true
    /// </summary>
    public bool IsPublished { get; set; } = true;

    /// <summary>
    /// Maximum capacity for the event
    /// Default: 20
    /// </summary>
    public int Capacity { get; set; } = 20;

    /// <summary>
    /// Venue ID (optional, can be null for tests)
    /// Default: 1 (test venue)
    /// </summary>
    public int? VenueId { get; set; } = 1;
}
