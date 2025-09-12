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
    /// Updated event description (optional)
    /// </summary>
    public string? Description { get; set; }

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
}