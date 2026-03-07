using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Data;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Api;

/// <summary>
/// Integration tests specifically for CSRF token handling infrastructure.
/// These tests verify that the CSRF token fetching and header management works correctly.
/// </summary>
[Collection("Database")]
public class CsrfTokenIntegrationTests : IntegrationTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private bool _disposed;

    public CsrfTokenIntegrationTests(DatabaseTestFixture fixture)
        : base(fixture)
    {
        _factory = CreateTestWebApplicationFactory();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _factory?.Dispose();
            _disposed = true;
        }
    }

    [Fact]
    public async Task FetchCsrfToken_WithAuthentication_ReturnsValidToken()
    {
        // Arrange
        var userId = await GetUserIdAsync("admin@witchcityrope.com");
        var jwtToken = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

        // Act
        var csrfToken = await FetchCsrfTokenAsync(client);

        // Assert
        csrfToken.Should().NotBeNullOrEmpty();
        csrfToken.Length.Should().BeGreaterThan(10); // CSRF tokens are reasonably long
    }

    [Fact(Skip = "NoOpAntiforgery in test factory returns tokens for all requests including unauthenticated")]
    public async Task FetchCsrfToken_WithoutAuthentication_ThrowsException()
    {
        // Arrange
        var client = _factory.CreateClient();
        // No Bearer token set

        // Act & Assert
        var act = async () => await FetchCsrfTokenAsync(client);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to fetch CSRF token*");
    }

    [Fact]
    public async Task CreateAuthenticatedClientWithCsrf_CreatesValidClient()
    {
        // Arrange
        var userId = await GetUserIdAsync("admin@witchcityrope.com");
        var jwtToken = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");

        // Act
        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, jwtToken);

        // Assert
        client.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");

        // Verify CSRF token header is present
        client.DefaultRequestHeaders.Contains("X-CSRF-TOKEN").Should().BeTrue();
    }

    [Fact]
    public async Task GetRequest_DoesNotRequireCsrfToken()
    {
        // Arrange - Client with authentication but NO CSRF token
        var userId = await GetUserIdAsync("member@witchcityrope.com");
        var jwtToken = GenerateJwtToken(userId, "member@witchcityrope.com", "Member");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        // Intentionally NOT fetching CSRF token

        // Act - GET request should work without CSRF token
        var response = await client.GetAsync("/api/venues");

        // Assert - GET requests don't require CSRF protection
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // NOTE: This test demonstrates what WILL happen when CSRF is enabled on endpoints
    // Currently many endpoints don't have CSRF validation yet, so this is documentation
    [Fact(Skip = "Demonstration test - enable when CSRF is added to venue endpoints")]
    public async Task PostRequest_WithoutCsrfToken_Returns400()
    {
        // Arrange - Client with authentication but NO CSRF token
        var userId = await GetUserIdAsync("admin@witchcityrope.com");
        var jwtToken = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        // Intentionally NOT fetching CSRF token

        var newVenue = new
        {
            Name = "Test Venue Without CSRF",
            Directions = "Test directions"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/venues", newVenue);

        // Assert - Should fail CSRF validation
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("CSRF");
    }

    // NOTE: This test demonstrates the correct pattern for CSRF-protected endpoints
    [Fact(Skip = "Demonstration test - venue endpoints don't have CSRF yet")]
    public async Task PostRequest_WithCsrfToken_Succeeds()
    {
        // Arrange - Client with both authentication AND CSRF token
        var userId = await GetUserIdAsync("admin@witchcityrope.com");
        var jwtToken = GenerateJwtToken(userId, "admin@witchcityrope.com", "Administrator");

        var client = await CreateAuthenticatedClientWithCsrfAsync(_factory, jwtToken);

        var newVenue = new
        {
            Name = $"Test Venue {Guid.NewGuid():N}"[..20],
            Directions = "Test directions"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/venues", newVenue);

        // Assert - Should succeed with valid CSRF token
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #region Helper Methods

    private async Task<Guid> GetUserIdAsync(string email)
    {
        await using var context = CreateDbContext();
        var user = await context.Users
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync();

        return user?.Id ?? throw new InvalidOperationException($"User {email} not found in test database");
    }

    #endregion
}
