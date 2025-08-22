namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Data Transfer Object for Event information.
/// Used in the Events feature vertical slice.
/// </summary>
public class EventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Location { get; set; } = string.Empty;
}