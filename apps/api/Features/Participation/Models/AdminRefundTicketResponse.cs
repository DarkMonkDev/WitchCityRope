namespace WitchCityRope.Api.Features.Participation.Models;

/// <summary>
/// Response DTO for admin ticket refund operation
/// Includes details of cascading effects (RSVP removal, volunteer shifts)
/// Auto-generated as TypeScript interface by NSwag
/// </summary>
public class AdminRefundTicketResponse
{
    /// <summary>
    /// Whether the ticket was successfully refunded
    /// </summary>
    public bool TicketRefunded { get; set; }

    /// <summary>
    /// Amount refunded for the ticket
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Whether the associated RSVP was also removed
    /// </summary>
    public bool RsvpRemoved { get; set; }

    /// <summary>
    /// Whether volunteer shifts were automatically removed
    /// </summary>
    public bool VolunteerShiftsRemoved { get; set; }

    /// <summary>
    /// List of volunteer position titles that were removed
    /// </summary>
    public List<string> VolunteerShiftNames { get; set; } = new();
}
