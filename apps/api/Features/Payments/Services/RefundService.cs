using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.Models.PayPal;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Consolidated refund processing service with integrated retry logic and audit trails
/// ARCHITECTURE FIX (2025-11-18): Now uses TicketPurchase (single source of truth)
/// CONSOLIDATED: Merged RefundAuditService and RefundRetryService into this class
/// </summary>
public class RefundService : IRefundService
{
    private readonly ApplicationDbContext _context;
    private readonly IPayPalService _payPalService;
    private readonly IEncryptionService _encryptionService;
    private readonly IVolunteerAssignmentService _volunteerAssignmentService;
    private readonly IEmailService _emailService;
    private readonly ILogger<RefundService> _logger;
    private const int MaxRetries = 5;

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
                "Processing refund for ticket {TicketId}, amount {RefundAmount}, processed by {UserId}",
                request.PaymentId, request.RefundAmount.ToDisplayString(), request.ProcessedByUserId);

            // Get the ticket purchase with User for email notification
            // ARCHITECTURE FIX: Now queries TicketPurchases instead of Payments
            var ticketPurchase = await _context.TicketPurchases
                .Include(tp => tp.User)
                .FirstOrDefaultAsync(tp => tp.Id == request.PaymentId, cancellationToken);

            if (ticketPurchase == null)
            {
                return Result<PaymentRefund>.Failure("Ticket purchase not found.");
            }

            // Check if ticket purchase is eligible for refund
            if (!IsTicketRefundEligible(ticketPurchase))
            {
                return Result<PaymentRefund>.Failure("This ticket is not eligible for refund. Only completed payments can be refunded.");
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
                TicketPurchaseId = request.PaymentId, // Now references TicketPurchase
                ProcessedByUserId = request.ProcessedByUserId,
                RefundReason = request.RefundReason.Trim(),
                RefundStatus = RefundStatus.Processing,
                IdempotencyKey = idempotencyKey,
                Metadata = request.Metadata
            };

            refund.SetRefundAmount(request.RefundAmount);

            _context.PaymentRefunds.Add(refund);
            await _context.SaveChangesAsync(cancellationToken);

            // Audit: Log refund request initiation
            await LogRefundRequestAsync(refund);

            // Process refund with PayPal if ticket has a PayPal Capture ID
            if (!string.IsNullOrEmpty(ticketPurchase.EncryptedPayPalCaptureId))
            {
                try
                {
                    // Decrypt PayPal Capture ID (required for refunds)
                    var captureId = await _encryptionService.DecryptAsync(ticketPurchase.EncryptedPayPalCaptureId);

                    _logger.LogInformation(
                        "Processing PayPal refund for ticket {TicketId} with capture ID (encrypted), idempotency key {IdempotencyKey}",
                        request.PaymentId, idempotencyKey);

                    // Process refund with PayPal using retry logic
                    var paypalRefundResult = await RefundWithRetryAsync(
                        captureId,
                        request.RefundAmount,
                        request.RefundReason,
                        idempotencyKey,
                        async (retryCount, errorMessage) =>
                        {
                            // Audit callback: Log each retry attempt
                            await LogRefundRetryAsync(refund.Id, retryCount, errorMessage);
                        },
                        cancellationToken);

                    if (paypalRefundResult.IsSuccess && paypalRefundResult.Value != null)
                    {
                        // Audit: Log successful refund
                        await LogRefundSuccessAsync(refund.Id, paypalRefundResult.Value, cancellationToken);

                        refund.RefundStatus = RefundStatus.Completed;

                        // Update ticket purchase status
                        ticketPurchase.PaymentStatus = "Refunded";

                        _logger.LogInformation(
                            "PayPal refund completed successfully for ticket {TicketId}, refund ID: {RefundId}",
                            request.PaymentId, refund.Id);
                    }
                    else
                    {
                        // Audit: Log refund failure
                        var errorMessage = $"PayPal refund failed: {paypalRefundResult.ErrorMessage}";
                        await LogRefundFailureAsync(refund.Id, errorMessage, cancellationToken);

                        refund.MarkFailed(errorMessage);

                        _logger.LogError("PayPal refund failed for ticket {TicketId}: {Error}",
                            request.PaymentId, paypalRefundResult.ErrorMessage);
                    }
                }
                catch (MaxRetriesExceededException ex)
                {
                    var errorMessage = $"Max retries exceeded: {ex.Message}";
                    await LogRefundFailureAsync(refund.Id, errorMessage, cancellationToken);

                    refund.MarkFailed(errorMessage);
                    _logger.LogError(ex, "Max retries exceeded for PayPal refund, ticket {TicketId}", request.PaymentId);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"PayPal processing error: {ex.Message}";
                    await LogRefundFailureAsync(refund.Id, errorMessage, cancellationToken);

                    refund.MarkFailed(errorMessage);
                    _logger.LogError(ex, "Error processing PayPal refund for ticket {TicketId}", request.PaymentId);
                }
            }
            else if (!string.IsNullOrEmpty(ticketPurchase.EncryptedPayPalOrderId))
            {
                // Legacy ticket without Capture ID - log warning and fail gracefully
                _logger.LogWarning(
                    "Ticket {TicketId} has PayPal Order ID but no Capture ID. " +
                    "Cannot process automatic refund. Manual refund required.",
                    request.PaymentId);

                var errorMessage = "Legacy payment without Capture ID. Manual refund required through PayPal dashboard.";
                await LogRefundFailureAsync(refund.Id, errorMessage, cancellationToken);

                refund.MarkFailed(errorMessage);

                await _context.SaveChangesAsync(cancellationToken);

                return Result<PaymentRefund>.Failure(
                    "This payment was created before Capture ID tracking was implemented. " +
                    "Please process the refund manually through the PayPal dashboard using Order ID.");
            }
            else
            {
                // Manual refund (no PayPal processing needed - e.g., cash/Venmo payment)
                refund.MarkCompleted();
                ticketPurchase.PaymentStatus = "Refunded";

                _logger.LogInformation(
                    "Manual refund marked complete for ticket {TicketId} (payment method: {PaymentMethod})",
                    request.PaymentId, ticketPurchase.PaymentMethod);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Refund {RefundId} processed successfully for ticket {TicketId}, status: {RefundStatus}",
                refund.Id, request.PaymentId, refund.RefundStatus);

            // Auto-cancel volunteer signups if refund was completed
            if (refund.RefundStatus == RefundStatus.Completed)
            {
                await AutoCancelVolunteerSignupsAsync(ticketPurchase, cancellationToken);

                // Send refund confirmation email
                if (ticketPurchase.User != null)
                {
                    await SendRefundConfirmationEmailAsync(ticketPurchase, refund, cancellationToken);
                }
            }

            return Result<PaymentRefund>.Success(refund);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for ticket {TicketId}", request.PaymentId);
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
                .Include(r => r.TicketPurchase) // FIXED: Was OriginalPayment
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
                .Where(r => r.TicketPurchaseId == paymentId) // FIXED: Was OriginalPaymentId
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
                .Include(r => r.TicketPurchase) // FIXED: Was OriginalPayment
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
            var ticketPurchase = await _context.TicketPurchases.FindAsync(new object[] { paymentId }, cancellationToken);

            if (ticketPurchase == null)
            {
                return Result<bool>.Failure("Ticket purchase not found.");
            }

            return Result<bool>.Success(IsTicketRefundEligible(ticketPurchase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking refund eligibility for ticket {TicketId}", paymentId);
            return Result<bool>.Failure($"Error checking refund eligibility: {ex.Message}");
        }
    }

    public async Task<Result<Money?>> GetMaximumRefundAmountAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ticketPurchase = await _context.TicketPurchases
                .AsNoTracking()
                .FirstOrDefaultAsync(tp => tp.Id == paymentId, cancellationToken);

            if (ticketPurchase == null)
            {
                return Result<Money?>.Failure("Ticket purchase not found.");
            }

            // Get all completed refunds for this ticket
            var completedRefunds = await _context.PaymentRefunds
                .AsNoTracking()
                .Where(r => r.TicketPurchaseId == paymentId && r.RefundStatus == RefundStatus.Completed)
                .ToListAsync(cancellationToken);

            var totalRefunded = completedRefunds.Sum(r => r.RefundAmountValue);
            var remainingAmount = ticketPurchase.TotalPrice - totalRefunded;

            if (remainingAmount <= 0)
            {
                return Result<Money?>.Success(Money.Zero("USD"));
            }

            return Result<Money?>.Success(Money.Create(remainingAmount, "USD"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating maximum refund amount for ticket {TicketId}", paymentId);
            return Result<Money?>.Failure($"Error calculating maximum refund amount: {ex.Message}");
        }
    }

    public async Task<Result<PaymentRefund>> UpdateRefundStatusAsync(
        Guid refundId,
        RefundStatus status,
        string? paypalRefundId = null,
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

            if (!string.IsNullOrEmpty(paypalRefundId))
            {
                refund.EncryptedPayPalRefundId = await _encryptionService.EncryptAsync(paypalRefundId);
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

    #region Private Helper Methods - Refund Eligibility

    /// <summary>
    /// Checks if a ticket purchase is eligible for refund
    /// Single Responsibility: Business rule validation
    /// </summary>
    private static bool IsTicketRefundEligible(TicketPurchase ticketPurchase)
    {
        // Must have a completed payment
        if (!ticketPurchase.IsPaymentCompleted)
        {
            return false;
        }

        // Must have a TotalPrice > 0 (can't refund free tickets)
        if (ticketPurchase.TotalPrice <= 0)
        {
            return false;
        }

        return true;
    }

    #endregion

    #region Private Helper Methods - Audit Logging (Consolidated from RefundAuditService)

    /// <summary>
    /// Logs refund request initiation
    /// Consolidated from RefundAuditService (SOLID: part of refund processing responsibility)
    /// </summary>
    private async Task LogRefundRequestAsync(PaymentRefund refund)
    {
        try
        {
            _logger.LogInformation(
                "Refund requested: ID={RefundId}, TicketId={TicketId}, " +
                "Amount={Amount}, ProcessedBy={ProcessedBy}, IdempotencyKey={IdempotencyKey}, Reason={Reason}",
                refund.Id,
                refund.TicketPurchaseId,
                refund.GetRefundAmount().ToDisplayString(),
                refund.ProcessedByUserId,
                refund.IdempotencyKey,
                refund.RefundReason);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log refund request for refund {RefundId}", refund.Id);
        }
    }

    /// <summary>
    /// Logs successful refund completion with PayPal response data
    /// Consolidated from RefundAuditService
    /// </summary>
    private async Task LogRefundSuccessAsync(
        Guid refundId,
        PayPalRefundResponse paypalRefund,
        CancellationToken cancellationToken)
    {
        try
        {
            var refund = await _context.PaymentRefunds.FindAsync(new object[] { refundId }, cancellationToken);
            if (refund == null)
            {
                _logger.LogWarning("Cannot log refund success - refund {RefundId} not found", refundId);
                return;
            }

            // Store encrypted PayPal refund ID
            refund.EncryptedPayPalRefundId = await _encryptionService.EncryptAsync(paypalRefund.RefundId);

            // Update refund status based on PayPal response
            refund.RefundStatus = paypalRefund.Status switch
            {
                "COMPLETED" => RefundStatus.Completed,
                "PENDING" => RefundStatus.Processing,
                "CANCELLED" => RefundStatus.Failed,
                "FAILED" => RefundStatus.Failed,
                _ => RefundStatus.Processing
            };

            // Store full PayPal response in metadata for debugging
            refund.Metadata["paypal_response"] = System.Text.Json.JsonSerializer.Serialize(paypalRefund);

            if (refund.RefundStatus == RefundStatus.Completed)
            {
                refund.ProcessedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Refund succeeded: ID={RefundId}, PayPalRefundId={PayPalRefundId}, Status={Status}, Amount={Amount}",
                refundId, paypalRefund.RefundId, paypalRefund.Status, paypalRefund.Amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log refund success for refund {RefundId}", refundId);
        }
    }

    /// <summary>
    /// Logs retry attempt for a refund operation
    /// Consolidated from RefundAuditService
    /// </summary>
    private async Task LogRefundRetryAsync(
        Guid refundId,
        int retryCount,
        string errorMessage)
    {
        try
        {
            var refund = await _context.PaymentRefunds.FindAsync(new object[] { refundId });
            if (refund == null)
            {
                _logger.LogWarning("Cannot log refund retry - refund {RefundId} not found", refundId);
                return;
            }

            refund.RetryCount = retryCount;
            refund.ErrorMessage = errorMessage;

            // Store retry history in metadata
            var retryKey = $"retry_{retryCount}";
            refund.Metadata[retryKey] = new
            {
                timestamp = DateTime.UtcNow,
                error = errorMessage,
                attempt = retryCount
            };

            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Refund retry {RetryCount}: ID={RefundId}, Error={ErrorMessage}",
                retryCount, refundId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log refund retry for refund {RefundId}", refundId);
        }
    }

    /// <summary>
    /// Logs final refund failure
    /// Consolidated from RefundAuditService
    /// </summary>
    private async Task LogRefundFailureAsync(
        Guid refundId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var refund = await _context.PaymentRefunds.FindAsync(new object[] { refundId }, cancellationToken);
            if (refund == null)
            {
                _logger.LogWarning("Cannot log refund failure - refund {RefundId} not found", refundId);
                return;
            }

            refund.RefundStatus = RefundStatus.Failed;
            refund.ErrorMessage = errorMessage;

            refund.Metadata["failure"] = new
            {
                timestamp = DateTime.UtcNow,
                error = errorMessage,
                total_retries = refund.RetryCount
            };

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                "Refund failed: ID={RefundId}, Error={ErrorMessage}, Retries={RetryCount}",
                refundId, errorMessage, refund.RetryCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log refund failure for refund {RefundId}", refundId);
        }
    }

    #endregion

    #region Private Helper Methods - Retry Logic (Consolidated from RefundRetryService)

    /// <summary>
    /// Executes PayPal refund with automatic retry logic for transient failures
    /// Consolidated from RefundRetryService (SOLID: implementation detail of refund processing)
    /// </summary>
    private async Task<Result<PayPalRefundResponse>> RefundWithRetryAsync(
        string captureId,
        Money? amount,
        string? reason,
        string idempotencyKey,
        Func<int, string, Task>? onRetry = null,
        CancellationToken cancellationToken = default)
    {
        var retryCount = 0;
        Exception? lastException = null;

        _logger.LogInformation(
            "Starting refund with retry for capture {CaptureId}, amount {Amount}, idempotency key {IdempotencyKey}",
            captureId, amount?.ToDisplayString() ?? "FULL", idempotencyKey);

        while (retryCount <= MaxRetries)
        {
            try
            {
                // Make API call (idempotency key ensures no duplicates even on retries)
                var result = await _payPalService.RefundCaptureAsync(
                    captureId,
                    amount,
                    reason ?? "Refund requested",
                    idempotencyKey,
                    $"Refund attempt {retryCount + 1}/{MaxRetries + 1}",
                    cancellationToken);

                // Success - return result
                if (result.IsSuccess && result.Value != null)
                {
                    if (retryCount > 0)
                    {
                        _logger.LogInformation(
                            "Refund succeeded on retry {RetryCount} for capture {CaptureId}",
                            retryCount, captureId);
                    }

                    return result;
                }

                // PayPal service returned failure (business logic error, not retryable)
                _logger.LogWarning(
                    "PayPal refund failed with business error: {ErrorMessage}. Not retrying.",
                    result.ErrorMessage);

                return result;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;

                // Check if retryable error (5xx, network)
                if (IsRetryableError(ex))
                {
                    if (retryCount >= MaxRetries)
                    {
                        _logger.LogError(
                            ex,
                            "Refund failed after {MaxRetries} retries for capture {CaptureId}",
                            MaxRetries, captureId);

                        throw new MaxRetriesExceededException(
                            $"Refund failed after {MaxRetries} retries: {ex.Message}",
                            ex);
                    }

                    retryCount++;

                    // Exponential backoff: 2^retryCount seconds
                    var delaySeconds = Math.Pow(2, retryCount);

                    _logger.LogWarning(
                        ex,
                        "Retryable error on attempt {Attempt}/{MaxAttempts} for capture {CaptureId}. " +
                        "Retrying in {DelaySeconds}s. Error: {ErrorMessage}",
                        retryCount, MaxRetries + 1, captureId, delaySeconds, ex.Message);

                    // Invoke callback if provided (for audit logging)
                    if (onRetry != null)
                    {
                        await onRetry(retryCount, ex.Message);
                    }

                    // Wait before retry
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    continue;
                }

                // Non-retryable error (4xx client error) - fail immediately
                _logger.LogError(
                    ex,
                    "Non-retryable HTTP error for capture {CaptureId}: {StatusCode} {ErrorMessage}",
                    captureId, ex.StatusCode, ex.Message);

                return Result<PayPalRefundResponse>.Failure(
                    $"Refund failed with client error: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                // Timeout error - retryable
                lastException = ex;

                if (retryCount >= MaxRetries)
                {
                    _logger.LogError(
                        ex,
                        "Refund timed out after {MaxRetries} retries for capture {CaptureId}",
                        MaxRetries, captureId);

                    throw new MaxRetriesExceededException(
                        $"Refund timed out after {MaxRetries} retries",
                        ex);
                }

                retryCount++;
                var delaySeconds = Math.Pow(2, retryCount);

                _logger.LogWarning(
                    "Request timeout on attempt {Attempt}/{MaxAttempts} for capture {CaptureId}. " +
                    "Retrying in {DelaySeconds}s.",
                    retryCount, MaxRetries + 1, captureId, delaySeconds);

                if (onRetry != null)
                {
                    await onRetry(retryCount, "Request timeout");
                }

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                continue;
            }
            catch (Exception ex)
            {
                // Unexpected error - log and return failure (don't retry unknown errors)
                _logger.LogError(
                    ex,
                    "Unexpected error during refund for capture {CaptureId}: {ErrorType} {ErrorMessage}",
                    captureId, ex.GetType().Name, ex.Message);

                return Result<PayPalRefundResponse>.Failure(
                    $"Unexpected error during refund: {ex.Message}");
            }
        }

        // Should never reach here (safety fallback)
        var fallbackMessage = lastException?.Message ?? "Unexpected retry loop exit";
        _logger.LogError(
            "Retry loop exited unexpectedly for capture {CaptureId}: {Message}",
            captureId, fallbackMessage);

        throw lastException ?? new Exception(fallbackMessage);
    }

    /// <summary>
    /// Determines if an HTTP error is retryable based on status code
    /// Consolidated from RefundRetryService
    /// </summary>
    private static bool IsRetryableError(HttpRequestException ex)
    {
        // Check for HTTP status code
        if (ex.StatusCode.HasValue)
        {
            var statusCode = (int)ex.StatusCode.Value;

            // 5xx server errors are retryable (transient server issues)
            if (statusCode >= 500 && statusCode < 600)
            {
                return true;
            }

            // 4xx client errors are NOT retryable (permanent request issues)
            if (statusCode >= 400 && statusCode < 500)
            {
                return false;
            }
        }

        // Network-level errors (no status code) are retryable
        if (ex.InnerException is SocketException or TaskCanceledException)
        {
            return true;
        }

        // Unknown errors without status code: retry to be safe
        if (!ex.StatusCode.HasValue)
        {
            return true;
        }

        return false;
    }

    #endregion

    #region Private Helper Methods - Business Logic

    /// <summary>
    /// Auto-cancels volunteer signups when ticket is refunded
    /// Single Responsibility: Side effect of refund completion
    /// </summary>
    private async Task AutoCancelVolunteerSignupsAsync(
        TicketPurchase ticketPurchase,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get event attendance to find the event ID
            var eventAttendance = await _context.EventAttendances
                .AsNoTracking()
                .FirstOrDefaultAsync(ea => ea.TicketPurchaseId == ticketPurchase.Id, cancellationToken);

            if (eventAttendance != null)
            {
                // Cancel all volunteer signups for this user and event
                var cancellationResult = await _volunteerAssignmentService.CancelAllVolunteerSignupsForUserEventAsync(
                    ticketPurchase.UserId,
                    eventAttendance.EventId,
                    "Refunded Ticket, so automatically canceled volunteer spot",
                    cancellationToken);

                if (cancellationResult.success && cancellationResult.cancelledCount > 0)
                {
                    _logger.LogInformation(
                        "Auto-cancelled {Count} volunteer signups for user {UserId} at event {EventId} due to ticket refund",
                        cancellationResult.cancelledCount, ticketPurchase.UserId, eventAttendance.EventId);
                }
                else if (!cancellationResult.success)
                {
                    _logger.LogWarning(
                        "Failed to auto-cancel volunteer signups for user {UserId} at event {EventId}: {Error}",
                        ticketPurchase.UserId, eventAttendance.EventId, cancellationResult.error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-cancelling volunteer signups for ticket {TicketId} refund", ticketPurchase.Id);
        }
    }

    /// <summary>
    /// Sends refund confirmation email to user
    /// Single Responsibility: Email notification side effect
    /// </summary>
    private async Task SendRefundConfirmationEmailAsync(
        TicketPurchase ticketPurchase,
        PaymentRefund refund,
        CancellationToken cancellationToken)
    {
        try
        {
            var timingMessage = GetRefundTimingMessage(ticketPurchase.PaymentMethod);
            var variables = new Dictionary<string, string>
            {
                { "user_name", ticketPurchase.User!.UserName ?? ticketPurchase.User.Email ?? "Valued Member" },
                { "refund_amount", refund.GetRefundAmount().ToDisplayString() },
                { "original_amount", Money.Create(ticketPurchase.TotalPrice, "USD").ToDisplayString() },
                { "payment_method", ticketPurchase.PaymentMethod },
                { "timing_message", timingMessage },
                { "refund_reason", refund.RefundReason },
                { "refund_id", refund.Id.ToString() }
            };

            var emailResult = await _emailService.SendTemplatedEmailAsync(
                ticketPurchase.User.Email!,
                ticketPurchase.User.UserName ?? ticketPurchase.User.Email ?? "Valued Member",
                EmailCategory.Admin,
                "RefundConfirmation",
                variables,
                cancellationToken);

            if (emailResult.IsSuccess)
            {
                _logger.LogInformation(
                    "Refund confirmation email sent successfully for refund {RefundId} to {Email}",
                    refund.Id, ticketPurchase.User.Email);
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
            _logger.LogError(ex, "Error sending refund confirmation email for refund {RefundId}", refund.Id);
        }
    }

    /// <summary>
    /// Gets the refund timing message based on payment method
    /// </summary>
    private static string GetRefundTimingMessage(string paymentMethod)
    {
        return paymentMethod.ToUpper() switch
        {
            "PAYPAL" => "Your refund will be available immediately in your PayPal account.",
            "VENMO" => "Your refund should appear in your Venmo account within a few hours.",
            "CASH" => "Please contact us at support@witchcityrope.com to arrange your cash refund.",
            _ => "Please contact us at support@witchcityrope.com for refund timing information."
        };
    }

    #endregion
}

/// <summary>
/// Exception thrown when max retries exceeded for a refund operation
/// Consolidated from RefundRetryService
/// </summary>
public class MaxRetriesExceededException : Exception
{
    public MaxRetriesExceededException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
