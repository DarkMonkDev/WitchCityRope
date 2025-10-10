using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Safety.Services;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Refund processing service with comprehensive audit trails and business rule validation
/// </summary>
public class RefundService : IRefundService
{
    private readonly ApplicationDbContext _context;
    private readonly IPayPalService _payPalService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        ApplicationDbContext context,
        IPayPalService payPalService,
        IEncryptionService encryptionService,
        ILogger<RefundService> logger)
    {
        _context = context;
        _payPalService = payPalService;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<Result<PaymentRefund>> ProcessRefundAsync(
        ProcessRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing refund for payment {PaymentId}, amount {RefundAmount}, processed by {UserId}",
                request.PaymentId, request.RefundAmount.ToDisplayString(), request.ProcessedByUserId);

            // Get the original payment
            var payment = await _context.Payments
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

            if (payment == null)
            {
                return Result<PaymentRefund>.Failure("Payment not found.");
            }

            // Check if payment is eligible for refund
            if (!payment.IsRefundEligible())
            {
                return Result<PaymentRefund>.Failure("Payment is not eligible for refund. Only completed payments can be refunded.");
            }

            // Calculate maximum refund amount available
            var maxRefundResult = await GetMaximumRefundAmountAsync(request.PaymentId, cancellationToken);
            if (!maxRefundResult.IsSuccess || maxRefundResult.Value == null)
            {
                return Result<PaymentRefund>.Failure($"Unable to calculate maximum refund amount: {maxRefundResult.ErrorMessage}");
            }

            // Validate refund amount doesn't exceed available amount
            if (request.RefundAmount > maxRefundResult.Value)
            {
                return Result<PaymentRefund>.Failure(
                    $"Refund amount {request.RefundAmount.ToDisplayString()} exceeds maximum available refund of {maxRefundResult.Value.ToDisplayString()}.");
            }

            // Validate refund reason meets minimum length requirement
            if (string.IsNullOrWhiteSpace(request.RefundReason) || request.RefundReason.Trim().Length < 10)
            {
                return Result<PaymentRefund>.Failure("Refund reason is required and must be at least 10 characters long.");
            }

            // Create refund record
            var refund = new PaymentRefund
            {
                OriginalPaymentId = request.PaymentId,
                ProcessedByUserId = request.ProcessedByUserId,
                RefundReason = request.RefundReason.Trim(),
                RefundStatus = RefundStatus.Processing,
                Metadata = request.Metadata
            };

            refund.SetRefundAmount(request.RefundAmount);

            _context.PaymentRefunds.Add(refund);

            // Create audit log for refund initiation
            var auditLog = PaymentAuditLog.RefundInitiated(
                request.PaymentId,
                request.ProcessedByUserId,
                request.RefundAmount.Amount,
                request.RefundReason,
                request.IpAddress);

            _context.PaymentAuditLog.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);

            // Process refund with PayPal if payment has a PayPal Order
            if (!string.IsNullOrEmpty(payment.EncryptedPayPalOrderId))
            {
                try
                {
                    // Decrypt PayPal Order ID
                    var paypalOrderId = await _encryptionService.DecryptAsync(payment.EncryptedPayPalOrderId);

                    // For PayPal refunds, we need the capture ID, not the order ID
                    // In a real implementation, we would need to get the capture from the order
                    // For now, we'll assume the order ID is the capture ID for simplicity
                    var captureId = paypalOrderId; // TODO: Get actual capture ID from PayPal order

                    // Process refund with PayPal
                    var paypalRefundResult = await _payPalService.RefundCaptureAsync(
                        captureId,
                        request.RefundAmount,
                        request.RefundReason,
                        $"Refund processed by user {request.ProcessedByUserId}",
                        cancellationToken);

                    if (paypalRefundResult.IsSuccess && paypalRefundResult.Value != null)
                    {
                        // Encrypt and store PayPal refund ID
                        refund.EncryptedPayPalRefundId = await _encryptionService.EncryptAsync(paypalRefundResult.Value.RefundId);
                        refund.RefundStatus = RefundStatus.Completed;

                        // Update payment status based on refund amount
                        var totalRefunded = payment.Refunds.Where(r => r.IsCompleted()).Sum(r => r.RefundAmountValue);

                        if (totalRefunded >= payment.AmountValue)
                        {
                            payment.Status = PaymentStatus.Refunded;
                        }
                        else
                        {
                            payment.Status = PaymentStatus.PartiallyRefunded;
                        }

                        payment.SetRefundAmount(Money.Create(totalRefunded, payment.Currency));
                        payment.RefundedAt = DateTime.UtcNow;

                        // Create completion audit log
                        var completionLog = PaymentAuditLog.RefundCompleted(
                            request.PaymentId,
                            request.RefundAmount.Amount,
                            paypalRefundResult.Value.RefundId);

                        _context.PaymentAuditLog.Add(completionLog);
                    }
                    else
                    {
                        refund.MarkFailed($"PayPal refund failed: {paypalRefundResult.ErrorMessage}");

                        _logger.LogError("PayPal refund failed for payment {PaymentId}: {Error}",
                            request.PaymentId, paypalRefundResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    refund.MarkFailed($"Stripe processing error: {ex.Message}");
                    _logger.LogError(ex, "Error processing Stripe refund for payment {PaymentId}", request.PaymentId);
                }
            }
            else
            {
                // Manual refund (no Stripe processing needed)
                refund.MarkCompleted();

                // Update payment status
                payment.Status = PaymentStatus.Refunded;
                payment.SetRefundAmount(request.RefundAmount);
                payment.RefundedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Refund {RefundId} processed successfully for payment {PaymentId}, status: {RefundStatus}",
                refund.Id, request.PaymentId, refund.RefundStatus);

            return Result<PaymentRefund>.Success(refund);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", request.PaymentId);
            return Result<PaymentRefund>.Failure($"An error occurred while processing the refund: {ex.Message}");
        }
    }

    public async Task<Result<PaymentRefund?>> GetRefundByIdAsync(
        Guid refundId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refund = await _context.PaymentRefunds
                .Include(r => r.OriginalPayment)
                .Include(r => r.ProcessedByUser)
                .FirstOrDefaultAsync(r => r.Id == refundId, cancellationToken);

            return Result<PaymentRefund?>.Success(refund);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refund {RefundId}", refundId);
            return Result<PaymentRefund?>.Failure($"Error retrieving refund: {ex.Message}");
        }
    }

    public async Task<Result<List<PaymentRefund>>> GetRefundsByPaymentIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refunds = await _context.PaymentRefunds
                .Where(r => r.OriginalPaymentId == paymentId)
                .Include(r => r.ProcessedByUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            return Result<List<PaymentRefund>>.Success(refunds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refunds for payment {PaymentId}", paymentId);
            return Result<List<PaymentRefund>>.Failure($"Error retrieving refunds: {ex.Message}");
        }
    }

    public async Task<Result<List<PaymentRefund>>> GetRefundsByProcessedByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refunds = await _context.PaymentRefunds
                .Where(r => r.ProcessedByUserId == userId)
                .Include(r => r.OriginalPayment)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            return Result<List<PaymentRefund>>.Success(refunds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refunds processed by user {UserId}", userId);
            return Result<List<PaymentRefund>>.Failure($"Error retrieving refunds: {ex.Message}");
        }
    }

    public async Task<Result<bool>> IsPaymentEligibleForRefundAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _context.Payments.FindAsync(new object[] { paymentId }, cancellationToken);

            if (payment == null)
            {
                return Result<bool>.Failure("Payment not found.");
            }

            return Result<bool>.Success(payment.IsRefundEligible());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking refund eligibility for payment {PaymentId}", paymentId);
            return Result<bool>.Failure($"Error checking refund eligibility: {ex.Message}");
        }
    }

    public async Task<Result<Money?>> GetMaximumRefundAmountAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            if (payment == null)
            {
                return Result<Money?>.Failure("Payment not found.");
            }

            var totalRefunded = payment.Refunds
                .Where(r => r.IsCompleted())
                .Sum(r => r.RefundAmountValue);

            var remainingAmount = payment.AmountValue - totalRefunded;

            if (remainingAmount <= 0)
            {
                return Result<Money?>.Success(Money.Zero(payment.Currency));
            }

            return Result<Money?>.Success(Money.Create(remainingAmount, payment.Currency));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating maximum refund amount for payment {PaymentId}", paymentId);
            return Result<Money?>.Failure($"Error calculating maximum refund amount: {ex.Message}");
        }
    }

    public async Task<Result<PaymentRefund>> UpdateRefundStatusAsync(
        Guid refundId,
        RefundStatus status,
        string? stripeRefundId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refund = await _context.PaymentRefunds.FindAsync(new object[] { refundId }, cancellationToken);
            if (refund == null)
            {
                return Result<PaymentRefund>.Failure("Refund not found.");
            }

            var oldStatus = refund.RefundStatus;
            refund.RefundStatus = status;

            if (status == RefundStatus.Completed)
            {
                refund.ProcessedAt = DateTime.UtcNow;
            }

            if (!string.IsNullOrEmpty(stripeRefundId))
            {
                // This would need encryption in a real implementation
                // refund.EncryptedStripeRefundId = await _encryptionService.EncryptAsync(stripeRefundId);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Refund {RefundId} status updated from {OldStatus} to {NewStatus}",
                refundId, oldStatus, status);

            return Result<PaymentRefund>.Success(refund);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating refund {RefundId} status", refundId);
            return Result<PaymentRefund>.Failure($"Error updating refund status: {ex.Message}");
        }
    }
}