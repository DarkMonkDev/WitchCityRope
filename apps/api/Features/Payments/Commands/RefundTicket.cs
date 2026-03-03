using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Payments.Commands;

/// <summary>
/// Command handler for processing PayPal refunds for ticket purchases
/// Follows vertical slice architecture with direct service injection
/// Delegates to RefundService for actual refund processing
/// </summary>
public class RefundTicket
{
    /// <summary>
    /// Refund response DTO
    /// </summary>
    public class RefundResponse
    {
        public Guid RefundId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Execute refund operation
    /// </summary>
    public static async Task<IResult> Execute(
        Guid ticketId,
        AdminRefundTicketRequest request,
        ApplicationDbContext dbContext,
        IRefundService refundService,
        ClaimsPrincipal user,
        ILogger<RefundTicket> logger,
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
                "Unauthorized refund attempt by user {UserId} with role {Role}",
                currentUserId, userRole);
            return Results.Problem(
                title: "Forbidden",
                detail: "Only admins and teachers can process refunds",
                statusCode: 403);
        }

        // 2. VALIDATION - Refund Reason
        if (string.IsNullOrWhiteSpace(request.RefundReason) || request.RefundReason.Length < 10)
        {
            return Results.Problem(
                title: "Invalid Request",
                detail: "RefundReason is required and must be at least 10 characters",
                statusCode: 400);
        }

        // 3. RETRIEVE TICKET PURCHASE
        var ticketPurchase = await dbContext.TicketPurchases
            .Include(tp => tp.User)
            .Include(tp => tp.TicketType)
            .FirstOrDefaultAsync(tp => tp.Id == ticketId, cancellationToken);

        if (ticketPurchase == null)
        {
            return Results.Problem(
                title: "Ticket Not Found",
                detail: $"Ticket purchase {ticketId} does not exist",
                statusCode: 404);
        }

        // 4. VALIDATE PAYMENT STATUS
        // ARCHITECTURE FIX: Now validates directly against TicketPurchase (single source of truth)
        if (!ticketPurchase.IsPaymentCompleted)
        {
            return Results.Problem(
                title: "Payment Not Completed",
                detail: "Only completed payments can be refunded. This ticket purchase is not completed.",
                statusCode: 400);
        }

        // 5. VALIDATE CAPTURE ID FOR PAYPAL PAYMENTS ONLY
        // For Cash/Venmo payments, RefundService will handle as manual refund (no PayPal processing)
        if (ticketPurchase.PaymentMethod.Equals("PayPal", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrEmpty(ticketPurchase.EncryptedPayPalCaptureId))
        {
            logger.LogError(
                "PayPal ticket {TicketId} missing Capture ID - cannot process automated refund",
                ticketId);
            return Results.Problem(
                title: "Payment Error",
                detail: "This PayPal payment is missing a Capture ID and cannot be automatically refunded. Please contact support.",
                statusCode: 500);
        }

        // 6. CHECK FOR EXISTING REFUND
        var existingRefund = await dbContext.PaymentRefunds
            .Where(pr => pr.TicketPurchaseId == ticketPurchase.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingRefund != null)
        {
            return Results.Problem(
                title: "Already Refunded",
                detail: "This ticket has already been refunded",
                statusCode: 400);
        }

        // 7. PREPARE REFUND REQUEST
        // ARCHITECTURE FIX: Now uses TicketPurchase ID instead of Payment ID
        var refundRequest = new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchase.Id,
            RefundAmount = Money.Create(ticketPurchase.TotalPrice, "USD"),
            RefundReason = request.RefundReason.Trim(),
            ProcessedByUserId = currentUserId,
            IpAddress = "admin-action",
            Metadata = new Dictionary<string, object>
            {
                ["ticket_id"] = ticketId.ToString(),
                ["also_remove_rsvp"] = request.AlsoRemoveRsvp,
                ["user_role"] = userRole ?? "Unknown",
                ["ticket_total_price"] = ticketPurchase.TotalPrice
            }
        };

        // 9. PROCESS REFUND USING REFUNDSERVICE
        logger.LogInformation(
            "Processing refund for ticket {TicketId}, user {UserId}, amount {Amount}",
            ticketId, ticketPurchase.UserId, ticketPurchase.TotalPrice);

        var refundResult = await refundService.ProcessRefundAsync(refundRequest, cancellationToken);

        if (!refundResult.IsSuccess)
        {
            logger.LogError(
                "RefundService failed for ticket {TicketId}: {Error}",
                ticketId, refundResult.ErrorMessage);

            return Results.Problem(
                title: "Refund Failed",
                detail: $"Failed to process refund: {refundResult.ErrorMessage}",
                statusCode: 500);
        }

        var paymentRefund = refundResult.Value!;

        logger.LogInformation(
            "Refund successful: RefundId {RefundId}, TicketId {TicketId}, Status {Status}",
            paymentRefund.Id, ticketId, paymentRefund.RefundStatus);

        // 10. UPDATE TICKET PURCHASE STATUS
        ticketPurchase.PaymentStatus = "Refunded";
        ticketPurchase.Notes += $"\n[REFUNDED {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC by {userRole}]: {request.RefundReason}";
        ticketPurchase.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        // 11. REMOVE RSVP IF REQUESTED
        if (request.AlsoRemoveRsvp)
        {
            var eventAttendance = await dbContext.EventAttendances
                .FirstOrDefaultAsync(ea => ea.UserId == ticketPurchase.UserId
                    && ea.TicketPurchaseId == ticketId
                    && ea.Status == AttendanceStatus.Active,
                    cancellationToken);

            if (eventAttendance != null)
            {
                eventAttendance.Status = AttendanceStatus.Cancelled;
                eventAttendance.CancelledAt = DateTime.UtcNow;
                eventAttendance.CancellationReason = $"Refunded by {userRole} - {request.RefundReason}";
                eventAttendance.UpdatedBy = currentUserId;
                eventAttendance.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Cancelled EventAttendance {AttendanceId} for ticket {TicketId}",
                    eventAttendance.Id, ticketId);
            }
        }

        // 12. RETURN SUCCESS RESPONSE
        return Results.Ok(new RefundResponse
        {
            RefundId = paymentRefund.Id,
            Amount = paymentRefund.RefundAmountValue,
            Currency = paymentRefund.RefundCurrency,
            Status = paymentRefund.RefundStatus.ToString(),
            Message = "Refund processed successfully"
        });
    }
}
