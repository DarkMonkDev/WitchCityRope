namespace WitchCityRope.Api.Features.Payments.Models.Responses;

/// <summary>
/// DTO representing a payment transaction for admin payment list
/// </summary>
public class PaymentTransactionDto
{
    /// <summary>
    /// Payment unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Ticket ID associated with this payment (for refund operations)
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// When the payment was processed
    /// </summary>
    public DateTime PaymentDate { get; set; }

    #region User Information

    /// <summary>
    /// User's display name (SceneName or FirstName LastName or Email)
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    #endregion

    #region Event Information

    /// <summary>
    /// Event title
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Session name if ticket is for specific session (null if event-wide or no sessions)
    /// </summary>
    public string? SessionName { get; set; }

    #endregion

    #region Payment Details

    /// <summary>
    /// Payment method type (PayPal, Free, Venmo)
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment currency (default USD)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Payment status (Paid, Refunded, Pending, Failed)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    #endregion

    #region Refund Information

    /// <summary>
    /// True if payment is eligible for refund (PayPal, Paid status, no existing refund)
    /// </summary>
    public bool IsRefundable { get; set; }

    /// <summary>
    /// Refund ID if payment has been refunded (null if not refunded)
    /// </summary>
    public Guid? RefundId { get; set; }

    /// <summary>
    /// Date when refund was processed (null if not refunded)
    /// </summary>
    public DateTime? RefundDate { get; set; }

    #endregion
}
