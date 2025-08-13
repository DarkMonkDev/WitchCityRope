extern alias WitchCityRopeWeb;
extern alias WitchCityRopeApi;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Very basic tests to verify the application starts correctly
/// </summary>
public class BasicSetupTests : IClassFixture<WebApplicationFactory<WitchCityRopeWeb::Program>>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public BasicSetupTests(WebApplicationFactory<WitchCityRopeWeb::Program> factory, ITestOutputHelper output)
    {
        _output = output;
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                // Any test-specific services
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Application_ShouldStart()
    {
        // Act
        var response = await _client.GetAsync("/health");
        
        // Log the response for debugging
        _output.WriteLine($"Health check response: {response.StatusCode}");
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {content}");
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HomePage_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/");
        
        // Log the response for debugging
        _output.WriteLine($"Home page response: {response.StatusCode}");
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content (first 500 chars): {content.Substring(0, Math.Min(500, content.Length))}");
        }

        // Assert - The home page should load without errors
        response.IsSuccessStatusCode.Should().BeTrue($"Expected success status code, but got {response.StatusCode}");
    }

    [Fact]
    public async Task PublicEndpoint_ShouldBeAccessibleWithoutAuth()
    {
        // Act
        var response = await _client.GetAsync("/events");
        
        // Log the response for debugging
        _output.WriteLine($"Events page response: {response.StatusCode}");

        // Assert - Should either return OK or redirect to login
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Redirect,
            HttpStatusCode.Found);
    }
}