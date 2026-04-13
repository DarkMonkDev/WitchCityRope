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
    Refunded,
    /// <summary>
    /// Purchase was cancelled but no automatic refund could be processed because the
    /// payment method (typically authorize-net / credit card) isn't handled by the
    /// self-service refund path. The money is still with the merchant and an admin
    /// must manually refund via the admin payments UI (or outside the app).
    ///
    /// Introduced 2026-04-12 for M2b (BE-12) — replaces the silent early-return at
    /// AttendanceService.cs:2663-2670 that used to leave such purchases reading
    /// "Completed" forever. See /docs/technical-debt.md entry and production incident
    /// 01-health-check-2026-04-12 Row A (Cepheus $30) for the real-world case this
    /// solves.
    /// </summary>
    AwaitingManualRefund
}
