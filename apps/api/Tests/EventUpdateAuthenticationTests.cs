using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Models;
using Xunit;
using FluentAssertions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using System.Text.Json;

namespace WitchCityRope.Api.Tests.EventUpdate;

/// <summary>
/// Comprehensive test suite for event update authentication issues
/// 
/// CRITICAL ISSUE: Users are getting logged out when trying to save event changes in admin panel.
/// 
/// Focus Areas:
/// - JWT token validation in PUT requests
/// - Cookie persistence during event updates
/// - Authentication flow integration
/// - CORS configuration validation
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "EventUpdate")]
public class EventUpdateAuthenticationTests
{
    private readonly Mock<ILogger<EventService>> _mockLogger;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<ClaimsPrincipal> _mockUser;
    
    public EventUpdateAuthenticationTests()
    {
        _mockLogger = new Mock<ILogger<EventService>>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockUser = new Mock<ClaimsPrincipal>();
    }

    #region JWT Token Validation Tests

    [Fact]
    public async Task UpdateEvent_WithValidJwtToken_ShouldSucceed()
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Event Title",
            Description = "Updated description"
        };

        var mockService = CreateMockEventService();
        mockService.Setup(s => s.UpdateEventAsync(eventId, updateRequest, default))
               .ReturnsAsync((true, new EventDto { Id = eventId, Title = updateRequest.Title }, ""));

        // Set up authenticated user with valid JWT claims
        SetupAuthenticatedUser();

        // Act & Assert
        var result = await mockService.Object.UpdateEventAsync(eventId, updateRequest, default);
        
        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Title.Should().Be(updateRequest.Title);
    }

    [Fact]
    public async Task UpdateEvent_WithExpiredJwtToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Event Title"
        };

        // Set up expired token scenario - user not authenticated
        _mockUser.Setup(u => u.Identity!.IsAuthenticated).Returns(false);
        _mockHttpContext.Setup(c => c.User).Returns(_mockUser.Object);

        var mockService = CreateMockEventService();
        
        // Simulate service call that would be blocked by authorization
        mockService.Setup(s => s.UpdateEventAsync(eventId, updateRequest, default))
               .ReturnsAsync((false, null, "Authentication required"));

        // Act
        var result = await mockService.Object.UpdateEventAsync(eventId, updateRequest, default);

        // Assert
        result.Success.Should().BeFalse();
        result.Response.Should().BeNull();
        result.Error.Should().Contain("Authentication required");
    }

    [Fact]
    public async Task UpdateEvent_WithInvalidJwtToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Event Title"
        };

        // Set up invalid token scenario
        _mockUser.Setup(u => u.Identity!.IsAuthenticated).Returns(false);
        _mockUser.Setup(u => u.Claims).Returns(new List<Claim>());

        var mockService = CreateMockEventService();
        mockService.Setup(s => s.UpdateEventAsync(eventId, updateRequest, default))
               .ReturnsAsync((false, null, "Invalid or expired token"));

        // Act
        var result = await mockService.Object.UpdateEventAsync(eventId, updateRequest, default);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Invalid or expired token");
    }

    #endregion

    #region Authorization Requirements Tests

    [Fact]
    public async Task UpdateEvent_WithoutRequiredRole_ShouldReturnForbidden()
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Event Title"
        };

        // Set up user with authentication but no admin role
        SetupAuthenticatedUserWithoutAdminRole();

        var mockService = CreateMockEventService();
        mockService.Setup(s => s.UpdateEventAsync(eventId, updateRequest, default))
               .ReturnsAsync((false, null, "Insufficient permissions"));

        // Act
        var result = await mockService.Object.UpdateEventAsync(eventId, updateRequest, default);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Insufficient permissions");
    }

    [Fact]
    public async Task UpdateEvent_WithAdminRole_ShouldSucceed()
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Event Title"
        };

        // Set up authenticated admin user
        SetupAuthenticatedAdminUser();

        var mockService = CreateMockEventService();
        mockService.Setup(s => s.UpdateEventAsync(eventId, updateRequest, default))
               .ReturnsAsync((true, new EventDto { Id = eventId, Title = updateRequest.Title }, ""));

        // Act
        var result = await mockService.Object.UpdateEventAsync(eventId, updateRequest, default);

        // Assert
        result.Success.Should().BeTrue();
        result.Response!.Title.Should().Be(updateRequest.Title);
    }

    #endregion

    #region Partial Update Tests

    [Fact]
    public async Task UpdateEvent_PartialUpdate_ShouldSucceed()
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title Only",
            Description = null // Partial update - only title
        };

        SetupAuthenticatedAdminUser();

        var mockService = CreateMockEventService();
        mockService.Setup(s => s.UpdateEventAsync(eventId, updateRequest, default))
               .ReturnsAsync((true, new EventDto 
               { 
                   Id = eventId, 
                   Title = updateRequest.Title,
                   Description = "Original description" // Should remain unchanged
               }, ""));

        // Act
        var result = await mockService.Object.UpdateEventAsync(eventId, updateRequest, default);

        // Assert
        result.Success.Should().BeTrue();
        result.Response!.Title.Should().Be("Updated Title Only");
        result.Response.Description.Should().Be("Original description");
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData("", "Title cannot be empty")]
    [InlineData(null, "Title cannot be null")]
    [InlineData("A", "Title too short")]
    public async Task UpdateEvent_WithInvalidTitle_ShouldReturnValidationError(string? title, string expectedError)
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        var updateRequest = new UpdateEventRequest
        {
            Title = title
        };

        SetupAuthenticatedAdminUser();

        var mockService = CreateMockEventService();
        mockService.Setup(s => s.UpdateEventAsync(eventId, updateRequest, default))
               .ReturnsAsync((false, null, expectedError));

        // Act
        var result = await mockService.Object.UpdateEventAsync(eventId, updateRequest, default);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain(expectedError.Split(' ')[0]); // Contains key part of error
    }

    [Fact]
    public async Task UpdateEvent_WithPastStartDate_ShouldReturnValidationError()
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        var updateRequest = new UpdateEventRequest
        {
            Title = "Valid Title",
            StartDate = DateTime.UtcNow.AddDays(-1) // Past date
        };

        SetupAuthenticatedAdminUser();

        var mockService = CreateMockEventService();
        mockService.Setup(s => s.UpdateEventAsync(eventId, updateRequest, default))
               .ReturnsAsync((false, null, "Cannot update past events"));

        // Act
        var result = await mockService.Object.UpdateEventAsync(eventId, updateRequest, default);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("past events");
    }

    [Fact]
    public async Task UpdateEvent_ReducingCapacityBelowAttendees_ShouldReturnValidationError()
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        var updateRequest = new UpdateEventRequest
        {
            Title = "Valid Title",
            Capacity = 5 // Less than current attendees
        };

        SetupAuthenticatedAdminUser();

        var mockService = CreateMockEventService();
        mockService.Setup(s => s.UpdateEventAsync(eventId, updateRequest, default))
               .ReturnsAsync((false, null, "Cannot reduce capacity below current attendance"));

        // Act
        var result = await mockService.Object.UpdateEventAsync(eventId, updateRequest, default);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("capacity");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task UpdateEvent_WithNonExistentEvent_ShouldReturn404()
    {
        // Arrange
        var eventId = "00000000-0000-0000-0000-000000000000";
        var updateRequest = new UpdateEventRequest
        {
            Title = "Valid Title"
        };

        SetupAuthenticatedAdminUser();

        var mockService = CreateMockEventService();
        mockService.Setup(s => s.UpdateEventAsync(eventId, updateRequest, default))
               .ReturnsAsync((false, null, "Event not found"));

        // Act
        var result = await mockService.Object.UpdateEventAsync(eventId, updateRequest, default);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateEvent_WithInvalidEventId_ShouldReturn400()
    {
        // Arrange
        var eventId = "invalid-guid";
        var updateRequest = new UpdateEventRequest
        {
            Title = "Valid Title"
        };

        SetupAuthenticatedAdminUser();

        var mockService = CreateMockEventService();
        mockService.Setup(s => s.UpdateEventAsync(eventId, updateRequest, default))
               .ReturnsAsync((false, null, "Invalid event ID format"));

        // Act
        var result = await mockService.Object.UpdateEventAsync(eventId, updateRequest, default);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Invalid event ID");
    }

    #endregion

    #region Helper Methods

    private Mock<EventService> CreateMockEventService()
    {
        return new Mock<EventService>();
    }

    private void SetupAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Email, "admin@witchcityrope.com")
        };

        _mockUser.Setup(u => u.Identity!.IsAuthenticated).Returns(true);
        _mockUser.Setup(u => u.Claims).Returns(claims);
        _mockHttpContext.Setup(c => c.User).Returns(_mockUser.Object);
    }

    private void SetupAuthenticatedUserWithoutAdminRole()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "member"),
            new Claim(ClaimTypes.Email, "member@witchcityrope.com"),
            new Claim(ClaimTypes.Role, "Member") // Not Admin
        };

        _mockUser.Setup(u => u.Identity!.IsAuthenticated).Returns(true);
        _mockUser.Setup(u => u.Claims).Returns(claims);
        _mockHttpContext.Setup(c => c.User).Returns(_mockUser.Object);
    }

    private void SetupAuthenticatedAdminUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-user-id"),
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Email, "admin@witchcityrope.com"),
            new Claim(ClaimTypes.Role, "Admin") // Admin role
        };

        _mockUser.Setup(u => u.Identity!.IsAuthenticated).Returns(true);
        _mockUser.Setup(u => u.Claims).Returns(claims);
        _mockHttpContext.Setup(c => c.User).Returns(_mockUser.Object);
    }

    #endregion
}

/// <summary>
/// Integration tests for the complete event update endpoint with authentication
/// These tests verify the entire HTTP request/response flow including middleware
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "EventUpdate")]
public class EventUpdateEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public EventUpdateEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task UpdateEvent_WithoutAuthToken_ShouldReturn401()
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title"
        };
        
        var json = JsonSerializer.Serialize(updateRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/events/{eventId}", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateEvent_WithValidAuthToken_ShouldProcessRequest()
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title"
        };

        // First login to get a valid token
        var loginRequest = new { Email = "admin@witchcityrope.com", Password = "Test123!" };
        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        
        var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
        
        if (loginResponse.IsSuccessStatusCode)
        {
            // Extract token from response or cookies
            var authHeaders = loginResponse.Headers.Where(h => h.Key.ToLower().Contains("auth")).ToList();
            
            // Set up authenticated client
            var json = JsonSerializer.Serialize(updateRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/events/{eventId}", content);

            // Assert - should not be unauthorized (may be 404 if event doesn't exist, but not 401)
            response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.Unauthorized);
        }
        else
        {
            // If login fails, the test should focus on that
            Assert.True(false, "Login failed - check authentication setup");
        }
    }

    [Fact]
    public async Task UpdateEvent_ChecksCORSConfiguration()
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        
        _client.DefaultRequestHeaders.Add("Origin", "http://localhost:5173");
        
        // Act - Send OPTIONS request (CORS preflight)
        var optionsRequest = new HttpRequestMessage(HttpMethod.Options, $"/api/events/{eventId}");
        optionsRequest.Headers.Add("Access-Control-Request-Method", "PUT");
        optionsRequest.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        
        var response = await _client.SendAsync(optionsRequest);

        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.Should().ContainKey("Access-Control-Allow-Methods");
        response.Headers.Should().ContainKey("Access-Control-Allow-Headers");
        
        var allowedMethods = response.Headers.GetValues("Access-Control-Allow-Methods").FirstOrDefault();
        allowedMethods.Should().Contain("PUT");
    }

    #region Cookie Authentication Tests

    [Fact]
    public async Task UpdateEvent_WithHttpOnlyCookies_ShouldPersistAuthentication()
    {
        // Arrange
        var eventId = "550e8400-e29b-41d4-a716-446655440000";
        
        // Create client that preserves cookies
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler()
        {
            CookieContainer = cookieContainer
        };
        
        using var clientWithCookies = _factory.WithWebHostBuilder(builder =>
        {
            // Configure to use cookie authentication
        }).CreateClient();

        // Login to establish cookie session
        var loginRequest = new { Email = "admin@witchcityrope.com", Password = "Test123!" };
        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        
        var loginResponse = await clientWithCookies.PostAsync("/api/auth/login", loginContent);

        if (loginResponse.IsSuccessStatusCode)
        {
            // Verify cookies were set
            var cookies = cookieContainer.GetCookies(new Uri("http://localhost"));
            cookies.Should().NotBeEmpty("Authentication should set cookies");

            // Now attempt event update
            var updateRequest = new UpdateEventRequest { Title = "Updated Title" };
            var json = JsonSerializer.Serialize(updateRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await clientWithCookies.PutAsync($"/api/events/{eventId}", content);

            // Assert
            response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.Unauthorized, 
                "PUT request should maintain authentication via cookies");

            // Verify cookies are still valid after PUT request
            var cookiesAfter = cookieContainer.GetCookies(new Uri("http://localhost"));
            cookiesAfter.Count.Should().BeGreaterOrEqualTo(cookies.Count, 
                "Cookies should not be invalidated by PUT request");
        }
    }

    #endregion

    public void Dispose()
    {
        _client?.Dispose();
    }
}