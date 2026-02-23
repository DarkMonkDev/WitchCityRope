namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Lightweight DTO for the admin events list table.
/// Uses SQL-level COUNT subqueries instead of loading full object graph.
/// </summary>
public record EventListItemDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsPublished { get; init; }
    public bool AllowRsvps { get; init; }
    public bool RequireTicketPurchase { get; init; }
    public int Capacity { get; init; }
    public int CurrentRSVPs { get; init; }
    public int CurrentTickets { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public List<EventListSessionDto> Sessions { get; init; } = new();
}
