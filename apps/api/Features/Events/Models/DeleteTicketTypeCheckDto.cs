namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// DTO for checking if a ticket type can be deleted
/// </summary>
public class DeleteTicketTypeCheckDto
{
    /// <summary>
    /// Whether the ticket type can be deleted
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Reason why deletion is blocked
    /// Possible values: "ticketsSold"
    /// </summary>
    public string? BlockReason { get; set; }

    /// <summary>
    /// Number of tickets sold for this ticket type
    /// </summary>
    public int TicketsSoldCount { get; set; }
}
