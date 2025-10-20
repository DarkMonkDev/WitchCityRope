namespace WitchCityRope.Api.Features.Volunteers.Models;

/// <summary>
/// Volunteer position response DTO
/// </summary>
public class VolunteerPositionDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SlotsNeeded { get; set; }
    public int SlotsFilled { get; set; }
    public int SlotsRemaining { get; set; }
    public bool RequiresExperience { get; set; }
    public string Requirements { get; set; } = string.Empty;
    public bool IsPublicFacing { get; set; }
    public bool IsFullyStaffed { get; set; }

    // Session information if session-specific
    public string? SessionName { get; set; }
    public DateTime? SessionStartTime { get; set; }
    public DateTime? SessionEndTime { get; set; }

    // User's signup status for this position (if authenticated)
    public bool HasUserSignedUp { get; set; }
    public Guid? UserSignupId { get; set; }
}

/// <summary>
/// Request to sign up for a volunteer position
/// </summary>
public class VolunteerSignupRequest
{
    public string? Notes { get; set; }
}

/// <summary>
/// Volunteer signup response DTO
/// </summary>
public class VolunteerSignupDto
{
    public Guid Id { get; set; }
    public Guid VolunteerPositionId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SignedUpAt { get; set; }
    public string? Notes { get; set; }
    public bool HasCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public bool HasCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Position information
    public string PositionTitle { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartDate { get; set; }
}
