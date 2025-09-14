using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;

namespace WitchCityRope.Api.Tests.Services;

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
    public async Task CreateOrderAsync_ShouldReturnMockOrder()
    {
        // Arrange
        var amount = new Money { Amount = 50.00m, Currency = "USD" };
        var customerId = Guid.NewGuid();
        var slidingScale = 25;

        // Act
        var result = await _service.CreateOrderAsync(amount, customerId, slidingScale);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OrderId.Should().StartWith("MOCK-ORDER-");
        result.Value.Status.Should().Be("CREATED");
        result.Value.Links.Should().HaveCount(3);
        result.Value.GetApprovalUrl().Should().Contain("/mock/paypal/approve");
    }

    [Fact]
    public async Task CaptureOrderAsync_WithValidOrder_ShouldReturnSuccess()
    {
        // Arrange
        var amount = new Money { Amount = 50.00m, Currency = "USD" };
        var createResult = await _service.CreateOrderAsync(amount, Guid.NewGuid(), 0);
        var orderId = createResult.Value.OrderId;

        // Act
        var captureResult = await _service.CaptureOrderAsync(orderId);

        // Assert
        captureResult.IsSuccess.Should().BeTrue();
        captureResult.Value.Should().NotBeNull();
        captureResult.Value.CaptureId.Should().StartWith("MOCK-CAPTURE-");
        captureResult.Value.Status.Should().Be("COMPLETED");
        captureResult.Value.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task CaptureOrderAsync_WithInvalidOrder_ShouldReturnFailure()
    {
        // Arrange
        var invalidOrderId = "INVALID-ORDER-123";

        // Act
        var result = await _service.CaptureOrderAsync(invalidOrderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void ValidateWebhookSignature_ShouldAlwaysReturnValid()
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
    public async Task ProcessWebhookEventAsync_ShouldAlwaysReturnSuccess()
    {
        // Arrange
        var webhookEvent = new Dictionary<string, object>
        {
            ["event_type"] = "PAYMENT.CAPTURE.COMPLETED",
            ["resource"] = new { id = "TEST-123" }
        };

        // Act
        var result = await _service.ProcessWebhookEventAsync(webhookEvent);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RefundCaptureAsync_WithValidCapture_ShouldReturnSuccess()
    {
        // Arrange
        var amount = new Money { Amount = 50.00m, Currency = "USD" };
        var createResult = await _service.CreateOrderAsync(amount, Guid.NewGuid(), 0);
        var captureResult = await _service.CaptureOrderAsync(createResult.Value.OrderId);
        var captureId = captureResult.Value.CaptureId;
        var refundAmount = new Money { Amount = 25.00m, Currency = "USD" };

        // Act
        var refundResult = await _service.RefundCaptureAsync(
            captureId, 
            refundAmount, 
            "Customer request",
            "Your refund is being processed");

        // Assert
        refundResult.IsSuccess.Should().BeTrue();
        refundResult.Value.Should().NotBeNull();
        refundResult.Value.RefundId.Should().StartWith("MOCK-REFUND-");
        refundResult.Value.Status.Should().Be("COMPLETED");
        refundResult.Value.CaptureId.Should().Be(captureId);
        refundResult.Value.Amount.Should().Be(refundAmount);
    }

    [Fact]
    public async Task GetOrderAsync_WithExistingOrder_ShouldReturnOrder()
    {
        // Arrange
        var amount = new Money { Amount = 50.00m, Currency = "USD" };
        var createResult = await _service.CreateOrderAsync(amount, Guid.NewGuid(), 0);
        var orderId = createResult.Value.OrderId;

        // Act
        var getResult = await _service.GetOrderAsync(orderId);

        // Assert
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().NotBeNull();
        getResult.Value.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task GetRefundAsync_WithExistingRefund_ShouldReturnRefund()
    {
        // Arrange
        var amount = new Money { Amount = 50.00m, Currency = "USD" };
        var createResult = await _service.CreateOrderAsync(amount, Guid.NewGuid(), 0);
        var captureResult = await _service.CaptureOrderAsync(createResult.Value.OrderId);
        var refundResult = await _service.RefundCaptureAsync(
            captureResult.Value.CaptureId,
            amount,
            "Test refund");
        var refundId = refundResult.Value.RefundId;

        // Act
        var getResult = await _service.GetRefundAsync(refundId);

        // Assert
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().NotBeNull();
        getResult.Value.RefundId.Should().Be(refundId);
    }
}