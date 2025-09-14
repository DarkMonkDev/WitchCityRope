using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Unit tests for MockPayPalService focusing on the fixed compilation issues
/// Tests the mock service behavior without external dependencies
/// </summary>
public class MockPayPalServiceTests
{
    private readonly MockPayPalService _service;
    private readonly Mock<ILogger<MockPayPalService>> _loggerMock;

    public MockPayPalServiceTests()
    {
        _loggerMock = new Mock<ILogger<MockPayPalService>>();
        _service = new MockPayPalService(_loggerMock.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidData_ReturnsSuccessfulOrder()
    {
        // Arrange
        var amount = Money.Create(50.00m, "USD");
        var customerId = Guid.NewGuid();
        var slidingScalePercentage = 20;
        var metadata = new Dictionary<string, string> { ["test"] = "data" };

        // Act
        var result = await _service.CreateOrderAsync(
            amount, 
            customerId, 
            slidingScalePercentage, 
            metadata);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OrderId.Should().StartWith("MOCK-ORDER-");
        result.Value.Status.Should().Be("CREATED");
        result.Value.Links.Should().HaveCount(3);
        result.Value.CreateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CaptureOrderAsync_WithExistingOrder_ReturnsSuccessfulCapture()
    {
        // Arrange - First create an order
        var amount = Money.Create(75.50m, "USD");
        var customerId = Guid.NewGuid();
        var createResult = await _service.CreateOrderAsync(amount, customerId, 0);
        var orderId = createResult.Value!.OrderId;

        // Act
        var captureResult = await _service.CaptureOrderAsync(orderId);

        // Assert
        captureResult.IsSuccess.Should().BeTrue();
        captureResult.Value.Should().NotBeNull();
        captureResult.Value!.CaptureId.Should().StartWith("MOCK-CAPTURE-");
        captureResult.Value.OrderId.Should().Be(orderId);
        captureResult.Value.Status.Should().Be("COMPLETED");
        captureResult.Value.Amount.Should().NotBeNull();
        captureResult.Value.Amount.Value.Should().Be("50.00"); // Mock service uses hardcoded amount
        captureResult.Value.Amount.CurrencyCode.Should().Be("USD");
        captureResult.Value.PayerEmail.Should().Be("test@example.com");
        captureResult.Value.PayerName.Should().Be("Test User");
    }

    [Fact]
    public async Task CaptureOrderAsync_WithNonexistentOrder_ReturnsFailure()
    {
        // Arrange
        var nonexistentOrderId = "FAKE-ORDER-ID";

        // Act
        var result = await _service.CaptureOrderAsync(nonexistentOrderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Order not found in mock service");
    }

    [Fact]
    public async Task GetOrderAsync_WithExistingOrder_ReturnsOrder()
    {
        // Arrange - Create an order first
        var amount = Money.Create(100.00m, "USD");
        var customerId = Guid.NewGuid();
        var createResult = await _service.CreateOrderAsync(amount, customerId, 25);
        var orderId = createResult.Value!.OrderId;

        // Act
        var getResult = await _service.GetOrderAsync(orderId);

        // Assert
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().NotBeNull();
        getResult.Value!.OrderId.Should().Be(orderId);
        getResult.Value.Status.Should().Be("CREATED");
    }

    [Fact]
    public async Task RefundCaptureAsync_WithValidCapture_ReturnsSuccessfulRefund()
    {
        // Arrange - Create order and capture it
        var amount = Money.Create(200.00m, "USD");
        var customerId = Guid.NewGuid();
        var createResult = await _service.CreateOrderAsync(amount, customerId, 0);
        var captureResult = await _service.CaptureOrderAsync(createResult.Value!.OrderId);
        var captureId = captureResult.Value!.CaptureId;

        var refundAmount = Money.Create(100.00m, "USD");
        var reason = "Customer request";
        var noteToPayer = "Partial refund processed";

        // Act
        var refundResult = await _service.RefundCaptureAsync(
            captureId, 
            refundAmount, 
            reason, 
            noteToPayer);

        // Assert
        refundResult.IsSuccess.Should().BeTrue();
        refundResult.Value.Should().NotBeNull();
        refundResult.Value!.RefundId.Should().StartWith("MOCK-REFUND-");
        refundResult.Value.CaptureId.Should().Be(captureId);
        refundResult.Value.Status.Should().Be("COMPLETED");
        refundResult.Value.Amount.Value.Should().Be("100.00");
        refundResult.Value.Amount.CurrencyCode.Should().Be("USD");
        refundResult.Value.Reason.Should().Be(reason);
        refundResult.Value.NoteToPayer.Should().Be(noteToPayer);
    }

    [Fact]
    public void ValidateWebhookSignature_AlwaysReturnsValid()
    {
        // Arrange
        var payload = "test-payload";
        var signature = "test-signature";
        var webhookId = "test-webhook-id";

        // Act
        var result = _service.ValidateWebhookSignature(payload, signature, webhookId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey("verified");
        result.Value["verified"].Should().Be(true);
        result.Value.Should().ContainKey("event_type");
        result.Value["event_type"].Should().Be("PAYMENT.CAPTURE.COMPLETED");
    }

    [Fact]
    public async Task ProcessWebhookEventAsync_AlwaysReturnsSuccess()
    {
        // Arrange
        var webhookEvent = new Dictionary<string, object>
        {
            ["event_type"] = "PAYMENT.CAPTURE.COMPLETED",
            ["resource"] = new Dictionary<string, object>
            {
                ["id"] = "test-capture-id"
            }
        };

        // Act
        var result = await _service.ProcessWebhookEventAsync(webhookEvent);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void MockService_LogsWarningOnConstruction()
    {
        // Arrange & Act - Constructor was already called in setup

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MOCK PayPal Service Active")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}