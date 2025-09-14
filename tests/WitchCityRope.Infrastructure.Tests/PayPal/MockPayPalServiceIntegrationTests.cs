using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.PayPal;

/// <summary>
/// Tests for MockPayPalService integration - validates mock service functionality
/// These tests ensure the mock service provides predictable behavior for CI/CD
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "PayPal")]
[Trait("Service", "Mock")]
public class MockPayPalServiceIntegrationTests : PayPalIntegrationTestBase
{
    private readonly MockPayPalService _mockService;
    private readonly ILogger<MockPayPalService> _logger;

    public MockPayPalServiceIntegrationTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _logger = ServiceProvider.GetRequiredService<ILogger<MockPayPalService>>();
        _mockService = new MockPayPalService(_logger);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCreateValidMockOrder()
    {
        // Arrange
        LogTestConfiguration();
        var amount = new Money { Amount = 75.00m, Currency = "USD" };
        var customerId = GetTestCustomerId();
        var slidingScale = 25;
        var metadata = GetTestMetadata();

        // Act
        var result = await _mockService.CreateOrderAsync(amount, customerId, slidingScale, metadata);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OrderId.Should().StartWith("MOCK-ORDER-");
        result.Value.Status.Should().Be("CREATED");
        result.Value.CreateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Value.Links.Should().HaveCount(3);
        
        // Verify link structure
        var approveLink = result.Value.Links.FirstOrDefault(l => l.Rel == "approve");
        approveLink.Should().NotBeNull();
        approveLink!.Href.Should().Contain("/mock/paypal/approve");
        approveLink.Method.Should().Be("GET");
    }

    [Fact]
    public async Task CompleteOrderFlow_ShouldWorkEndToEnd()
    {
        // Arrange
        var amount = new Money { Amount = 50.00m, Currency = "USD" };
        var customerId = GetTestCustomerId();
        var metadata = GetTestMetadata();

        // Act 1: Create Order
        var createResult = await _mockService.CreateOrderAsync(amount, customerId, 0, metadata);
        
        // Assert 1: Order creation
        createResult.IsSuccess.Should().BeTrue();
        var orderId = createResult.Value.OrderId;

        // Act 2: Get Order
        var getResult = await _mockService.GetOrderAsync(orderId);
        
        // Assert 2: Order retrieval
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.OrderId.Should().Be(orderId);
        getResult.Value.Status.Should().Be("CREATED");

        // Act 3: Capture Order
        var captureResult = await _mockService.CaptureOrderAsync(orderId);
        
        // Assert 3: Order capture
        captureResult.IsSuccess.Should().BeTrue();
        captureResult.Value.Should().NotBeNull();
        captureResult.Value.OrderId.Should().Be(orderId);
        captureResult.Value.Status.Should().Be("COMPLETED");
        captureResult.Value.CaptureId.Should().StartWith("MOCK-CAPTURE-");

        // Act 4: Verify order status changed
        var updatedOrderResult = await _mockService.GetOrderAsync(orderId);
        
        // Assert 4: Order status updated
        updatedOrderResult.IsSuccess.Should().BeTrue();
        updatedOrderResult.Value.Status.Should().Be("COMPLETED");
    }

    [Fact]
    public async Task RefundFlow_ShouldWorkEndToEnd()
    {
        // Arrange - Create and capture order first
        var amount = new Money { Amount = 100.00m, Currency = "USD" };
        var customerId = GetTestCustomerId();
        
        var createResult = await _mockService.CreateOrderAsync(amount, customerId, 0);
        var captureResult = await _mockService.CaptureOrderAsync(createResult.Value.OrderId);
        var captureId = captureResult.Value.CaptureId;
        
        var refundAmount = new Money { Amount = 50.00m, Currency = "USD" };
        var reason = "Customer request";
        var noteToPlayer = "Your partial refund is being processed";

        // Act 1: Process Refund
        var refundResult = await _mockService.RefundCaptureAsync(captureId, refundAmount, reason, noteToPlayer);
        
        // Assert 1: Refund creation
        refundResult.IsSuccess.Should().BeTrue();
        refundResult.Value.Should().NotBeNull();
        refundResult.Value.RefundId.Should().StartWith("MOCK-REFUND-");
        refundResult.Value.CaptureId.Should().Be(captureId);
        refundResult.Value.Status.Should().Be("COMPLETED");
        refundResult.Value.Amount.Should().BeEquivalentTo(refundAmount);
        refundResult.Value.Reason.Should().Be(reason);
        refundResult.Value.NoteToPayer.Should().Be(noteToPlayer);

        // Act 2: Get Refund
        var getRefundResult = await _mockService.GetRefundAsync(refundResult.Value.RefundId);
        
        // Assert 2: Refund retrieval
        getRefundResult.IsSuccess.Should().BeTrue();
        getRefundResult.Value.RefundId.Should().Be(refundResult.Value.RefundId);
        getRefundResult.Value.Status.Should().Be("COMPLETED");
    }

    [Fact]
    public async Task WebhookValidation_ShouldAlwaysReturnValid()
    {
        // Arrange
        var payload = """
                     {
                         "id": "WH-TEST-123",
                         "event_type": "PAYMENT.CAPTURE.COMPLETED",
                         "resource": {
                             "id": "CAPTURE-123",
                             "status": "COMPLETED"
                         }
                     }
                     """;
        var signature = "mock-signature-from-paypal";
        var webhookId = "test-webhook-id";

        // Act
        var result = _mockService.ValidateWebhookSignature(payload, signature, webhookId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey("verified");
        result.Value["verified"].Should().Be(true);
        result.Value.Should().ContainKey("event_type");
        result.Value["event_type"].Should().Be("PAYMENT.CAPTURE.COMPLETED");
        result.Value.Should().ContainKey("resource");
        
        // Verify resource structure
        var resource = result.Value["resource"] as Dictionary<string, object>;
        resource.Should().NotBeNull();
        resource!.Should().ContainKey("id");
        resource["id"].Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessWebhookEvent_ShouldHandleAllEventTypes()
    {
        // Arrange
        var testEvents = new[]
        {
            new Dictionary<string, object> { ["event_type"] = "PAYMENT.CAPTURE.COMPLETED", ["resource"] = new { id = "CAPTURE-123" } },
            new Dictionary<string, object> { ["event_type"] = "PAYMENT.CAPTURE.REFUNDED", ["resource"] = new { id = "REFUND-123" } },
            new Dictionary<string, object> { ["event_type"] = "PAYMENT.CAPTURE.DENIED", ["resource"] = new { id = "CAPTURE-456" } },
            new Dictionary<string, object> { ["event_type"] = "UNKNOWN_EVENT_TYPE", ["resource"] = new { } }
        };

        foreach (var webhookEvent in testEvents)
        {
            // Act
            var result = await _mockService.ProcessWebhookEventAsync(webhookEvent);

            // Assert
            result.IsSuccess.Should().BeTrue($"Event type {webhookEvent["event_type"]} should be processed successfully");
        }
    }

    [Fact]
    public async Task ErrorScenarios_ShouldReturnPredictableFailures()
    {
        // Act & Assert: Invalid order operations
        var invalidOrderResult = await _mockService.GetOrderAsync("INVALID-ORDER-ID");
        invalidOrderResult.IsSuccess.Should().BeFalse();
        invalidOrderResult.Error.Should().Contain("not found");

        var invalidCaptureResult = await _mockService.CaptureOrderAsync("INVALID-ORDER-ID");
        invalidCaptureResult.IsSuccess.Should().BeFalse();
        invalidCaptureResult.Error.Should().Contain("not found");

        // Act & Assert: Invalid refund operations
        var invalidRefundResult = await _mockService.RefundCaptureAsync("INVALID-CAPTURE-ID", new Money { Amount = 10m, Currency = "USD" }, "test");
        invalidRefundResult.IsSuccess.Should().BeFalse();
        invalidRefundResult.Error.Should().Contain("not found");

        var getInvalidRefundResult = await _mockService.GetRefundAsync("INVALID-REFUND-ID");
        getInvalidRefundResult.IsSuccess.Should().BeFalse();
        getInvalidRefundResult.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task PerformanceTest_ShouldHandleMultipleOperations()
    {
        // Arrange
        const int operationCount = 10;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act: Create multiple orders in parallel
        var createTasks = Enumerable.Range(0, operationCount)
            .Select(async i => 
            {
                var amount = new Money { Amount = 50.00m + i, Currency = "USD" };
                return await _mockService.CreateOrderAsync(amount, GetTestCustomerId(), i * 10);
            })
            .ToArray();

        var results = await Task.WhenAll(createTasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(operationCount);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        
        // Performance assertion - mock service should be very fast
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Mock service should handle 10 operations in under 1 second");
        
        // Verify all orders have unique IDs
        var orderIds = results.Select(r => r.Value.OrderId).ToArray();
        orderIds.Should().OnlyHaveUniqueItems();
        orderIds.Should().AllSatisfy(id => id.Should().StartWith("MOCK-ORDER-"));
    }
}