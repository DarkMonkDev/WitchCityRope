namespace WitchCityRope.Api.Features.Logging.Models;

public class LogEntryDto
{
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string LevelName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? SourceContext { get; set; }
    public Guid? UserId { get; set; }
    public Guid? CorrelationId { get; set; }
    public string? RequestPath { get; set; }
}
