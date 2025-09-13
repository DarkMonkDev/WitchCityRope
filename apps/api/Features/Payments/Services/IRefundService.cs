using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.ValueObjects;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Refund processing service interface
/// Handles payment refunds with comprehensive audit trails
/// </summary>
public interface IRefundService
{
    /// <summary>
    /// Process refund for a completed payment
    /// </summary>
    Task<Result<PaymentRefund>> ProcessRefundAsync(
        ProcessRefundRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get refund by ID
    /// </summary>
    Task<Result<PaymentRefund?>> GetRefundByIdAsync(
        Guid refundId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all refunds for a payment
    /// </summary>
    Task<Result<List<PaymentRefund>>> GetRefundsByPaymentIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get refunds processed by a specific user
    /// </summary>
    Task<Result<List<PaymentRefund>>> GetRefundsByProcessedByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if payment is eligible for refund
    /// </summary>
    Task<Result<bool>> IsPaymentEligibleForRefundAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate maximum refund amount available
    /// </summary>
    Task<Result<Money?>> GetMaximumRefundAmountAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update refund status (typically called by webhooks)
    /// </summary>
    Task<Result<PaymentRefund>> UpdateRefundStatusAsync(
        Guid refundId,
        Models.RefundStatus status,
        string? stripeRefundId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for processing refunds
/// </summary>
public class ProcessRefundRequest
{
    public Guid PaymentId { get; set; }
    public Money RefundAmount { get; set; } = null!;
    public string RefundReason { get; set; } = string.Empty;
    public Guid ProcessedByUserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}