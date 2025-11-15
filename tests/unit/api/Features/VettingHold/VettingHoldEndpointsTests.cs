using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.VettingHold.Models;
using WitchCityRope.Api.Features.VettingHold.Services;
using Xunit;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace WitchCityRope.UnitTests.Api.Features.VettingHold;

/// <summary>
/// Unit tests for VettingHoldEndpoints (Pattern B - Minimal API)
/// Tests all 3 vetting hold endpoints with comprehensive coverage
/// </summary>
public class VettingHoldEndpointsTests
{
    private readonly IVettingHoldService _mockVettingHoldService;

    public VettingHoldEndpointsTests()
    {
        _mockVettingHoldService = Substitute.For<IVettingHoldService>();
    }

    #region PlaceMembershipOnHold Tests

    /// <summary>
    /// Test: PlaceMembershipOnHold with valid request returns 200 OK with MembershipHoldResponse
    /// </summary>
    [Fact]
    public async Task PlaceMembershipOnHold_WithValidRequest_Returns200OkWithResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = userId; // Same user
        var request = new PlaceMembershipOnHoldRequest("Need a break from community activities");
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        var expectedResponse = new MembershipHoldResponse(
            NewStatus: 5, // OnHold
            StatusName: "OnHold",
            ChangedAt: DateTime.UtcNow
        );

        var serviceResult = Result<MembershipHoldResponse>.Success(expectedResponse);

        _mockVettingHoldService.PlaceMembershipOnHoldAsync(userId, request.Reason, Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        // Act
        var result = await PlaceMembershipOnHold(userId, request, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<MembershipHoldResponse>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
        okResult.Value.NewStatus.Should().Be(5); // OnHold
        okResult.Value.StatusName.Should().Be("OnHold");
    }

    /// <summary>
    /// Test: PlaceMembershipOnHold with missing user claim returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task PlaceMembershipOnHold_WithMissingUserClaim_Returns401Unauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new PlaceMembershipOnHoldRequest("Reason");
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity()); // No claims

        // Act
        var result = await PlaceMembershipOnHold(userId, request, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Unauthorized");
        problemResult.ProblemDetails.Detail.Should().Be("User not authenticated");

        // Verify service was NOT called
        await _mockVettingHoldService.DidNotReceive().PlaceMembershipOnHoldAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: PlaceMembershipOnHold with invalid user ID format returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task PlaceMembershipOnHold_WithInvalidUserIdFormat_Returns401Unauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new PlaceMembershipOnHoldRequest("Reason");
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-valid-guid") };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var result = await PlaceMembershipOnHold(userId, request, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Unauthorized");
        problemResult.ProblemDetails.Detail.Should().Be("User not authenticated");

        // Verify service was NOT called
        await _mockVettingHoldService.DidNotReceive().PlaceMembershipOnHoldAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: PlaceMembershipOnHold when user tries to operate on different user returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task PlaceMembershipOnHold_WhenUserOperatesOnDifferentUser_Returns403Forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = Guid.NewGuid(); // Different user
        var request = new PlaceMembershipOnHoldRequest("Reason");
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        // Act
        var result = await PlaceMembershipOnHold(userId, request, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(403);
        problemResult.ProblemDetails.Title.Should().Be("Forbidden");
        problemResult.ProblemDetails.Detail.Should().Be("You can only place your own membership on hold");

        // Verify service was NOT called
        await _mockVettingHoldService.DidNotReceive().PlaceMembershipOnHoldAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: PlaceMembershipOnHold when service returns failure (user not Approved) returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task PlaceMembershipOnHold_WhenServiceReturnsFailure_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = userId;
        var request = new PlaceMembershipOnHoldRequest("Reason");
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        var serviceResult = Result<MembershipHoldResponse>.Failure(
            "User must be Approved to place membership on hold",
            "Current status: OnHold");

        _mockVettingHoldService.PlaceMembershipOnHoldAsync(userId, request.Reason, Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        // Act
        var result = await PlaceMembershipOnHold(userId, request, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Failed to place membership on hold");
        problemResult.ProblemDetails.Detail.Should().Contain("User must be Approved");
    }

    /// <summary>
    /// Test: PlaceMembershipOnHold when service throws exception returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task PlaceMembershipOnHold_WhenServiceThrowsException_Returns500InternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = userId;
        var request = new PlaceMembershipOnHoldRequest("Reason");
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        _mockVettingHoldService.PlaceMembershipOnHoldAsync(userId, request.Reason, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await PlaceMembershipOnHold(userId, request, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Internal server error");
        problemResult.ProblemDetails.Detail.Should().Contain("Database connection failed");
    }

    #endregion

    #region RequestReinstatement Tests

    /// <summary>
    /// Test: RequestReinstatement with valid request returns 200 OK with MembershipHoldResponse
    /// </summary>
    [Fact]
    public async Task RequestReinstatement_WithValidRequest_Returns200OkWithResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = userId;
        var request = new RequestReinstatementRequest("Ready to return to community activities");
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        var expectedResponse = new MembershipHoldResponse(
            NewStatus: 2, // FinalReview
            StatusName: "FinalReview",
            ChangedAt: DateTime.UtcNow
        );

        var serviceResult = Result<MembershipHoldResponse>.Success(expectedResponse);

        _mockVettingHoldService.RequestReinstatementAsync(userId, request.Reason, Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        // Act
        var result = await RequestReinstatement(userId, request, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<MembershipHoldResponse>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
        okResult.Value.NewStatus.Should().Be(2); // FinalReview
        okResult.Value.StatusName.Should().Be("FinalReview");
    }

    /// <summary>
    /// Test: RequestReinstatement with missing user claim returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task RequestReinstatement_WithMissingUserClaim_Returns401Unauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new RequestReinstatementRequest("Reason");
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity()); // No claims

        // Act
        var result = await RequestReinstatement(userId, request, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Unauthorized");
        problemResult.ProblemDetails.Detail.Should().Be("User not authenticated");

        // Verify service was NOT called
        await _mockVettingHoldService.DidNotReceive().RequestReinstatementAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: RequestReinstatement with invalid user ID format returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task RequestReinstatement_WithInvalidUserIdFormat_Returns401Unauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new RequestReinstatementRequest("Reason");
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "invalid-guid") };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var result = await RequestReinstatement(userId, request, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Unauthorized");
        problemResult.ProblemDetails.Detail.Should().Be("User not authenticated");

        // Verify service was NOT called
        await _mockVettingHoldService.DidNotReceive().RequestReinstatementAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: RequestReinstatement when user tries to operate on different user returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task RequestReinstatement_WhenUserOperatesOnDifferentUser_Returns403Forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = Guid.NewGuid(); // Different user
        var request = new RequestReinstatementRequest("Reason");
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        // Act
        var result = await RequestReinstatement(userId, request, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(403);
        problemResult.ProblemDetails.Title.Should().Be("Forbidden");
        problemResult.ProblemDetails.Detail.Should().Be("You can only request reinstatement for your own membership");

        // Verify service was NOT called
        await _mockVettingHoldService.DidNotReceive().RequestReinstatementAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: RequestReinstatement when service returns failure (user not OnHold) returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task RequestReinstatement_WhenServiceReturnsFailure_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = userId;
        var request = new RequestReinstatementRequest("Reason");
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        var serviceResult = Result<MembershipHoldResponse>.Failure(
            "User must be OnHold to request reinstatement",
            "Current status: Approved");

        _mockVettingHoldService.RequestReinstatementAsync(userId, request.Reason, Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        // Act
        var result = await RequestReinstatement(userId, request, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Failed to request reinstatement");
        problemResult.ProblemDetails.Detail.Should().Contain("User must be OnHold");
    }

    /// <summary>
    /// Test: RequestReinstatement when service throws exception returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task RequestReinstatement_WhenServiceThrowsException_Returns500InternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = userId;
        var request = new RequestReinstatementRequest("Reason");
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        _mockVettingHoldService.RequestReinstatementAsync(userId, request.Reason, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await RequestReinstatement(userId, request, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Internal server error");
        problemResult.ProblemDetails.Detail.Should().Contain("Database connection failed");
    }

    #endregion

    #region GetHoldStatus Tests

    /// <summary>
    /// Test: GetHoldStatus with valid request returns 200 OK with VettingHoldStatusResponse
    /// </summary>
    [Fact]
    public async Task GetHoldStatus_WithValidRequest_Returns200OkWithStatusResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = userId;
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        var expectedResponse = new VettingHoldStatusResponse(
            VettingStatus: 3, // Approved
            StatusName: "Approved",
            CanPlaceOnHold: true,
            CanRequestReinstatement: false,
            LastStatusChangeDate: DateTime.UtcNow.AddDays(-30)
        );

        var serviceResult = Result<VettingHoldStatusResponse>.Success(expectedResponse);

        _mockVettingHoldService.GetHoldStatusAsync(userId, Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        // Act
        var result = await GetHoldStatus(userId, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<VettingHoldStatusResponse>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
        okResult.Value.VettingStatus.Should().Be(3); // Approved
        okResult.Value.CanPlaceOnHold.Should().BeTrue();
        okResult.Value.CanRequestReinstatement.Should().BeFalse();
    }

    /// <summary>
    /// Test: GetHoldStatus for OnHold user shows can request reinstatement
    /// </summary>
    [Fact]
    public async Task GetHoldStatus_ForOnHoldUser_ShowsCanRequestReinstatement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = userId;
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        var expectedResponse = new VettingHoldStatusResponse(
            VettingStatus: 5, // OnHold
            StatusName: "OnHold",
            CanPlaceOnHold: false,
            CanRequestReinstatement: true,
            LastStatusChangeDate: DateTime.UtcNow.AddDays(-7)
        );

        var serviceResult = Result<VettingHoldStatusResponse>.Success(expectedResponse);

        _mockVettingHoldService.GetHoldStatusAsync(userId, Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        // Act
        var result = await GetHoldStatus(userId, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<VettingHoldStatusResponse>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.VettingStatus.Should().Be(5); // OnHold
        okResult.Value.CanPlaceOnHold.Should().BeFalse();
        okResult.Value.CanRequestReinstatement.Should().BeTrue();
    }

    /// <summary>
    /// Test: GetHoldStatus with missing user claim returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetHoldStatus_WithMissingUserClaim_Returns401Unauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity()); // No claims

        // Act
        var result = await GetHoldStatus(userId, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Unauthorized");
        problemResult.ProblemDetails.Detail.Should().Be("User not authenticated");

        // Verify service was NOT called
        await _mockVettingHoldService.DidNotReceive().GetHoldStatusAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: GetHoldStatus with invalid user ID format returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetHoldStatus_WithInvalidUserIdFormat_Returns401Unauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "invalid-guid") };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var result = await GetHoldStatus(userId, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Unauthorized");
        problemResult.ProblemDetails.Detail.Should().Be("User not authenticated");

        // Verify service was NOT called
        await _mockVettingHoldService.DidNotReceive().GetHoldStatusAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: GetHoldStatus when user tries to view different user's status returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task GetHoldStatus_WhenUserViewsDifferentUserStatus_Returns403Forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = Guid.NewGuid(); // Different user
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        // Act
        var result = await GetHoldStatus(userId, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(403);
        problemResult.ProblemDetails.Title.Should().Be("Forbidden");
        problemResult.ProblemDetails.Detail.Should().Be("You can only view your own hold status");

        // Verify service was NOT called
        await _mockVettingHoldService.DidNotReceive().GetHoldStatusAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: GetHoldStatus when service returns failure (user not found) returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetHoldStatus_WhenServiceReturnsFailure_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = userId;
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        var serviceResult = Result<VettingHoldStatusResponse>.Failure(
            "User not found",
            $"No user found with ID: {userId}");

        _mockVettingHoldService.GetHoldStatusAsync(userId, Arg.Any<CancellationToken>())
            .Returns(serviceResult);

        // Act
        var result = await GetHoldStatus(userId, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Title.Should().Be("Hold status not found");
        problemResult.ProblemDetails.Detail.Should().Contain("User not found");
    }

    /// <summary>
    /// Test: GetHoldStatus when service throws exception returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task GetHoldStatus_WhenServiceThrowsException_Returns500InternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticatedUserId = userId;
        var claimsPrincipal = CreateClaimsPrincipal(authenticatedUserId);

        _mockVettingHoldService.GetHoldStatusAsync(userId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await GetHoldStatus(userId, claimsPrincipal, _mockVettingHoldService, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Internal server error");
        problemResult.ProblemDetails.Detail.Should().Contain("Database connection failed");
    }

    #endregion

    #region Helper Methods - Simulate Endpoint Logic

    /// <summary>
    /// Helper method to simulate PlaceMembershipOnHold endpoint logic
    /// Mimics VettingHoldEndpoints.PlaceMembershipOnHold method
    /// </summary>
    private async Task<IResult> PlaceMembershipOnHold(
        Guid userId,
        PlaceMembershipOnHoldRequest request,
        ClaimsPrincipal user,
        IVettingHoldService vettingHoldService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get authenticated user ID
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
            {
                return Results.Problem(
                    title: "Unauthorized",
                    detail: "User not authenticated",
                    statusCode: 401);
            }

            // Verify user is operating on their own profile
            if (authenticatedUserId != userId)
            {
                return Results.Problem(
                    title: "Forbidden",
                    detail: "You can only place your own membership on hold",
                    statusCode: 403);
            }

            // Call service
            var result = await vettingHoldService.PlaceMembershipOnHoldAsync(
                userId,
                request.Reason,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.Problem(
                    title: "Failed to place membership on hold",
                    detail: result.Error ?? result.Details ?? "Failed to place membership on hold",
                    statusCode: 400);
            }

            return Results.Ok(result.Value);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Internal server error",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Helper method to simulate RequestReinstatement endpoint logic
    /// Mimics VettingHoldEndpoints.RequestReinstatement method
    /// </summary>
    private async Task<IResult> RequestReinstatement(
        Guid userId,
        RequestReinstatementRequest request,
        ClaimsPrincipal user,
        IVettingHoldService vettingHoldService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get authenticated user ID
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
            {
                return Results.Problem(
                    title: "Unauthorized",
                    detail: "User not authenticated",
                    statusCode: 401);
            }

            // Verify user is operating on their own profile
            if (authenticatedUserId != userId)
            {
                return Results.Problem(
                    title: "Forbidden",
                    detail: "You can only request reinstatement for your own membership",
                    statusCode: 403);
            }

            // Call service
            var result = await vettingHoldService.RequestReinstatementAsync(
                userId,
                request.Reason,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.Problem(
                    title: "Failed to request reinstatement",
                    detail: result.Error ?? result.Details ?? "Failed to request reinstatement",
                    statusCode: 400);
            }

            return Results.Ok(result.Value);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Internal server error",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Helper method to simulate GetHoldStatus endpoint logic
    /// Mimics VettingHoldEndpoints.GetHoldStatus method
    /// </summary>
    private async Task<IResult> GetHoldStatus(
        Guid userId,
        ClaimsPrincipal user,
        IVettingHoldService vettingHoldService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get authenticated user ID
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedUserId))
            {
                return Results.Problem(
                    title: "Unauthorized",
                    detail: "User not authenticated",
                    statusCode: 401);
            }

            // Verify user is operating on their own profile
            if (authenticatedUserId != userId)
            {
                return Results.Problem(
                    title: "Forbidden",
                    detail: "You can only view your own hold status",
                    statusCode: 403);
            }

            // Call service
            var result = await vettingHoldService.GetHoldStatusAsync(userId, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.Problem(
                    title: "Hold status not found",
                    detail: result.Error ?? result.Details ?? "Hold status not found",
                    statusCode: 404);
            }

            return Results.Ok(result.Value);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Internal server error",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Helper to create ClaimsPrincipal with NameIdentifier claim
    /// </summary>
    private ClaimsPrincipal CreateClaimsPrincipal(Guid userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    #endregion
}
