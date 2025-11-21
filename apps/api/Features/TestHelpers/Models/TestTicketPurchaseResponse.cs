namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Response model for created test ticket purchase
/// Contains identifiers needed for test assertions and cleanup
/// </summary>
public class TestTicketPurchaseResponse
{
    /// <summary>
    /// TicketPurchase ID (primary key)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Transaction reference (use this to find payment in UI)
    /// </summary>
    public string PaymentReference { get; set; } = string.Empty;

    /// <summary>
    /// Total price of the purchase
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Payment method used
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Payment status
    /// </summary>
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>
    /// User ID who made the purchase
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Ticket type ID
    /// </summary>
    public Guid TicketTypeId { get; set; }

    /// <summary>
    /// Quantity purchased
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Whether PayPal Capture ID was included
    /// </summary>
    public bool HasPayPalCaptureId { get; set; }

    /// <summary>
    /// Event name for the payment (for test isolation)
    /// </summary>
    public string EventName { get; set; } = string.Empty;
}
