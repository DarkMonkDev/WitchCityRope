namespace WitchCityRope.Api.Features.Logging.Models;

public class ClientErrorReport
{
    public required string Message { get; set; }
    public string? Stack { get; set; }
    public required string Type { get; set; }
    public required string Url { get; set; }
    public required string Timestamp { get; set; }
    public required string UserAgent { get; set; }
    public string? ComponentStack { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ClientErrorBatch
{
    public required List<ClientErrorReport> Errors { get; set; }
}
