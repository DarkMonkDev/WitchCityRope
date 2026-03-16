using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Participation.Models;

/// <summary>
/// Request model for purchasing one or more tickets for a class event
/// </summary>
public class CreateTicketPurchaseRequest
{
    /// <summary>
    /// Event ID to purchase ticket for
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// Ticket type IDs to purchase in a single transaction
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one ticket type must be specified")]
    public List<Guid> TicketTypeIds { get; set; } = new();

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

    /// <summary>
    /// Actual amount to charge for this purchase (after sliding scale discount).
    /// Calculated by the frontend and validated by the checkout endpoint.
    /// Used to set TicketPurchase.TotalPrice instead of the base ticket type price,
    /// which is null for sliding scale tickets and would result in $0.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Sliding scale discount percentage applied (0-75%).
    /// Stored on TicketPurchase for audit trail and refund calculations.
    /// 0 = full price, 75 = maximum discount.
    /// </summary>
    public decimal SlidingScalePercentage { get; set; }
}
