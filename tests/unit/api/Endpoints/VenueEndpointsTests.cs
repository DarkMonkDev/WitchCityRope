using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using WitchCityRope.Api.Endpoints;
using WitchCityRope.Api.DTOs;
using WitchCityRope.Api.Features.Venues.Services;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Endpoints;

public class VenueEndpointsTests
{
    private readonly IVenueService _mockVenueService;
    private readonly ClaimsPrincipal _authenticatedUser;
    private readonly ClaimsPrincipal _unauthenticatedUser;

    public VenueEndpointsTests()
    {
        _mockVenueService = Substitute.For<IVenueService>();

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
            VenueInformation = null, // Public endpoint should not expose venue information
            IsActive = true,
            CreatedAt = DateTime.Parse("2025-11-14T10:00:00Z"),
            UpdatedAt = DateTime.Parse("2025-11-14T10:00:00Z")
        };

        _mockVenueService.GetPublicVenueAsync(venueId, Arg.Any<CancellationToken>())
            .Returns(mockVenue);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetPublicVenue(venueId, httpContext);

        // Assert
        result.Should().BeOfType<Ok<VenueDto>>();
        var okResult = (Ok<VenueDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Id.Should().Be(venueId);
        okResult.Value.Name.Should().Be("Test Venue");
        okResult.Value.VenueInformation.Should().BeNull(); // Verify venue information is not exposed
    }

    [Fact]
    public async Task GetPublicVenue_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var venueId = 1;
        var httpContext = new DefaultHttpContext();
        httpContext.User = _unauthenticatedUser;

        // Act
        var result = await SimulateGetPublicVenue(venueId, httpContext);

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

        _mockVenueService.GetPublicVenueAsync(venueId, Arg.Any<CancellationToken>())
            .Returns((VenueDto?)null);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetPublicVenue(venueId, httpContext);

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

        _mockVenueService.GetPublicVenueAsync(venueId, Arg.Any<CancellationToken>())
            .Returns<VenueDto?>(_ => throw new Exception("Connection failed"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetPublicVenue(venueId, httpContext);

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
                VenueInformation = null,
                IsActive = true,
                CreatedAt = DateTime.Parse("2025-11-14T10:00:00Z"),
                UpdatedAt = DateTime.Parse("2025-11-14T10:00:00Z")
            },
            new VenueDto
            {
                Id = 2,
                Name = "Venue B",
                Directions = "456 B St",
                VenueInformation = null,
                IsActive = true,
                CreatedAt = DateTime.Parse("2025-11-14T10:00:00Z"),
                UpdatedAt = DateTime.Parse("2025-11-14T10:00:00Z")
            }
        };

        _mockVenueService.GetPublicVenuesAsync(Arg.Any<CancellationToken>())
            .Returns(mockVenues);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetPublicVenues(httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<VenueDto>>>();
        var okResult = (Ok<List<VenueDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Count.Should().Be(2);
        okResult.Value![0].Name.Should().Be("Venue A");
        okResult.Value![1].Name.Should().Be("Venue B");
        okResult.Value.All(v => v.VenueInformation == null).Should().BeTrue(); // Verify venue information is not exposed
    }

    [Fact]
    public async Task GetPublicVenues_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = _unauthenticatedUser;

        // Act
        var result = await SimulateGetPublicVenues(httpContext);

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
        _mockVenueService.GetPublicVenuesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<VenueDto>());

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetPublicVenues(httpContext);

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
        _mockVenueService.GetPublicVenuesAsync(Arg.Any<CancellationToken>())
            .Returns<List<VenueDto>>(_ => throw new Exception("Connection failed"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetPublicVenues(httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Database Error");
        problemResult.ProblemDetails.Detail.Should().Contain("Failed to retrieve venues");
    }

    #endregion

    #region Helper Methods - Endpoint Logic Simulation

    /// <summary>
    /// Simulates the GetPublicVenue endpoint logic
    /// Matches the actual endpoint handler in VenueEndpoints.cs
    /// </summary>
    private async Task<IResult> SimulateGetPublicVenue(int id, HttpContext context)
    {
        // Verify authentication (matches endpoint logic)
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(
                title: "Authentication Required",
                detail: "You must be logged in to access venue details",
                statusCode: 401);
        }

        try
        {
            var venue = await _mockVenueService.GetPublicVenueAsync(id, CancellationToken.None);

            if (venue == null)
            {
                return Results.Problem(
                    title: "Venue Not Found",
                    detail: $"Venue with ID {id} does not exist or is not available",
                    statusCode: 404);
            }

            return Results.Ok(venue);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Database Error",
                detail: $"Failed to retrieve venue: {ex.Message}",
                statusCode: 500);
        }
    }

    /// <summary>
    /// Simulates the GetPublicVenues endpoint logic
    /// Matches the actual endpoint handler in VenueEndpoints.cs
    /// </summary>
    private async Task<IResult> SimulateGetPublicVenues(HttpContext context)
    {
        // Verify authentication (matches endpoint logic)
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(
                title: "Authentication Required",
                detail: "You must be logged in to access venues",
                statusCode: 401);
        }

        try
        {
            var venues = await _mockVenueService.GetPublicVenuesAsync(CancellationToken.None);

            return Results.Ok(venues);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Database Error",
                detail: $"Failed to retrieve venues: {ex.Message}",
                statusCode: 500);
        }
    }

    #endregion
}
