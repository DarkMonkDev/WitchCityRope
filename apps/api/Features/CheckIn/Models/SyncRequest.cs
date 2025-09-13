using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.CheckIn.Models;

/// <summary>
/// Request model for offline synchronization
/// </summary>
public class SyncRequest
{
    [Required]
    public string DeviceId { get; set; } = string.Empty;
    
    [Required]
    public List<PendingCheckIn> PendingCheckIns { get; set; } = new();
    
    [Required]
    public string LastSyncTimestamp { get; set; } = string.Empty;
}

/// <summary>
/// Pending check-in for offline sync
/// </summary>
public class PendingCheckIn
{
    [Required]
    public string LocalId { get; set; } = string.Empty;
    
    [Required]
    public string AttendeeId { get; set; } = string.Empty;
    
    [Required]
    public string CheckInTime { get; set; } = string.Empty;
    
    [Required]
    public string StaffMemberId { get; set; } = string.Empty;
    
    public string? Notes { get; set; }
    public bool IsManualEntry { get; set; }
    public ManualEntryData? ManualEntryData { get; set; }
}

/// <summary>
/// Response model for sync operation
/// </summary>
public class SyncResponse
{
    public bool Success { get; set; }
    public int ProcessedCount { get; set; }
    public List<SyncConflict> Conflicts { get; set; } = new();
    public List<AttendeeResponse> UpdatedAttendees { get; set; } = new();
    public string NewSyncTimestamp { get; set; } = string.Empty;
}

/// <summary>
/// Sync conflict information
/// </summary>
public class SyncConflict
{
    public string LocalId { get; set; } = string.Empty;
    public string AttendeeId { get; set; } = string.Empty;
    public string ConflictType { get; set; } = string.Empty;
    public object? ServerData { get; set; }
    public object? LocalData { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}