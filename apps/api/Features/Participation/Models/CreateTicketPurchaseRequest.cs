using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Participation.Models;

/// <summary>
/// Request model for purchasing a ticket for a class event
/// </summary>
public class CreateTicketPurchaseRequest
{
    /// <summary>
    /// Event ID to purchase ticket for
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// Ticket type ID to purchase
    /// </summary>
    [Required]
    public Guid TicketTypeId { get; set; }

    /// <summary>
    /// Optional notes from the participant
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Payment method details (for future payment integration)
    /// Currently stubbed for basic ticket tracking
    /// </summary>
    public string? PaymentMethodId { get; set; }

    /// <summary>
    /// Event Waiver acceptance - REQUIRED for ticket purchase
    /// </summary>
    [Required]
    public bool EventWaiverAccepted { get; set; }
}