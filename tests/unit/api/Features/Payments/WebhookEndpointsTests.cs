using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Payments.Models.PayPal;
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
using WitchCityRope.Api.Features.Webhooks.Services;
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
    private readonly IPayPalWebhookVerificationService _mockVerificationService;
    private readonly IPayPalWebhookProcessingService _mockProcessingService;
    private readonly ILogger<WebhookEndpoints> _mockLogger;
    private readonly IConfiguration _mockConfiguration;
    private readonly WebhookEndpoints _controller;
    private readonly DefaultHttpContext _httpContext;

    public WebhookEndpointsTests()
    {
        _mockPayPalService = Substitute.For<IPayPalService>();
        _mockVerificationService = Substitute.For<IPayPalWebhookVerificationService>();
        _mockProcessingService = Substitute.For<IPayPalWebhookProcessingService>();
        _mockLogger = Substitute.For<ILogger<WebhookEndpoints>>();
        _mockConfiguration = Substitute.For<IConfiguration>();

        _controller = new WebhookEndpoints(
            _mockPayPalService,
            _mockVerificationService,
            _mockProcessingService,
            _mockLogger);

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

        _mockVerificationService.VerifyWebhookSignatureAsync(
            Arg.Any<HttpRequest>(), payload, Arg.Any<CancellationToken>())
            .Returns(true);

        var webhookEvent = new PayPalWebhookEvent
        {
            EventType = "PAYMENT.CAPTURE.COMPLETED",
            Id = "WH-123"
        };

        _mockPayPalService.ValidateWebhookSignatureTyped(payload, signature, webhookId)
            .Returns(Result<PayPalWebhookEvent>.Success(webhookEvent));

        _mockProcessingService.ProcessEventAsync(webhookEvent, transmissionId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);
    }

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

    [Fact]
    public async Task HandlePayPalWebhook_WithInvalidSignature_ReturnsBadRequest()
    {
        // Arrange
        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\"}";
        var transmissionId = "transmission-123";

        SetupHttpContextWithPayload(payload, "invalid-signature", transmissionId);

        _mockVerificationService.VerifyWebhookSignatureAsync(
            Arg.Any<HttpRequest>(), payload, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var problemResult = (ObjectResult)result;
        problemResult.StatusCode.Should().Be(400);

        var problemDetails = problemResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Detail.Should().Contain("Invalid webhook signature");
    }

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

        _mockVerificationService.VerifyWebhookSignatureAsync(
            Arg.Any<HttpRequest>(), payload, Arg.Any<CancellationToken>())
            .Returns(true);

        var webhookEvent = new PayPalWebhookEvent
        {
            EventType = "PAYMENT.CAPTURE.COMPLETED",
            Id = "WH-123"
        };

        _mockPayPalService.ValidateWebhookSignatureTyped(payload, signature, webhookId)
            .Returns(Result<PayPalWebhookEvent>.Success(webhookEvent));

        _mockProcessingService.ProcessEventAsync(webhookEvent, transmissionId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Database error"));

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var problemResult = (ObjectResult)result;
        problemResult.StatusCode.Should().Be(500);

        var problemDetails = problemResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Detail.Should().Contain("Failed to process webhook event");
    }

    [Fact]
    public async Task HandlePayPalWebhook_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var payload = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\"}";
        var transmissionId = "transmission-123";

        SetupHttpContextWithPayload(payload, "valid-signature", transmissionId);

        _mockVerificationService.VerifyWebhookSignatureAsync(
            Arg.Any<HttpRequest>(), payload, Arg.Any<CancellationToken>())
            .Returns<bool>(x => throw new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _controller.HandlePayPalWebhook();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var problemResult = (ObjectResult)result;
        problemResult.StatusCode.Should().Be(500);

        var problemDetails = problemResult.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Detail.Should().Contain("Internal server error processing webhook");
    }

    #endregion

    #region GET /api/webhooks/paypal/health Tests

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
        DateTime.TryParse(timestamp, out _).Should().BeTrue();
    }

    #endregion

    #region Helper Methods

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
