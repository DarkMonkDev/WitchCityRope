namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Data Transfer Object for VolunteerPosition information - SIMPLE VERSION for admin/event management operations.
/// Used in the Events feature vertical slice for CRUD operations on events and their volunteer positions.
/// </summary>
/// <remarks>
/// This is the SIMPLE version of VolunteerPositionDto designed for admin and event management operations.
/// It contains only the core fields needed for creating, updating, and displaying events in admin contexts.
///
/// IMPORTANT: A second, richer version exists at Features/Volunteers/Models/VolunteerModels.cs that includes
/// additional user-facing fields like CanSignUp, CanCancel, HasUserSignedUp, and session timing information.
/// That version is used by the Volunteers feature for public-facing volunteer signup operations where user
/// context and permission checks are needed.
///
/// Both DTOs exist intentionally as part of our vertical slice architecture - each feature slice has its own
/// models optimized for its specific use case. This prevents coupling between features and keeps models focused.
///
/// This DTO is used by:
/// - EventDto (includes list of volunteer positions)
/// - CreateEventRequest (for creating events with volunteer positions)
/// - UpdateEventRequest (for updating event volunteer positions)
/// - EventService (for admin event management operations)
/// </remarks>
public class VolunteerPositionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SlotsNeeded { get; set; }
    public int SlotsFilled { get; set; }
    public string? SessionId { get; set; }

    /// <summary>
    /// Constructor to map from VolunteerPosition entity
    /// </summary>
    public VolunteerPositionDto() { }

    /// <summary>
    /// Constructor to map from VolunteerPosition entity
    /// </summary>
    public VolunteerPositionDto(WitchCityRope.Api.Models.VolunteerPosition volunteerPosition)
    {
        Id = volunteerPosition.Id.ToString();
        Title = volunteerPosition.Title;
        Description = volunteerPosition.Description;
        SlotsNeeded = volunteerPosition.SlotsNeeded;
        SlotsFilled = volunteerPosition.SlotsFilled;
        SessionId = volunteerPosition.SessionId?.ToString();
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