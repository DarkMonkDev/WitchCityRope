namespace WitchCityRope.Api.Features.Backup.Models;

public class BackupMetadata
{
    public string FileName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public long SizeBytes { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string PgVersion { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "manual-trigger";
    public string BackupType { get; set; } = "full";
    public DatabaseStats? DbStats { get; set; }
}

public class DatabaseStats
{
    public int TableCount { get; set; }
    public long EstimatedRowCount { get; set; }
}
