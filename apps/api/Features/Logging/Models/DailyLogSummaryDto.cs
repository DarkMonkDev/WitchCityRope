namespace WitchCityRope.Api.Features.Logging.Models;

public class DailyLogSummaryDto
{
    public DateOnly Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Subcategory { get; set; }
    public int Count { get; set; }
    public string? Metadata { get; set; }
}
