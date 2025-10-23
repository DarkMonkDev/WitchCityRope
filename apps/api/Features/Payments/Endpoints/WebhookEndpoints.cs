using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.Extensions;

namespace WitchCityRope.Api.Features.Payments.Endpoints;

/// <summary>
/// PayPal webhook endpoints for payment processing events
/// These endpoints are called by PayPal to notify us of payment status changes
/// </summary>
[ApiController]
[Route("api/webhooks")]
[AllowAnonymous] // Webhooks don't use standard authentication
public class WebhookEndpoints : ControllerBase
{
    private readonly IPayPalService _payPalService;
    private readonly ILogger<WebhookEndpoints> _logger;

    public WebhookEndpoints(IPayPalService payPalService, ILogger<WebhookEndpoints> logger)
    {
        _payPalService = payPalService;
        _logger = logger;
    }

    /// <summary>
    /// Handle PayPal webhook events
    /// Validates webhook signature and processes payment status updates
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
            // Read the request body
            string payload;
            using (var reader = new StreamReader(HttpContext.Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            // Get the PayPal signature from headers
            var signature = HttpContext.Request.Headers["PAYPAL-TRANSMISSION-SIG"].FirstOrDefault();
            var transmissionId = HttpContext.Request.Headers["PAYPAL-TRANSMISSION-ID"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(transmissionId))
            {
                _logger.LogWarning("PayPal webhook received without required signature headers");
                return Problem(
                    title: "Bad Request",
                    detail: "Missing PayPal signature headers",
                    statusCode: 400);
            }

            if (string.IsNullOrEmpty(payload))
            {
                _logger.LogWarning("PayPal webhook received with empty payload");
                return Problem(
                    title: "Bad Request",
                    detail: "Empty webhook payload",
                    statusCode: 400);
            }

            // Get webhook ID from configuration
            var webhookId = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["PayPal:WebhookId"];

            if (string.IsNullOrEmpty(webhookId))
            {
                _logger.LogError("PayPal webhook ID not configured");
                return Problem(
                    title: "Server Error",
                    detail: "Webhook configuration error",
                    statusCode: 500);
            }

            _logger.LogInformation("Processing PayPal webhook, payload length: {Length}", payload.Length);

            // Validate webhook signature and parse event
            var validationResult = _payPalService.ValidateWebhookSignature(payload, signature, webhookId);

            if (!validationResult.IsSuccess || validationResult.Value == null)
            {
                _logger.LogWarning("PayPal webhook signature validation failed: {Error}", validationResult.ErrorMessage);
                return Problem(
                    title: "Bad Request",
                    detail: "Invalid webhook signature",
                    statusCode: 400);
            }

            var paypalEvent = validationResult.Value;

            // Extract event type and ID for logging using extension methods
            var eventType = paypalEvent.GetStringValue("event_type") ?? "unknown";
            var eventId = paypalEvent.GetStringValue("id") ?? "unknown";

            _logger.LogInformation(
                "Valid PayPal webhook received: {EventType}, Event ID: {EventId}",
                eventType, eventId);

            // Process the webhook event
            var processingResult = await _payPalService.ProcessWebhookEventAsync(paypalEvent, cancellationToken);

            if (!processingResult.IsSuccess)
            {
                _logger.LogError(
                    "Failed to process PayPal webhook event {EventId} of type {EventType}: {Error}",
                    eventId, eventType, processingResult.ErrorMessage);

                // Return 500 so PayPal will retry the webhook
                return Problem(
                    title: "Server Error",
                    detail: "Failed to process webhook event",
                    statusCode: 500);
            }

            _logger.LogInformation(
                "Successfully processed PayPal webhook event {EventId} of type {EventType}",
                eventId, eventType);

            // Return 200 to acknowledge receipt to PayPal
            return Ok(new { received = true, eventId = eventId, eventType = eventType });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing PayPal webhook");

            // Return 500 so PayPal will retry the webhook
            return Problem(
                title: "Server Error",
                detail: "Internal server error processing webhook",
                statusCode: 500);
        }
    }

    /// <summary>
    /// Health check endpoint for webhook monitoring
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