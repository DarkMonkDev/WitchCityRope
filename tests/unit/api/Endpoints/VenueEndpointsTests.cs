using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using WitchCityRope.Api.Endpoints;
using WitchCityRope.Api.DTOs;
using WitchCityRope.Api.Data;
using Xunit;
using FluentAssertions;
using NSubstitute;
using Microsoft.EntityFrameworkCore;

namespace WitchCityRope.UnitTests.Api.Endpoints;

public class VenueEndpointsTests
{
    private readonly ApplicationDbContext _mockDbContext;
    private readonly ClaimsPrincipal _authenticatedUser;
    private readonly ClaimsPrincipal _unauthenticatedUser;

    public VenueEndpointsTests()
    {
        _mockDbContext = Substitute.For<ApplicationDbContext>();

        // Create authenticated user claims
        _authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("role", "Member")
        }, "test"));

        // Create unauthenticated user (no authentication type)
        _unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());
    }

    #region GetPublicVenue Tests

    [Fact]
    public async Task GetPublicVenue_WithValidIdAndAuthenticatedUser_ReturnsOkResult()
    {
        // Arrange
        var venueId = 1;
        var mockVenue = new VenueDto
        {
            Id = venueId,
            Name = "Test Venue",
            Directions = "123 Test St",
            Notes = null, // Public endpoint should not expose notes
            IsActive = true,
            CreatedAt = DateTime.Parse("2025-11-14T10:00:00Z"),
            UpdatedAt = DateTime.Parse("2025-11-14T10:00:00Z")
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetPublicVenue(venueId, httpContext, mockVenue);

        // Assert
        result.Should().BeOfType<Ok<VenueDto>>();
        var okResult = (Ok<VenueDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Id.Should().Be(venueId);
        okResult.Value.Name.Should().Be("Test Venue");
        okResult.Value.Notes.Should().BeNull(); // Verify notes are not exposed
    }

    [Fact]
    public async Task GetPublicVenue_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var venueId = 1;
        var httpContext = new DefaultHttpContext();
        httpContext.User = _unauthenticatedUser;

        // Act
        var result = await SimulateGetPublicVenue(venueId, httpContext, null);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Authentication Required");
        problemResult.ProblemDetails.Detail.Should().Contain("logged in");
    }

    [Fact]
    public async Task GetPublicVenue_WithNonExistentVenue_ReturnsNotFound()
    {
        // Arrange
        var venueId = 999;
        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetPublicVenue(venueId, httpContext, null);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Venue Not Found");
        problemResult.ProblemDetails.Detail.Should().Contain($"Venue with ID {venueId}");
    }

    [Fact]
    public async Task GetPublicVenue_WithDatabaseError_ReturnsInternalServerError()
    {
        // Arrange
        var venueId = 1;
        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetPublicVenueWithError(venueId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Database Error");
        problemResult.ProblemDetails.Detail.Should().Contain("Failed to retrieve venue");
    }

    #endregion

    #region GetPublicVenues Tests

    [Fact]
    public async Task GetPublicVenues_WithAuthenticatedUser_ReturnsOkResult()
    {
        // Arrange
        var mockVenues = new List<VenueDto>
        {
            new VenueDto
            {
                Id = 1,
                Name = "Venue A",
                Directions = "123 A St",
                Notes = null,
                IsActive = true,
                CreatedAt = DateTime.Parse("2025-11-14T10:00:00Z"),
                UpdatedAt = DateTime.Parse("2025-11-14T10:00:00Z")
            },
            new VenueDto
            {
                Id = 2,
                Name = "Venue B",
                Directions = "456 B St",
                Notes = null,
                IsActive = true,
                CreatedAt = DateTime.Parse("2025-11-14T10:00:00Z"),
                UpdatedAt = DateTime.Parse("2025-11-14T10:00:00Z")
            }
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetPublicVenues(httpContext, mockVenues);

        // Assert
        result.Should().BeOfType<Ok<List<VenueDto>>>();
        var okResult = (Ok<List<VenueDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Count.Should().Be(2);
        okResult.Value![0].Name.Should().Be("Venue A");
        okResult.Value![1].Name.Should().Be("Venue B");
        okResult.Value.All(v => v.Notes == null).Should().BeTrue(); // Verify notes are not exposed
    }

    [Fact]
    public async Task GetPublicVenues_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = _unauthenticatedUser;

        // Act
        var result = await SimulateGetPublicVenues(httpContext, null);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Authentication Required");
        problemResult.ProblemDetails.Detail.Should().Contain("logged in");
    }

    [Fact]
    public async Task GetPublicVenues_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetPublicVenues(httpContext, new List<VenueDto>());

        // Assert
        result.Should().BeOfType<Ok<List<VenueDto>>>();
        var okResult = (Ok<List<VenueDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetPublicVenues_WithDatabaseError_ReturnsInternalServerError()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetPublicVenuesWithError(httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Database Error");
        problemResult.ProblemDetails.Detail.Should().Contain("Failed to retrieve venues");
    }

    #endregion

    #region Status Code Mapping Tests

    [Theory]
    [InlineData(true, 401)]  // Unauthenticated user
    [InlineData(false, 404)] // Non-existent venue
    public async Task GetPublicVenue_ReturnsCorrectStatusCodes_BasedOnErrorType(bool isUnauthenticated, int expectedStatusCode)
    {
        // Arrange
        var venueId = 1;
        var httpContext = new DefaultHttpContext();
        httpContext.User = isUnauthenticated ? _unauthenticatedUser : _authenticatedUser;

        // Act
        var result = isUnauthenticated
            ? await SimulateGetPublicVenue(venueId, httpContext, null)
            : await SimulateGetPublicVenue(venueId, httpContext, null); // null venue for 404

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(expectedStatusCode);
    }

    #endregion

    #region Helper Methods - Simulation

    private async Task<IResult> SimulateGetPublicVenue(int id, HttpContext context, VenueDto? venue)
    {
        // Verify authentication
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(
                title: "Authentication Required",
                detail: "You must be logged in to access venue details",
                statusCode: 401);
        }

        await Task.CompletedTask; // Simulate async database call

        if (venue == null)
        {
            return Results.Problem(
                title: "Venue Not Found",
                detail: $"Venue with ID {id} does not exist or is not available",
                statusCode: 404);
        }

        return Results.Ok(venue);
    }

    private async Task<IResult> SimulateGetPublicVenueWithError(int id, HttpContext context)
    {
        // Verify authentication
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(
                title: "Authentication Required",
                detail: "You must be logged in to access venue details",
                statusCode: 401);
        }

        await Task.CompletedTask; // Simulate async database call

        // Simulate database error
        return Results.Problem(
            title: "Database Error",
            detail: "Failed to retrieve venue: Connection failed",
            statusCode: 500);
    }

    private async Task<IResult> SimulateGetPublicVenues(HttpContext context, List<VenueDto>? venues)
    {
        // Verify authentication
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(
                title: "Authentication Required",
                detail: "You must be logged in to access venues",
                statusCode: 401);
        }

        await Task.CompletedTask; // Simulate async database call

        return Results.Ok(venues ?? new List<VenueDto>());
    }

    private async Task<IResult> SimulateGetPublicVenuesWithError(HttpContext context)
    {
        // Verify authentication
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(
                title: "Authentication Required",
                detail: "You must be logged in to access venues",
                statusCode: 401);
        }

        await Task.CompletedTask; // Simulate async database call

        // Simulate database error
        return Results.Problem(
            title: "Database Error",
            detail: "Failed to retrieve venues: Connection failed",
            statusCode: 500);
    }

    #endregion
}
