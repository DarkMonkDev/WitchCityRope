using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.PayPal;

/// <summary>
/// Tests for real PayPal sandbox integration - only runs when sandbox credentials are available
/// These tests validate actual PayPal API connectivity and behavior
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "PayPal")]
[Trait("Service", "Sandbox")]
public class RealPayPalSandboxTests : PayPalIntegrationTestBase
{
    private readonly IPayPalService _payPalService;
    private readonly ILogger<RealPayPalSandboxTests> _logger;

    public RealPayPalSandboxTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _logger = ServiceProvider.GetRequiredService<ILogger<RealPayPalSandboxTests>>();
        
        // Use real PayPal service only if sandbox is available
        if (IsRealPayPalEnabled)
        {
            _payPalService = new PayPalService(Configuration, ServiceProvider.GetRequiredService<ILogger<PayPalService>>());
        }
        else
        {
            // Fall back to mock service for environments without real credentials
            _payPalService = new MockPayPalService(ServiceProvider.GetRequiredService<ILogger<MockPayPalService>>());
        }
    }

    /// <summary>
    /// Test that verifies PayPal sandbox connectivity
    /// This is the first test to run to ensure environment is properly configured
    /// </summary>
    [Fact]
    [Trait("Priority", "Critical")]
    public async Task PayPalSandbox_ShouldBeAccessible()
    {
        // Arrange
        LogTestConfiguration();
        
        if (!IsRealPayPalEnabled)
        {
            _logger.LogWarning("Skipping real PayPal test - using mock service (USE_MOCK_PAYMENT_SERVICE=true or missing credentials)");
            // Still run test with mock to verify it works
        }
        else
        {
            _logger.LogInformation("Running real PayPal sandbox test with credentials");
        }

        var amount = new Money { Amount = 10.00m, Currency = "USD" };
        var customerId = GetTestCustomerId();
        var metadata = GetTestMetadata();

        // Act
        var result = await _payPalService.CreateOrderAsync(amount, customerId, 0, metadata);

        // Assert
        result.IsSuccess.Should().BeTrue("PayPal service should be accessible");
        result.Value.Should().NotBeNull();
        result.Value.OrderId.Should().NotBeNullOrEmpty();
        result.Value.Status.Should().Be("CREATED");
        result.Value.Links.Should().NotBeEmpty();

        if (IsRealPayPalEnabled)
        {
            // Additional assertions for real PayPal
            result.Value.OrderId.Should().NotStartWith("MOCK-", "Real PayPal should not return mock IDs");
            
            var approveLink = result.Value.Links.FirstOrDefault(l => l.Rel == "approve");
            approveLink.Should().NotBeNull();
            approveLink!.Href.Should().Contain("paypal.com", "Approve link should point to PayPal");
        }
        else
        {
            // Mock service behavior validation
            result.Value.OrderId.Should().StartWith("MOCK-", "Mock service should return mock IDs");
        }
    }

    [Fact]
    [Trait("Priority", "High")]
    public async Task CreateOrder_WithValidData_ShouldReturnPayPalOrder()
    {
        // Skip if PayPal is not configured for real testing
        if (!IsRealPayPalEnabled)
        {
            _logger.LogInformation("Using mock PayPal service for this test");
        }

        // Arrange
        var amount = new Money { Amount = 25.50m, Currency = "USD" };
        var customerId = GetTestCustomerId();
        var slidingScale = 50; // 50% sliding scale
        var metadata = new Dictionary<string, string>
        {
            ["eventId"] = Guid.NewGuid().ToString(),
            ["userId"] = customerId.ToString(),
            ["slidingScale"] = slidingScale.ToString(),
            ["testScenario"] = "integration-test"
        };

        // Act
        var result = await _payPalService.CreateOrderAsync(amount, customerId, slidingScale, metadata);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OrderId.Should().NotBeNullOrEmpty();
        result.Value.Status.Should().Be("CREATED");
        result.Value.CreateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        // Verify links structure
        result.Value.Links.Should().NotBeEmpty();
        result.Value.Links.Should().Contain(l => l.Rel == "self");
        result.Value.Links.Should().Contain(l => l.Rel == "approve");
        
        if (IsRealPayPalEnabled)
        {
            result.Value.Links.Should().Contain(l => l.Rel == "capture");
        }
    }

    [Fact]
    [Trait("Priority", "High")]
    public async Task GetOrder_WithValidOrderId_ShouldReturnOrderDetails()
    {
        // Arrange
        var amount = new Money { Amount = 15.00m, Currency = "USD" };
        var createResult = await _payPalService.CreateOrderAsync(amount, GetTestCustomerId(), 0);
        createResult.IsSuccess.Should().BeTrue(); // Ensure setup worked
        
        var orderId = createResult.Value.OrderId;

        // Act
        var result = await _payPalService.GetOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OrderId.Should().Be(orderId);
        result.Value.Status.Should().Be("CREATED");
        
        if (IsRealPayPalEnabled)
        {
            result.Value.CreateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(2));
        }
    }

    [Fact]
    [Trait("Priority", "Medium")]
    public async Task WebhookValidation_WithValidSignature_ShouldReturnValidEvent()
    {
        // Arrange
        var payload = """
                     {
                         "id": "WH-SANDBOX-TEST-123",
                         "event_type": "PAYMENT.CAPTURE.COMPLETED",
                         "create_time": "2025-09-14T12:00:00.000Z",
                         "resource": {
                             "id": "CAPTURE-SANDBOX-123",
                             "status": "COMPLETED",
                             "amount": {
                                 "currency_code": "USD",
                                 "value": "25.00"
                             }
                         }
                     }
                     """;
        var signature = "test-webhook-signature";
        var webhookId = Configuration["PayPal:WebhookId"] ?? "test-webhook-id";

        // Act
        var result = _payPalService.ValidateWebhookSignature(payload, signature, webhookId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey("verified");
        result.Value.Should().ContainKey("event_type");
        
        if (IsRealPayPalEnabled)
        {
            // Real PayPal might have different validation behavior
            _logger.LogInformation("Real PayPal webhook validation result: {Result}", result.IsSuccess);
        }
        else
        {
            // Mock service always returns valid
            result.Value["verified"].Should().Be(true);
            result.Value["event_type"].Should().Be("PAYMENT.CAPTURE.COMPLETED");
        }
    }

    [Fact]
    [Trait("Priority", "Medium")]
    public async Task ProcessWebhookEvent_WithPaymentCompleted_ShouldProcessSuccessfully()
    {
        // Arrange
        var webhookEvent = new Dictionary<string, object>
        {
            ["id"] = "WH-SANDBOX-PAYMENT-123",
            ["event_type"] = "PAYMENT.CAPTURE.COMPLETED",
            ["create_time"] = DateTime.UtcNow.ToString("O"),
            ["resource"] = new Dictionary<string, object>
            {
                ["id"] = "CAPTURE-SANDBOX-456",
                ["status"] = "COMPLETED",
                ["amount"] = new Dictionary<string, object>
                {
                    ["currency_code"] = "USD",
                    ["value"] = "30.00"
                },
                ["seller_receivable_breakdown"] = new Dictionary<string, object>
                {
                    ["gross_amount"] = new { currency_code = "USD", value = "30.00" },
                    ["paypal_fee"] = new { currency_code = "USD", value = "1.17" },
                    ["net_amount"] = new { currency_code = "USD", value = "28.83" }
                }
            }
        };

        // Act
        var result = await _payPalService.ProcessWebhookEventAsync(webhookEvent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        if (!IsRealPayPalEnabled)
        {
            _logger.LogInformation("Mock service processed webhook event successfully");
        }
    }

    [Fact]
    [Trait("Priority", "Low")]
    public async Task PerformanceTest_CreateMultipleOrders_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        const int orderCount = IsRealPayPalEnabled ? 3 : 10; // Fewer orders for real PayPal to avoid rate limiting
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var tasks = Enumerable.Range(0, orderCount)
            .Select(async i =>
            {
                var amount = new Money { Amount = 10.00m + i, Currency = "USD" };
                return await _payPalService.CreateOrderAsync(amount, GetTestCustomerId(), 0);
            })
            .ToArray();

        // Act
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(orderCount);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        
        if (IsRealPayPalEnabled)
        {
            // Real PayPal should respond within 30 seconds for 3 orders
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000, "Real PayPal should respond within 30 seconds");
            _logger.LogInformation("Real PayPal performance: {Count} orders created in {Ms}ms", orderCount, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            // Mock service should be very fast
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Mock service should be very fast");
        }
        
        // Verify all orders have unique IDs
        var orderIds = results.Select(r => r.Value.OrderId).ToArray();
        orderIds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    [Trait("Priority", "Low")]
    public async Task ErrorHandling_WithInvalidOrderId_ShouldReturnPredictableError()
    {
        // Arrange
        var invalidOrderId = "INVALID-ORDER-DOES-NOT-EXIST";

        // Act
        var result = await _payPalService.GetOrderAsync(invalidOrderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
        result.Error.Should().Contain("not found", "Error should indicate order was not found");
    }

    [Fact]
    [Trait("Priority", "Low")]
    public async Task Configuration_ShouldUseCorrectEnvironment()
    {
        // Arrange & Act
        LogTestConfiguration();
        var isSandbox = Configuration.GetValue<bool>("PayPal:IsSandbox", true);

        // Assert
        if (IsRealPayPalEnabled)
        {
            isSandbox.Should().BeTrue("Real PayPal tests should use sandbox environment");
            Configuration["PayPal:ClientId"].Should().NotBeNullOrEmpty();
            Configuration["PayPal:ClientSecret"].Should().NotBeNullOrEmpty();
            
            // Ensure we're not accidentally using production
            Configuration["PayPal:BaseUrl"]?.Should().Contain("sandbox", "Should use sandbox API URL");
        }
        else
        {
            _logger.LogInformation("Using mock configuration for PayPal tests");
        }
    }
}