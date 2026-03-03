namespace WitchCityRope.Api.Features.Logging.Entities;

/// <summary>
/// Read-only entity mapping to the logging.application_logs table.
/// Serilog writes to this table directly via the PostgreSQL sink.
/// EF Core uses this entity only for querying (admin reporting).
/// </summary>
public class ApplicationLog
{
    public long Id { get; set; }

    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Serilog numeric level (0=Verbose, 1=Debug, 2=Information, 3=Warning, 4=Error, 5=Fatal)
    /// </summary>
    public short Level { get; set; }

    public string LevelName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? MessageTemplate { get; set; }

    public string? Exception { get; set; }

    public string? SourceContext { get; set; }

    /// <summary>
    /// All structured log properties stored as JSONB
    /// </summary>
    public string? Properties { get; set; }

    public Guid? UserId { get; set; }

    public Guid? CorrelationId { get; set; }

    public string? RequestPath { get; set; }

    public string? MachineName { get; set; }
}
