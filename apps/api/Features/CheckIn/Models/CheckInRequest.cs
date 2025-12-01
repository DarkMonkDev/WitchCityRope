using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.CheckIn.Models;

/// <summary>
/// Request model for processing check-in
/// </summary>
public class CheckInRequest
{
    [Required]
    public string AttendeeId { get; set; } = string.Empty;

    [Required]
    public string CheckInTime { get; set; } = string.Empty;

    /// <summary>
    /// Optional staff member ID (GUID format). If not provided, kiosk mode is assumed (Guid.Empty used).
    /// </summary>
    public string? StaffMemberId { get; set; }

    public string? Notes { get; set; }
    public bool OverrideCapacity { get; set; } = false;
    public bool IsManualEntry { get; set; } = false;
    public ManualEntryData? ManualEntryData { get; set; }
}

/// <summary>
/// Data for manual entry (walk-in) attendees
/// </summary>
public class ManualEntryData
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    public string? DietaryRestrictions { get; set; }
    public string? AccessibilityNeeds { get; set; }
    public bool HasCompletedWaiver { get; set; } = false;
}

/// <summary>
/// Response model for check-in operation
/// </summary>
public class CheckInResponse
{
    public bool Success { get; set; }
    public string AttendeeId { get; set; } = string.Empty;
    public string CheckInTime { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public CapacityInfo CurrentCapacity { get; set; } = new();
    public string? AuditLogId { get; set; }

    /// <summary>
    /// Session ID this check-in is for
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Session name for display
    /// </summary>
    public string SessionName { get; set; } = string.Empty;
}

/// <summary>
/// Capacity information for events
/// </summary>
public class CapacityInfo
{
    public int TotalCapacity { get; set; }
    public int CheckedInCount { get; set; }
    public int WaitlistCount { get; set; }
    public int AvailableSpots { get; set; }
    public bool IsAtCapacity { get; set; }
    public bool CanOverride { get; set; }
}