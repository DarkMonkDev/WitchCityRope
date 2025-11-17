using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.Models.PayPal;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Comprehensive audit logging service for PayPal refund operations.
///
/// <para>
/// Audit trail captures:
/// - Refund request initiation (who, when, why, how much)
/// - Retry attempts (count, error messages, timing)
/// - Refund success (PayPal refund ID, status, completion time)
/// - Refund failure (error messages, final status)
/// </para>
///
/// <para>
/// Why comprehensive audit logging matters:
/// - Compliance: Financial transactions require complete audit trails
/// - Troubleshooting: Diagnose why refunds failed or required retries
/// - Customer support: Answer questions about refund timing and status
/// - Security: Detect suspicious refund patterns or abuse
/// - Analytics: Track refund success rates, retry patterns, failure reasons
/// </para>
///
/// <para>
/// Audit data storage:
/// - PaymentRefund entity: Primary refund record with status and metadata
/// - RetryCount field: Number of retry attempts made
/// - ErrorMessage field: Last error encountered (if failed)
/// - PayPalResponse field: Full PayPal API response (JSONB for queryability)
/// - Metadata field: Additional context (admin notes, IP address, etc.)
/// </para>
/// </summary>
public class RefundAuditService : IRefundAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RefundAuditService> _logger;

    public RefundAuditService(
        ApplicationDbContext db,
        ILogger<RefundAuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Logs refund request initiation.
    /// Called when admin/teacher initiates a refund request.
    /// </summary>
    /// <param name="refund">PaymentRefund entity (already created and saved)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task LogRefundRequestAsync(
        PaymentRefund refund,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Refund requested: ID={RefundId}, PaymentId={PaymentId}, " +
                "Amount={Amount}, ProcessedBy={ProcessedBy}, IdempotencyKey={IdempotencyKey}, Reason={Reason}",
                refund.Id,
                refund.OriginalPaymentId,
                refund.GetRefundAmount().ToDisplayString(),
                refund.ProcessedByUserId,
                refund.IdempotencyKey,
                refund.RefundReason);

            // Refund entity already exists in database
            // This method provides structured logging for monitoring/alerting
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Audit logging failure should not break refund flow
            _logger.LogError(ex,
                "Failed to log refund request for refund {RefundId}",
                refund.Id);
        }
    }

    /// <summary>
    /// Logs successful refund completion with PayPal response data.
    /// Updates refund entity with PayPal refund ID, status, and full API response.
    /// </summary>
    /// <param name="refundId">Refund entity ID</param>
    /// <param name="paypalRefund">PayPal refund response containing refund ID and status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task LogRefundSuccessAsync(
        Guid refundId,
        PayPalRefundResponse paypalRefund,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refund = await _db.PaymentRefunds.FindAsync(new object[] { refundId }, cancellationToken);
            if (refund == null)
            {
                _logger.LogWarning(
                    "Cannot log refund success - refund {RefundId} not found in database",
                    refundId);
                return;
            }

            // Store PayPal refund ID (note: should be encrypted in production)
            // TODO: Use IEncryptionService to encrypt PayPal refund ID for PCI compliance
            refund.EncryptedPayPalRefundId = paypalRefund.RefundId;

            // Update refund status based on PayPal response
            refund.RefundStatus = paypalRefund.Status switch
            {
                "COMPLETED" => RefundStatus.Completed,
                "PENDING" => RefundStatus.Processing,
                "CANCELLED" => RefundStatus.Failed,
                "FAILED" => RefundStatus.Failed,
                _ => RefundStatus.Processing
            };

            // Store full PayPal response as JSONB for queryability and debugging
            refund.Metadata["paypal_response"] = JsonSerializer.Serialize(paypalRefund);

            // Mark completion time if refund is completed
            if (refund.RefundStatus == RefundStatus.Completed)
            {
                refund.ProcessedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Refund succeeded: ID={RefundId}, PayPalRefundId={PayPalRefundId}, " +
                "Status={Status}, Amount={Amount}",
                refundId,
                paypalRefund.RefundId,
                paypalRefund.Status,
                paypalRefund.Amount);
        }
        catch (Exception ex)
        {
            // Audit logging failure should not break refund flow
            _logger.LogError(ex,
                "Failed to log refund success for refund {RefundId}, PayPal refund {PayPalRefundId}",
                refundId, paypalRefund.RefundId);
        }
    }

    /// <summary>
    /// Logs retry attempt for a refund operation.
    /// Updates refund entity with retry count and error message for troubleshooting.
    /// </summary>
    /// <param name="refundId">Refund entity ID</param>
    /// <param name="retryCount">Current retry attempt number (1-5)</param>
    /// <param name="errorMessage">Error message from failed attempt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task LogRefundRetryAsync(
        Guid refundId,
        int retryCount,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refund = await _db.PaymentRefunds.FindAsync(new object[] { refundId }, cancellationToken);
            if (refund == null)
            {
                _logger.LogWarning(
                    "Cannot log refund retry - refund {RefundId} not found in database",
                    refundId);
                return;
            }

            // Update retry count
            refund.RetryCount = retryCount;

            // Store error message (will be overwritten on next retry or success)
            refund.ErrorMessage = errorMessage;

            // Store retry history in metadata for detailed troubleshooting
            var retryKey = $"retry_{retryCount}";
            refund.Metadata[retryKey] = new
            {
                timestamp = DateTime.UtcNow,
                error = errorMessage,
                attempt = retryCount
            };

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Refund retry {RetryCount}: ID={RefundId}, Error={ErrorMessage}",
                retryCount, refundId, errorMessage);
        }
        catch (Exception ex)
        {
            // Audit logging failure should not break retry flow
            _logger.LogError(ex,
                "Failed to log refund retry {RetryCount} for refund {RefundId}",
                retryCount, refundId);
        }
    }

    /// <summary>
    /// Logs final refund failure after all retries exhausted or non-retryable error.
    /// Updates refund entity with failed status and error message.
    /// </summary>
    /// <param name="refundId">Refund entity ID</param>
    /// <param name="errorMessage">Final error message describing failure</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task LogRefundFailureAsync(
        Guid refundId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refund = await _db.PaymentRefunds.FindAsync(new object[] { refundId }, cancellationToken);
            if (refund == null)
            {
                _logger.LogWarning(
                    "Cannot log refund failure - refund {RefundId} not found in database",
                    refundId);
                return;
            }

            // Mark refund as failed
            refund.RefundStatus = RefundStatus.Failed;

            // Store final error message
            refund.ErrorMessage = errorMessage;

            // Store failure details in metadata
            refund.Metadata["failure"] = new
            {
                timestamp = DateTime.UtcNow,
                error = errorMessage,
                total_retries = refund.RetryCount
            };

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                "Refund failed: ID={RefundId}, Error={ErrorMessage}, Retries={RetryCount}",
                refundId, errorMessage, refund.RetryCount);
        }
        catch (Exception ex)
        {
            // Audit logging failure should not break failure handling flow
            _logger.LogError(ex,
                "Failed to log refund failure for refund {RefundId}",
                refundId);
        }
    }

    /// <summary>
    /// Retrieves refund audit history including all retry attempts and status changes.
    /// Useful for admin UI to show refund processing timeline.
    /// </summary>
    /// <param name="refundId">Refund entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refund entity with full audit trail in metadata</returns>
    public async Task<PaymentRefund?> GetRefundAuditHistoryAsync(
        Guid refundId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.PaymentRefunds
                .AsNoTracking()
                .Include(r => r.OriginalPayment)
                .Include(r => r.ProcessedByUser)
                .FirstOrDefaultAsync(r => r.Id == refundId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve refund audit history for refund {RefundId}",
                refundId);
            return null;
        }
    }
}

/// <summary>
/// Interface for refund audit logging service
/// </summary>
public interface IRefundAuditService
{
    /// <summary>
    /// Logs refund request initiation.
    /// </summary>
    Task LogRefundRequestAsync(PaymentRefund refund, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs successful refund completion with PayPal response data.
    /// </summary>
    Task LogRefundSuccessAsync(Guid refundId, PayPalRefundResponse paypalRefund, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs retry attempt for a refund operation.
    /// </summary>
    Task LogRefundRetryAsync(Guid refundId, int retryCount, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs final refund failure after all retries exhausted or non-retryable error.
    /// </summary>
    Task LogRefundFailureAsync(Guid refundId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves refund audit history including all retry attempts and status changes.
    /// </summary>
    Task<PaymentRefund?> GetRefundAuditHistoryAsync(Guid refundId, CancellationToken cancellationToken = default);
}
