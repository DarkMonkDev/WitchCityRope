namespace WitchCityRope.Api.Features.CheckIn.Models;

/// <summary>
/// Response model for event check-in dashboard
/// </summary>
public class DashboardResponse
{
    public string EventId { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string EventDate { get; set; } = string.Empty;
    public string EventStatus { get; set; } = string.Empty;
    public CapacityInfo Capacity { get; set; } = new();
    public List<RecentCheckIn> RecentCheckIns { get; set; } = new();
    public List<StaffMember> StaffOnDuty { get; set; } = new();
    public SyncStatus SyncStatus { get; set; } = new();
}

/// <summary>
/// Recent check-in information
/// </summary>
public class RecentCheckIn
{
    public string AttendeeId { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string CheckInTime { get; set; } = string.Empty;
    public string StaffMemberName { get; set; } = string.Empty;
    public bool IsManualEntry { get; set; }
}

/// <summary>
/// Staff member information
/// </summary>
public class StaffMember
{
    public string UserId { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string LastActivity { get; set; } = string.Empty;
}

/// <summary>
/// Sync status information for offline operations
/// </summary>
public class SyncStatus
{
    public int PendingCount { get; set; }
    public string LastSync { get; set; } = string.Empty;
    public int ConflictCount { get; set; }
}