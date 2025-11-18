using Microsoft.AspNetCore.Mvc;

namespace WitchCityRope.Api.Features.Payments.Models.Requests;

/// <summary>
/// Query parameters for filtering and paginating payment transactions
/// </summary>
public class PaymentListQueryParameters
{
    /// <summary>
    /// Search term for user name, event name, or transaction ID
    /// </summary>
    [FromQuery(Name = "searchTerm")]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by payment date >= StartDate
    /// </summary>
    [FromQuery(Name = "startDate")]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by payment date <= EndDate
    /// </summary>
    [FromQuery(Name = "endDate")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by payment methods (PayPal, Free, Venmo) - comma-separated
    /// </summary>
    [FromQuery(Name = "paymentMethods")]
    public string? PaymentMethods { get; set; }

    /// <summary>
    /// Filter by payment statuses (Paid, Refunded, Pending, Failed) - comma-separated
    /// </summary>
    [FromQuery(Name = "statuses")]
    public string? Statuses { get; set; }

    /// <summary>
    /// Filter by amount >= MinAmount
    /// </summary>
    [FromQuery(Name = "minAmount")]
    public decimal? MinAmount { get; set; }

    /// <summary>
    /// Filter by amount <= MaxAmount
    /// </summary>
    [FromQuery(Name = "maxAmount")]
    public decimal? MaxAmount { get; set; }

    /// <summary>
    /// Sort column: PaymentDate, Amount, UserName, Status
    /// </summary>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; } = "PaymentDate";

    /// <summary>
    /// Sort direction: Asc, Desc
    /// </summary>
    [FromQuery(Name = "sortDirection")]
    public string SortDirection { get; set; } = "Desc";

    /// <summary>
    /// Page number (1-indexed)
    /// </summary>
    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of results per page
    /// </summary>
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 50;
}
