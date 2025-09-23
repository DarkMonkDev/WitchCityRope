namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Query parameters for admin applications list
/// </summary>
public class VettingApplicationsQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public string? SearchQuery { get; set; }
    public string SortBy { get; set; } = "SubmittedAt";
    public string SortDirection { get; set; } = "Desc";
    public DateTime? SubmittedAfter { get; set; }
    public DateTime? SubmittedBefore { get; set; }
}