namespace WitchCityRope.Api.Features.Logging.Entities;

/// <summary>
/// Aggregated daily log summary written by Hangfire background jobs.
/// Used for admin dashboards and trend analysis.
/// </summary>
public class DailyLogSummary
{
    public long Id { get; set; }

    /// <summary>
    /// The date being summarized (UTC date only)
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Summary category (e.g. "cc_failure", "auth_failure", "api_error")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Optional subcategory (e.g. "card_declined", "invalid_credentials")
    /// </summary>
    public string? Subcategory { get; set; }

    /// <summary>
    /// Number of occurrences for this date/category/subcategory
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Additional context stored as JSONB (e.g. sample error messages, affected users)
    /// </summary>
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
