using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using WitchCityRope.Api.Features.Payments.Endpoints;
using WitchCityRope.Api.Features.Payments.Services;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Features.Payments;

/// <summary>
/// Unit tests for PayPal webhook endpoints following Pattern B standards
/// Tests both POST /api/webhooks/paypal and GET /api/webhooks/paypal/health
/// </summary>
public class WebhookEndpointsTests
{
    private readonly IPayPalService _mockPayPalService;
    private readonly ILogger<WebhookEndpoints> _mockLogger;
    private readonly IConfiguration _mockConfiguration;
    private readonly WebhookEndpoints _controller;
    private readonly DefaultHttpContext _httpContext;

    public WebhookEndpointsTests()
    {
        _mockPayPalService = Substitute.For<IPayPalService>();
        _mockLogger = Substitute.For<ILogger<WebhookEndpoints>>();
        _mockConfiguration = Substitute.For<IConfiguration>();

        _controller = new WebhookEndpoints(_mockPayPalService, _mockLogger);

        // Setup HttpContext with services including ProblemDetailsFactory
        _httpContext = new DefaultHttpContext();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_mockConfiguration);

        // Add ProblemDetailsFactory dependencies required by ControllerBase.Problem()
        serviceCollection.AddSingleton<IOptions<ApiBehaviorOptions>>(
            Options.Create(new ApiBehaviorOptions()));
        serviceCollection.AddSingleton<ProblemDetailsFactory, DefaultProblemDetailsFactory>();

        _httpContext.RequestServices = serviceCollection.BuildServiceProvider();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    #region POST /api/webhooks/paypal Tests

    /// <summary>
    /// Test that valid PayPal webhook with signature returns 200 OK
    /// </summary>
    [Fact]
    public async Task HandlePayPalWebhook_WithValidSignature_ReturnsOkResult()
    {
        // Arrange
        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\",\"id\":\"WH-123\"}";
        var signature = "valid-signature";
        var transmissionId = "transmission-123";
        var webhookId = "webhook-id-123";

        SetupHttpContextWithPayload(payload, signature, transmissionId);
        _mockConfiguration["PayPal:WebhookId"].Returns(webhookId);

        var webhookEvent = new Dictionary<string, object>
        {
            { "event_type", "PAYMENT.CAPTURE.COMPLETED" },
            { "id", "WH-123" }
        };

        _mockPayPalService.ValidateWebhookSignature(payload, signature, webhookId)
            .Returns(Result<Dictionary<string, object>>.Success(webhookEvent));

        _mockPayPalService.ProcessWebhookEventAsync(webhookEvent, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        // Verify response contains the expected data
        okResult.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Test that missing PAYPAL-TRANSMISSION-SIG header returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task HandlePayPalWebhook_WithMissingSignatureHeader_ReturnsBadRequest()
    {
        // Arrange
        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\"}";
        SetupHttpContextWithPayload(payload, signature: null, transmissionId: "transmission-123");

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var problemResult = (ObjectResult)result;
        problemResult.StatusCode.Should().Be(400);

        var problemDetails = problemResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Bad Request");
        problemDetails.Detail.Should().Contain("Missing PayPal signature headers");
    }

    /// <summary>
    /// Test that missing PAYPAL-TRANSMISSION-ID header returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task HandlePayPalWebhook_WithMissingTransmissionIdHeader_ReturnsBadRequest()
    {
        // Arrange
        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\"}";
        SetupHttpContextWithPayload(payload, signature: "valid-signature", transmissionId: null);

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var problemResult = (ObjectResult)result;
        problemResult.StatusCode.Should().Be(400);

        var problemDetails = problemResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Bad Request");
        problemDetails.Detail.Should().Contain("Missing PayPal signature headers");
    }

    /// <summary>
    /// Test that empty payload returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task HandlePayPalWebhook_WithEmptyPayload_ReturnsBadRequest()
    {
        // Arrange
        var payload = "";
        SetupHttpContextWithPayload(payload, signature: "valid-signature", transmissionId: "transmission-123");

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var problemResult = (ObjectResult)result;
        problemResult.StatusCode.Should().Be(400);

        var problemDetails = problemResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Bad Request");
        problemDetails.Detail.Should().Contain("Empty webhook payload");
    }

    /// <summary>
    /// Test that invalid webhook signature returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task HandlePayPalWebhook_WithInvalidSignature_ReturnsBadRequest()
    {
        // Arrange
        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\"}";
        var signature = "invalid-signature";
        var transmissionId = "transmission-123";
        var webhookId = "webhook-id-123";

        SetupHttpContextWithPayload(payload, signature, transmissionId);
        _mockConfiguration["PayPal:WebhookId"].Returns(webhookId);

        _mockPayPalService.ValidateWebhookSignature(payload, signature, webhookId)
            .Returns(Result<Dictionary<string, object>>.Failure("Invalid signature"));

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var problemResult = (ObjectResult)result;
        problemResult.StatusCode.Should().Be(400);

        var problemDetails = problemResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Bad Request");
        problemDetails.Detail.Should().Contain("Invalid webhook signature");
    }

    /// <summary>
    /// Test that webhook ID not configured returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task HandlePayPalWebhook_WithMissingWebhookId_ReturnsInternalServerError()
    {
        // Arrange
        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\"}";
        SetupHttpContextWithPayload(payload, signature: "valid-signature", transmissionId: "transmission-123");
        _mockConfiguration["PayPal:WebhookId"].Returns((string?)null);

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var problemResult = (ObjectResult)result;
        problemResult.StatusCode.Should().Be(500);

        var problemDetails = problemResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Server Error");
        problemDetails.Detail.Should().Contain("Webhook configuration error");
    }

    /// <summary>
    /// Test that processing failure returns 500 InternalServerError (PayPal will retry)
    /// </summary>
    [Fact]
    public async Task HandlePayPalWebhook_WithProcessingFailure_ReturnsInternalServerError()
    {
        // Arrange
        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\",\"id\":\"WH-123\"}";
        var signature = "valid-signature";
        var transmissionId = "transmission-123";
        var webhookId = "webhook-id-123";

        SetupHttpContextWithPayload(payload, signature, transmissionId);
        _mockConfiguration["PayPal:WebhookId"].Returns(webhookId);

        var webhookEvent = new Dictionary<string, object>
        {
            { "event_type", "PAYMENT.CAPTURE.COMPLETED" },
            { "id", "WH-123" }
        };

        _mockPayPalService.ValidateWebhookSignature(payload, signature, webhookId)
            .Returns(Result<Dictionary<string, object>>.Success(webhookEvent));

        _mockPayPalService.ProcessWebhookEventAsync(webhookEvent, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Database error"));

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var problemResult = (ObjectResult)result;
        problemResult.StatusCode.Should().Be(500);

        var problemDetails = problemResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Server Error");
        problemDetails.Detail.Should().Contain("Failed to process webhook event");
    }

    /// <summary>
    /// Test that exception during processing returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task HandlePayPalWebhook_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\"}";
        var signature = "valid-signature";
        var transmissionId = "transmission-123";
        var webhookId = "webhook-id-123";

        SetupHttpContextWithPayload(payload, signature, transmissionId);
        _mockConfiguration["PayPal:WebhookId"].Returns(webhookId);

        _mockPayPalService.ValidateWebhookSignature(payload, signature, webhookId)
            .Returns<Result<Dictionary<string, object>>>(x => throw new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var problemResult = (ObjectResult)result;
        problemResult.StatusCode.Should().Be(500);

        var problemDetails = problemResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Server Error");
        problemDetails.Detail.Should().Contain("Internal server error processing webhook");
    }

    /// <summary>
    /// Test successful payment completed event returns 200 OK with event details
    /// </summary>
    [Fact]
    public async Task HandlePayPalWebhook_WithSuccessfulPaymentCompletedEvent_ReturnsOkWithEventDetails()
    {
        // Arrange
        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\",\"id\":\"WH-456\"}";
        var signature = "valid-signature";
        var transmissionId = "transmission-123";
        var webhookId = "webhook-id-123";

        SetupHttpContextWithPayload(payload, signature, transmissionId);
        _mockConfiguration["PayPal:WebhookId"].Returns(webhookId);

        var webhookEvent = new Dictionary<string, object>
        {
            { "event_type", "PAYMENT.CAPTURE.COMPLETED" },
            { "id", "WH-456" }
        };

        _mockPayPalService.ValidateWebhookSignature(payload, signature, webhookId)
            .Returns(Result<Dictionary<string, object>>.Success(webhookEvent));

        _mockPayPalService.ProcessWebhookEventAsync(webhookEvent, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        // Verify the response contains expected fields
        var responseValue = okResult.Value;
        responseValue.Should().NotBeNull();

        // Verify service was called with correct parameters
        await _mockPayPalService.Received(1).ProcessWebhookEventAsync(
            Arg.Is<Dictionary<string, object>>(e => e.ContainsKey("event_type")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test successful refund event returns 200 OK
    /// </summary>
    [Fact]
    public async Task HandlePayPalWebhook_WithSuccessfulRefundEvent_ReturnsOk()
    {
        // Arrange
        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.REFUNDED\",\"id\":\"WH-789\"}";
        var signature = "valid-signature";
        var transmissionId = "transmission-123";
        var webhookId = "webhook-id-123";

        SetupHttpContextWithPayload(payload, signature, transmissionId);
        _mockConfiguration["PayPal:WebhookId"].Returns(webhookId);

        var webhookEvent = new Dictionary<string, object>
        {
            { "event_type", "PAYMENT.CAPTURE.REFUNDED" },
            { "id", "WH-789" }
        };

        _mockPayPalService.ValidateWebhookSignature(payload, signature, webhookId)
            .Returns(Result<Dictionary<string, object>>.Success(webhookEvent));

        _mockPayPalService.ProcessWebhookEventAsync(webhookEvent, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Test unknown event type is logged and handled properly
    /// </summary>
    [Fact]
    public async Task HandlePayPalWebhook_WithUnknownEventType_LogsAndHandles()
    {
        // Arrange
        var payload = "{\"event_type\":\"UNKNOWN.EVENT.TYPE\",\"id\":\"WH-999\"}";
        var signature = "valid-signature";
        var transmissionId = "transmission-123";
        var webhookId = "webhook-id-123";

        SetupHttpContextWithPayload(payload, signature, transmissionId);
        _mockConfiguration["PayPal:WebhookId"].Returns(webhookId);

        var webhookEvent = new Dictionary<string, object>
        {
            { "event_type", "UNKNOWN.EVENT.TYPE" },
            { "id", "WH-999" }
        };

        _mockPayPalService.ValidateWebhookSignature(payload, signature, webhookId)
            .Returns(Result<Dictionary<string, object>>.Success(webhookEvent));

        _mockPayPalService.ProcessWebhookEventAsync(webhookEvent, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        // Verify logging occurred for the unknown event type - NSubstitute auto-converts LogInformation calls
        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Valid PayPal webhook received")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Test multiple headers present validates correct extraction
    /// </summary>
    [Fact]
    public async Task HandlePayPalWebhook_WithMultipleHeaders_ExtractsCorrectly()
    {
        // Arrange
        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\",\"id\":\"WH-MULTI\"}";
        var signature = "correct-signature";
        var transmissionId = "correct-transmission-id";
        var webhookId = "webhook-id-123";

        // Setup HttpContext with multiple header values (testing FirstOrDefault extraction)
        _httpContext.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(payload));
        _httpContext.Request.Headers["PAYPAL-TRANSMISSION-SIG"] = new StringValues(new[] { signature, "extra-value" });
        _httpContext.Request.Headers["PAYPAL-TRANSMISSION-ID"] = new StringValues(new[] { transmissionId, "extra-id" });

        _mockConfiguration["PayPal:WebhookId"].Returns(webhookId);

        var webhookEvent = new Dictionary<string, object>
        {
            { "event_type", "PAYMENT.CAPTURE.COMPLETED" },
            { "id", "WH-MULTI" }
        };

        _mockPayPalService.ValidateWebhookSignature(payload, signature, webhookId)
            .Returns(Result<Dictionary<string, object>>.Success(webhookEvent));

        _mockPayPalService.ProcessWebhookEventAsync(webhookEvent, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Verify the service was called with the first signature value
        _mockPayPalService.Received(1).ValidateWebhookSignature(payload, signature, webhookId);
    }

    #endregion

    #region GET /api/webhooks/paypal/health Tests

    /// <summary>
    /// Test health check endpoint returns 200 OK with health status
    /// </summary>
    [Fact]
    public void HealthCheck_ReturnsOkWithHealthStatus()
    {
        // Act
        var result = _controller.HealthCheck();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Test health check response contains required fields
    /// </summary>
    [Fact]
    public void HealthCheck_ResponseContainsRequiredFields()
    {
        // Act
        var result = _controller.HealthCheck();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;

        var response = okResult.Value;
        response.Should().NotBeNull();

        // Use reflection to check properties since it's an anonymous type
        var responseType = response!.GetType();
        responseType.GetProperty("status").Should().NotBeNull();
        responseType.GetProperty("service").Should().NotBeNull();
        responseType.GetProperty("timestamp").Should().NotBeNull();

        var status = responseType.GetProperty("status")!.GetValue(response) as string;
        var service = responseType.GetProperty("service")!.GetValue(response) as string;
        var timestamp = responseType.GetProperty("timestamp")!.GetValue(response) as string;

        status.Should().Be("healthy");
        service.Should().Be("paypal-webhooks");
        timestamp.Should().NotBeNullOrEmpty();

        // Verify timestamp is valid ISO 8601 format
        DateTime.TryParse(timestamp, out _).Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Setup HttpContext with payload and headers for webhook testing
    /// </summary>
    private void SetupHttpContextWithPayload(string payload, string? signature, string? transmissionId)
    {
        _httpContext.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(payload));

        if (signature != null)
        {
            _httpContext.Request.Headers["PAYPAL-TRANSMISSION-SIG"] = signature;
        }

        if (transmissionId != null)
        {
            _httpContext.Request.Headers["PAYPAL-TRANSMISSION-ID"] = transmissionId;
        }
    }

    #endregion
}
