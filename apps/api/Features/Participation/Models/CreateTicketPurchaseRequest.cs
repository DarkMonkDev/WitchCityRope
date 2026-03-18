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

    /// <summary>
    /// Optional structured ticket selections with quantity and assignment info.
    /// When present, takes precedence over TicketTypeIds.
    /// Each item specifies a ticket type, quantity (1-N), and optional assignees.
    /// Backward compatible: if null/empty, falls back to TicketTypeIds (one per type).
    /// </summary>
    public List<TicketSelectionItem>? TicketSelections { get; set; }
}

/// <summary>
/// Represents a ticket type selection with quantity and optional assignees.
/// Used in multi-ticket purchase flow.
/// </summary>
public class TicketSelectionItem
{
    /// <summary>
    /// The ticket type being purchased
    /// </summary>
    [Required]
    public Guid TicketTypeId { get; set; }

    /// <summary>
    /// Number of tickets of this type to purchase (1-10)
    /// </summary>
    [Required]
    [Range(1, 10)]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Optional per-ticket assignees. Length must be less than or equal to Quantity - 1.
    /// First ticket is always for the purchaser. Remaining can be assigned.
    /// Null entries in the list = "assign later" (ticket stays with purchaser).
    /// Null/empty list = all tickets are for the purchaser (or assign later).
    /// </summary>
    public List<Guid?>? Assignees { get; set; }
}
