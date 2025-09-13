using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Safety.Services;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Payment processing service supporting sliding scale pricing and comprehensive audit trails
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext context,
        IStripeService stripeService,
        IEncryptionService encryptionService,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<Result<Payment>> ProcessPaymentAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing payment for user {UserId}, registration {RegistrationId}, sliding scale {SlidingScale}%",
                request.UserId, request.EventRegistrationId, request.SlidingScalePercentage);

            // Validate sliding scale percentage
            if (!IsValidSlidingScalePercentage(request.SlidingScalePercentage))
            {
                return Result<Payment>.Failure("Invalid sliding scale percentage. Must be between 0% and 75%.");
            }

            // Calculate final amount with sliding scale discount
            var finalAmount = CalculateFinalAmount(request.OriginalAmount, request.SlidingScalePercentage);

            // Check if payment already exists for this registration
            var existingPayment = await _context.Payments
                .Where(p => p.EventRegistrationId == request.EventRegistrationId)
                .Where(p => p.Status == PaymentStatus.Completed)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingPayment != null)
            {
                return Result<Payment>.Failure("Payment already completed for this event registration.");
            }

            // Create payment record
            var payment = new Payment
            {
                EventRegistrationId = request.EventRegistrationId,
                UserId = request.UserId,
                PaymentMethodType = request.PaymentMethodType,
                SlidingScalePercentage = request.SlidingScalePercentage,
                Status = PaymentStatus.Pending
            };

            payment.SetAmount(finalAmount);

            _context.Payments.Add(payment);

            // Create audit log for payment initiation
            var auditLog = PaymentAuditLog.PaymentInitiated(
                payment.Id,
                request.UserId,
                request.IpAddress,
                request.UserAgent);

            _context.PaymentAuditLog.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);

            // Get or create Stripe customer
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null)
            {
                return Result<Payment>.Failure("User not found.");
            }

            var customerResult = await _stripeService.CreateOrRetrieveCustomerAsync(
                request.UserId,
                user.Email ?? "",
                user.SceneName,
                cancellationToken);

            if (!customerResult.IsSuccess || customerResult.Value == null)
            {
                await LogPaymentFailureAsync(payment.Id, "stripe_customer_creation_failed", 
                    customerResult.ErrorMessage, cancellationToken);
                return Result<Payment>.Failure($"Failed to create Stripe customer: {customerResult.ErrorMessage}");
            }

            // Encrypt and store Stripe customer ID
            payment.EncryptedStripeCustomerId = await _encryptionService.EncryptAsync(customerResult.Value.Id);

            // Create PaymentIntent with Stripe
            var paymentIntentMetadata = new Dictionary<string, string>
            {
                ["payment_id"] = payment.Id.ToString(),
                ["user_id"] = request.UserId.ToString(),
                ["event_registration_id"] = request.EventRegistrationId.ToString(),
                ["sliding_scale_percentage"] = request.SlidingScalePercentage.ToString("F2")
            };

            var paymentIntentResult = await _stripeService.CreatePaymentIntentAsync(
                finalAmount,
                finalAmount.Currency,
                request.UserId,
                request.StripePaymentMethodId,
                paymentIntentMetadata,
                cancellationToken);

            if (!paymentIntentResult.IsSuccess || paymentIntentResult.Value == null)
            {
                await LogPaymentFailureAsync(payment.Id, "stripe_payment_intent_failed",
                    paymentIntentResult.ErrorMessage, cancellationToken);
                return Result<Payment>.Failure($"Failed to create payment intent: {paymentIntentResult.ErrorMessage}");
            }

            // Encrypt and store PaymentIntent ID
            payment.EncryptedStripePaymentIntentId = await _encryptionService.EncryptAsync(paymentIntentResult.Value.Id);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment {PaymentId} created successfully for user {UserId}, final amount: {Amount}",
                payment.Id, request.UserId, finalAmount.ToDisplayString());

            return Result<Payment>.Success(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for user {UserId}", request.UserId);
            return Result<Payment>.Failure($"An error occurred while processing the payment: {ex.Message}");
        }
    }

    public async Task<Result<Payment?>> GetPaymentByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.AuditLogs)
                .Include(p => p.Refunds)
                .Include(p => p.Failures)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            return Result<Payment?>.Success(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment {PaymentId}", paymentId);
            return Result<Payment?>.Failure($"Error retrieving payment: {ex.Message}");
        }
    }

    public async Task<Result<List<Payment>>> GetPaymentsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payments = await _context.Payments
                .Where(p => p.UserId == userId)
                .Include(p => p.Refunds)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            return Result<List<Payment>>.Success(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for user {UserId}", userId);
            return Result<List<Payment>>.Failure($"Error retrieving payments: {ex.Message}");
        }
    }

    public async Task<Result<Payment?>> GetPaymentByRegistrationIdAsync(
        Guid eventRegistrationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.EventRegistrationId == eventRegistrationId, cancellationToken);

            return Result<Payment?>.Success(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment for registration {RegistrationId}", eventRegistrationId);
            return Result<Payment?>.Failure($"Error retrieving payment: {ex.Message}");
        }
    }

    public async Task<Result<PaymentStatus?>> GetPaymentStatusByRegistrationIdAsync(
        Guid eventRegistrationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _context.Payments
                .Where(p => p.EventRegistrationId == eventRegistrationId)
                .Select(p => (PaymentStatus?)p.Status)
                .FirstOrDefaultAsync(cancellationToken);

            return Result<PaymentStatus?>.Success(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment status for registration {RegistrationId}", eventRegistrationId);
            return Result<PaymentStatus?>.Failure($"Error retrieving payment status: {ex.Message}");
        }
    }

    public async Task<Result<Payment>> UpdatePaymentStatusAsync(
        Guid paymentId,
        PaymentStatus status,
        string? stripePaymentIntentId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _context.Payments.FindAsync(new object[] { paymentId }, cancellationToken);
            if (payment == null)
            {
                return Result<Payment>.Failure("Payment not found.");
            }

            var oldStatus = payment.Status;
            payment.Status = status;

            if (status == PaymentStatus.Completed && !payment.ProcessedAt.HasValue)
            {
                payment.ProcessedAt = DateTime.UtcNow;

                // Create completion audit log
                var completionLog = PaymentAuditLog.PaymentCompleted(
                    paymentId,
                    stripePaymentIntentId ?? "unknown",
                    payment.AmountValue);

                _context.PaymentAuditLog.Add(completionLog);
            }

            // Create status change audit log
            var statusLog = PaymentAuditLog.StatusChanged(paymentId, oldStatus.ToString(), status.ToString());
            _context.PaymentAuditLog.Add(statusLog);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment {PaymentId} status updated from {OldStatus} to {NewStatus}",
                paymentId, oldStatus, status);

            return Result<Payment>.Success(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment {PaymentId} status", paymentId);
            return Result<Payment>.Failure($"Error updating payment status: {ex.Message}");
        }
    }

    public ValueObjects.Money CalculateFinalAmount(
        ValueObjects.Money originalAmount,
        decimal slidingScalePercentage)
    {
        if (slidingScalePercentage < 0 || slidingScalePercentage > 75)
        {
            throw new ArgumentException("Sliding scale percentage must be between 0% and 75%");
        }

        return originalAmount.ApplySlidingScale(slidingScalePercentage);
    }

    public bool IsValidSlidingScalePercentage(decimal percentage)
    {
        return percentage >= 0 && percentage <= 75;
    }

    public async Task<Result> CreateAuditLogAsync(
        PaymentAuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _context.PaymentAuditLog.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log for payment {PaymentId}", auditLog.PaymentId);
            return Result.Failure($"Error creating audit log: {ex.Message}");
        }
    }

    private async Task LogPaymentFailureAsync(
        Guid paymentId,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var failure = PaymentFailure.FromSystemError(paymentId, errorCode, errorMessage);
            _context.PaymentFailures.Add(failure);

            var auditLog = PaymentAuditLog.PaymentFailed(paymentId, errorMessage, errorCode);
            _context.PaymentAuditLog.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log payment failure for payment {PaymentId}", paymentId);
        }
    }
}