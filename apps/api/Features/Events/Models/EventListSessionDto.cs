namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Minimal session DTO for the admin events list table.
/// Contains only the fields needed for date/time display.
/// </summary>
public record EventListSessionDto
{
    public string Id { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public DateTime StartDate { get; init; }
}
