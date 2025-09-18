using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text;
using WitchCityRope.Api;

namespace WitchCityRope.Tests.Common.TestBase;

/// <summary>
/// Base class for testing Vertical Slice Architecture features
/// Provides WebApplicationFactory setup for testing API endpoints
/// </summary>
public abstract class VerticalSliceTestBase : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private bool _disposed = false;

    protected WebApplicationFactory<Program> App => _factory;
    protected HttpClient Client => _client;

    protected VerticalSliceTestBase()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(ConfigureTestServices);
            });

        _client = _factory.CreateClient();
    }

    /// <summary>
    /// Override this method to configure additional test services
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Override in derived classes for custom service configuration
    }

    /// <summary>
    /// Helper method for making POST requests with JSON payload
    /// </summary>
    protected async Task<HttpResponseMessage> PostJsonAsync<T>(string endpoint, T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await Client.PostAsync(endpoint, content);
    }

    /// <summary>
    /// Helper method for making PUT requests with JSON payload
    /// </summary>
    protected async Task<HttpResponseMessage> PutJsonAsync<T>(string endpoint, T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await Client.PutAsync(endpoint, content);
    }

    /// <summary>
    /// Helper method for making PATCH requests with JSON payload
    /// </summary>
    protected async Task<HttpResponseMessage> PatchJsonAsync<T>(string endpoint, T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await Client.PatchAsync(endpoint, content);
    }

    /// <summary>
    /// Helper method for deserializing response content
    /// </summary>
    protected async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    /// <summary>
    /// Helper method for testing POST endpoints with expected status code
    /// </summary>
    protected async Task<HttpResponseMessage> TestPostEndpoint<TRequest>(
        string endpoint,
        TRequest request,
        System.Net.HttpStatusCode expectedStatusCode = System.Net.HttpStatusCode.OK)
    {
        var response = await PostJsonAsync(endpoint, request);

        if (response.StatusCode != expectedStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Expected status code {expectedStatusCode} but got {response.StatusCode}. Response: {content}");
        }

        return response;
    }

    /// <summary>
    /// Helper method for testing GET endpoints with expected status code
    /// </summary>
    protected async Task<HttpResponseMessage> TestGetEndpoint(
        string endpoint,
        System.Net.HttpStatusCode expectedStatusCode = System.Net.HttpStatusCode.OK)
    {
        var response = await Client.GetAsync(endpoint);

        if (response.StatusCode != expectedStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Expected status code {expectedStatusCode} but got {response.StatusCode}. Response: {content}");
        }

        return response;
    }

    /// <summary>
    /// Get a service from the DI container for testing
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return _factory.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Create a service scope for scoped services
    /// </summary>
    protected IServiceScope CreateScope()
    {
        return _factory.Services.CreateScope();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _client?.Dispose();
            _factory?.Dispose();
            _disposed = true;
        }
    }
}