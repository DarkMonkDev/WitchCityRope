using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Models.Requests;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;

namespace WitchCityRope.Api.Features.Payments.Commands;

/// <summary>
/// Command handler for processing variable amount PayPal refunds
/// Supports partial and full refunds WITHOUT canceling RSVP/ticket
/// Follows vertical slice architecture with direct service injection
/// Delegates to RefundService for actual refund processing
/// </summary>
public class ProcessVariableRefund
{
    /// <summary>
    /// Variable refund response DTO
    /// </summary>
    public class VariableRefundResponse
    {
        public Guid RefundId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal RemainingRefundableAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Execute variable refund operation
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

        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Administrator" && userRole != "Teacher")
        {
            logger.LogWarning(
                "Unauthorized variable refund attempt by user {UserId} with role {Role}",
                currentUserId, userRole);
            return Results.Problem(
                title: "Forbidden",
                detail: "Only admins and teachers can process refunds",
                statusCode: 403);
        }

        // 2. VALIDATION - Refund Amount
        if (request.RefundAmount <= 0)
        {
            return Results.Problem(
                title: "Invalid Request",
                detail: "RefundAmount must be greater than 0",
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

        // 5. VALIDATE PAYMENT STATUS
        if (!ticketPurchase.IsPaymentCompleted)
        {
            return Results.Problem(
                title: "Payment Not Completed",
                detail: "Only completed payments can be refunded. This transaction is not completed.",
                statusCode: 400);
        }

        // 6. VALIDATE PAYMENT IS PAYPAL
        if (!ticketPurchase.PaymentMethod.Equals("PayPal", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                title: "Invalid Payment Method",
                detail: $"Only PayPal payments can be refunded through this endpoint. This payment was made via {ticketPurchase.PaymentMethod}.",
                statusCode: 400);
        }

        // 7. VALIDATE CAPTURE ID EXISTS
        if (string.IsNullOrEmpty(ticketPurchase.EncryptedPayPalCaptureId))
        {
            logger.LogError(
                "Transaction {TransactionId} missing PayPal Capture ID - cannot process refund",
                transactionId);
            return Results.Problem(
                title: "Payment Error",
                detail: "This transaction is missing PayPal Capture ID. This payment cannot be refunded. Please contact support.",
                statusCode: 500);
        }

        // 8. CALCULATE TOTAL REFUNDED AMOUNT
        var existingRefunds = await dbContext.PaymentRefunds
            .Where(pr => pr.TicketPurchaseId == ticketPurchase.Id)
            .ToListAsync(cancellationToken);

        var totalRefunded = existingRefunds.Sum(r => r.RefundAmountValue);
        var remainingRefundableAmount = ticketPurchase.TotalPrice - totalRefunded;

        // 9. VALIDATE REFUND AMOUNT DOES NOT EXCEED REMAINING
        if (request.RefundAmount > remainingRefundableAmount)
        {
            logger.LogWarning(
                "Refund amount {RequestedAmount} exceeds remaining refundable amount {RemainingAmount} for transaction {TransactionId}",
                request.RefundAmount, remainingRefundableAmount, transactionId);

            return Results.Problem(
                title: "Refund Amount Exceeds Limit",
                detail: $"Refund amount ${request.RefundAmount:F2} exceeds remaining refundable amount ${remainingRefundableAmount:F2}. Original amount: ${ticketPurchase.TotalPrice:F2}, Already refunded: ${totalRefunded:F2}",
                statusCode: 400);
        }

        // 10. PREPARE REFUND REQUEST
        var refundRequest = new ProcessRefundRequest
        {
            PaymentId = ticketPurchase.Id,
            RefundAmount = Money.Create(request.RefundAmount, "USD"),
            RefundReason = request.RefundReason.Trim(),
            ProcessedByUserId = currentUserId,
            IpAddress = "admin-action",
            Metadata = new Dictionary<string, object>
            {
                ["transaction_id"] = transactionId.ToString(),
                ["refund_type"] = "variable_amount",
                ["is_partial_refund"] = request.RefundAmount < ticketPurchase.TotalPrice,
                ["original_amount"] = ticketPurchase.TotalPrice,
                ["total_previously_refunded"] = totalRefunded,
                ["remaining_after_this_refund"] = remainingRefundableAmount - request.RefundAmount,
                ["user_role"] = userRole ?? "Unknown",
                ["rsvp_ticket_not_cancelled"] = true  // CRITICAL: Variable refunds do NOT cancel RSVP/ticket
            }
        };

        // 11. PROCESS REFUND USING REFUNDSERVICE
        logger.LogInformation(
            "Processing variable refund for transaction {TransactionId}, user {UserId}, amount {Amount} (original: {OriginalAmount}, previously refunded: {PreviouslyRefunded})",
            transactionId, ticketPurchase.UserId, request.RefundAmount, ticketPurchase.TotalPrice, totalRefunded);

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

        logger.LogInformation(
            "Variable refund successful: RefundId {RefundId}, TransactionId {TransactionId}, Amount {Amount}, Status {Status}",
            paymentRefund.Id, transactionId, request.RefundAmount, paymentRefund.RefundStatus);

        // 12. UPDATE TICKET PURCHASE STATUS
        var newTotalRefunded = totalRefunded + request.RefundAmount;

        // Determine new payment status
        if (newTotalRefunded >= ticketPurchase.TotalPrice)
        {
            ticketPurchase.PaymentStatus = "Refunded";  // Full amount refunded
        }
        else
        {
            ticketPurchase.PaymentStatus = "PartiallyRefunded";  // Partial refund
        }

        // Add disclaimer to notes
        ticketPurchase.Notes += $"\n[FINANCIAL REFUND {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC by {userRole}]: ${request.RefundAmount:F2} refunded. Total refunded: ${newTotalRefunded:F2} of ${ticketPurchase.TotalPrice:F2}. RSVP/Ticket NOT cancelled. Reason: {request.RefundReason}";
        ticketPurchase.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated ticket purchase {TransactionId} status to {Status}",
            transactionId, ticketPurchase.PaymentStatus);

        // 13. RETURN SUCCESS RESPONSE
        var remainingAfterRefund = remainingRefundableAmount - request.RefundAmount;

        return Results.Ok(new VariableRefundResponse
        {
            RefundId = paymentRefund.Id,
            Amount = paymentRefund.RefundAmountValue,
            Currency = paymentRefund.RefundCurrency,
            Status = paymentRefund.RefundStatus.ToString(),
            Message = request.RefundAmount < ticketPurchase.TotalPrice
                ? "Partial refund processed successfully. RSVP/Ticket NOT cancelled."
                : "Full refund processed successfully. RSVP/Ticket NOT cancelled.",
            RemainingRefundableAmount = remainingAfterRefund,
            PaymentStatus = ticketPurchase.PaymentStatus
        });
    }
}
