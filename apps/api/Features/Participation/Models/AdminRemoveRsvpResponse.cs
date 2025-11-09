namespace WitchCityRope.Api.Features.Participation.Models;

/// <summary>
/// Response DTO for admin RSVP removal operation
/// Includes details of cascading effects (ticket refund, volunteer shifts)
/// Auto-generated as TypeScript interface by NSwag
/// </summary>
public class AdminRemoveRsvpResponse
{
    /// <summary>
    /// Whether the RSVP was successfully removed
    /// </summary>
    public bool RsvpRemoved { get; set; }

    /// <summary>
    /// Whether an associated ticket was refunded
    /// </summary>
    public bool TicketRefunded { get; set; }

    /// <summary>
    /// Amount refunded for the ticket (null if no ticket)
    /// </summary>
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// Whether volunteer shifts were automatically removed
    /// </summary>
    public bool VolunteerShiftsRemoved { get; set; }

    /// <summary>
    /// List of volunteer position titles that were removed
    /// </summary>
    public List<string> VolunteerShiftNames { get; set; } = new();
}
