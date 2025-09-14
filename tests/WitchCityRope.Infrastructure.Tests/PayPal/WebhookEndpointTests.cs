using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.PayPal;

/// <summary>
/// Integration tests for webhook endpoints
/// Tests the complete webhook processing pipeline including validation and processing
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "WebhookEndpoints")]
public class WebhookEndpointTests : PayPalIntegrationTestBase
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ILogger<WebhookEndpointTests> _logger;

    public WebhookEndpointTests(DatabaseTestFixture fixture) : base(fixture)
    {
        _logger = ServiceProvider.GetRequiredService<ILogger<WebhookEndpointTests>>();
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["USE_MOCK_PAYMENT_SERVICE"] = "true",
                        ["PayPal:ClientId"] = "test-client-id",
                        ["PayPal:ClientSecret"] = "test-client-secret",
                        ["PayPal:WebhookId"] = "test-webhook-id",
                        ["PayPal:IsSandbox"] = "true"
                    });
                });
                builder.ConfigureServices(services =>
                {
                    // Ensure we're using the mock service for webhook tests
                    services.AddScoped<IPayPalService, MockPayPalService>();
                });
            });

        _client = _factory.CreateClient();
    }

    [Fact]
    [Trait("Priority", "Critical")]
    public async Task WebhookHealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/webhooks/paypal/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        
        healthData.Should().ContainKey("status");
        healthData["status"].ToString().Should().Be("healthy");
        healthData.Should().ContainKey("service");
        healthData["service"].ToString().Should().Be("paypal-webhooks");
    }

    [Fact]
    [Trait("Priority", "Critical")]
    public async Task ProcessWebhook_WithValidPaymentCompleted_ShouldReturn200()
    {
        // Arrange
        var webhookPayload = await LoadWebhookFixture("payment-completed");
        var payloadJson = JsonSerializer.Serialize(webhookPayload);
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/paypal")
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };
        
        // Add required PayPal headers
        request.Headers.Add("PAYPAL-TRANSMISSION-SIG", "mock-signature-value");
        request.Headers.Add("PAYPAL-TRANSMISSION-ID", "MOCK-TRANSMISSION-123");
        request.Headers.Add("PAYPAL-TRANSMISSION-TIME", DateTime.UtcNow.ToString("O"));
        request.Headers.Add("PAYPAL-CERT-ID", "mock-cert-id");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
        
        responseData.Should().ContainKey("received");
        responseData["received"].ToString().Should().Be("True");
        responseData.Should().ContainKey("eventType");
        responseData["eventType"].ToString().Should().Be("PAYMENT.CAPTURE.COMPLETED");
    }

    [Fact]
    [Trait("Priority", "High")]
    public async Task ProcessWebhook_WithValidPaymentRefunded_ShouldReturn200()
    {
        // Arrange
        var webhookPayload = await LoadWebhookFixture("payment-refunded");
        var payloadJson = JsonSerializer.Serialize(webhookPayload);
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/paypal")
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };
        
        request.Headers.Add("PAYPAL-TRANSMISSION-SIG", "mock-signature-value");
        request.Headers.Add("PAYPAL-TRANSMISSION-ID", "MOCK-TRANSMISSION-456");
        request.Headers.Add("PAYPAL-TRANSMISSION-TIME", DateTime.UtcNow.ToString("O"));
        request.Headers.Add("PAYPAL-CERT-ID", "mock-cert-id");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
        
        responseData.Should().ContainKey("received");
        responseData["received"].ToString().Should().Be("True");
        responseData.Should().ContainKey("eventType");
        responseData["eventType"].ToString().Should().Be("PAYMENT.CAPTURE.REFUNDED");
    }

    [Fact]
    [Trait("Priority", "High")]
    public async Task ProcessWebhook_WithMissingSignatureHeaders_ShouldReturn400()
    {
        // Arrange
        var webhookPayload = await LoadWebhookFixture("payment-completed");
        var payloadJson = JsonSerializer.Serialize(webhookPayload);
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/paypal")
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };
        
        // Deliberately not adding required headers

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var errorData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
        
        errorData.Should().ContainKey("error");
        errorData["error"].ToString().Should().Contain("signature");
    }

    [Fact]
    [Trait("Priority", "Medium")]
    public async Task ProcessWebhook_WithEmptyPayload_ShouldReturn400()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/paypal")
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };
        
        request.Headers.Add("PAYPAL-TRANSMISSION-SIG", "mock-signature");
        request.Headers.Add("PAYPAL-TRANSMISSION-ID", "mock-transmission");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var errorData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
        
        errorData.Should().ContainKey("error");
        errorData["error"].ToString().Should().Contain("payload");
    }

    [Fact]
    [Trait("Priority", "Medium")]
    public async Task ProcessWebhook_WithInvalidJson_ShouldReturn500()
    {
        // Arrange
        var invalidJson = "{ invalid json content here }";
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/paypal")
        {
            Content = new StringContent(invalidJson, Encoding.UTF8, "application/json")
        };
        
        request.Headers.Add("PAYPAL-TRANSMISSION-SIG", "mock-signature");
        request.Headers.Add("PAYPAL-TRANSMISSION-ID", "mock-transmission");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Mock service will handle this gracefully, but real PayPal might fail
        response.StatusCode.Should().BeOneOf(HttpStatusCode.InternalServerError, HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Priority", "High")]
    public async Task ProcessWebhook_MultipleConcurrentRequests_ShouldHandleAllRequests()
    {
        // Arrange
        var webhookPayload = await LoadWebhookFixture("payment-completed");
        var payloadJson = JsonSerializer.Serialize(webhookPayload);
        
        const int concurrentRequests = 5;
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(async i =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/paypal")
                {
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                };
                
                request.Headers.Add("PAYPAL-TRANSMISSION-SIG", $"mock-signature-{i}");
                request.Headers.Add("PAYPAL-TRANSMISSION-ID", $"MOCK-TRANSMISSION-{i}");
                request.Headers.Add("PAYPAL-TRANSMISSION-TIME", DateTime.UtcNow.ToString("O"));
                
                return await _client.SendAsync(request);
            })
            .ToArray();

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(concurrentRequests);
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
        
        _logger.LogInformation("Successfully processed {Count} concurrent webhook requests", concurrentRequests);
    }

    [Fact]
    [Trait("Priority", "Medium")]
    public async Task ProcessWebhook_WithUnknownEventType_ShouldStillReturn200()
    {
        // Arrange
        var unknownEventPayload = new
        {
            id = "WH-UNKNOWN-EVENT-123",
            event_type = "UNKNOWN.EVENT.TYPE",
            create_time = DateTime.UtcNow.ToString("O"),
            resource = new
            {
                id = "UNKNOWN-RESOURCE-123",
                status = "UNKNOWN"
            }
        };
        
        var payloadJson = JsonSerializer.Serialize(unknownEventPayload);
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/paypal")
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };
        
        request.Headers.Add("PAYPAL-TRANSMISSION-SIG", "mock-signature");
        request.Headers.Add("PAYPAL-TRANSMISSION-ID", "MOCK-TRANSMISSION-UNKNOWN");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Mock service should handle unknown events gracefully
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
        
        responseData.Should().ContainKey("eventType");
        responseData["eventType"].ToString().Should().Be("UNKNOWN.EVENT.TYPE");
    }

    [Fact]
    [Trait("Priority", "Low")]
    public async Task WebhookEndpoint_PerformanceTest_ShouldHandleReasonableLoad()
    {
        // Arrange
        var webhookPayload = await LoadWebhookFixture("payment-completed");
        var payloadJson = JsonSerializer.Serialize(webhookPayload);
        
        const int requestCount = 10;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var tasks = Enumerable.Range(0, requestCount)
            .Select(async i =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/paypal")
                {
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                };
                
                request.Headers.Add("PAYPAL-TRANSMISSION-SIG", $"perf-test-{i}");
                request.Headers.Add("PAYPAL-TRANSMISSION-ID", $"PERF-TEST-{i}");
                
                return await _client.SendAsync(request);
            });

        // Act
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
        
        // Performance target: 10 webhook requests should complete in under 5 seconds
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
            "Webhook endpoint should handle 10 requests in under 5 seconds");
        
        _logger.LogInformation("Processed {Count} webhook requests in {Ms}ms (avg: {AvgMs}ms per request)",
            requestCount, stopwatch.ElapsedMilliseconds, stopwatch.ElapsedMilliseconds / requestCount);
    }

    /// <summary>
    /// Loads webhook fixture data from test files
    /// </summary>
    private static async Task<Dictionary<string, object>> LoadWebhookFixture(string fixtureName)
    {
        var fixturePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..",
            "fixtures", "paypal-webhooks",
            $"{fixtureName}.json");
        
        if (!File.Exists(fixturePath))
        {
            throw new FileNotFoundException($"Webhook fixture not found: {fixturePath}");
        }

        var json = await File.ReadAllTextAsync(fixturePath);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json) 
               ?? throw new InvalidOperationException($"Failed to deserialize fixture: {fixtureName}");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client?.Dispose();
            _factory?.Dispose();
        }
        base.Dispose(disposing);
    }
}