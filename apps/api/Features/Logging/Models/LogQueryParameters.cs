namespace WitchCityRope.Api.Features.Logging.Models;

public class LogQueryParameters
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? Level { get; set; }
    public string? SourceContext { get; set; }
    public Guid? UserId { get; set; }
    public Guid? CorrelationId { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
