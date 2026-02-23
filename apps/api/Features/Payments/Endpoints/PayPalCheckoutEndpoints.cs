using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Safety.Services;

namespace WitchCityRope.Api.Features.Payments.Endpoints;

/// <summary>
/// Server-side PayPal checkout endpoints.
/// These replace client-side order creation/capture to ensure the backend
/// always controls and verifies payment processing.
/// </summary>
[ApiController]
[Route("api/paypal")]
[Authorize]
public class PayPalCheckoutEndpoints : ControllerBase
{
    private readonly IPayPalService _payPalService;
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<PayPalCheckoutEndpoints> _logger;

    public PayPalCheckoutEndpoints(
        IPayPalService payPalService,
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<PayPalCheckoutEndpoints> logger)
    {
        _payPalService = payPalService;
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Create a PayPal order server-side.
    /// Called by the frontend PayPal button's createOrder callback.
    /// Returns the order ID for the PayPal JS SDK to open the popup.
    /// Also stores the encrypted OrderId + hash on the TicketPurchase for webhook lookup.
    /// </summary>
    [HttpPost("create-order")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        if (request.Amount <= 0)
        {
            return Problem(
                title: "Bad Request",
                detail: "Amount must be greater than zero",
                statusCode: 400);
        }

        if (request.SlidingScalePercentage < 0 || request.SlidingScalePercentage > 75)
        {
            return Problem(
                title: "Bad Request",
                detail: "Sliding scale percentage must be between 0 and 75",
                statusCode: 400);
        }

        _logger.LogInformation(
            "Creating PayPal order for user {UserId}, amount {Amount}, ticketPurchaseId {TicketPurchaseId}",
            userId, request.Amount, request.TicketPurchaseId);

        var money = Money.Create(request.Amount, request.Currency ?? "USD");

        var metadata = new Dictionary<string, string>();
        if (request.TicketPurchaseId.HasValue)
            metadata["ticketPurchaseId"] = request.TicketPurchaseId.Value.ToString();
        if (!string.IsNullOrEmpty(request.EventTitle))
            metadata["eventTitle"] = request.EventTitle;

        var result = await _payPalService.CreateOrderAsync(
            money,
            userGuid,
            request.SlidingScalePercentage,
            metadata,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to create PayPal order: {Error}", result.ErrorMessage);
            return Problem(
                title: "Payment Error",
                detail: result.ErrorMessage,
                statusCode: 500);
        }

        var orderId = result.Value!.OrderId;

        // Persist OrderId + hash on TicketPurchase for webhook lookup
        if (request.TicketPurchaseId.HasValue)
        {
            var ticketPurchase = await _context.TicketPurchases
                .FirstOrDefaultAsync(tp => tp.Id == request.TicketPurchaseId.Value, cancellationToken);

            if (ticketPurchase != null)
            {
                ticketPurchase.EncryptedPayPalOrderId = await _encryptionService.EncryptAsync(orderId);
                ticketPurchase.PayPalOrderIdHash = ComputeSha256Hash(orderId);
                ticketPurchase.PaymentMethod = "PayPal";
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Stored PayPal OrderId on TicketPurchase {TicketPurchaseId}",
                    request.TicketPurchaseId.Value);
            }
        }

        return Ok(new CreateOrderResponse { OrderId = orderId });
    }

    /// <summary>
    /// Capture a PayPal order server-side after user approval.
    /// Called by the frontend PayPal button's onApprove callback.
    /// Persists capture details back to TicketPurchase.
    /// </summary>
    [HttpPost("capture-order")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CaptureOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CaptureOrder(
        [FromBody] CaptureOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(request.OrderId))
        {
            return Problem(
                title: "Bad Request",
                detail: "OrderId is required",
                statusCode: 400);
        }

        _logger.LogInformation(
            "Capturing PayPal order {OrderId} for user {UserId}",
            request.OrderId, userId);

        var result = await _payPalService.CaptureOrderAsync(request.OrderId, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to capture PayPal order {OrderId}: {Error}",
                request.OrderId, result.ErrorMessage);
            return Problem(
                title: "Payment Error",
                detail: result.ErrorMessage,
                statusCode: 500);
        }

        var capture = result.Value!;

        // Persist capture details to TicketPurchase
        var orderIdHash = ComputeSha256Hash(request.OrderId);
        var ticketPurchase = await _context.TicketPurchases
            .FirstOrDefaultAsync(tp => tp.PayPalOrderIdHash == orderIdHash, cancellationToken);

        if (ticketPurchase != null)
        {
            ticketPurchase.EncryptedPayPalCaptureId = await _encryptionService.EncryptAsync(capture.CaptureId);
            ticketPurchase.EncryptedPayPalPayerId = capture.PayerId != null
                ? await _encryptionService.EncryptAsync(capture.PayerId)
                : null;
            ticketPurchase.PaymentStatus = "Completed";
            ticketPurchase.PaymentMethod = "PayPal";
            ticketPurchase.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "PayPal capture persisted to TicketPurchase {TicketPurchaseId}",
                ticketPurchase.Id);
        }
        else
        {
            _logger.LogWarning(
                "No TicketPurchase found for PayPal OrderId hash. OrderId: {OrderId}",
                request.OrderId);
        }

        return Ok(new CaptureOrderResponse
        {
            CaptureId = capture.CaptureId,
            Status = capture.Status,
            Amount = capture.Amount.Value,
            Currency = capture.Amount.CurrencyCode,
            PayerId = capture.PayerId
        });
    }

    private static string ComputeSha256Hash(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hashBytes);
    }
}

// --- Request/Response DTOs ---

public class CreateOrderRequest
{
    public Guid? TicketPurchaseId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public int SlidingScalePercentage { get; set; }
    public string? EventTitle { get; set; }
}

public class CreateOrderResponse
{
    public string OrderId { get; set; } = string.Empty;
}

public class CaptureOrderRequest
{
    public string OrderId { get; set; } = string.Empty;
}

public class CaptureOrderResponse
{
    public string CaptureId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Amount { get; set; } = "0.00";
    public string Currency { get; set; } = "USD";
    public string? PayerId { get; set; }
}
