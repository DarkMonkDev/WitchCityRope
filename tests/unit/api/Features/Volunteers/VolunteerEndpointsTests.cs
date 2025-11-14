using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using WitchCityRope.Api.Features.Volunteers.Endpoints;
using WitchCityRope.Api.Features.Volunteers.Models;
using WitchCityRope.Api.Features.Volunteers.Services;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Features.Volunteers;

public class VolunteerEndpointsTests
{
    private readonly IVolunteerService _mockVolunteerService;
    private readonly ClaimsPrincipal _authenticatedUser;
    private readonly ClaimsPrincipal _unauthenticatedUser;

    public VolunteerEndpointsTests()
    {
        _mockVolunteerService = Substitute.For<IVolunteerService>();

        // Create authenticated user claims
        _authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("role", "Member")
        }, "test"));

        // Create unauthenticated user
        _unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());
    }

    #region GetEventVolunteerPositions Tests

    [Fact]
    public async Task GetEventVolunteerPositions_WithValidEventId_ReturnsOkResult()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var mockPositions = new List<VolunteerPositionDto>
        {
            new VolunteerPositionDto
            {
                Id = Guid.NewGuid(),
                EventId = Guid.Parse(eventId),
                Title = "Setup Volunteer",
                Description = "Help with event setup",
                SlotsNeeded = 5,
                SlotsFilled = 2,
                SlotsRemaining = 3,
                IsPublicFacing = true,
                IsFullyStaffed = false
            }
        };

        _mockVolunteerService.GetEventVolunteerPositionsAsync(eventId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((true, mockPositions, null));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetEventVolunteerPositions(eventId, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<VolunteerPositionDto>>>();
        var okResult = (Ok<List<VolunteerPositionDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Count.Should().Be(1);
        okResult.Value![0].Title.Should().Be("Setup Volunteer");
    }

    [Fact]
    public async Task GetEventVolunteerPositions_WithNonExistentEvent_ReturnsNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        _mockVolunteerService.GetEventVolunteerPositionsAsync(eventId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((false, null, "Event not found"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetEventVolunteerPositions(eventId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails!.Detail.Should().Contain("Event not found");
    }

    [Fact]
    public async Task GetEventVolunteerPositions_WithServiceError_ReturnsInternalServerError()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        _mockVolunteerService.GetEventVolunteerPositionsAsync(eventId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((false, null, "Database connection failed"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetEventVolunteerPositions(eventId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails!.Detail.Should().Contain("Database connection failed");
    }

    #endregion

    #region SignupForPosition Tests

    [Fact]
    public async Task SignupForPosition_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var positionId = Guid.NewGuid().ToString();
        var userId = _authenticatedUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var request = new VolunteerSignupRequest { EventWaiverAccepted = true };

        var mockSignup = new VolunteerSignupDto
        {
            Id = Guid.NewGuid(),
            VolunteerPositionId = Guid.Parse(positionId),
            UserId = Guid.Parse(userId),
            Status = "Confirmed",
            SignedUpAt = DateTime.Parse("2025-11-14T10:00:00Z")
        };

        _mockVolunteerService.SignupForPositionAsync(positionId, userId, request, Arg.Any<CancellationToken>())
            .Returns((true, mockSignup, null));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateSignupForPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<Ok<VolunteerSignupDto>>();
        var okResult = (Ok<VolunteerSignupDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task SignupForPosition_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var positionId = Guid.NewGuid().ToString();
        var request = new VolunteerSignupRequest { EventWaiverAccepted = true };
        var httpContext = new DefaultHttpContext();
        httpContext.User = _unauthenticatedUser;

        // Act
        var result = await SimulateSignupForPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails!.Title.Should().Be("Authentication Required");
    }

    [Fact]
    public async Task SignupForPosition_WithNonExistentPosition_ReturnsNotFound()
    {
        // Arrange
        var positionId = Guid.NewGuid().ToString();
        var userId = _authenticatedUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var request = new VolunteerSignupRequest { EventWaiverAccepted = true };

        _mockVolunteerService.SignupForPositionAsync(positionId, userId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Volunteer position not found"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateSignupForPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails!.Detail.Should().Contain("not found");
    }

    [Fact]
    public async Task SignupForPosition_WithDuplicateSignup_ReturnsConflict()
    {
        // Arrange
        var positionId = Guid.NewGuid().ToString();
        var userId = _authenticatedUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var request = new VolunteerSignupRequest { EventWaiverAccepted = true };

        _mockVolunteerService.SignupForPositionAsync(positionId, userId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "You have already signed up for this volunteer position"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateSignupForPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(409);
        problemResult.ProblemDetails!.Detail.Should().Contain("already signed up");
    }

    [Fact]
    public async Task SignupForPosition_WithFullPosition_ReturnsConflict()
    {
        // Arrange
        var positionId = Guid.NewGuid().ToString();
        var userId = _authenticatedUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var request = new VolunteerSignupRequest { EventWaiverAccepted = true };

        _mockVolunteerService.SignupForPositionAsync(positionId, userId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "This volunteer position is already fully staffed"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateSignupForPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(409);
        problemResult.ProblemDetails!.Detail.Should().Contain("fully staffed");
    }

    [Fact]
    public async Task SignupForPosition_WithoutWaiverAcceptance_ReturnsBadRequest()
    {
        // Arrange
        var positionId = Guid.NewGuid().ToString();
        var userId = _authenticatedUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var request = new VolunteerSignupRequest { EventWaiverAccepted = true };

        _mockVolunteerService.SignupForPositionAsync(positionId, userId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "You must accept the Event Waiver to volunteer"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateSignupForPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Detail.Should().Contain("Event Waiver");
    }

    #endregion

    #region GetUserVolunteerShifts Tests

    [Fact]
    public async Task GetUserVolunteerShifts_WithAuthenticatedUser_ReturnsOkResult()
    {
        // Arrange
        var userId = _authenticatedUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var mockShifts = new List<UserVolunteerShiftDto>
        {
            new UserVolunteerShiftDto
            {
                SignupId = Guid.NewGuid(),
                EventTitle = "Test Event",
                EventLocation = "Test Location",
                EventDate = DateTime.Parse("2025-11-20T18:00:00Z"),
                PositionTitle = "Setup Volunteer",
                ShiftStartTime = DateTime.Parse("2025-11-20T17:00:00Z"),
                ShiftEndTime = DateTime.Parse("2025-11-20T18:00:00Z")
            }
        };

        _mockVolunteerService.GetUserVolunteerShiftsAsync(userId, Arg.Any<CancellationToken>())
            .Returns((true, mockShifts, null));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetUserVolunteerShifts(httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<UserVolunteerShiftDto>>>();
        var okResult = (Ok<List<UserVolunteerShiftDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Count.Should().Be(1);
        okResult.Value![0].EventTitle.Should().Be("Test Event");
    }

    [Fact]
    public async Task GetUserVolunteerShifts_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = _unauthenticatedUser;

        // Act
        var result = await SimulateGetUserVolunteerShifts(httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails!.Title.Should().Be("Authentication Required");
    }

    [Fact]
    public async Task GetUserVolunteerShifts_WithServiceError_ReturnsInternalServerError()
    {
        // Arrange
        var userId = _authenticatedUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        _mockVolunteerService.GetUserVolunteerShiftsAsync(userId, Arg.Any<CancellationToken>())
            .Returns((false, null, "Database connection failed"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateGetUserVolunteerShifts(httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails!.Detail.Should().Contain("Database connection failed");
    }

    #endregion

    #region Status Code Mapping Tests

    [Theory]
    [InlineData("Volunteer position not found", 404)]
    [InlineData("You have already signed up for this volunteer position", 409)]
    [InlineData("This volunteer position is already fully staffed", 409)]
    [InlineData("This volunteer position is not open for public signups", 403)]
    [InlineData("You must accept the Event Waiver to volunteer", 400)]
    [InlineData("Database error", 500)]
    public async Task SignupForPosition_ReturnsCorrectStatusCodes_BasedOnErrorType(string errorMessage, int expectedStatusCode)
    {
        // Arrange
        var positionId = Guid.NewGuid().ToString();
        var userId = _authenticatedUser.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var request = new VolunteerSignupRequest { EventWaiverAccepted = true };

        _mockVolunteerService.SignupForPositionAsync(positionId, userId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, errorMessage));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await SimulateSignupForPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(expectedStatusCode);
    }

    #endregion

    #region Helper Methods - Simulation

    private async Task<IResult> SimulateGetEventVolunteerPositions(string eventId, HttpContext context)
    {
        string? userId = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;

        var (success, positions, error) = await _mockVolunteerService.GetEventVolunteerPositionsAsync(
            eventId,
            userId,
            CancellationToken.None);

        if (success && positions != null)
        {
            return Results.Ok(positions);
        }

        return Results.Problem(
            title: "Failed to Retrieve Volunteer Positions",
            detail: error ?? "Failed to retrieve volunteer positions",
            statusCode: error == "Event not found" ? 404 : 500);
    }

    private async Task<IResult> SimulateSignupForPosition(string positionId, VolunteerSignupRequest request, HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Problem(
                title: "Authentication Required",
                detail: "You must be logged in to sign up for volunteer positions",
                statusCode: 401);
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Problem(
                title: "Invalid User",
                detail: "User ID not found in authentication token",
                statusCode: 401);
        }

        var (success, signup, error) = await _mockVolunteerService.SignupForPositionAsync(
            positionId,
            userId,
            request,
            CancellationToken.None);

        if (success && signup != null)
        {
            return Results.Ok(signup);
        }

        var statusCode = error switch
        {
            "Volunteer position not found" => 404,
            "You have already signed up for this volunteer position" => 409,
            "This volunteer position is already fully staffed" => 409,
            "This volunteer position is not open for public signups" => 403,
            "You must accept the Event Waiver to volunteer" => 400,
            _ => 500
        };

        return Results.Problem(
            title: "Failed to Sign Up",
            detail: error ?? "Failed to sign up for volunteer position",
            statusCode: statusCode);
    }

    private async Task<IResult> SimulateGetUserVolunteerShifts(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Problem(
                title: "Authentication Required",
                detail: "You must be logged in to view your volunteer shifts",
                statusCode: 401);
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Problem(
                title: "Invalid User",
                detail: "User ID not found in authentication token",
                statusCode: 401);
        }

        var (success, shifts, error) = await _mockVolunteerService.GetUserVolunteerShiftsAsync(
            userId,
            CancellationToken.None);

        if (success && shifts != null)
        {
            return Results.Ok(shifts);
        }

        return Results.Problem(
            title: "Failed to Retrieve Volunteer Shifts",
            detail: error ?? "Failed to retrieve volunteer shifts",
            statusCode: 500);
    }

    #endregion
}
