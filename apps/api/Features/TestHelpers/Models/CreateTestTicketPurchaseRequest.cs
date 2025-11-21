using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Request model for creating test ticket purchases
/// Used in E2E tests to create isolated payment data
/// </summary>
public class CreateTestTicketPurchaseRequest
{
    /// <summary>
    /// Amount for the ticket purchase (required)
    /// </summary>
    [Required]
    [Range(0.01, 10000.00)]
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Payment method (defaults to 'PayPal')
    /// </summary>
    public string PaymentMethod { get; set; } = "PayPal";

    /// <summary>
    /// Payment status (defaults to 'Completed')
    /// </summary>
    public string PaymentStatus { get; set; } = "Completed";

    /// <summary>
    /// Optional transaction reference
    /// If not provided, a unique one will be generated
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Optional notes for the purchase
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User ID for the purchase (defaults to admin user)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Ticket type ID (if not provided, uses first available ticket type)
    /// </summary>
    public Guid? TicketTypeId { get; set; }

    /// <summary>
    /// Quantity of tickets (defaults to 1)
    /// </summary>
    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Whether to include mock encrypted PayPal Capture ID
    /// Required for PayPal refund testing (defaults to true for PayPal payments)
    /// </summary>
    public bool? IncludePayPalCaptureId { get; set; }

    /// <summary>
    /// Optional unique event name for test isolation
    /// If provided, creates/finds an event with this name
    /// </summary>
    public string? EventName { get; set; }
}
