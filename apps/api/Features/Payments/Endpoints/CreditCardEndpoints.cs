using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Safety.Services;

namespace WitchCityRope.Api.Features.Payments.Endpoints;

/// <summary>
/// Credit card payment endpoints using Accept.js nonce-based processing.
/// Card data never touches our server - Accept.js tokenizes directly with Authorize.net.
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize]
public class CreditCardEndpoints : ControllerBase
{
    private readonly IAuthorizeNetService _authorizeNetService;
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<CreditCardEndpoints> _logger;

    public CreditCardEndpoints(
        IAuthorizeNetService authorizeNetService,
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<CreditCardEndpoints> logger)
    {
        _authorizeNetService = authorizeNetService;
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Process a credit card payment using an Accept.js nonce.
    /// The frontend sends card data directly to Authorize.net via Accept.js,
    /// receives a nonce, and sends only the nonce to this endpoint.
    /// Persists transaction details back to TicketPurchase.
    /// </summary>
    [HttpPost("credit-card")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CreditCardPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ProcessCreditCardPayment(
        [FromBody] CreditCardPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
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

        if (string.IsNullOrEmpty(request.Nonce) || string.IsNullOrEmpty(request.DataDescriptor))
        {
            return Problem(
                title: "Bad Request",
                detail: "Payment nonce and data descriptor are required",
                statusCode: 400);
        }

        _logger.LogInformation(
            "Processing credit card payment for user {UserId}, amount {Amount}, ticketPurchaseId {TicketPurchaseId}",
            userId, request.Amount, request.TicketPurchaseId);

        var result = await _authorizeNetService.ProcessPaymentWithNonceAsync(
            request.Amount,
            request.Nonce,
            request.DataDescriptor,
            $"WitchCityRope Event Payment",
            request.TicketPurchaseId?.ToString());

        if (!result.Success)
        {
            _logger.LogWarning(
                "Credit card payment failed for user {UserId}: {Error}",
                userId, result.ErrorMessage);

            return Problem(
                title: "Payment Failed",
                detail: result.ErrorMessage ?? "Payment was declined",
                statusCode: 400);
        }

        // Persist payment details to TicketPurchase
        if (request.TicketPurchaseId.HasValue)
        {
            var ticketPurchase = await _context.TicketPurchases
                .FirstOrDefaultAsync(tp => tp.Id == request.TicketPurchaseId.Value, cancellationToken);

            if (ticketPurchase != null)
            {
                ticketPurchase.EncryptedAuthNetTransactionId = !string.IsNullOrEmpty(result.TransactionId)
                    ? await _encryptionService.EncryptAsync(result.TransactionId)
                    : null;
                ticketPurchase.CreditCardLastFour = request.LastFourDigits;
                ticketPurchase.CreditCardType = request.CardType;
                ticketPurchase.PaymentStatus = "Completed";
                ticketPurchase.PaymentMethod = "authorize-net";
                ticketPurchase.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Credit card payment persisted to TicketPurchase {TicketPurchaseId}",
                    ticketPurchase.Id);
            }
            else
            {
                _logger.LogWarning(
                    "No TicketPurchase found for ID {TicketPurchaseId}",
                    request.TicketPurchaseId.Value);
            }
        }

        return Ok(new CreditCardPaymentResponse
        {
            TransactionId = result.TransactionId ?? "",
            Status = "Completed",
            AuthCode = result.AuthCode
        });
    }
}

// --- Request/Response DTOs ---

public class CreditCardPaymentRequest
{
    public string Nonce { get; set; } = string.Empty;
    public string DataDescriptor { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? TicketPurchaseId { get; set; }
    public string? LastFourDigits { get; set; }
    public string? CardType { get; set; }
}

public class CreditCardPaymentResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AuthCode { get; set; }
}
