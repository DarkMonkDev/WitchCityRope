using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.PayPal;

/// <summary>
/// Integration tests specifically designed for CI/CD pipeline validation
/// These tests ensure PayPal functionality works correctly in automated environments
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "CI/CD")]
[Trait("Priority", "Critical")]
public class PayPalCiCdIntegrationTests : PayPalIntegrationTestBase
{
    private readonly ILogger<PayPalCiCdIntegrationTests> _logger;

    public PayPalCiCdIntegrationTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _logger = ServiceProvider.GetRequiredService<ILogger<PayPalCiCdIntegrationTests>>();
    }

    [Fact]
    [Trait("Stage", "Health")]
    public void CiCdEnvironment_ShouldBeConfiguredCorrectly()
    {
        // Act
        PayPalTestHelpers.EnvironmentHelpers.LogEnvironmentInfo(_logger);
        
        // Assert CI/CD specific configuration
        if (PayPalTestHelpers.EnvironmentHelpers.IsCiEnvironment)
        {
            Configuration.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE", false)
                .Should().BeTrue("CI environment should use mock service");
            
            var logLevel = Configuration.GetValue<string>("LOG_LEVEL", "Information");
            logLevel.Should().BeOneOf("Warning", "Error", "Information", "Debug", 
                "CI environment should have reasonable log level");
        }

        // Common assertions for all environments
        Configuration["PayPal:ClientId"].Should().NotBeNullOrEmpty();
        Configuration["PayPal:ClientSecret"].Should().NotBeNullOrEmpty();
        Configuration["PayPal:WebhookId"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Stage", "ServiceCreation")]
    public void ServiceCreation_ShouldWorkInCiCdEnvironment()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Configuration);
        services.AddLogging();

        // Act - Simulate service registration (matches Program.cs)
        if (Configuration.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE", false))
        {
            services.AddScoped<IPayPalService, MockPayPalService>();
        }
        else
        {
            services.AddScoped<IPayPalService, PayPalService>();
        }

        using var serviceProvider = services.BuildServiceProvider();
        var payPalService = serviceProvider.GetRequiredService<IPayPalService>();

        // Assert
        payPalService.Should().NotBeNull();
        
        if (PayPalTestHelpers.EnvironmentHelpers.ShouldUseMockService)
        {
            payPalService.Should().BeOfType<MockPayPalService>("CI environment should use mock service");
        }
        
        _logger.LogInformation("Service creation successful: {ServiceType}", payPalService.GetType().Name);
    }

    [Fact]
    [Trait("Stage", "BasicFunctionality")]
    public async Task BasicPayPalOperations_ShouldWorkInCiCd()
    {
        // Arrange
        var service = Configuration.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE", false) 
            ? new MockPayPalService(ServiceProvider.GetRequiredService<ILogger<MockPayPalService>>())
            : new PayPalService(Configuration, ServiceProvider.GetRequiredService<ILogger<PayPalService>>()) as IPayPalService;

        var amount = PayPalTestHelpers.TestAmounts.Medium;
        var customerId = PayPalTestHelpers.TestCustomers.GetRandomCustomer();
        var metadata = PayPalTestHelpers.TestMetadata.CreateEventMetadata(Guid.NewGuid(), customerId);

        // Act & Assert - Create Order
        var createResult = await service.CreateOrderAsync(amount, customerId, 0, metadata);
        createResult.IsSuccess.Should().BeTrue("Order creation should work in CI/CD");
        createResult.Value.Should().NotBeNull();
        PayPalTestHelpers.ValidationHelpers.IsValidOrderId(createResult.Value.OrderId).Should().BeTrue();

        // Act & Assert - Get Order
        var getResult = await service.GetOrderAsync(createResult.Value.OrderId);
        getResult.IsSuccess.Should().BeTrue("Order retrieval should work in CI/CD");

        // Act & Assert - Capture Order
        var captureResult = await service.CaptureOrderAsync(createResult.Value.OrderId);
        captureResult.IsSuccess.Should().BeTrue("Order capture should work in CI/CD");
        PayPalTestHelpers.ValidationHelpers.IsValidCaptureId(captureResult.Value.CaptureId).Should().BeTrue();

        _logger.LogInformation("Basic PayPal operations completed successfully in CI/CD environment");
    }

    [Fact]
    [Trait("Stage", "WebhookProcessing")]
    public async Task WebhookProcessing_ShouldWorkInCiCd()
    {
        // Arrange
        var service = Configuration.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE", false)
            ? new MockPayPalService(ServiceProvider.GetRequiredService<ILogger<MockPayPalService>>())
            : new PayPalService(Configuration, ServiceProvider.GetRequiredService<ILogger<PayPalService>>()) as IPayPalService;

        var webhookEvent = PayPalTestHelpers.WebhookTestData.CreatePaymentCompletedEvent("CAPTURE-CI-TEST", 25.00m);
        
        // Act
        var result = await service.ProcessWebhookEventAsync(webhookEvent);

        // Assert
        result.IsSuccess.Should().BeTrue("Webhook processing should work in CI/CD");
        
        _logger.LogInformation("Webhook processing completed successfully in CI/CD environment");
    }

    [Fact]
    [Trait("Stage", "Performance")]
    public async Task Performance_ShouldMeetCiCdRequirements()
    {
        // Arrange
        var service = Configuration.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE", false)
            ? new MockPayPalService(ServiceProvider.GetRequiredService<ILogger<MockPayPalService>>())
            : new PayPalService(Configuration, ServiceProvider.GetRequiredService<ILogger<PayPalService>>()) as IPayPalService;

        var operationsCount = PayPalTestHelpers.EnvironmentHelpers.ShouldUseMockService ? 20 : 5;
        var maxDurationSeconds = PayPalTestHelpers.EnvironmentHelpers.ShouldUseMockService ? 2 : 30;

        // Act
        var (_, duration) = await PayPalTestHelpers.PerformanceHelpers.MeasureAsync(async () =>
        {
            var tasks = Enumerable.Range(0, operationsCount)
                .Select(async i =>
                {
                    var amount = new Api.Features.Payments.ValueObjects.Money 
                    { 
                        Amount = 10.00m + i, 
                        Currency = "USD" 
                    };
                    return await service.CreateOrderAsync(amount, Guid.NewGuid(), 0);
                });

            var results = await Task.WhenAll(tasks);
            results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        });

        // Assert
        duration.Should().BeLessThan(TimeSpan.FromSeconds(maxDurationSeconds),
            $"CI/CD performance: {operationsCount} operations should complete within {maxDurationSeconds}s");

        _logger.LogInformation("CI/CD Performance test: {Count} operations in {Duration:F2}s", 
            operationsCount, duration.TotalSeconds);
    }

    [Fact]
    [Trait("Stage", "Reliability")]
    public async Task ReliabilityTest_ShouldHandleRepeatedOperations()
    {
        // Arrange
        var service = Configuration.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE", false)
            ? new MockPayPalService(ServiceProvider.GetRequiredService<ILogger<MockPayPalService>>())
            : new PayPalService(Configuration, ServiceProvider.GetRequiredService<ILogger<PayPalService>>()) as IPayPalService;

        const int iterations = 5;
        var allResults = new List<bool>();

        // Act - Run same operation multiple times
        for (int i = 0; i < iterations; i++)
        {
            var result = await PayPalTestHelpers.RetryHelpers.WithRetryAsync(async () =>
            {
                var amount = PayPalTestHelpers.TestAmounts.Small;
                var customerId = PayPalTestHelpers.TestCustomers.GetRandomCustomer();
                return await service.CreateOrderAsync(amount, customerId, 0);
            }, maxAttempts: 3);

            allResults.Add(result.IsSuccess);
        }

        // Assert
        allResults.Should().AllSatisfy(success => success.Should().BeTrue(),
            "All repeated operations should succeed in CI/CD environment");

        _logger.LogInformation("Reliability test: {Count}/{Total} operations succeeded",
            allResults.Count(x => x), allResults.Count);
    }

    [Fact]
    [Trait("Stage", "Configuration")]
    public void ConfigurationValidation_ForCiCd()
    {
        // Arrange & Act
        var requiredKeys = new[]
        {
            "USE_MOCK_PAYMENT_SERVICE",
            "PayPal:ClientId",
            "PayPal:ClientSecret",
            "PayPal:WebhookId",
            "PayPal:IsSandbox"
        };

        var missingKeys = new List<string>();
        var configuredKeys = new List<string>();

        foreach (var key in requiredKeys)
        {
            var value = Configuration[key];
            if (string.IsNullOrEmpty(value))
            {
                missingKeys.Add(key);
            }
            else
            {
                configuredKeys.Add(key);
            }
        }

        // Assert
        configuredKeys.Should().NotBeEmpty("At least some configuration keys should be present");

        if (PayPalTestHelpers.EnvironmentHelpers.IsCiEnvironment)
        {
            // In CI, we need basic config even for mock service
            missingKeys.Should().NotContain("USE_MOCK_PAYMENT_SERVICE", 
                "CI environment must explicitly set USE_MOCK_PAYMENT_SERVICE");
        }

        _logger.LogInformation("Configuration validation - Configured: {Configured}, Missing: {Missing}",
            string.Join(", ", configuredKeys), string.Join(", ", missingKeys));
    }

    [Theory]
    [InlineData(true, "MockPayPalService")]
    [InlineData(false, "PayPalService")]
    [Trait("Stage", "ServiceSelection")]
    public void ServiceSelection_ShouldRespectConfiguration(bool useMockService, string expectedServiceName)
    {
        // Arrange
        var testConfig = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["USE_MOCK_PAYMENT_SERVICE"] = useMockService.ToString().ToLower(),
                ["PayPal:ClientId"] = "test-client",
                ["PayPal:ClientSecret"] = "test-secret",
                ["PayPal:WebhookId"] = "test-webhook"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(testConfig);
        services.AddLogging();

        // Act
        if (testConfig.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE", false))
        {
            services.AddScoped<IPayPalService, MockPayPalService>();
        }
        else
        {
            services.AddScoped<IPayPalService, PayPalService>();
        }

        using var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IPayPalService>();

        // Assert
        service.GetType().Name.Should().Be(expectedServiceName,
            $"USE_MOCK_PAYMENT_SERVICE={useMockService} should result in {expectedServiceName}");

        _logger.LogInformation("Service selection test: {UseMock} → {ServiceType}",
            useMockService, service.GetType().Name);
    }

    [Fact]
    [Trait("Stage", "ErrorHandling")]
    public async Task ErrorHandling_ShouldBeRobustInCiCd()
    {
        // Arrange
        var service = Configuration.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE", false)
            ? new MockPayPalService(ServiceProvider.GetRequiredService<ILogger<MockPayPalService>>())
            : new PayPalService(Configuration, ServiceProvider.GetRequiredService<ILogger<PayPalService>>()) as IPayPalService;

        // Act & Assert - Invalid operations should return predictable errors
        var invalidOrderResult = await service.GetOrderAsync("INVALID-ORDER-ID-FOR-CI-TEST");
        invalidOrderResult.IsSuccess.Should().BeFalse("Invalid order lookup should fail gracefully");
        invalidOrderResult.Error.Should().NotBeNullOrEmpty("Error message should be provided");

        var invalidCaptureResult = await service.CaptureOrderAsync("INVALID-ORDER-ID-FOR-CI-TEST");
        invalidCaptureResult.IsSuccess.Should().BeFalse("Invalid capture should fail gracefully");
        invalidCaptureResult.Error.Should().NotBeNullOrEmpty("Error message should be provided");

        _logger.LogInformation("Error handling validation completed - all errors handled gracefully");
    }
}