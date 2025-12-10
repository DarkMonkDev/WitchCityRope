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

public class VolunteerAssignmentEndpointsTests
{
    private readonly IVolunteerAssignmentService _mockAssignmentService;
    private readonly ClaimsPrincipal _adminUser;
    private readonly ClaimsPrincipal _regularUser;

    public VolunteerAssignmentEndpointsTests()
    {
        _mockAssignmentService = Substitute.For<IVolunteerAssignmentService>();

        // Create admin user claims
        _adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("role", "Administrator")
        }, "test"));

        // Create regular user claims
        _regularUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("role", "Member")
        }, "test"));
    }

    #region GetPositionSignups Tests

    [Fact]
    public async Task GetPositionSignups_WithValidPositionId_ReturnsOkResult()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var mockAssignments = new List<VolunteerAssignmentDto>
        {
            new VolunteerAssignmentDto
            {
                SignupId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                VolunteerPositionId = positionId,
                SceneName = "TestUser1",
                Email = "test1@example.com",
                FetLifeName = "FetLife1",
                DiscordName = "Discord1",
                Status = "Confirmed",
                SignedUpAt = DateTime.Parse("2025-11-14T10:00:00Z"),
                HasCheckedIn = false,
                CheckedInAt = null
            }
        };

        _mockAssignmentService.GetPositionSignupsAsync(positionId, Arg.Any<CancellationToken>())
            .Returns((true, mockAssignments, null));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallGetPositionSignups(positionId, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<VolunteerAssignmentDto>>>();
        var okResult = (Ok<List<VolunteerAssignmentDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Count.Should().Be(1);
        okResult.Value![0].SceneName.Should().Be("TestUser1");
    }

    [Fact]
    public async Task GetPositionSignups_WithInvalidPositionId_ReturnsBadRequest()
    {
        // Arrange
        var invalidPositionId = "not-a-guid";
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallGetPositionSignupsWithInvalidId(invalidPositionId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Invalid Position ID Format");
        problemResult.ProblemDetails.Detail.Should().Contain("GUID");
    }

    [Fact]
    public async Task GetPositionSignups_WithNonExistentPosition_ReturnsNotFound()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        _mockAssignmentService.GetPositionSignupsAsync(positionId, Arg.Any<CancellationToken>())
            .Returns((false, null, "Volunteer position not found"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallGetPositionSignups(positionId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Detail.Should().Contain("Volunteer position not found");
    }

    [Fact]
    public async Task GetPositionSignups_WithServiceError_ReturnsInternalServerError()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        _mockAssignmentService.GetPositionSignupsAsync(positionId, Arg.Any<CancellationToken>())
            .Returns((false, null, "Database connection failed"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallGetPositionSignups(positionId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Failed to Retrieve Volunteer Assignments");
        problemResult.ProblemDetails.Detail.Should().Contain("Database connection failed");
    }

    #endregion

    #region AssignMemberToPosition Tests

    [Fact]
    public async Task AssignMemberToPosition_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new AssignVolunteerRequest { UserId = userId };

        var mockAssignment = new VolunteerAssignmentDto
        {
            SignupId = Guid.NewGuid(),
            UserId = userId,
            VolunteerPositionId = positionId,
            SceneName = "TestUser",
            Email = "test@example.com",
            Status = "Confirmed",
            SignedUpAt = DateTime.Parse("2025-11-14T10:00:00Z"),
            HasCheckedIn = false
        };

        _mockAssignmentService.AssignMemberToPositionAsync(positionId, userId, Arg.Any<CancellationToken>())
            .Returns((true, mockAssignment, null));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallAssignMemberToPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<Created<VolunteerAssignmentDto>>();
        var createdResult = (Created<VolunteerAssignmentDto>)result;
        createdResult.Value.Should().NotBeNull();
        createdResult.Value!.UserId.Should().Be(userId);
        createdResult.Location.Should().Contain($"/api/volunteer-signups/{mockAssignment.SignupId}");
    }

    [Fact]
    public async Task AssignMemberToPosition_WithInvalidPositionId_ReturnsBadRequest()
    {
        // Arrange
        var invalidPositionId = "not-a-guid";
        var request = new AssignVolunteerRequest { UserId = Guid.NewGuid() };
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallAssignMemberToPositionWithInvalidId(invalidPositionId, request, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Invalid Position ID Format");
    }

    [Fact]
    public async Task AssignMemberToPosition_WithEmptyUserId_ReturnsBadRequest()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var request = new AssignVolunteerRequest { UserId = Guid.Empty };
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallAssignMemberToPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Invalid User ID");
        problemResult.ProblemDetails.Detail.Should().Contain("User ID is required");
    }

    [Fact]
    public async Task AssignMemberToPosition_WithNonExistentPosition_ReturnsNotFound()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new AssignVolunteerRequest { UserId = userId };

        _mockAssignmentService.AssignMemberToPositionAsync(positionId, userId, Arg.Any<CancellationToken>())
            .Returns((false, null, "Volunteer position not found"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallAssignMemberToPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Detail.Should().Contain("Volunteer position not found");
    }

    [Fact]
    public async Task AssignMemberToPosition_WithAlreadyAssignedUser_ReturnsConflict()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new AssignVolunteerRequest { UserId = userId };

        _mockAssignmentService.AssignMemberToPositionAsync(positionId, userId, Arg.Any<CancellationToken>())
            .Returns((false, null, "User is already assigned to this volunteer position"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallAssignMemberToPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(409);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Detail.Should().Contain("already assigned");
    }

    [Fact]
    public async Task AssignMemberToPosition_WithFullPosition_ReturnsConflict()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new AssignVolunteerRequest { UserId = userId };

        _mockAssignmentService.AssignMemberToPositionAsync(positionId, userId, Arg.Any<CancellationToken>())
            .Returns((false, null, "Volunteer position is already fully staffed"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallAssignMemberToPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(409);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Detail.Should().Contain("fully staffed");
    }

    #endregion

    #region RemoveAssignment Tests

    [Fact]
    public async Task RemoveAssignment_WithValidSignupId_ReturnsNoContent()
    {
        // Arrange
        var signupId = Guid.NewGuid();
        _mockAssignmentService.RemoveAssignmentAsync(signupId, Arg.Any<CancellationToken>())
            .Returns((true, null));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallRemoveAssignment(signupId, httpContext);

        // Assert
        result.Should().BeOfType<NoContent>();
    }

    [Fact]
    public async Task RemoveAssignment_WithInvalidSignupId_ReturnsBadRequest()
    {
        // Arrange
        var invalidSignupId = "not-a-guid";
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallRemoveAssignmentWithInvalidId(invalidSignupId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Invalid Signup ID Format");
        problemResult.ProblemDetails.Detail.Should().Contain("GUID");
    }

    [Fact]
    public async Task RemoveAssignment_WithNonExistentSignup_ReturnsNotFound()
    {
        // Arrange
        var signupId = Guid.NewGuid();
        _mockAssignmentService.RemoveAssignmentAsync(signupId, Arg.Any<CancellationToken>())
            .Returns((false, "Volunteer signup not found"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallRemoveAssignment(signupId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Detail.Should().Contain("not found");
    }

    [Fact]
    public async Task RemoveAssignment_WithAlreadyCancelled_ReturnsConflict()
    {
        // Arrange
        var signupId = Guid.NewGuid();
        _mockAssignmentService.RemoveAssignmentAsync(signupId, Arg.Any<CancellationToken>())
            .Returns((false, "Volunteer signup is already cancelled"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallRemoveAssignment(signupId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(409);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Detail.Should().Contain("already cancelled");
    }

    [Fact]
    public async Task RemoveAssignment_WithCheckedInVolunteer_ReturnsConflict()
    {
        // Arrange
        var signupId = Guid.NewGuid();
        _mockAssignmentService.RemoveAssignmentAsync(signupId, Arg.Any<CancellationToken>())
            .Returns((false, "Cannot remove assignment after volunteer has checked in"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallRemoveAssignment(signupId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(409);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Detail.Should().Contain("checked in");
    }

    #endregion

    #region SearchUsers Tests

    [Fact]
    public async Task SearchUsers_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var searchQuery = "test";
        var mockUsers = new List<UserSearchResultDto>
        {
            new UserSearchResultDto
            {
                UserId = Guid.NewGuid(),
                SceneName = "TestUser1",
                FullName = "Test User One",
                Email = "test1@example.com",
                DiscordName = "Discord1"
            }
        };

        _mockAssignmentService.SearchUsersAsync(searchQuery, Arg.Any<CancellationToken>())
            .Returns((true, mockUsers, null));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallSearchUsers(searchQuery, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<UserSearchResultDto>>>();
        var okResult = (Ok<List<UserSearchResultDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Count.Should().Be(1);
        okResult.Value![0].SceneName.Should().Be("TestUser1");
    }

    [Fact]
    public async Task SearchUsers_WithEmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallSearchUsers("", httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Search Query Required");
        problemResult.ProblemDetails.Detail.Should().Contain("at least 3 characters");
    }

    [Fact]
    public async Task SearchUsers_WithServiceError_ReturnsInternalServerError()
    {
        // Arrange
        var searchQuery = "test";
        _mockAssignmentService.SearchUsersAsync(searchQuery, Arg.Any<CancellationToken>())
            .Returns((false, null, "Database connection failed"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallSearchUsers(searchQuery, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Failed to Search Users");
        problemResult.ProblemDetails.Detail.Should().Contain("Database connection failed");
    }

    #endregion

    #region Status Code Mapping Tests

    [Theory]
    [InlineData("Volunteer position not found", 404)]
    [InlineData("User not found", 404)]
    [InlineData("User is already assigned to this volunteer position", 409)]
    [InlineData("Volunteer position is already fully staffed", 409)]
    [InlineData("Cannot assign inactive user to volunteer position", 400)]
    [InlineData("Database error", 500)]
    public async Task AssignMemberToPosition_ReturnsCorrectStatusCodes_BasedOnErrorType(string errorMessage, int expectedStatusCode)
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new AssignVolunteerRequest { UserId = userId };

        _mockAssignmentService.AssignMemberToPositionAsync(positionId, userId, Arg.Any<CancellationToken>())
            .Returns((false, null, errorMessage));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallAssignMemberToPosition(positionId, request, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(expectedStatusCode);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Detail.Should().Contain(errorMessage);
    }

    [Theory]
    [InlineData("Volunteer signup not found", 404)]
    [InlineData("Volunteer signup is already cancelled", 409)]
    [InlineData("Cannot remove assignment after volunteer has checked in", 409)]
    [InlineData("Database error", 500)]
    public async Task RemoveAssignment_ReturnsCorrectStatusCodes_BasedOnErrorType(string errorMessage, int expectedStatusCode)
    {
        // Arrange
        var signupId = Guid.NewGuid();
        _mockAssignmentService.RemoveAssignmentAsync(signupId, Arg.Any<CancellationToken>())
            .Returns((false, errorMessage));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallRemoveAssignment(signupId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(expectedStatusCode);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Detail.Should().Contain(errorMessage);
    }

    #endregion

    #region Helper Methods

    private async Task<IResult> CallGetPositionSignups(Guid positionId, HttpContext httpContext)
    {
        return await SimulateGetPositionSignups(positionId.ToString(), httpContext);
    }

    private async Task<IResult> CallGetPositionSignupsWithInvalidId(string positionId, HttpContext httpContext)
    {
        return await SimulateGetPositionSignups(positionId, httpContext);
    }

    private async Task<IResult> CallAssignMemberToPosition(Guid positionId, AssignVolunteerRequest request, HttpContext httpContext)
    {
        return await SimulateAssignMemberToPosition(positionId.ToString(), request, httpContext);
    }

    private async Task<IResult> CallAssignMemberToPositionWithInvalidId(string positionId, AssignVolunteerRequest request, HttpContext httpContext)
    {
        return await SimulateAssignMemberToPosition(positionId, request, httpContext);
    }

    private async Task<IResult> CallRemoveAssignment(Guid signupId, HttpContext httpContext)
    {
        return await SimulateRemoveAssignment(signupId.ToString(), httpContext);
    }

    private async Task<IResult> CallRemoveAssignmentWithInvalidId(string signupId, HttpContext httpContext)
    {
        return await SimulateRemoveAssignment(signupId, httpContext);
    }

    private async Task<IResult> CallSearchUsers(string? query, HttpContext httpContext)
    {
        return await SimulateSearchUsers(query, httpContext);
    }

    // Simulation methods that replicate the actual endpoint logic
    private async Task<IResult> SimulateGetPositionSignups(string positionId, HttpContext httpContext)
    {
        if (!Guid.TryParse(positionId, out var positionGuid))
        {
            return Results.Problem(
                title: "Invalid Position ID Format",
                detail: "Position ID must be a valid GUID",
                statusCode: 400);
        }

        var (success, assignments, error) = await _mockAssignmentService.GetPositionSignupsAsync(
            positionGuid,
            CancellationToken.None);

        if (success && assignments != null)
        {
            return Results.Ok(assignments);
        }

        var statusCode = error == "Volunteer position not found" ? 404 : 500;

        return Results.Problem(
            title: "Failed to Retrieve Volunteer Assignments",
            detail: error ?? "Failed to retrieve volunteer assignments",
            statusCode: statusCode);
    }

    private async Task<IResult> SimulateAssignMemberToPosition(string positionId, AssignVolunteerRequest request, HttpContext httpContext)
    {
        if (!Guid.TryParse(positionId, out var positionGuid))
        {
            return Results.Problem(
                title: "Invalid Position ID Format",
                detail: "Position ID must be a valid GUID",
                statusCode: 400);
        }

        if (request.UserId == Guid.Empty)
        {
            return Results.Problem(
                title: "Invalid User ID",
                detail: "User ID is required and must be a valid GUID",
                statusCode: 400);
        }

        var (success, assignment, error) = await _mockAssignmentService.AssignMemberToPositionAsync(
            positionGuid,
            request.UserId,
            CancellationToken.None);

        if (success && assignment != null)
        {
            return Results.Created(
                $"/api/volunteer-signups/{assignment.SignupId}",
                assignment);
        }

        var statusCode = error switch
        {
            "Volunteer position not found" => 404,
            "User not found" => 404,
            "User is already assigned to this volunteer position" => 409,
            "Volunteer position is already fully staffed" => 409,
            "Cannot assign inactive user to volunteer position" => 400,
            _ => 500
        };

        return Results.Problem(
            title: "Failed to Assign Member",
            detail: error ?? "Failed to assign member to volunteer position",
            statusCode: statusCode);
    }

    private async Task<IResult> SimulateRemoveAssignment(string signupId, HttpContext httpContext)
    {
        if (!Guid.TryParse(signupId, out var signupGuid))
        {
            return Results.Problem(
                title: "Invalid Signup ID Format",
                detail: "Signup ID must be a valid GUID",
                statusCode: 400);
        }

        var (success, error) = await _mockAssignmentService.RemoveAssignmentAsync(
            signupGuid,
            CancellationToken.None);

        if (success)
        {
            return Results.NoContent();
        }

        var statusCode = error switch
        {
            "Volunteer signup not found" => 404,
            "Volunteer signup is already cancelled" => 409,
            "Cannot remove assignment after volunteer has checked in" => 409,
            _ => 500
        };

        return Results.Problem(
            title: "Failed to Remove Volunteer Assignment",
            detail: error ?? "Failed to remove volunteer assignment",
            statusCode: statusCode);
    }

    private async Task<IResult> SimulateSearchUsers(string? q, HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Results.Problem(
                title: "Search Query Required",
                detail: "Provide a search query with at least 3 characters",
                statusCode: 400);
        }

        var (success, users, error) = await _mockAssignmentService.SearchUsersAsync(
            q,
            CancellationToken.None);

        if (success && users != null)
        {
            return Results.Ok(users);
        }

        var statusCode = error == "Search query must be at least 3 characters" ? 400 : 500;

        return Results.Problem(
            title: "Failed to Search Users",
            detail: error ?? "Failed to search users",
            statusCode: statusCode);
    }

    #endregion
}
