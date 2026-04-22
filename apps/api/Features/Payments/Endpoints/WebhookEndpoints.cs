using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Webhooks.Services;

namespace WitchCityRope.Api.Features.Payments.Endpoints;

/// <summary>
/// PayPal webhook endpoints for payment processing events.
/// Uses real RSA-SHA256 signature verification via PayPalWebhookVerificationService.
/// </summary>
[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public class WebhookEndpoints : ControllerBase
{
    private readonly IPayPalService _payPalService;
    private readonly IPayPalWebhookVerificationService _webhookVerificationService;
    private readonly IPayPalWebhookProcessingService _webhookProcessingService;
    private readonly ILogger<WebhookEndpoints> _logger;

    public WebhookEndpoints(
        IPayPalService payPalService,
        IPayPalWebhookVerificationService webhookVerificationService,
        IPayPalWebhookProcessingService webhookProcessingService,
        ILogger<WebhookEndpoints> logger)
    {
        _payPalService = payPalService;
        _webhookVerificationService = webhookVerificationService;
        _webhookProcessingService = webhookProcessingService;
        _logger = logger;
    }

    /// <summary>
    /// Handle PayPal webhook events.
    /// Validates webhook signature using RSA-SHA256 verification, then processes the event.
    /// </summary>
    [HttpPost("paypal")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HandlePayPalWebhook(CancellationToken cancellationToken = default)
    {
        try
        {
            // Read the raw request body
            string payload;
            using (var reader = new StreamReader(HttpContext.Request.Body))
            {
                payload = await reader.ReadToEndAsync(cancellationToken);
            }

            // Quick header validation
            var transmissionId = HttpContext.Request.Headers["PAYPAL-TRANSMISSION-ID"].FirstOrDefault();
            if (string.IsNullOrEmpty(transmissionId))
            {
                _logger.LogWarning("PayPal webhook received without PAYPAL-TRANSMISSION-ID header");
                return Problem( // ARCH-ALLOW: MVC controller webhook — header validation, no service Result to route through
                    title: "Bad Request",
                    detail: "Missing PayPal signature headers",
                    statusCode: 400);
            }

            if (string.IsNullOrEmpty(payload))
            {
                _logger.LogWarning("PayPal webhook received with empty payload");
                return Problem( // ARCH-ALLOW: MVC controller webhook — payload validation, no service Result to route through
                    title: "Bad Request",
                    detail: "Empty webhook payload",
                    statusCode: 400);
            }

            _logger.LogInformation("Processing PayPal webhook, transmission ID: {TransmissionId}, payload length: {Length}",
                transmissionId, payload.Length);

            // Verify webhook signature using real RSA-SHA256 verification
            var isSignatureValid = await _webhookVerificationService.VerifyWebhookSignatureAsync(
                HttpContext.Request,
                payload,
                cancellationToken);

            if (!isSignatureValid)
            {
                _logger.LogWarning("PayPal webhook signature verification failed for transmission {TransmissionId}",
                    transmissionId);
                return Problem( // ARCH-ALLOW: MVC controller webhook — signature verification, no Result<T> flow
                    title: "Bad Request",
                    detail: "Invalid webhook signature",
                    statusCode: 400);
            }

            // Parse and validate the webhook event
            var signature = HttpContext.Request.Headers["PAYPAL-TRANSMISSION-SIG"].FirstOrDefault() ?? "";
            var webhookId = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["PayPal:WebhookId"] ?? "";

            var validationResult = _payPalService.ValidateWebhookSignatureTyped(payload, signature, webhookId);

            if (!validationResult.IsSuccess || validationResult.Value == null)
            {
                _logger.LogWarning("PayPal webhook payload parsing failed: {Error}", validationResult.ErrorMessage);
                return Problem( // ARCH-ALLOW: MVC controller returns IActionResult — ToProblem yields IResult (mismatched types)
                    title: "Bad Request",
                    detail: "Invalid webhook payload",
                    statusCode: 400);
            }

            var paypalEvent = validationResult.Value;

            _logger.LogInformation(
                "Valid PayPal webhook received: {EventType}, Event ID: {EventId}",
                paypalEvent.EventType, paypalEvent.Id);

            // Process the webhook event via dedicated processing service
            var processingResult = await _webhookProcessingService.ProcessEventAsync(
                paypalEvent, transmissionId, cancellationToken);

            if (!processingResult.IsSuccess)
            {
                _logger.LogError(
                    "Failed to process PayPal webhook event {EventId} of type {EventType}: {Error}",
                    paypalEvent.Id, paypalEvent.EventType, processingResult.ErrorMessage);

                return Problem( // ARCH-ALLOW: MVC controller returns IActionResult — ToProblem yields IResult (mismatched types)
                    title: "Server Error",
                    detail: "Failed to process webhook event",
                    statusCode: 500);
            }

            _logger.LogInformation(
                "Successfully processed PayPal webhook event {EventId} of type {EventType}",
                paypalEvent.Id, paypalEvent.EventType);

            return Ok(new { received = true, eventId = paypalEvent.Id, eventType = paypalEvent.EventType });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing PayPal webhook");
            return Problem( // ARCH-ALLOW: handler catch-all — MVC controller webhook path
                title: "Server Error",
                detail: "Internal server error processing webhook",
                statusCode: 500);
        }
    }

    /// <summary>
    /// Health check endpoint for webhook monitoring.
    /// </summary>
    [HttpGet("paypal/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "paypal-webhooks",
            timestamp = DateTime.UtcNow.ToString("O")
        });
    }
}
