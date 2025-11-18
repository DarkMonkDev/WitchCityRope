using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Features.Volunteers.Services;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Refund processing service with comprehensive audit trails and business rule validation
/// </summary>
public class RefundService : IRefundService
{
    private readonly ApplicationDbContext _context;
    private readonly IPayPalService _payPalService;
    private readonly IEncryptionService _encryptionService;
    private readonly IVolunteerAssignmentService _volunteerAssignmentService;
    private readonly IEmailService _emailService;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        ApplicationDbContext context,
        IPayPalService payPalService,
        IEncryptionService encryptionService,
        IVolunteerAssignmentService volunteerAssignmentService,
        IEmailService emailService,
        ILogger<RefundService> logger)
    {
        _context = context;
        _payPalService = payPalService;
        _encryptionService = encryptionService;
        _volunteerAssignmentService = volunteerAssignmentService;
        _emailService = emailService;
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

            // Get the original payment with User for email notification
            var payment = await _context.Payments
                .Include(p => p.Refunds)
                .Include(p => p.User)
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

            // Generate idempotency key for this refund
            var idempotencyKey = $"WCR-{Guid.NewGuid():N}";

            // Create refund record
            var refund = new PaymentRefund
            {
                OriginalPaymentId = request.PaymentId,
                ProcessedByUserId = request.ProcessedByUserId,
                RefundReason = request.RefundReason.Trim(),
                RefundStatus = RefundStatus.Processing,
                IdempotencyKey = idempotencyKey,
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

            // Process refund with PayPal if payment has a PayPal Capture ID
            if (!string.IsNullOrEmpty(payment.EncryptedPayPalCaptureId))
            {
                try
                {
                    // Decrypt PayPal Capture ID (required for refunds)
                    var captureId = await _encryptionService.DecryptAsync(payment.EncryptedPayPalCaptureId);

                    _logger.LogInformation(
                        "Processing PayPal refund for payment {PaymentId} with capture ID (encrypted), idempotency key {IdempotencyKey}",
                        request.PaymentId, idempotencyKey);

                    // Process refund with PayPal using Capture ID and idempotency key
                    var paypalRefundResult = await _payPalService.RefundCaptureAsync(
                        captureId,
                        request.RefundAmount,
                        request.RefundReason,
                        idempotencyKey,
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
                    _logger.LogError(ex, "Error processing PayPal refund for payment {PaymentId}", request.PaymentId);
                }
            }
            else if (!string.IsNullOrEmpty(payment.EncryptedPayPalOrderId))
            {
                // Legacy payment without Capture ID - log warning and fail gracefully
                _logger.LogWarning(
                    "Payment {PaymentId} has PayPal Order ID but no Capture ID. " +
                    "Cannot process automatic refund. Manual refund required.",
                    request.PaymentId);

                refund.MarkFailed(
                    "Legacy payment without Capture ID. Manual refund required through PayPal dashboard.");

                await _context.SaveChangesAsync(cancellationToken);

                return Result<PaymentRefund>.Failure(
                    "This payment was created before Capture ID tracking was implemented. " +
                    "Please process the refund manually through the PayPal dashboard using Order ID.");
            }
            else
            {
                // Manual refund (no PayPal processing needed - e.g., cash payment)
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

            // Auto-cancel volunteer signups if refund was completed
            if (refund.RefundStatus == RefundStatus.Completed)
            {
                try
                {
                    // Get event attendance to find the event ID
                    var eventAttendance = await _context.EventAttendances
                        .AsNoTracking()
                        .FirstOrDefaultAsync(ep => ep.Id == payment.EventRegistrationId, cancellationToken);

                    if (eventAttendance != null)
                    {
                        // Cancel all volunteer signups for this user and event
                        var cancellationResult = await _volunteerAssignmentService.CancelAllVolunteerSignupsForUserEventAsync(
                            payment.UserId,
                            eventAttendance.EventId,
                            "Refunded Ticket, so automatically canceled volunteer spot",
                            cancellationToken);

                        if (cancellationResult.success && cancellationResult.cancelledCount > 0)
                        {
                            _logger.LogInformation(
                                "Auto-cancelled {Count} volunteer signups for user {UserId} at event {EventId} due to ticket refund",
                                cancellationResult.cancelledCount, payment.UserId, eventAttendance.EventId);
                        }
                        else if (!cancellationResult.success)
                        {
                            _logger.LogWarning(
                                "Failed to auto-cancel volunteer signups for user {UserId} at event {EventId}: {Error}",
                                payment.UserId, eventAttendance.EventId, cancellationResult.error);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "EventAttendance {EventRegistrationId} not found, cannot auto-cancel volunteer signups",
                            payment.EventRegistrationId);
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the refund if volunteer cancellation fails
                    _logger.LogError(ex,
                        "Error auto-cancelling volunteer signups for payment {PaymentId} refund", request.PaymentId);
                }
            }

            // Send refund confirmation email if refund was completed
            if (refund.RefundStatus == RefundStatus.Completed && payment.User != null)
            {
                try
                {
                    var timingMessage = GetRefundTimingMessage(payment.PaymentMethodType);
                    var variables = new Dictionary<string, string>
                    {
                        { "user_name", payment.User.UserName ?? payment.User.Email ?? "Valued Member" },
                        { "refund_amount", refund.GetRefundAmount().ToDisplayString() },
                        { "original_amount", payment.GetAmount().ToDisplayString() },
                        { "payment_method", FormatPaymentMethodType(payment.PaymentMethodType) },
                        { "timing_message", timingMessage },
                        { "refund_reason", refund.RefundReason },
                        { "refund_id", refund.Id.ToString() }
                    };

                    var emailResult = await _emailService.SendTemplatedEmailAsync(
                        payment.User.Email!,
                        payment.User.UserName ?? payment.User.Email ?? "Valued Member",
                        EmailCategory.Admin,
                        "RefundConfirmation",
                        variables,
                        cancellationToken);

                    if (emailResult.IsSuccess)
                    {
                        _logger.LogInformation(
                            "Refund confirmation email sent successfully for refund {RefundId} to {Email}",
                            refund.Id, payment.User.Email);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to send refund confirmation email for refund {RefundId}: {Error}",
                            refund.Id, emailResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the refund if email sending fails
                    _logger.LogError(ex,
                        "Error sending refund confirmation email for refund {RefundId}", refund.Id);
                }
            }

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
                .AsNoTracking()
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
                .AsNoTracking()
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
                .AsNoTracking()
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
                .AsNoTracking()
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

    /// <summary>
    /// Gets the refund timing message based on payment method type
    /// </summary>
    private static string GetRefundTimingMessage(PaymentMethodType paymentMethodType)
    {
        return paymentMethodType switch
        {
            PaymentMethodType.SavedCard => "Please allow 1-2 billing cycles (up to 30 days) for the refund to appear on your credit/debit card statement.",
            PaymentMethodType.NewCard => "Please allow 1-2 billing cycles (up to 30 days) for the refund to appear on your credit/debit card statement.",
            PaymentMethodType.PayPal => "Your refund will be available immediately in your PayPal account.",
            PaymentMethodType.Venmo => "Your refund should appear in your Venmo account within a few hours.",
            PaymentMethodType.BankTransfer => "Please allow 3-5 business days for the refund to appear in your bank account.",
            PaymentMethodType.Cash => "Please contact us at support@witchcityrope.com to arrange your cash refund.",
            _ => "Please contact us at support@witchcityrope.com for refund timing information."
        };
    }

    /// <summary>
    /// Formats payment method type for display in emails
    /// </summary>
    private static string FormatPaymentMethodType(PaymentMethodType paymentMethodType)
    {
        return paymentMethodType switch
        {
            PaymentMethodType.SavedCard => "Credit/Debit Card",
            PaymentMethodType.NewCard => "Credit/Debit Card",
            PaymentMethodType.PayPal => "PayPal",
            PaymentMethodType.Venmo => "Venmo",
            PaymentMethodType.BankTransfer => "Bank Transfer",
            PaymentMethodType.Cash => "Cash",
            _ => paymentMethodType.ToString()
        };
    }
}