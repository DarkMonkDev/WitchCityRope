namespace WitchCityRope.Api.Features.CheckIn.Models;

/// <summary>
/// Response model for attendee information in check-in interface
/// </summary>
public class AttendeeResponse
{
    public string AttendeeId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RegistrationStatus { get; set; } = string.Empty;
    public string? TicketNumber { get; set; }
    public string? CheckInTime { get; set; }
    public bool IsFirstTime { get; set; }
    public string? DietaryRestrictions { get; set; }
    public string? AccessibilityNeeds { get; set; }
    public string? Pronouns { get; set; }
    public bool HasCompletedWaiver { get; set; }
    public int? WaitlistPosition { get; set; }
}

/// <summary>
/// Response model for attendee list with pagination
/// </summary>
public class CheckInAttendeesResponse
{
    public string EventId { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string EventDate { get; set; } = string.Empty;
    public int TotalCapacity { get; set; }
    public int CheckedInCount { get; set; }
    public int AvailableSpots { get; set; }
    public List<AttendeeResponse> Attendees { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

/// <summary>
/// Pagination information
/// </summary>
public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}