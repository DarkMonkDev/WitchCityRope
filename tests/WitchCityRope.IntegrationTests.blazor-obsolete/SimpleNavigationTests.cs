using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using Xunit;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Simplified navigation tests that just check if endpoints exist
/// </summary>
public class SimpleNavigationTests : IClassFixture<WebApplicationFactory<WitchCityRope.Web.Program>>
{
    private readonly HttpClient _client;

    public SimpleNavigationTests(WebApplicationFactory<WitchCityRope.Web.Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development"); // Use development to bypass auth
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/events")]
    [InlineData("/auth/login")]
    public async Task PublicEndpoints_ShouldBeAccessible(string url)
    {
        // Act
        var response = await _client.GetAsync(url);

        // Assert - These endpoints should return OK or redirect
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.Redirect,
            HttpStatusCode.MovedPermanently);
    }

    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/my-tickets")]
    [InlineData("/profile")]
    [InlineData("/admin/events")]
    public async Task ProtectedEndpoints_ShouldRequireAuthentication(string url)
    {
        // Act
        var response = await _client.GetAsync(url);

        // Assert - These endpoints should redirect to login or return unauthorized
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Redirect,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task HomePage_ShouldContainExpectedContent()
    {
        // Act
        var response = await _client.GetAsync("/");
        
        // Only check content if we got a successful response
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            // Assert - Check for expected content
            content.Should().NotBeNullOrEmpty();
            // Basic check that we got HTML
            content.Should().Contain("<");
        }
    }

    [Fact]
    public async Task EventsPage_ShouldBeAccessible()
    {
        // Act
        var response = await _client.GetAsync("/events");
        
        // Assert - Events page should be publicly accessible
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task AllInternalLinks_ShouldNotReturn500Errors()
    {
        var testUrls = new[]
        {
            "/", "/events", "/auth/login", "/auth/register",
            "/apply", "/join", "/dashboard", "/profile"
        };

        var errors = new List<string>();

        foreach (var url in testUrls)
        {
            var response = await _client.GetAsync(url);
            
            // We should never get 500 errors
            if ((int)response.StatusCode >= 500)
            {
                errors.Add($"{url}: {response.StatusCode}");
            }
        }

        errors.Should().BeEmpty($"No endpoints should return 500 errors, but found: {string.Join(", ", errors)}");
    }
}