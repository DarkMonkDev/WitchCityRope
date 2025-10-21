namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Data Transfer Object for VolunteerPosition information.
/// Used in the Events feature vertical slice.
/// </summary>
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