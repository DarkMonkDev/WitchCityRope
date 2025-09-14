using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WitchCityRope.Tests.Common.Fixtures;

namespace WitchCityRope.Infrastructure.Tests.PayPal;

/// <summary>
/// Base class for PayPal integration tests with configuration management
/// Supports both mock and real PayPal sandbox environments
/// </summary>
[Collection("Database")]
public abstract class PayPalIntegrationTestBase : IntegrationTestBase
{
    protected IConfiguration Configuration { get; private set; } = null!;
    protected IServiceCollection Services { get; private set; } = null!;
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    
    /// <summary>
    /// Indicates if we're using real PayPal sandbox (vs mock service)
    /// </summary>
    protected bool IsRealPayPalEnabled => 
        Configuration.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE") == false &&
        !string.IsNullOrEmpty(Configuration["PayPal:ClientId"]) &&
        !string.IsNullOrEmpty(Configuration["PayPal:ClientSecret"]);

    protected PayPalIntegrationTestBase(DatabaseTestFixture fixture) : base(fixture)
    {
        InitializeServices();
    }

    private void InitializeServices()
    {
        // Build configuration with test values
        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Default mock configuration
                ["USE_MOCK_PAYMENT_SERVICE"] = "true",
                ["PayPal:ClientId"] = "test-client-id",
                ["PayPal:ClientSecret"] = "test-client-secret", 
                ["PayPal:IsSandbox"] = "true",
                ["PayPal:WebhookId"] = "test-webhook-id",
                ["PayPal:BaseUrl"] = "https://api.sandbox.paypal.com",
                
                // Override with environment variables if available
                ["PayPal:ClientId"] = Environment.GetEnvironmentVariable("PAYPAL_CLIENT_ID") ?? "test-client-id",
                ["PayPal:ClientSecret"] = Environment.GetEnvironmentVariable("PAYPAL_CLIENT_SECRET") ?? "test-client-secret",
                ["USE_MOCK_PAYMENT_SERVICE"] = Environment.GetEnvironmentVariable("USE_MOCK_PAYMENT_SERVICE") ?? "true"
            })
            .AddEnvironmentVariables()
            .Build();

        // Setup service collection for DI
        Services = new ServiceCollection();
        Services.AddSingleton(Configuration);
        Services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        ServiceProvider = Services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets a unique customer ID for testing
    /// </summary>
    protected static Guid GetTestCustomerId() => Guid.NewGuid();

    /// <summary>
    /// Gets test metadata for payments
    /// </summary>
    protected static Dictionary<string, string> GetTestMetadata() => new()
    {
        ["eventId"] = Guid.NewGuid().ToString(),
        ["userId"] = Guid.NewGuid().ToString(),
        ["slidingScale"] = "25"
    };

    /// <summary>
    /// Logs test configuration details for debugging
    /// </summary>
    protected void LogTestConfiguration()
    {
        var logger = ServiceProvider.GetRequiredService<ILogger<PayPalIntegrationTestBase>>();
        logger.LogInformation("PayPal Test Configuration:");
        logger.LogInformation("  Mock Service: {UseMock}", !IsRealPayPalEnabled);
        logger.LogInformation("  Sandbox Mode: {IsSandbox}", Configuration["PayPal:IsSandbox"]);
        logger.LogInformation("  Client ID: {ClientId}", Configuration["PayPal:ClientId"]?[..8] + "***");
        logger.LogInformation("  Webhook ID: {WebhookId}", Configuration["PayPal:WebhookId"]);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServiceProvider?.Dispose();
        }
        base.Dispose(disposing);
    }
}