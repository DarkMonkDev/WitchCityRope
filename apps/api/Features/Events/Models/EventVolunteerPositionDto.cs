namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Data Transfer Object for VolunteerPosition information - SIMPLE VERSION for admin/event management operations.
/// Used in the Events feature vertical slice for CRUD operations on events and their volunteer positions.
/// </summary>
/// <remarks>
/// This is the SIMPLE version for admin and event management operations.
/// It contains only the core fields needed for creating, updating, and displaying events in admin contexts.
///
/// A richer version exists at Features/Volunteers/Models/VolunteerModels.cs (VolunteerPositionDto) that includes
/// additional user-facing fields like CanSignUp, CanCancel, HasUserSignedUp, and session timing information.
/// That version is used by the Volunteers feature for public-facing volunteer signup operations.
///
/// This DTO is used by:
/// - EventDto (includes list of volunteer positions)
/// - CreateEventRequest (for creating events with volunteer positions)
/// - UpdateEventRequest (for updating event volunteer positions)
/// - EventService (for admin event management operations)
/// </remarks>
public class EventVolunteerPositionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SlotsNeeded { get; set; }
    public int SlotsFilled { get; set; }
    public string? SessionId { get; set; }

    /// <summary>
    /// Start time for this volunteer shift (HH:mm format)
    /// </summary>
    public string? StartTime { get; set; }

    /// <summary>
    /// End time for this volunteer shift (HH:mm format)
    /// </summary>
    public string? EndTime { get; set; }

    /// <summary>
    /// Whether this position is visible on the public event page
    /// </summary>
    public bool IsPublicFacing { get; set; }

    /// <summary>
    /// Constructor to map from VolunteerPosition entity
    /// </summary>
    public EventVolunteerPositionDto() { }

    /// <summary>
    /// Constructor to map from VolunteerPosition entity
    /// </summary>
    public EventVolunteerPositionDto(WitchCityRope.Api.Models.VolunteerPosition volunteerPosition)
    {
        Id = volunteerPosition.Id.ToString();
        Title = volunteerPosition.Title;
        Description = volunteerPosition.Description;
        SlotsNeeded = volunteerPosition.SlotsNeeded;
        SlotsFilled = volunteerPosition.SlotsFilled;
        SessionId = volunteerPosition.SessionId.ToString();
        StartTime = volunteerPosition.StartTime;
        EndTime = volunteerPosition.EndTime;
        IsPublicFacing = volunteerPosition.IsPublicFacing;
    }

    /// <summary>
    /// Gets remaining volunteer slots needed
    /// </summary>
    public int SlotsRemaining => SlotsNeeded - SlotsFilled;

    /// <summary>
    /// Gets whether all volunteer slots are filled
    /// </summary>
    public bool IsFullyStaffed => SlotsFilled >= SlotsNeeded;
}