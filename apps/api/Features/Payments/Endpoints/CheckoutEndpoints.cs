using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Payments.Endpoints;

/// <summary>
/// Unified checkout endpoint that atomically handles:
/// validate → create pending ticket → charge card → finalize.
/// Replaces the broken two-step flow where CC was charged before ticket creation.
/// </summary>
[ApiController]
[Route("api/checkout")]
[Authorize]
public class CheckoutEndpoints : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IAuthorizeNetService _authorizeNetService;
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IEventEmailService _eventEmailService;
    private readonly ILogger<CheckoutEndpoints> _logger;

    public CheckoutEndpoints(
        IAttendanceService attendanceService,
        IAuthorizeNetService authorizeNetService,
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IEventEmailService eventEmailService,
        ILogger<CheckoutEndpoints> logger)
    {
        _attendanceService = attendanceService;
        _authorizeNetService = authorizeNetService;
        _context = context;
        _encryptionService = encryptionService;
        _eventEmailService = eventEmailService;
        _logger = logger;
    }

    /// <summary>
    /// Unified credit card checkout: validate, create pending ticket, charge card, finalize.
    /// If payment fails, pending tickets are rolled back.
    /// If finalization fails after payment, an automatic void/refund is attempted.
    /// </summary>
    [HttpPost("credit-card")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreditCardCheckout(
        [FromBody] CheckoutRequest request,
        [FromServices] IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];

        using (_logger.BeginScope(new Dictionary<string, object> { ["CheckoutCorrelationId"] = correlationId }))
        {
            // ================================================================
            // STAGE 1: VALIDATE
            // ================================================================
            _logger.LogInformation(
                "[Checkout:{CorrelationId}] STAGE 1/4: Validating checkout request. Event={EventId}, Amount={Amount}, IdempotencyKey={IdempotencyKey}",
                correlationId, request.EventId, request.Amount, request.IdempotencyKey);

            // CSRF validation (financial transaction)
            try
            {
                await antiforgery.ValidateRequestAsync(HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                _logger.LogWarning("[Checkout:{CorrelationId}] CSRF validation failed", correlationId);
                return CheckoutProblem(correlationId, "validation",
                    "Security token expired. Please refresh the page and try again.",
                    paymentCharged: false);
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("[Checkout:{CorrelationId}] User={UserId}", correlationId, userId);

            if (request.Amount <= 0)
            {
                return CheckoutProblem(correlationId, "validation",
                    "Payment amount must be greater than zero.",
                    paymentCharged: false);
            }

            if (string.IsNullOrEmpty(request.Nonce) || string.IsNullOrEmpty(request.DataDescriptor))
            {
                return CheckoutProblem(correlationId, "validation",
                    "Payment card information is missing. Please re-enter your card details.",
                    paymentCharged: false);
            }

            if (request.TicketTypeIds == null || request.TicketTypeIds.Count == 0)
            {
                return CheckoutProblem(correlationId, "validation",
                    "No tickets selected.",
                    paymentCharged: false);
            }

            if (string.IsNullOrEmpty(request.IdempotencyKey))
            {
                return CheckoutProblem(correlationId, "validation",
                    "Missing checkout session identifier. Please refresh and try again.",
                    paymentCharged: false);
            }

            // Idempotency check: has this checkout already been processed?
            var existingPurchase = await _context.TicketPurchases
                .AsNoTracking()
                .Where(tp => tp.IdempotencyKey == request.IdempotencyKey && tp.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingPurchase != null)
            {
                if (existingPurchase.PaymentStatus is TicketPurchasePaymentStatus.Completed or TicketPurchasePaymentStatus.Confirmed)
                {
                    _logger.LogInformation(
                        "[Checkout:{CorrelationId}] Idempotent duplicate detected. Returning cached success for TicketPurchase {TicketPurchaseId}",
                        correlationId, existingPurchase.Id);

                    return Ok(new CheckoutResponse
                    {
                        TransactionId = "already-processed",
                        TicketPurchaseIds = new List<Guid> { existingPurchase.Id },
                        ConfirmationNumber = existingPurchase.PaymentReference,
                        Status = "Completed",
                        AmountCharged = existingPurchase.TotalPrice
                    });
                }

                if (existingPurchase.PaymentStatus == TicketPurchasePaymentStatus.Failed)
                {
                    _logger.LogInformation(
                        "[Checkout:{CorrelationId}] Previous attempt with same key failed. Allowing retry.",
                        correlationId);
                    // Fall through to create new ticket purchase
                }
            }

            _logger.LogInformation("[Checkout:{CorrelationId}] STAGE 1 COMPLETE: Validation passed", correlationId);

            // ================================================================
            // STAGE 2: CREATE PENDING TICKET PURCHASE
            // ================================================================
            _logger.LogInformation(
                "[Checkout:{CorrelationId}] STAGE 2/4: Creating pending ticket purchase. TicketTypes=[{TicketTypeIds}]",
                correlationId, string.Join(", ", request.TicketTypeIds));

            var ticketRequest = new CreateTicketPurchaseRequest
            {
                EventId = request.EventId,
                TicketTypeIds = request.TicketTypeIds,
                EventWaiverAccepted = request.EventWaiverAccepted,
                PaymentMethodId = "authnet-pending",
                Notes = $"Checkout {correlationId}",
                Amount = request.Amount
            };

            var ticketResult = await _attendanceService.CreateTicketPurchaseAsync(
                ticketRequest, userId, cancellationToken);

            if (!ticketResult.IsSuccess)
            {
                _logger.LogWarning(
                    "[Checkout:{CorrelationId}] STAGE 2 FAILED: {Error}",
                    correlationId, ticketResult.Error);

                return CheckoutProblem(correlationId, "ticket_creation",
                    ticketResult.Error ?? "Unable to create ticket purchase.",
                    paymentCharged: false);
            }

            // Find the ticket purchases we just created (they have our idempotency key pattern in Notes)
            var pendingPurchases = await _context.TicketPurchases
                .Where(tp => tp.UserId == userId
                    && tp.PaymentMethod == "authnet-pending"
                    && tp.Notes.Contains($"Checkout {correlationId}"))
                .ToListAsync(cancellationToken);

            if (pendingPurchases.Count == 0)
            {
                _logger.LogError(
                    "[Checkout:{CorrelationId}] STAGE 2 ANOMALY: CreateTicketPurchaseAsync succeeded but no pending purchases found",
                    correlationId);

                return CheckoutProblem(correlationId, "ticket_creation",
                    "Ticket records were not created correctly. Please try again.",
                    paymentCharged: false);
            }

            // Set idempotency key on all created purchases
            foreach (var tp in pendingPurchases)
            {
                tp.IdempotencyKey = request.IdempotencyKey;
            }
            await _context.SaveChangesAsync(cancellationToken);

            var ticketPurchaseIds = pendingPurchases.Select(tp => tp.Id).ToList();
            var confirmationNumber = pendingPurchases.First().PaymentReference;

            _logger.LogInformation(
                "[Checkout:{CorrelationId}] STAGE 2 COMPLETE: Created {Count} pending ticket purchase(s). IDs=[{Ids}]",
                correlationId, pendingPurchases.Count, string.Join(", ", ticketPurchaseIds));

            // ================================================================
            // STAGE 3: CHARGE CREDIT CARD
            // ================================================================
            _logger.LogInformation(
                "[Checkout:{CorrelationId}] STAGE 3/4: Charging credit card. Amount={Amount}",
                correlationId, request.Amount);

            var paymentResult = await _authorizeNetService.ProcessPaymentWithNonceAsync(
                request.Amount,
                request.Nonce,
                request.DataDescriptor,
                $"WitchCityRope Tickets - {confirmationNumber}",
                ticketPurchaseIds.First().ToString());

            if (!paymentResult.Success)
            {
                _logger.LogWarning(
                    "[Checkout:{CorrelationId}] STAGE 3 FAILED: Payment declined. " +
                    "ResponseCode={AuthNetResponseCode}, ErrorCode={AuthNetErrorCode}, Error={AuthNetErrorMessage}, " +
                    "Amount={Amount}, EventId={EventId}, AVS={AvsResultCode}, CVV={CvvResultCode}. Rolling back pending tickets.",
                    correlationId, paymentResult.ResponseCode, paymentResult.ErrorCode, paymentResult.ErrorMessage,
                    request.Amount, request.EventId, paymentResult.AvsResultCode, paymentResult.CvvResultCode);

                // Roll back: cancel the pending ticket purchases and attendance records
                await RollbackPendingPurchasesAsync(ticketPurchaseIds, correlationId, cancellationToken);

                var userMessage = paymentResult.ErrorMessage?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
                    ? "This appears to be a duplicate transaction. Please wait a moment before trying again."
                    : paymentResult.ErrorMessage ?? "Your card was declined. Please try a different card or contact your bank.";

                return CheckoutProblem(correlationId, "payment", userMessage, paymentCharged: false);
            }

            _logger.LogInformation(
                "[Checkout:{CorrelationId}] STAGE 3 COMPLETE: Payment approved. TransactionId={TransactionId}, AuthCode={AuthCode}",
                correlationId, paymentResult.TransactionId, paymentResult.AuthCode);

            // ================================================================
            // STAGE 4: FINALIZE - Link payment to tickets
            // ================================================================
            _logger.LogInformation(
                "[Checkout:{CorrelationId}] STAGE 4/4: Finalizing. Linking payment to {Count} ticket purchase(s).",
                correlationId, pendingPurchases.Count);

            try
            {
                // Re-fetch to ensure we have tracked entities
                var purchasesToFinalize = await _context.TicketPurchases
                    .Where(tp => ticketPurchaseIds.Contains(tp.Id))
                    .ToListAsync(cancellationToken);

                foreach (var tp in purchasesToFinalize)
                {
                    tp.EncryptedAuthNetTransactionId = !string.IsNullOrEmpty(paymentResult.TransactionId)
                        ? await _encryptionService.EncryptAsync(paymentResult.TransactionId)
                        : null;
                    tp.CreditCardLastFour = request.LastFourDigits;
                    tp.CreditCardType = request.CardType;
                    tp.PaymentStatus = TicketPurchasePaymentStatus.Completed;
                    tp.PaymentMethod = "authorize-net";
                    tp.ProcessedAt = DateTime.UtcNow;
                    tp.UpdatedAt = DateTime.UtcNow;

                    // Safety net: update TotalPrice from the charged amount if it's still $0
                    if (tp.TotalPrice == 0m && request.Amount > 0m)
                    {
                        _logger.LogWarning(
                            "TicketPurchase {Id} had TotalPrice=$0, updating from charged amount ${Amount}",
                            tp.Id, request.Amount);
                        tp.TotalPrice = request.Amount;
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "[Checkout:{CorrelationId}] STAGE 4 COMPLETE: All ticket purchases finalized successfully. ConfirmationNumber={ConfirmationNumber}",
                    correlationId, confirmationNumber);

                // Activate attendance records now that payment is confirmed
                var activateResult = await _attendanceService.ActivateAttendanceForPurchasesAsync(
                    ticketPurchaseIds, cancellationToken);
                if (!activateResult.IsSuccess)
                {
                    _logger.LogError(
                        "[Checkout:{CorrelationId}] Failed to activate attendance for purchases [{Ids}]: {Error}",
                        correlationId, string.Join(", ", ticketPurchaseIds), activateResult.Error);
                }

                // Post-purchase emails (non-fatal side effect)
                try
                {
                    await _eventEmailService.SendPostPurchaseEmailsAsync(userId, ticketPurchaseIds, cancellationToken);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx,
                        "[Checkout:{CorrelationId}] Post-purchase email failed (non-fatal). Purchases=[{Ids}]",
                        correlationId, string.Join(", ", ticketPurchaseIds));
                }
            }
            catch (Exception ex)
            {
                // CRITICAL: Payment was charged but we failed to finalize the ticket records
                _logger.LogCritical(ex,
                    "[Checkout:{CorrelationId}] STAGE 4 CRITICAL FAILURE: Payment charged (TransactionId={TransactionId}, Amount={Amount}) " +
                    "but failed to finalize ticket purchases [{TicketPurchaseIds}]. Attempting auto-void.",
                    correlationId, paymentResult.TransactionId, request.Amount,
                    string.Join(", ", ticketPurchaseIds));

                var refundInitiated = false;
                try
                {
                    if (!string.IsNullOrEmpty(paymentResult.TransactionId))
                    {
                        var voidResult = await _authorizeNetService.RefundAsync(
                            paymentResult.TransactionId,
                            request.Amount,
                            request.LastFourDigits ?? "0000",
                            $"Auto-void: finalization failed for checkout {correlationId}");

                        refundInitiated = voidResult.Success;

                        _logger.LogWarning(
                            "[Checkout:{CorrelationId}] Auto-void result: Success={Success}, WasVoided={WasVoided}",
                            correlationId, voidResult.Success, voidResult.WasVoided);
                    }
                }
                catch (Exception refundEx)
                {
                    _logger.LogCritical(refundEx,
                        "[Checkout:{CorrelationId}] Auto-void ALSO FAILED for TransactionId={TransactionId}. MANUAL INTERVENTION REQUIRED.",
                        correlationId, paymentResult.TransactionId);
                }

                var detail = refundInitiated
                    ? $"Your payment was processed but we encountered an error completing your registration. " +
                      $"A refund has been initiated. Reference: {correlationId}"
                    : $"Your payment was processed but we encountered an error completing your registration. " +
                      $"Please contact info@witchcityrope.com with reference: {correlationId}";

                return CheckoutProblem(correlationId, "finalization", detail,
                    paymentCharged: true, refundInitiated: refundInitiated);
            }

            // ================================================================
            // SUCCESS
            // ================================================================
            _logger.LogInformation(
                "[Checkout:{CorrelationId}] CHECKOUT COMPLETE. User={UserId}, Event={EventId}, " +
                "Amount={Amount}, TransactionId={TransactionId}, Confirmation={ConfirmationNumber}, " +
                "TicketPurchases={Count}",
                correlationId, userId, request.EventId, request.Amount,
                paymentResult.TransactionId, confirmationNumber, pendingPurchases.Count);

            return Ok(new CheckoutResponse
            {
                TransactionId = paymentResult.TransactionId ?? "",
                TicketPurchaseIds = ticketPurchaseIds,
                ConfirmationNumber = confirmationNumber,
                Status = "Completed",
                AuthCode = paymentResult.AuthCode,
                AmountCharged = request.Amount
            });
        }
    }

    /// <summary>
    /// Roll back pending ticket purchases and their attendance records when payment fails.
    /// </summary>
    private async Task RollbackPendingPurchasesAsync(
        List<Guid> ticketPurchaseIds, string correlationId, CancellationToken cancellationToken)
    {
        try
        {
            // Mark ticket purchases as failed
            var purchases = await _context.TicketPurchases
                .Where(tp => ticketPurchaseIds.Contains(tp.Id))
                .ToListAsync(cancellationToken);

            foreach (var tp in purchases)
            {
                tp.PaymentStatus = TicketPurchasePaymentStatus.Failed;
                tp.UpdatedAt = DateTime.UtcNow;
            }

            // Cancel associated attendance records
            var attendances = await _context.EventAttendances
                .Where(ea => ea.TicketPurchaseId.HasValue
                    && ticketPurchaseIds.Contains(ea.TicketPurchaseId.Value))
                .ToListAsync(cancellationToken);

            foreach (var att in attendances)
            {
                att.Status = AttendanceStatus.Cancelled;
                att.CancelledAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[Checkout:{CorrelationId}] Rolled back {PurchaseCount} ticket purchase(s) and {AttendanceCount} attendance record(s)",
                correlationId, purchases.Count, attendances.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Checkout:{CorrelationId}] Error during rollback of ticket purchases [{TicketPurchaseIds}]",
                correlationId, string.Join(", ", ticketPurchaseIds));
        }
    }

    /// <summary>
    /// Creates a ProblemDetails response with checkout-specific extensions.
    /// </summary>
    private IActionResult CheckoutProblem(
        string correlationId, string failureStage, string detail,
        bool paymentCharged, bool refundInitiated = false)
    {
        return Problem(
            title: failureStage switch
            {
                "validation" => "Checkout Validation Failed",
                "ticket_creation" => "Unable to Process Order",
                "payment" => "Payment Failed",
                "finalization" => "Payment Processing Error",
                _ => "Checkout Failed"
            },
            detail: detail,
            statusCode: paymentCharged ? 500 : 400,
            extensions: new Dictionary<string, object?>
            {
                ["checkoutCorrelationId"] = correlationId,
                ["failureStage"] = failureStage,
                ["paymentCharged"] = paymentCharged,
                ["refundInitiated"] = refundInitiated
            });
    }
}

// --- Request/Response DTOs ---

public class CheckoutRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> TicketTypeIds { get; set; } = new();

    [Required]
    public bool EventWaiverAccepted { get; set; }

    [Required]
    public string Nonce { get; set; } = string.Empty;

    [Required]
    public string DataDescriptor { get; set; } = string.Empty;

    [Required]
    public decimal Amount { get; set; }

    public string? LastFourDigits { get; set; }
    public string? CardType { get; set; }

    [Required]
    [MaxLength(50)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class CheckoutResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public List<Guid> TicketPurchaseIds { get; set; } = new();
    public string ConfirmationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AuthCode { get; set; }
    public decimal AmountCharged { get; set; }
}
