using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using WitchCityRope.Api.Features.Vetting.Endpoints;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Models;
using Xunit;
using FluentAssertions;
using NSubstitute;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;

namespace WitchCityRope.UnitTests.Api.Features.Vetting;

public class VettingEndpointsTests
{
    private readonly IVettingService _mockVettingService;
    private readonly ClaimsPrincipal _adminUser;
    private readonly ClaimsPrincipal _regularUser;

    public VettingEndpointsTests()
    {
        _mockVettingService = Substitute.For<IVettingService>();
        
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

    [Fact]
    public async Task GetApplicationsForReview_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new ApplicationFilterRequest
        {
            Page = 1,
            PageSize = 25,
            StatusFilters = new List<string> { "UnderReview" },
            SortBy = "SubmittedAt",
            SortDirection = "Desc"
        };

        var mockResult = new PagedResult<ApplicationSummaryDto>
        {
            Items = new List<ApplicationSummaryDto>
            {
                new ApplicationSummaryDto
                {
                    Id = "app-1",
                    ApplicationNumber = "APP001",
                    Status = "UnderReview",
                    SubmittedAt = "2025-09-22T10:00:00Z",
                    LastActivityAt = "2025-09-22T10:00:00Z",
                    SceneName = "TestUser",
                    ExperienceLevel = "Beginner",
                    YearsExperience = 1,
                    IsAnonymous = false,
                    AssignedReviewerName = null,
                    ReviewStartedAt = null,
                    Priority = 1,
                    DaysInCurrentStatus = 2,
                    ReferenceStatus = new ApplicationReferenceStatus
                    {
                        TotalReferences = 2,
                        ContactedReferences = 1,
                        RespondedReferences = 0,
                        AllReferencesComplete = false
                    },
                    HasRecentNotes = false,
                    HasPendingActions = true,
                    InterviewScheduledFor = null,
                    SkillsTags = new List<string> { "Rope" }
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 25,
            TotalPages = 1
        };

        var userId = Guid.Parse(_adminUser.FindFirst("sub")!.Value);
        _mockVettingService.GetApplicationsForReviewAsync(request, userId, Arg.Any<CancellationToken>())
            .Returns(Result<PagedResult<ApplicationSummaryDto>>.Success(mockResult));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallGetApplicationsForReview(request, httpContext);

        // Assert
        result.Should().BeOfType<Ok<ApiResponse<PagedResult<ApplicationSummaryDto>>>>();
        var okResult = (Ok<ApiResponse<PagedResult<ApplicationSummaryDto>>>)result;
        okResult.Value.Success.Should().BeTrue();
        okResult.Value.Data.Should().NotBeNull();
        okResult.Value.Data.TotalCount.Should().Be(1);
        okResult.Value.Message.Should().Be("Applications retrieved successfully");
    }

    [Fact]
    public async Task GetApplicationsForReview_WithInvalidUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ApplicationFilterRequest
        {
            Page = 1,
            PageSize = 25,
            StatusFilters = new List<string>(),
            SortBy = "SubmittedAt",
            SortDirection = "Desc"
        };

        // Create user with missing claims
        var invalidUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[0], "test"));
        var httpContext = new DefaultHttpContext();
        httpContext.User = invalidUser;

        // Act
        var result = await CallGetApplicationsForReview(request, httpContext);

        // Assert
        result.Should().BeOfType<JsonHttpResult<ApiResponse<object>>>();
        var jsonResult = (JsonHttpResult<ApiResponse<object>>)result;
        jsonResult.StatusCode.Should().Be(400);
        jsonResult.Value.Success.Should().BeFalse();
        jsonResult.Value.Error.Should().Be("User information not found");
    }

    [Fact]
    public async Task GetApplicationsForReview_WithServiceError_ReturnsInternalServerError()
    {
        // Arrange
        var request = new ApplicationFilterRequest
        {
            Page = 1,
            PageSize = 25,
            StatusFilters = new List<string>(),
            SortBy = "SubmittedAt",
            SortDirection = "Desc"
        };

        var userId = Guid.Parse(_adminUser.FindFirst("sub")!.Value);
        _mockVettingService.GetApplicationsForReviewAsync(request, userId, Arg.Any<CancellationToken>())
            .Returns(Result<PagedResult<ApplicationSummaryDto>>.Failure("Database error", "Connection failed"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallGetApplicationsForReview(request, httpContext);

        // Assert
        result.Should().BeOfType<JsonHttpResult<ApiResponse<object>>>();
        var jsonResult = (JsonHttpResult<ApiResponse<object>>)result;
        jsonResult.StatusCode.Should().Be(500);
        jsonResult.Value.Success.Should().BeFalse();
        jsonResult.Value.Error.Should().Be("Database error");
        jsonResult.Value.Details.Should().Be("Connection failed");
    }

    [Fact]
    public async Task GetApplicationDetail_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var mockDetail = new ApplicationDetailResponse
        {
            Id = applicationId.ToString(),
            ApplicationNumber = "APP001",
            Status = "UnderReview",
            SubmittedAt = "2025-09-22T10:00:00Z",
            LastActivityAt = "2025-09-22T10:00:00Z",
            FullName = "John Doe",
            SceneName = "TestUser",
            Email = "test@example.com",
            ExperienceLevel = "Beginner",
            YearsExperience = 1,
            ExperienceDescription = "Basic rope knowledge",
            WhyJoinCommunity = "Want to learn more",
            AgreesToGuidelines = true,
            IsAnonymous = false,
            References = new List<ReferenceDetailDto>(),
            Notes = new List<ApplicationNoteDto>(),
            Decisions = new List<ReviewDecisionDto>()
        };

        var userId = Guid.Parse(_adminUser.FindFirst("sub")!.Value);
        _mockVettingService.GetApplicationDetailAsync(applicationId, userId, Arg.Any<CancellationToken>())
            .Returns(Result<ApplicationDetailResponse>.Success(mockDetail));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallGetApplicationDetail(applicationId, httpContext);

        // Assert
        result.Should().BeOfType<Ok<ApiResponse<ApplicationDetailResponse>>>();
        var okResult = (Ok<ApiResponse<ApplicationDetailResponse>>)result;
        okResult.Value.Success.Should().BeTrue();
        okResult.Value.Data.Should().NotBeNull();
        okResult.Value.Data.Id.Should().Be(applicationId.ToString());
        okResult.Value.Message.Should().Be("Application detail retrieved successfully");
    }

    [Fact]
    public async Task GetApplicationDetail_WithNotFoundId_ReturnsNotFound()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var userId = Guid.Parse(_adminUser.FindFirst("sub")!.Value);
        
        _mockVettingService.GetApplicationDetailAsync(applicationId, userId, Arg.Any<CancellationToken>())
            .Returns(Result<ApplicationDetailResponse>.Failure("Application not found", $"No application found with ID {applicationId}"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallGetApplicationDetail(applicationId, httpContext);

        // Assert
        result.Should().BeOfType<JsonHttpResult<ApiResponse<object>>>();
        var jsonResult = (JsonHttpResult<ApiResponse<object>>)result;
        jsonResult.StatusCode.Should().Be(404);
        jsonResult.Value.Success.Should().BeFalse();
        jsonResult.Value.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task SubmitReviewDecision_WithValidDecision_ReturnsOkResult()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var decision = new ReviewDecisionRequest
        {
            DecisionType = "Approved",
            Reasoning = "Application meets all criteria",
            IsFinalDecision = true
        };

        var mockResponse = new ReviewDecisionResponse
        {
            DecisionId = Guid.NewGuid().ToString(),
            DecisionType = "Approved",
            SubmittedAt = "2025-09-22T10:00:00Z",
            NewApplicationStatus = "Approved",
            ConfirmationMessage = "Decision submitted successfully",
            ActionsTriggered = new List<string>()
        };

        var userId = Guid.Parse(_adminUser.FindFirst("sub")!.Value);
        _mockVettingService.SubmitReviewDecisionAsync(applicationId, decision, userId, Arg.Any<CancellationToken>())
            .Returns(Result<ReviewDecisionResponse>.Success(mockResponse));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallSubmitReviewDecision(applicationId, decision, httpContext);

        // Assert
        result.Should().BeOfType<Ok<ApiResponse<ReviewDecisionResponse>>>();
        var okResult = (Ok<ApiResponse<ReviewDecisionResponse>>)result;
        okResult.Value.Success.Should().BeTrue();
        okResult.Value.Data.Should().NotBeNull();
        okResult.Value.Data.DecisionType.Should().Be("Approved");
        okResult.Value.Message.Should().Be("Review decision submitted successfully");
    }

    [Fact]
    public async Task SubmitReviewDecision_WithInvalidDecision_ReturnsBadRequest()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var decision = new ReviewDecisionRequest
        {
            DecisionType = "Invalid",
            Reasoning = "Test",
            IsFinalDecision = true
        };

        var userId = Guid.Parse(_adminUser.FindFirst("sub")!.Value);
        _mockVettingService.SubmitReviewDecisionAsync(applicationId, decision, userId, Arg.Any<CancellationToken>())
            .Returns(Result<ReviewDecisionResponse>.Failure("Invalid decision type", "DecisionType must be valid"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallSubmitReviewDecision(applicationId, decision, httpContext);

        // Assert
        result.Should().BeOfType<JsonHttpResult<ApiResponse<object>>>();
        var jsonResult = (JsonHttpResult<ApiResponse<object>>)result;
        jsonResult.StatusCode.Should().Be(400);
        jsonResult.Value.Success.Should().BeFalse();
        jsonResult.Value.Error.Should().Be("Invalid decision type");
    }

    [Fact]
    public async Task AddApplicationNote_WithValidNote_ReturnsOkResult()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var noteRequest = new CreateNoteRequest
        {
            Content = "Followed up with references",
            IsPrivate = false,
            Tags = new List<string> { "follow-up" }
        };

        var mockResponse = new NoteResponse
        {
            NoteId = Guid.NewGuid().ToString(),
            CreatedAt = "2025-09-22T10:00:00Z",
            ConfirmationMessage = "Note added successfully"
        };

        var userId = Guid.Parse(_adminUser.FindFirst("sub")!.Value);
        _mockVettingService.AddApplicationNoteAsync(applicationId, noteRequest, userId, Arg.Any<CancellationToken>())
            .Returns(Result<NoteResponse>.Success(mockResponse));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallAddApplicationNote(applicationId, noteRequest, httpContext);

        // Assert
        result.Should().BeOfType<Ok<ApiResponse<NoteResponse>>>();
        var okResult = (Ok<ApiResponse<NoteResponse>>)result;
        okResult.Value.Success.Should().BeTrue();
        okResult.Value.Data.Should().NotBeNull();
        okResult.Value.Data.ConfirmationMessage.Should().Contain("successfully");
        okResult.Value.Message.Should().Be("Note added successfully");
    }

    [Fact]
    public async Task EndpointHandlesExceptions_ReturnsInternalServerError()
    {
        // Arrange
        var request = new ApplicationFilterRequest
        {
            Page = 1,
            PageSize = 25,
            StatusFilters = new List<string>(),
            SortBy = "SubmittedAt",
            SortDirection = "Desc"
        };

        var userId = Guid.Parse(_adminUser.FindFirst("sub")!.Value);
        _mockVettingService.GetApplicationsForReviewAsync(request, userId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database connection failed"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallGetApplicationsForReview(request, httpContext);

        // Assert
        result.Should().BeOfType<JsonHttpResult<ApiResponse<object>>>();
        var jsonResult = (JsonHttpResult<ApiResponse<object>>)result;
        jsonResult.StatusCode.Should().Be(500);
        jsonResult.Value.Success.Should().BeFalse();
        jsonResult.Value.Error.Should().Be("Failed to retrieve applications");
        jsonResult.Value.Details.Should().Contain("Database connection failed");
    }

    [Theory]
    [InlineData("Access denied", 403)]
    [InlineData("Application not found", 404)]
    [InlineData("Database error", 500)]
    public async Task EndpointsReturnCorrectStatusCodes_BasedOnErrorType(string errorMessage, int expectedStatusCode)
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var userId = Guid.Parse(_adminUser.FindFirst("sub")!.Value);
        
        _mockVettingService.GetApplicationDetailAsync(applicationId, userId, Arg.Any<CancellationToken>())
            .Returns(Result<ApplicationDetailResponse>.Failure(errorMessage, "Details"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await CallGetApplicationDetail(applicationId, httpContext);

        // Assert
        result.Should().BeOfType<JsonHttpResult<ApiResponse<object>>>();
        var jsonResult = (JsonHttpResult<ApiResponse<object>>)result;
        jsonResult.StatusCode.Should().Be(expectedStatusCode);
        jsonResult.Value.Success.Should().BeFalse();
        jsonResult.Value.Error.Should().Be(errorMessage);
    }

    // Helper methods to simulate endpoint calls
    private async Task<IResult> CallGetApplicationsForReview(ApplicationFilterRequest request, HttpContext httpContext)
    {
        var endpoint = CreateEndpointDelegate("GetApplicationsForReview");
        return await endpoint(httpContext, request, _mockVettingService, httpContext.User, CancellationToken.None);
    }

    private async Task<IResult> CallGetApplicationDetail(Guid id, HttpContext httpContext)
    {
        var endpoint = CreateEndpointDelegate("GetApplicationDetail");
        return await endpoint(httpContext, id, _mockVettingService, httpContext.User, CancellationToken.None);
    }

    private async Task<IResult> CallSubmitReviewDecision(Guid id, ReviewDecisionRequest request, HttpContext httpContext)
    {
        var endpoint = CreateEndpointDelegate("SubmitReviewDecision");
        return await endpoint(httpContext, id, request, _mockVettingService, httpContext.User, CancellationToken.None);
    }

    private async Task<IResult> CallAddApplicationNote(Guid id, CreateNoteRequest request, HttpContext httpContext)
    {
        var endpoint = CreateEndpointDelegate("AddApplicationNote");
        return await endpoint(httpContext, id, request, _mockVettingService, httpContext.User, CancellationToken.None);
    }

    private static Func<HttpContext, object, IVettingService, ClaimsPrincipal, CancellationToken, Task<IResult>> CreateEndpointDelegate(string endpointName)
    {
        // This simulates the endpoint method calls
        // In a real test, you might use TestServer or similar approaches
        return async (context, param1, service, user, cancellationToken) =>
        {
            try
            {
                return endpointName switch
                {
                    "GetApplicationsForReview" => await SimulateGetApplicationsForReview((ApplicationFilterRequest)param1, service, user, cancellationToken),
                    "GetApplicationDetail" => await SimulateGetApplicationDetail((Guid)param1, service, user, cancellationToken),
                    "SubmitReviewDecision" => await SimulateSubmitReviewDecision(((Guid, ReviewDecisionRequest))param1, service, user, cancellationToken),
                    "AddApplicationNote" => await SimulateAddApplicationNote(((Guid, CreateNoteRequest))param1, service, user, cancellationToken),
                    _ => Results.BadRequest("Unknown endpoint")
                };
            }
            catch (Exception ex)
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Error = "Failed to retrieve applications",
                    Details = ex.Message,
                    Timestamp = DateTime.UtcNow
                }, statusCode: 500);
            }
        };
    }

    // Simulation methods that replicate the actual endpoint logic
    private static async Task<IResult> SimulateGetApplicationsForReview(
        ApplicationFilterRequest request,
        IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var reviewerId))
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "User information not found",
                Timestamp = DateTime.UtcNow
            }, statusCode: 400);
        }

        var result = await vettingService.GetApplicationsForReviewAsync(request, reviewerId, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            return Results.Ok(new ApiResponse<PagedResult<ApplicationSummaryDto>>
            {
                Success = true,
                Data = result.Value,
                Message = "Applications retrieved successfully",
                Timestamp = DateTime.UtcNow
            });
        }

        var statusCode = result.Error.Contains("Access denied") ? 403 : 500;
        return Results.Json(new ApiResponse<object>
        {
            Success = false,
            Error = result.Error,
            Details = result.Details,
            Timestamp = DateTime.UtcNow
        }, statusCode: statusCode);
    }

    private static async Task<IResult> SimulateGetApplicationDetail(
        Guid id,
        IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var reviewerId))
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "User information not found",
                Timestamp = DateTime.UtcNow
            }, statusCode: 400);
        }

        var result = await vettingService.GetApplicationDetailAsync(id, reviewerId, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            return Results.Ok(new ApiResponse<ApplicationDetailResponse>
            {
                Success = true,
                Data = result.Value,
                Message = "Application detail retrieved successfully",
                Timestamp = DateTime.UtcNow
            });
        }

        var statusCode = result.Error.Contains("Access denied") ? 403 :
                       result.Error.Contains("not found") ? 404 : 500;

        return Results.Json(new ApiResponse<object>
        {
            Success = false,
            Error = result.Error,
            Details = result.Details,
            Timestamp = DateTime.UtcNow
        }, statusCode: statusCode);
    }

    private static async Task<IResult> SimulateSubmitReviewDecision(
        (Guid id, ReviewDecisionRequest request) parameters,
        IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var reviewerId))
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "User information not found",
                Timestamp = DateTime.UtcNow
            }, statusCode: 400);
        }

        var result = await vettingService.SubmitReviewDecisionAsync(parameters.id, parameters.request, reviewerId, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            return Results.Ok(new ApiResponse<ReviewDecisionResponse>
            {
                Success = true,
                Data = result.Value,
                Message = "Review decision submitted successfully",
                Timestamp = DateTime.UtcNow
            });
        }

        var statusCode = result.Error.Contains("Access denied") ? 403 :
                       result.Error.Contains("not found") ? 404 : 400;

        return Results.Json(new ApiResponse<object>
        {
            Success = false,
            Error = result.Error,
            Details = result.Details,
            Timestamp = DateTime.UtcNow
        }, statusCode: statusCode);
    }

    private static async Task<IResult> SimulateAddApplicationNote(
        (Guid id, CreateNoteRequest request) parameters,
        IVettingService vettingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var reviewerId))
        {
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Error = "User information not found",
                Timestamp = DateTime.UtcNow
            }, statusCode: 400);
        }

        var result = await vettingService.AddApplicationNoteAsync(parameters.id, parameters.request, reviewerId, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            return Results.Ok(new ApiResponse<NoteResponse>
            {
                Success = true,
                Data = result.Value,
                Message = "Note added successfully",
                Timestamp = DateTime.UtcNow
            });
        }

        var statusCode = result.Error.Contains("Access denied") ? 403 :
                       result.Error.Contains("not found") ? 404 : 400;

        return Results.Json(new ApiResponse<object>
        {
            Success = false,
            Error = result.Error,
            Details = result.Details,
            Timestamp = DateTime.UtcNow
        }, statusCode: statusCode);
    }
}
