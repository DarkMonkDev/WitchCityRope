namespace WitchCityRope.Api.Features.Backup.Models;

// Response DTOs
public class BackupJobResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int EstimatedSeconds { get; set; }
    public string? PreBackupFileName { get; set; }
}

public class BackupListItem
{
    public string FileName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public long SizeBytes { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
}

public class BackupListResponse
{
    public List<BackupListItem> Backups { get; set; } = new();
    public int TotalCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public string TotalSizeFormatted { get; set; } = string.Empty;
}

public class BackupJobStatusResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // enqueued|running|succeeded|failed
    public int Progress { get; set; } // 0-100
    public string Message { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}

public class StorageSummaryResponse
{
    public int TotalBackups { get; set; }
    public long TotalSizeBytes { get; set; }
    public string TotalSizeFormatted { get; set; } = string.Empty;
    public long LimitBytes { get; set; }
    public string LimitFormatted { get; set; } = string.Empty;
    public double PercentUsed { get; set; }
}

// Request DTOs
public class RestoreRequest
{
    public string FileName { get; set; } = string.Empty;
    public string Confirmation { get; set; } = string.Empty;
    public bool CreatePreBackup { get; set; } = true;
}
