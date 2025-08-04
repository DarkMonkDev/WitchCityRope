using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Tests to verify HTTPS redirection is properly disabled in test environments
/// </summary>
public class HttpsRedirectionTests : IntegrationTestBase
{
    public HttpsRedirectionTests(PostgreSqlFixture postgresFixture) : base(postgresFixture)
    {
    }

    [Fact]
    public async Task TestEnvironment_ShouldNotRedirectToHttps()
    {
        // Arrange
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // Important: disable auto-redirect to catch redirect responses
        });

        // Act
        var response = await client.GetAsync("/");

        // Assert
        // Should get 200 OK, not 301/302 redirect
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Ensure we're not getting a redirect response
        Assert.False(response.Headers.Location != null, 
            "Response should not contain a Location header for redirect");
    }

    [Fact]
    public async Task HealthEndpoint_ShouldBeAccessibleViaHttp()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StaticFiles_ShouldBeAccessibleViaHttp()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/css/site.css");

        // Assert
        // Should either be OK or NotFound (if file doesn't exist), but not a redirect
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected OK or NotFound, but got {response.StatusCode}");
    }
}