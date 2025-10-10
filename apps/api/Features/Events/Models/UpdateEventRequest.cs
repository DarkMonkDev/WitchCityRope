namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Request model for updating an existing event
/// Supports partial updates - only non-null fields will be updated
/// </summary>
public class UpdateEventRequest
{
    /// <summary>
    /// Updated event title (optional)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Updated short description (optional)
    /// Brief summary for event cards and listings
    /// </summary>
    public string? ShortDescription { get; set; }

    /// <summary>
    /// Updated full event description (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Updated event policies and safety guidelines (optional)
    /// </summary>
    public string? Policies { get; set; }

    /// <summary>
    /// Updated event start date/time in UTC (optional)
    /// CRITICAL: Must be UTC for PostgreSQL TIMESTAMPTZ compatibility
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Updated event end date/time in UTC (optional)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Updated event location (optional)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Updated maximum number of attendees (optional)
    /// Cannot be reduced below current attendance
    /// </summary>
    public int? Capacity { get; set; }

    /// <summary>
    /// Updated pricing information as JSON (optional)
    /// </summary>
    public string? PricingTiers { get; set; }

    /// <summary>
    /// Updated publishing status (optional)
    /// </summary>
    public bool? IsPublished { get; set; }

    /// <summary>
    /// Updated sessions list (optional)
    /// If provided, will replace all existing sessions with these
    /// </summary>
    public List<SessionDto>? Sessions { get; set; }

    /// <summary>
    /// Updated ticket types list (optional)
    /// If provided, will replace all existing ticket types with these
    /// </summary>
    public List<TicketTypeDto>? TicketTypes { get; set; }

    /// <summary>
    /// Updated teacher/organizer user IDs (optional)
    /// If provided, will replace all existing teacher associations with these
    /// </summary>
    public List<string>? TeacherIds { get; set; }

    /// <summary>
    /// Updated volunteer positions list (optional)
    /// If provided, will replace all existing volunteer positions with these
    /// </summary>
    public List<VolunteerPositionDto>? VolunteerPositions { get; set; }
}