namespace WitchCityRope.Api.Features.Reports.Models;

/// <summary>
/// Response DTO for the daily transactions endpoint.
/// Returns daily aggregated transaction data for charting.
/// </summary>
public class DailyTransactionsResponse
{
    public List<DailyTransactionDay> Days { get; set; } = new();
}

/// <summary>
/// Represents one day's aggregated transaction data.
/// Used by the frontend chart to display daily transaction volume,
/// failed credit card attempts, and incomplete payments.
/// </summary>
public class DailyTransactionDay
{
    public DateOnly Date { get; set; }
    public int CompletedCount { get; set; }
    public decimal CompletedAmount { get; set; }
    public int FailedCount { get; set; }
    public int IncompleteCount { get; set; }
}
