using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.Payments.Services;

namespace WitchCityRope.Api.Features.Payments.Endpoints;

/// <summary>
/// Stripe webhook endpoints for payment processing events
/// These endpoints are called by Stripe to notify us of payment status changes
/// </summary>
[ApiController]
[Route("api/webhooks")]
[AllowAnonymous] // Webhooks don't use standard authentication
public class WebhookEndpoints : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<WebhookEndpoints> _logger;

    public WebhookEndpoints(IStripeService stripeService, ILogger<WebhookEndpoints> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    /// <summary>
    /// Handle Stripe webhook events
    /// Validates webhook signature and processes payment status updates
    /// </summary>
    [HttpPost("stripe")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HandleStripeWebhook(CancellationToken cancellationToken = default)
    {
        try
        {
            // Read the request body
            string payload;
            using (var reader = new StreamReader(HttpContext.Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            // Get the Stripe signature from headers
            var signature = HttpContext.Request.Headers["Stripe-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Stripe webhook received without signature header");
                return BadRequest(new { error = "Missing Stripe-Signature header" });
            }

            if (string.IsNullOrEmpty(payload))
            {
                _logger.LogWarning("Stripe webhook received with empty payload");
                return BadRequest(new { error = "Empty webhook payload" });
            }

            // Get webhook secret from configuration
            var webhookSecret = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["Stripe:WebhookSecret"];

            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogError("Stripe webhook secret not configured");
                return StatusCode(500, new { error = "Webhook configuration error" });
            }

            _logger.LogInformation("Processing Stripe webhook, payload length: {Length}", payload.Length);

            // Validate webhook signature and parse event
            var validationResult = _stripeService.ValidateWebhookSignature(payload, signature, webhookSecret);

            if (!validationResult.IsSuccess || validationResult.Value == null)
            {
                _logger.LogWarning("Stripe webhook signature validation failed: {Error}", validationResult.ErrorMessage);
                return BadRequest(new { error = "Invalid webhook signature" });
            }

            var stripeEvent = validationResult.Value;

            _logger.LogInformation(
                "Valid Stripe webhook received: {EventType}, Event ID: {EventId}",
                stripeEvent.Type, stripeEvent.Id);

            // Process the webhook event
            var processingResult = await _stripeService.ProcessWebhookEventAsync(stripeEvent, cancellationToken);

            if (!processingResult.IsSuccess)
            {
                _logger.LogError(
                    "Failed to process Stripe webhook event {EventId} of type {EventType}: {Error}",
                    stripeEvent.Id, stripeEvent.Type, processingResult.ErrorMessage);

                // Return 500 so Stripe will retry the webhook
                return StatusCode(500, new { error = "Failed to process webhook event" });
            }

            _logger.LogInformation(
                "Successfully processed Stripe webhook event {EventId} of type {EventType}",
                stripeEvent.Id, stripeEvent.Type);

            // Return 200 to acknowledge receipt to Stripe
            return Ok(new { received = true, eventId = stripeEvent.Id, eventType = stripeEvent.Type });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Stripe webhook");

            // Return 500 so Stripe will retry the webhook
            return StatusCode(500, new { error = "Internal server error processing webhook" });
        }
    }

    /// <summary>
    /// Health check endpoint for webhook monitoring
    /// </summary>
    [HttpGet("stripe/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new 
        { 
            status = "healthy", 
            service = "stripe-webhooks",
            timestamp = DateTime.UtcNow.ToString("O")
        });
    }
}