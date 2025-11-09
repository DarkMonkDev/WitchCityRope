namespace WitchCityRope.Api.Features.Participation.Models;

/// <summary>
/// Request DTO for admin ticket refund operation
/// Auto-generated as TypeScript interface by NSwag
/// </summary>
public class AdminRefundTicketRequest
{
    /// <summary>
    /// Whether to also remove the RSVP along with the ticket refund
    /// Default: true
    /// </summary>
    public bool AlsoRemoveRsvp { get; set; } = true;
}
