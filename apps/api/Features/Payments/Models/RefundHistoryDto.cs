namespace WitchCityRope.Api.Features.Payments.Models;

/// <summary>
/// DTO for individual refund records displayed in admin ticket tables and refund modals.
/// Provides a per-refund breakdown so admins can see the full refund history
/// before issuing additional refunds or cancellations.
/// Auto-generated as TypeScript interface by NSwag.
/// </summary>
public class RefundHistoryDto
{
    /// <summary>
    /// Unique identifier for this refund record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Refund amount in USD
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Admin-provided reason for the refund
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Refund processing status (Completed, Failed, Processing, Cancelled)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the refund was processed (UTC)
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Scene name of the admin/teacher who processed the refund
    /// </summary>
    public string ProcessedByName { get; set; } = string.Empty;

    /// <summary>
    /// Authorize.net refund transaction id, DECRYPTED for admin-facing reconciliation.
    /// Null for PayPal/manual refunds and for historical Authorize.net refunds created
    /// before the id was persisted. Decryption is acceptable here because this DTO is
    /// only ever returned to authenticated Administrator/Teacher users.
    /// </summary>
    public string? AuthNetRefundTransactionId { get; set; }

    /// <summary>
    /// True when the Authorize.net operation was a void of an unsettled charge rather
    /// than a true post-settlement refund. False for PayPal/manual refunds.
    /// </summary>
    public bool WasVoided { get; set; }
}
