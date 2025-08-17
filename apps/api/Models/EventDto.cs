namespace WitchCityRope.Api.Models;

/// <summary>
/// Data Transfer Object for Event information.
/// Simple model for Step 1 of vertical slice proof-of-concept.
/// </summary>
public class EventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Location { get; set; } = string.Empty;
}