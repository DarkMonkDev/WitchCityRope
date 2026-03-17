using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Payments.Models.Requests;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Payments.Commands;

/// <summary>
/// Unified command handler for ticket refunds and cancellations.
/// Supports: partial/full refunds, $0 cancellations, ticket cancellation (revoke access),
/// and optional RSVP removal. Replaces the old separate RefundTicket command.
/// Follows vertical slice architecture with direct service injection.
/// Delegates to RefundService for actual payment processing.
/// </summary>
public class ProcessVariableRefund
{
    /// <summary>
    /// Response DTO for refund/cancellation operations.
    /// Includes both financial and access-related outcomes.
    /// </summary>
    public class VariableRefundResponse
    {
        public Guid RefundId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = PaymentConstants.Currency;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal RemainingRefundableAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>
        /// Whether the ticket was cancelled (EventAttendance set to Cancelled).
        /// True only when CancelTicket was requested and succeeded.
        /// </summary>
        public bool TicketCancelled { get; set; }

        /// <summary>
        /// Whether the RSVP was removed (RSVP-type EventAttendance set to Cancelled).
        /// True only when AlsoRemoveRsvp was requested and an active RSVP existed.
        /// </summary>
        public bool RsvpRemoved { get; set; }
    }

    /// <summary>
    /// Execute refund and/or cancellation operation
    /// </summary>
    public static async Task<IResult> Execute(
        Guid transactionId,
        VariableRefundRequest request,
        ApplicationDbContext dbContext,
        IRefundService refundService,
        ClaimsPrincipal user,
        ILogger<ProcessVariableRefund> logger,
        CancellationToken cancellationToken = default)
    {
        // 1. AUTHENTICATION & AUTHORIZATION
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var currentUserId))
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        // Multi-role support: IsInRole checks all role claims in JWT
        // Collect role string for logging and audit notes below
        var userRole = string.Join(",", user.FindAll(ClaimTypes.Role).Select(c => c.Value));
        if (!user.IsInRole("Administrator") && !user.IsInRole("Teacher"))
        {
            logger.LogWarning(
                "Unauthorized refund attempt by user {UserId} with role {Role}",
                currentUserId, userRole);
            return Results.Problem(
                title: "Forbidden",
                detail: "Only admins and teachers can process refunds",
                statusCode: 403);
        }

        // 2. VALIDATION - Refund Amount
        // $0 is allowed when cancelling a ticket (cancel without refund).
        // Must be > 0 for refund-only operations (no cancel).
        if (request.RefundAmount < 0)
        {
            return Results.Problem(
                title: "Invalid Request",
                detail: "RefundAmount cannot be negative",
                statusCode: 400);
        }

        if (request.RefundAmount == 0 && !request.CancelTicket)
        {
            return Results.Problem(
                title: "Invalid Request",
                detail: "RefundAmount must be greater than 0 for refund-only operations. To cancel without a refund, set CancelTicket to true.",
                statusCode: 400);
        }

        // 3. VALIDATION - Refund Reason
        if (string.IsNullOrWhiteSpace(request.RefundReason) || request.RefundReason.Length < 10)
        {
            return Results.Problem(
                title: "Invalid Request",
                detail: "RefundReason is required and must be at least 10 characters",
                statusCode: 400);
        }

        // 4. RETRIEVE TICKET PURCHASE
        var ticketPurchase = await dbContext.TicketPurchases
            .Include(tp => tp.User)
            .Include(tp => tp.TicketType)
            .FirstOrDefaultAsync(tp => tp.Id == transactionId, cancellationToken);

        if (ticketPurchase == null)
        {
            return Results.Problem(
                title: "Transaction Not Found",
                detail: $"Transaction {transactionId} does not exist",
                statusCode: 404);
        }

        logger.LogInformation(
            "TicketPurchase retrieved: Id={Id}, PaymentMethod={Method}, EncryptedCaptureId={CaptureId}",
            ticketPurchase.Id,
            ticketPurchase.PaymentMethod,
            ticketPurchase.EncryptedPayPalCaptureId == null
                ? "NULL"
                : $"LENGTH:{ticketPurchase.EncryptedPayPalCaptureId.Length}");

        // 5. VALIDATE PAYMENT STATUS (only required when issuing a financial refund)
        if (request.RefundAmount > 0 && !ticketPurchase.IsPaymentCompleted)
        {
            return Results.Problem(
                title: "Payment Not Completed",
                detail: "Only completed payments can be refunded. This transaction is not completed.",
                statusCode: 400);
        }

        // 6. VALIDATE CAPTURE ID FOR PAYPAL PAYMENTS ONLY (only when refunding money)
        // For Cash/Venmo payments, RefundService will handle as manual refund (no PayPal processing)
        if (request.RefundAmount > 0
            && ticketPurchase.PaymentMethod.Equals("PayPal", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrEmpty(ticketPurchase.EncryptedPayPalCaptureId))
        {
            logger.LogError(
                "PayPal transaction {TransactionId} missing Capture ID - cannot process automated refund",
                transactionId);
            return Results.Problem(
                title: "Payment Error",
                detail: "This PayPal payment is missing a Capture ID and cannot be automatically refunded. Please contact support.",
                statusCode: 500);
        }

        // 7. CALCULATE TOTAL REFUNDED AMOUNT
        var existingRefunds = await dbContext.PaymentRefunds
            .Where(pr => pr.TicketPurchaseId == ticketPurchase.Id)
            .ToListAsync(cancellationToken);

        var totalRefunded = existingRefunds.Sum(r => r.RefundAmountValue);
        var remainingRefundableAmount = ticketPurchase.TotalPrice - totalRefunded;

        // 8. VALIDATE REFUND AMOUNT DOES NOT EXCEED REMAINING (only when refunding money)
        if (request.RefundAmount > 0 && request.RefundAmount > remainingRefundableAmount)
        {
            logger.LogWarning(
                "Refund amount {RequestedAmount} exceeds remaining refundable amount {RemainingAmount} for transaction {TransactionId}",
                request.RefundAmount, remainingRefundableAmount, transactionId);

            return Results.Problem(
                title: "Refund Amount Exceeds Limit",
                detail: $"Refund amount ${request.RefundAmount:F2} exceeds remaining refundable amount ${remainingRefundableAmount:F2}. Original amount: ${ticketPurchase.TotalPrice:F2}, Already refunded: ${totalRefunded:F2}",
                statusCode: 400);
        }

        // 9. PROCESS FINANCIAL REFUND (if amount > 0)
        Guid refundId = Guid.Empty;
        decimal refundedAmount = 0;
        string refundStatus = "NoRefund";
        var remainingAfterRefund = remainingRefundableAmount;

        if (request.RefundAmount > 0)
        {
            var refundRequest = new ProcessRefundRequest
            {
                TicketPurchaseId = ticketPurchase.Id,
                RefundAmount = Money.Create(request.RefundAmount, PaymentConstants.Currency),
                RefundReason = request.RefundReason.Trim(),
                ProcessedByUserId = currentUserId,
                IpAddress = "admin-action",
                Metadata = new Dictionary<string, object>
                {
                    ["transaction_id"] = transactionId.ToString(),
                    ["refund_type"] = request.CancelTicket ? "cancel_with_refund" : "variable_amount",
                    ["is_partial_refund"] = request.RefundAmount < ticketPurchase.TotalPrice,
                    ["original_amount"] = ticketPurchase.TotalPrice,
                    ["total_previously_refunded"] = totalRefunded,
                    ["remaining_after_this_refund"] = remainingRefundableAmount - request.RefundAmount,
                    ["user_role"] = string.IsNullOrEmpty(userRole) ? "Unknown" : userRole,
                    ["cancel_ticket"] = request.CancelTicket,
                    ["also_remove_rsvp"] = request.AlsoRemoveRsvp
                }
            };

            logger.LogInformation(
                "Processing refund for transaction {TransactionId}, user {UserId}, amount {Amount} (original: {OriginalAmount}, previously refunded: {PreviouslyRefunded}), cancelTicket={CancelTicket}",
                transactionId, ticketPurchase.UserId, request.RefundAmount, ticketPurchase.TotalPrice, totalRefunded, request.CancelTicket);

            var refundResult = await refundService.ProcessRefundAsync(refundRequest, cancellationToken);

            if (!refundResult.IsSuccess)
            {
                logger.LogError(
                    "RefundService failed for transaction {TransactionId}: {Error}",
                    transactionId, refundResult.ErrorMessage);

                return Results.Problem(
                    title: "Refund Failed",
                    detail: $"Failed to process refund: {refundResult.ErrorMessage}",
                    statusCode: 500);
            }

            var paymentRefund = refundResult.Value!;
            refundId = paymentRefund.Id;
            refundedAmount = paymentRefund.RefundAmountValue;
            refundStatus = paymentRefund.RefundStatus.ToString();

            logger.LogInformation(
                "Refund successful: RefundId {RefundId}, TransactionId {TransactionId}, Amount {Amount}, Status {Status}",
                paymentRefund.Id, transactionId, request.RefundAmount, paymentRefund.RefundStatus);

            // Update ticket purchase payment status
            var newTotalRefunded = totalRefunded + request.RefundAmount;
            ticketPurchase.PaymentStatus = newTotalRefunded >= ticketPurchase.TotalPrice
                ? TicketPurchasePaymentStatus.Refunded
                : TicketPurchasePaymentStatus.PartiallyRefunded;

            ticketPurchase.Notes += $"\n[REFUND ${request.RefundAmount:F2} by {userRole} {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC]: {request.RefundReason.Trim()}";
            ticketPurchase.UpdatedAt = DateTime.UtcNow;
            remainingAfterRefund = remainingRefundableAmount - request.RefundAmount;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 10. CANCEL TICKET if requested (revoke event access)
        bool ticketCancelled = false;
        bool rsvpRemoved = false;

        if (request.CancelTicket)
        {
            // Cancel all Ticket-type EventAttendance records linked to this TicketPurchase
            var ticketAttendances = await dbContext.EventAttendances
                .Where(ea => ea.TicketPurchaseId == transactionId
                    && ea.AttendanceType == AttendanceType.Ticket
                    && ea.Status == AttendanceStatus.Active)
                .ToListAsync(cancellationToken);

            foreach (var attendance in ticketAttendances)
            {
                attendance.Status = AttendanceStatus.Cancelled;
                attendance.CancelledAt = DateTime.UtcNow;
                attendance.CancellationReason = request.RefundAmount > 0
                    ? $"Cancelled with ${request.RefundAmount:F2} refund by {userRole} - {request.RefundReason.Trim()}"
                    : $"Cancelled (no refund) by {userRole} - {request.RefundReason.Trim()}";
                attendance.UpdatedBy = currentUserId;
                attendance.UpdatedAt = DateTime.UtcNow;
            }

            ticketCancelled = ticketAttendances.Count > 0;

            if (ticketCancelled)
            {
                logger.LogInformation(
                    "Cancelled {Count} Ticket attendance records for TicketPurchase {TransactionId}",
                    ticketAttendances.Count, transactionId);
            }

            // If $0 cancel (no refund processed above), still add a note to the ticket purchase
            if (request.RefundAmount == 0)
            {
                ticketPurchase.Notes += $"\n[CANCELLED (no refund) by {userRole} {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC]: {request.RefundReason.Trim()}";
                ticketPurchase.UpdatedAt = DateTime.UtcNow;
            }

            // 11. REMOVE RSVP if requested
            if (request.AlsoRemoveRsvp)
            {
                // Cancel all RSVP-type EventAttendance records for the same user + event.
                // This catches both auto-created RSVPs (from ticket purchase on social events)
                // and independently-created RSVPs.
                var eventId = ticketAttendances.FirstOrDefault()?.EventId
                    ?? await dbContext.EventAttendances
                        .Where(ea => ea.TicketPurchaseId == transactionId)
                        .Select(ea => ea.EventId)
                        .FirstOrDefaultAsync(cancellationToken);

                if (eventId != Guid.Empty)
                {
                    var rsvpAttendances = await dbContext.EventAttendances
                        .Where(ea => ea.UserId == ticketPurchase.UserId
                            && ea.EventId == eventId
                            && ea.AttendanceType == AttendanceType.RSVP
                            && ea.Status == AttendanceStatus.Active)
                        .ToListAsync(cancellationToken);

                    foreach (var rsvp in rsvpAttendances)
                    {
                        rsvp.Status = AttendanceStatus.Cancelled;
                        rsvp.CancelledAt = DateTime.UtcNow;
                        rsvp.CancellationReason = $"RSVP removed during ticket cancellation by {userRole} - {request.RefundReason.Trim()}";
                        rsvp.UpdatedBy = currentUserId;
                        rsvp.UpdatedAt = DateTime.UtcNow;
                    }

                    rsvpRemoved = rsvpAttendances.Count > 0;

                    if (rsvpRemoved)
                    {
                        logger.LogInformation(
                            "Cancelled {Count} RSVP attendance records for user {UserId} event {EventId}",
                            rsvpAttendances.Count, ticketPurchase.UserId, eventId);
                    }
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 12. BUILD RESPONSE MESSAGE
        var messageParts = new List<string>();

        if (request.RefundAmount > 0)
        {
            messageParts.Add(refundedAmount < ticketPurchase.TotalPrice
                ? $"Partial refund of ${refundedAmount:F2} processed successfully."
                : $"Full refund of ${refundedAmount:F2} processed successfully.");
        }

        if (ticketCancelled)
        {
            messageParts.Add("Ticket cancelled - member has lost event access.");
        }
        else if (!request.CancelTicket)
        {
            messageParts.Add("Ticket NOT cancelled - member retains event access.");
        }

        if (rsvpRemoved)
        {
            messageParts.Add("RSVP also removed.");
        }
        else if (request.CancelTicket && !request.AlsoRemoveRsvp)
        {
            messageParts.Add("RSVP retained - member keeps free RSVP access.");
        }

        var message = string.Join(" ", messageParts);

        return Results.Ok(new VariableRefundResponse
        {
            RefundId = refundId,
            Amount = refundedAmount,
            Currency = PaymentConstants.Currency,
            Status = refundStatus,
            Message = message,
            RemainingRefundableAmount = remainingAfterRefund,
            PaymentStatus = ticketPurchase.PaymentStatus.ToString(),
            TicketCancelled = ticketCancelled,
            RsvpRemoved = rsvpRemoved
        });
    }
}
