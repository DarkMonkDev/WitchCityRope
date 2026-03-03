namespace WitchCityRope.Api.Models;

/// <summary>
/// Payment status for ticket purchases.
/// Stored as string in database via EF value converter for backward compatibility.
/// </summary>
public enum TicketPurchasePaymentStatus
{
    Pending,
    Completed,
    /// <summary>
    /// Legacy PayPal status, treated as equivalent to Completed.
    /// PayPal webhooks may return "Confirmed" instead of "Completed".
    /// </summary>
    Confirmed,
    Failed,
    PartiallyRefunded,
    Refunded
}
