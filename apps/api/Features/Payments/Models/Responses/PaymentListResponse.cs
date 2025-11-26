namespace WitchCityRope.Api.Features.Payments.Models.Responses;

/// <summary>
/// Response containing paginated list of payment transactions
/// </summary>
public class PaymentListResponse
{
    /// <summary>
    /// List of payment transactions for current page
    /// </summary>
    public List<PaymentTransactionDto> Transactions { get; set; } = new();

    /// <summary>
    /// Total number of transactions matching filters
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-indexed)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of results per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages (calculated from TotalCount and PageSize)
    /// </summary>
    public int TotalPages { get; set; }
}
