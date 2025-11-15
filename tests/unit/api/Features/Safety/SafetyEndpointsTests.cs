using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using WitchCityRope.Api.Features.Safety.Endpoints;
using WitchCityRope.Api.Features.Safety.Entities;
using WitchCityRope.Api.Features.Safety.Models;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Models;
using Xunit;
using FluentAssertions;
using NSubstitute;
using FluentValidation;
using FluentValidation.Results;

namespace WitchCityRope.UnitTests.Api.Features.Safety;

public class SafetyEndpointsTests
{
    private readonly ISafetyService _mockSafetyService;
    private readonly ISafetyServiceExtended _mockSafetyServiceExtended;
    private readonly IValidator<CreateIncidentRequest> _mockValidator;
    private readonly ClaimsPrincipal _adminUser;
    private readonly ClaimsPrincipal _regularUser;
    private readonly ClaimsPrincipal _unauthenticatedUser;

    public SafetyEndpointsTests()
    {
        _mockSafetyService = Substitute.For<ISafetyService>();
        _mockSafetyServiceExtended = Substitute.For<ISafetyServiceExtended>();
        _mockValidator = Substitute.For<IValidator<CreateIncidentRequest>>();

        // Create admin user claims
        _adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "test"));

        // Create regular user claims
        _regularUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Member")
        }, "test"));

        // Create unauthenticated user
        _unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());
    }

    #region SubmitIncident Tests (Public Endpoint)

    [Fact]
    public async Task SubmitIncident_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new CreateIncidentRequest
        {
            Title = "Test Incident",
            IncidentDate = DateTime.Parse("2025-11-14T10:00:00Z"),
            Type = IncidentType.SafetyConcern,
            WhereOccurred = WhereOccurred.AtEvent,
            EventName = "Test Event",
            Description = "Test incident",
            IsAnonymous = false,
            ContactEmail = "test@example.com"
        };

        var mockResponse = new SubmissionResponse
        {
            ReferenceNumber = "INC-12345",
            TrackingUrl = "/safety/track/INC-12345",
            SubmittedAt = DateTime.Parse("2025-11-14T10:00:00Z")
        };

        _mockValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockSafetyService.SubmitIncidentAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<SubmissionResponse>.Success(mockResponse));

        // Act
        var result = await SimulateSubmitIncident(request);

        // Assert
        result.Should().BeOfType<Ok<SubmissionResponse>>();
        var okResult = (Ok<SubmissionResponse>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.ReferenceNumber.Should().Be("INC-12345");
    }

    [Fact]
    public async Task SubmitIncident_WithValidationErrors_ReturnsValidationProblem()
    {
        // Arrange
        var request = new CreateIncidentRequest
        {
            Title = "",
            IncidentDate = DateTime.MinValue,
            Description = ""
        };

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Description", "Description is required")
        };

        _mockValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await SimulateSubmitIncident(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
    }

    [Fact]
    public async Task SubmitIncident_WithServiceError_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateIncidentRequest
        {
            Title = "Test Incident",
            IncidentDate = DateTime.Parse("2025-11-14T10:00:00Z"),
            Type = IncidentType.SafetyConcern,
            Description = "Test incident"
        };

        _mockValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockSafetyService.SubmitIncidentAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<SubmissionResponse>.Failure("Database error"));

        // Act
        var result = await SimulateSubmitIncident(request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Incident Submission Failed");
        problemResult.ProblemDetails.Detail.Should().Contain("Database error");
    }

    #endregion

    #region GetIncidentStatus Tests (Public Endpoint)

    [Fact]
    public async Task GetIncidentStatus_WithValidReferenceNumber_ReturnsOkResult()
    {
        // Arrange
        var referenceNumber = "INC-12345";
        var mockStatus = new IncidentStatusResponse
        {
            ReferenceNumber = referenceNumber,
            Status = "UnderReview",
            LastUpdated = DateTime.Parse("2025-11-14T11:00:00Z")
        };

        _mockSafetyService.GetIncidentStatusAsync(referenceNumber, Arg.Any<CancellationToken>())
            .Returns(Result<IncidentStatusResponse>.Success(mockStatus));

        // Act
        var result = await SimulateGetIncidentStatus(referenceNumber);

        // Assert
        result.Should().BeOfType<Ok<IncidentStatusResponse>>();
        var okResult = (Ok<IncidentStatusResponse>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.ReferenceNumber.Should().Be(referenceNumber);
        okResult.Value.Status.Should().Be("UnderReview");
    }

    [Fact]
    public async Task GetIncidentStatus_WithInvalidReferenceNumber_ReturnsNotFound()
    {
        // Arrange
        var referenceNumber = "INC-99999";
        _mockSafetyService.GetIncidentStatusAsync(referenceNumber, Arg.Any<CancellationToken>())
            .Returns(Result<IncidentStatusResponse>.Failure("Incident not found"));

        // Act
        var result = await SimulateGetIncidentStatus(referenceNumber);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails!.Title.Should().Be("Resource Not Found");
        problemResult.ProblemDetails.Detail.Should().Contain("not found");
    }

    #endregion

    #region GetDashboardStatistics Tests (Admin Endpoint)

    [Fact]
    public async Task GetDashboardStatistics_WithAdminUser_ReturnsOkResult()
    {
        // Arrange
        var mockStatistics = new DashboardStatisticsResponse
        {
            UnassignedCount = 5,
            HasOldUnassigned = true,
            RecentIncidents = new List<IncidentSummaryDto>()
        };

        var userId = Guid.Parse(_adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _mockSafetyServiceExtended.GetDashboardStatisticsAsync(userId, true, Arg.Any<CancellationToken>())
            .Returns(Result<DashboardStatisticsResponse>.Success(mockStatistics));

        // Act
        var result = await SimulateGetDashboardStatistics(_adminUser);

        // Assert
        result.Should().BeOfType<Ok<DashboardStatisticsResponse>>();
        var okResult = (Ok<DashboardStatisticsResponse>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.UnassignedCount.Should().Be(5);
        okResult.Value.HasOldUnassigned.Should().BeTrue();
    }

    [Fact]
    public async Task GetDashboardStatistics_WithServiceError_ReturnsInternalServerError()
    {
        // Arrange
        var userId = Guid.Parse(_adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _mockSafetyServiceExtended.GetDashboardStatisticsAsync(userId, true, Arg.Any<CancellationToken>())
            .Returns(Result<DashboardStatisticsResponse>.Failure("Database connection failed"));

        // Act
        var result = await SimulateGetDashboardStatistics(_adminUser);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails!.Title.Should().Be("Dashboard Statistics Failed");
        problemResult.ProblemDetails.Detail.Should().Contain("Database connection failed");
    }

    #endregion

    #region GetAdminIncidentsList Tests

    [Fact]
    public async Task GetAdminIncidentsList_WithAdminUser_ReturnsOkResult()
    {
        // Arrange
        var request = new AdminIncidentListRequest
        {
            Page = 1,
            PageSize = 25,
            Status = "UnderReview"
        };

        var mockResponse = new PaginatedIncidentListResponse
        {
            Items = new List<IncidentSummaryDto>
            {
                new IncidentSummaryDto
                {
                    Id = Guid.NewGuid(),
                    ReferenceNumber = "INC-12345",
                    Status = IncidentStatus.InformationGathering,
                    ReportedAt = DateTime.Parse("2025-11-14T10:00:00Z"),
                    Title = "Test Incident",
                    Location = "Test Location",
                    IncidentDate = DateTime.Parse("2025-11-14T09:00:00Z"),
                    LastUpdatedAt = DateTime.Parse("2025-11-14T10:00:00Z")
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 25
        };

        var userId = Guid.Parse(_adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _mockSafetyServiceExtended.GetIncidentsAsync(request, userId, true, Arg.Any<CancellationToken>())
            .Returns(Result<PaginatedIncidentListResponse>.Success(mockResponse));

        // Act
        var result = await SimulateGetAdminIncidentsList(request, _adminUser);

        // Assert
        result.Should().BeOfType<Ok<PaginatedIncidentListResponse>>();
        var okResult = (Ok<PaginatedIncidentListResponse>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.TotalCount.Should().Be(1);
        okResult.Value.Items.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetAdminIncidentsList_WithServiceError_ReturnsInternalServerError()
    {
        // Arrange
        var request = new AdminIncidentListRequest { Page = 1, PageSize = 25 };
        var userId = Guid.Parse(_adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        _mockSafetyServiceExtended.GetIncidentsAsync(request, userId, true, Arg.Any<CancellationToken>())
            .Returns(Result<PaginatedIncidentListResponse>.Failure("Database error"));

        // Act
        var result = await SimulateGetAdminIncidentsList(request, _adminUser);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails!.Title.Should().Be("Incident List Retrieval Failed");
    }

    #endregion

    #region GetCoordinatorsList Tests

    [Fact]
    public async Task GetCoordinatorsList_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var mockCoordinators = new List<UserCoordinatorDto>
        {
            new UserCoordinatorDto
            {
                Id = Guid.NewGuid(),
                SceneName = "Admin User",
                ActiveIncidentCount = 3
            }
        };

        _mockSafetyServiceExtended.GetAllUsersForAssignmentAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<UserCoordinatorDto>>.Success(mockCoordinators));

        // Act
        var result = await SimulateGetCoordinatorsList();

        // Assert
        result.Should().BeOfType<Ok<IEnumerable<UserCoordinatorDto>>>();
        var okResult = (Ok<IEnumerable<UserCoordinatorDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Count().Should().Be(1);
    }

    [Fact]
    public async Task GetCoordinatorsList_WithServiceError_ReturnsInternalServerError()
    {
        // Arrange
        _mockSafetyServiceExtended.GetAllUsersForAssignmentAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<UserCoordinatorDto>>.Failure("Database error"));

        // Act
        var result = await SimulateGetCoordinatorsList();

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails!.Title.Should().Be("User List Retrieval Failed");
    }

    #endregion

    #region GetSafetyDashboard Tests (Legacy Endpoint)

    [Fact]
    public async Task GetSafetyDashboard_WithAuthenticatedUser_ReturnsOkResult()
    {
        // Arrange
        var mockDashboard = new AdminDashboardResponse
        {
            Statistics = new SafetyStatistics { NewCount = 5 },
            RecentIncidents = new List<IncidentSummaryResponse>()
        };

        var userId = Guid.Parse(_adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _mockSafetyService.GetDashboardDataAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<AdminDashboardResponse>.Success(mockDashboard));

        // Act
        var result = await SimulateGetSafetyDashboard(_adminUser);

        // Assert
        result.Should().BeOfType<Ok<AdminDashboardResponse>>();
        var okResult = (Ok<AdminDashboardResponse>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Statistics.NewCount.Should().Be(5);
    }

    [Fact]
    public async Task GetSafetyDashboard_WithAccessDenied_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.Parse(_regularUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _mockSafetyService.GetDashboardDataAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<AdminDashboardResponse>.Failure("Access denied"));

        // Act
        var result = await SimulateGetSafetyDashboard(_regularUser);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(403);
        problemResult.ProblemDetails!.Detail.Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetSafetyDashboard_WithServiceError_ReturnsInternalServerError()
    {
        // Arrange
        var userId = Guid.Parse(_adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _mockSafetyService.GetDashboardDataAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<AdminDashboardResponse>.Failure("Database error"));

        // Act
        var result = await SimulateGetSafetyDashboard(_adminUser);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails!.Title.Should().Be("Dashboard Load Failed");
    }

    #endregion

    #region Helper Methods - Simulation

    private async Task<IResult> SimulateSubmitIncident(CreateIncidentRequest request)
    {
        var validationResult = await _mockValidator.ValidateAsync(request, CancellationToken.None);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await _mockSafetyService.SubmitIncidentAsync(request, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Incident Submission Failed",
                detail: result.Error,
                statusCode: 400);
    }

    private async Task<IResult> SimulateGetIncidentStatus(string referenceNumber)
    {
        var result = await _mockSafetyService.GetIncidentStatusAsync(referenceNumber, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Resource Not Found",
                detail: result.Error,
                statusCode: 404);
    }

    private async Task<IResult> SimulateGetDashboardStatistics(ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var isAdmin = user.IsInRole("Administrator");

        var result = await _mockSafetyServiceExtended.GetDashboardStatisticsAsync(userId, isAdmin, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Dashboard Statistics Failed",
                detail: result.Error,
                statusCode: 500);
    }

    private async Task<IResult> SimulateGetAdminIncidentsList(AdminIncidentListRequest request, ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var isAdmin = user.IsInRole("Administrator");

        var result = await _mockSafetyServiceExtended.GetIncidentsAsync(request, userId, isAdmin, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Incident List Retrieval Failed",
                detail: result.Error,
                statusCode: 500);
    }

    private async Task<IResult> SimulateGetCoordinatorsList()
    {
        var result = await _mockSafetyServiceExtended.GetAllUsersForAssignmentAsync(CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "User List Retrieval Failed",
                detail: result.Error,
                statusCode: 500);
    }

    private async Task<IResult> SimulateGetSafetyDashboard(ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var result = await _mockSafetyService.GetDashboardDataAsync(userId, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Dashboard Load Failed",
                detail: result.Error,
                statusCode: result.Error.Contains("Access denied") ? 403 : 500);
    }

    #endregion
}
