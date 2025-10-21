using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Models;

/// <summary>
/// VolunteerSignup entity tracking user signups for volunteer positions
/// Links users to specific volunteer opportunities they've committed to
/// </summary>
public class VolunteerSignup
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the volunteer position
    /// </summary>
    [Required]
    public Guid VolunteerPositionId { get; set; }

    /// <summary>
    /// Reference to the user who signed up
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Current status of the volunteer signup
    /// </summary>
    [Required]
    public VolunteerSignupStatus Status { get; set; } = VolunteerSignupStatus.Confirmed;

    /// <summary>
    /// When the user signed up
    /// </summary>
    [Required]
    public DateTime SignedUpAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the volunteer checked in for their shift
    /// </summary>
    public bool HasCheckedIn { get; set; } = false;

    /// <summary>
    /// When the volunteer checked in (if applicable)
    /// </summary>
    public DateTime? CheckedInAt { get; set; }

    /// <summary>
    /// Whether the volunteer completed their shift
    /// </summary>
    public bool HasCompleted { get; set; } = false;

    /// <summary>
    /// When the shift was marked as completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When record was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When record was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to volunteer position
    /// </summary>
    public VolunteerPosition? VolunteerPosition { get; set; }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public ApplicationUser? User { get; set; }
}

/// <summary>
/// Status of a volunteer signup
/// </summary>
public enum VolunteerSignupStatus
{
    /// <summary>
    /// Volunteer has signed up and confirmed
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Volunteer has cancelled their signup
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Volunteer was a no-show
    /// </summary>
    NoShow = 3,

    /// <summary>
    /// Shift completed successfully
    /// </summary>
    Completed = 4
}
