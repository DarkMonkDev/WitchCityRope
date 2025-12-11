using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Volunteers.Models;

/// <summary>
/// Volunteer position response DTO - RICH VERSION for user-facing volunteer signup API.
/// Contains additional fields for user context, permissions, and session information.
/// </summary>
/// <remarks>
/// This is the RICH version of VolunteerPositionDto designed for user-facing volunteer signup operations.
/// It extends the basic volunteer position fields with user-specific context and permission checks:
///
/// KEY ADDITIONAL FIELDS:
/// - CanSignUp: Whether the current user can sign up (based on timing rules, capacity, existing signup)
/// - CanCancel: Whether the current user can cancel their signup (based on cancellation window)
/// - HasUserSignedUp: Whether the current user has already signed up for this position
/// - UserSignupId: The ID of the user's signup if they have one
/// - Session timing info: SessionName, SessionStartTime, SessionEndTime for session-specific positions
///
/// IMPORTANT: A simpler version exists at Features/Events/Models/VolunteerPositionDto.cs that contains
/// only the core fields needed for admin event management CRUD operations. That version is used by the
/// Events feature for creating/updating events without the user-context complexity.
///
/// Both DTOs exist intentionally as part of our vertical slice architecture - each feature slice has its own
/// models optimized for its specific use case. This prevents coupling between features and keeps models focused.
///
/// This DTO is used by:
/// - VolunteerService (calculates CanSignUp/CanCancel based on business rules)
/// - VolunteerEndpoints (public-facing volunteer signup/cancellation API)
/// </remarks>
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
    public bool IsPublicFacing { get; set; }
    public bool IsFullyStaffed { get; set; }

    // Session information if session-specific
    public string? SessionName { get; set; }
    public DateTime? SessionStartTime { get; set; }
    public DateTime? SessionEndTime { get; set; }

    // User's signup status for this position (if authenticated)
    public bool HasUserSignedUp { get; set; }
    public Guid? UserSignupId { get; set; }

    /// <summary>
    /// Whether the user can cancel their volunteer signup based on event timing rules
    /// Determined by event's VolunteerCancellationCloseHours setting
    /// </summary>
    public bool CanCancel { get; set; }

    /// <summary>
    /// Whether new signups are currently allowed for this position
    /// Determined by event's VolunteerRegistrationCloseHours setting
    /// False when timing window closed, position full, or user already signed up
    /// </summary>
    public bool CanSignUp { get; set; }

    /// <summary>
    /// Reason why signup is blocked (null if CanSignUp is true)
    /// Possible values: "TimingClosed", "PositionFull", "AlreadySignedUp", "NoTicketForSession"
    /// Used by frontend to display appropriate messaging to users
    /// </summary>
    public string? SignupBlockedReason { get; set; }
}

/// <summary>
/// Request to sign up for a volunteer position
/// </summary>
public class VolunteerSignupRequest
{
    /// <summary>
    /// Indicates whether the user has accepted the Event Waiver
    /// Defaults to true for backward compatibility with tests
    /// In production, frontend should explicitly set this to true
    /// </summary>
    public bool EventWaiverAccepted { get; set; } = true;
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
    public bool HasCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public bool HasCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Position information
    public string PositionTitle { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartDate { get; set; }
}
