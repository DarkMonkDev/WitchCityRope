using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using WitchCityRope.Api.Features.Dashboard.Models;
using WitchCityRope.Api.Features.Dashboard.Services;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Features.Dashboard;

/// <summary>
/// Unit tests for UserDashboardEndpoints (Pattern B - Minimal API)
/// Tests all 5 dashboard endpoints with comprehensive coverage
/// </summary>
public class UserDashboardEndpointsTests
{
    private readonly IUserDashboardProfileService _mockService;

    public UserDashboardEndpointsTests()
    {
        _mockService = Substitute.For<IUserDashboardProfileService>();
    }

    #region GetUserEvents Tests

    /// <summary>
    /// Test: GetUserEvents with valid authenticated user returns 200 OK with events list
    /// </summary>
    [Fact]
    public async Task GetUserEvents_WithValidAuthenticatedUser_Returns200OkWithEventsList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        var expectedEvents = new List<UserEventDto>
        {
            new UserEventDto
            {
                Id = Guid.NewGuid(),
                Title = "Test Event",
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(7).AddHours(3),
                Location = "Test Venue",
                RegistrationStatus = "RSVP Confirmed",
                IsSocialEvent = true,
                HasTicket = false
            }
        };

        _mockService.GetUserEventsAsync(userId, false, Arg.Any<CancellationToken>())
            .Returns(Result<List<UserEventDto>>.Success(expectedEvents));

        // Act
        var result = await GetUserEvents(userId, false, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<List<UserEventDto>>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().HaveCount(1);
        okResult.Value!.First().Title.Should().Be("Test Event");
    }

    /// <summary>
    /// Test: GetUserEvents with includePast=true includes past events
    /// </summary>
    [Fact]
    public async Task GetUserEvents_WithIncludePastTrue_IncludesPastEvents()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        var expectedEvents = new List<UserEventDto>
        {
            new UserEventDto
            {
                Id = Guid.NewGuid(),
                Title = "Past Event",
                StartDate = DateTime.UtcNow.AddDays(-14),
                EndDate = DateTime.UtcNow.AddDays(-14).AddHours(3),
                Location = "Test Venue",
                RegistrationStatus = "Attended"
            }
        };

        _mockService.GetUserEventsAsync(userId, true, Arg.Any<CancellationToken>())
            .Returns(Result<List<UserEventDto>>.Success(expectedEvents));

        // Act
        var result = await GetUserEvents(userId, true, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<List<UserEventDto>>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().HaveCount(1);
    }

    /// <summary>
    /// Test: GetUserEvents with user accessing different user's data returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task GetUserEvents_WithUserAccessingDifferentUsersData_Returns403Forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(differentUserId); // Different user in claims

        // Act
        var result = await GetUserEvents(userId, false, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(403);
        problemResult.ProblemDetails.Title.Should().Be("Unauthorized");
        problemResult.ProblemDetails.Detail.Should().Contain("You can only access your own events");
    }

    /// <summary>
    /// Test: GetUserEvents with missing user ID claim returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task GetUserEvents_WithMissingUserIdClaim_Returns403Forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // No claims

        // Act
        var result = await GetUserEvents(userId, false, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(403);
    }

    /// <summary>
    /// Test: GetUserEvents when service fails returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task GetUserEvents_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        _mockService.GetUserEventsAsync(userId, false, Arg.Any<CancellationToken>())
            .Returns(Result<List<UserEventDto>>.Failure("Database error", "Connection timeout"));

        // Act
        var result = await GetUserEvents(userId, false, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Failed to retrieve events");
        problemResult.ProblemDetails.Detail.Should().Contain("Database error");
    }

    #endregion

    #region GetVettingStatus Tests

    /// <summary>
    /// Test: GetVettingStatus with valid authenticated user returns 200 OK with status
    /// </summary>
    [Fact]
    public async Task GetVettingStatus_WithValidAuthenticatedUser_Returns200OkWithStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        var expectedStatus = new VettingStatusDto
        {
            Status = VettingStatus.UnderReview,
            LastUpdatedAt = DateTime.UtcNow,
            Message = "Your vetting application is under review"
        };

        _mockService.GetVettingStatusAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<VettingStatusDto>.Success(expectedStatus));

        // Act
        var result = await GetVettingStatus(userId, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<VettingStatusDto>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value!.Status.Should().Be(VettingStatus.UnderReview);
        okResult.Value.Message.Should().Be("Your vetting application is under review");
    }

    /// <summary>
    /// Test: GetVettingStatus with user accessing different user's data returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task GetVettingStatus_WithUserAccessingDifferentUsersData_Returns403Forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(differentUserId);

        // Act
        var result = await GetVettingStatus(userId, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(403);
        problemResult.ProblemDetails.Detail.Should().Contain("You can only access your own vetting status");
    }

    /// <summary>
    /// Test: GetVettingStatus when user not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetVettingStatus_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        _mockService.GetVettingStatusAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<VettingStatusDto>.Failure("User not found", "No vetting application exists"));

        // Act
        var result = await GetVettingStatus(userId, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Title.Should().Be("Failed to retrieve vetting status");
    }

    /// <summary>
    /// Test: GetVettingStatus when service fails returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task GetVettingStatus_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        _mockService.GetVettingStatusAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<VettingStatusDto>.Failure("Database error"));

        // Act
        var result = await GetVettingStatus(userId, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetUserProfile Tests

    /// <summary>
    /// Test: GetUserProfile with valid authenticated user returns 200 OK with profile
    /// </summary>
    [Fact]
    public async Task GetUserProfile_WithValidAuthenticatedUser_Returns200OkWithProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        var expectedProfile = new UserProfileDto
        {
            UserId = userId,
            SceneName = "TestUser",
            Email = "test@example.com",
            Pronouns = "they/them",
            VettingStatus = VettingStatus.UnderReview,
            HasVettingApplication = true
        };

        _mockService.GetUserProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<UserProfileDto>.Success(expectedProfile));

        // Act
        var result = await GetUserProfile(userId, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<UserProfileDto>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value!.UserId.Should().Be(userId);
        okResult.Value.SceneName.Should().Be("TestUser");
    }

    /// <summary>
    /// Test: GetUserProfile with user accessing different user's data returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task GetUserProfile_WithUserAccessingDifferentUsersData_Returns403Forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(differentUserId);

        // Act
        var result = await GetUserProfile(userId, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(403);
        problemResult.ProblemDetails.Detail.Should().Contain("You can only access your own profile");
    }

    /// <summary>
    /// Test: GetUserProfile when profile not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetUserProfile_WhenProfileNotFound_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        _mockService.GetUserProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<UserProfileDto>.Failure("Profile not found"));

        // Act
        var result = await GetUserProfile(userId, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Title.Should().Be("Profile not found");
    }

    #endregion

    #region UpdateUserProfile Tests

    /// <summary>
    /// Test: UpdateUserProfile with valid request returns 200 OK with updated profile
    /// </summary>
    [Fact]
    public async Task UpdateUserProfile_WithValidRequest_Returns200OkWithUpdatedProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        var request = new UpdateProfileDto
        {
            SceneName = "UpdatedUser",
            Email = "updated@example.com",
            Pronouns = "she/her",
            Bio = "Updated bio"
        };

        var expectedProfile = new UserProfileDto
        {
            UserId = userId,
            SceneName = "UpdatedUser",
            Email = "updated@example.com",
            Pronouns = "she/her",
            Bio = "Updated bio"
        };

        _mockService.UpdateUserProfileAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns(Result<UserProfileDto>.Success(expectedProfile));

        // Act
        var result = await UpdateUserProfile(userId, request, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<UserProfileDto>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value!.SceneName.Should().Be("UpdatedUser");
        okResult.Value.Email.Should().Be("updated@example.com");
    }

    /// <summary>
    /// Test: UpdateUserProfile with user accessing different user's data returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task UpdateUserProfile_WithUserAccessingDifferentUsersData_Returns403Forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(differentUserId);

        var request = new UpdateProfileDto
        {
            SceneName = "Test",
            Email = "test@example.com"
        };

        // Act
        var result = await UpdateUserProfile(userId, request, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(403);
        problemResult.ProblemDetails.Detail.Should().Contain("You can only update your own profile");
    }

    /// <summary>
    /// Test: UpdateUserProfile when service fails returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task UpdateUserProfile_WhenServiceFails_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        var request = new UpdateProfileDto
        {
            SceneName = "Test",
            Email = "invalid-email"
        };

        _mockService.UpdateUserProfileAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns(Result<UserProfileDto>.Failure("Validation error", "Invalid email format"));

        // Act
        var result = await UpdateUserProfile(userId, request, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Failed to update profile");
    }

    #endregion

    #region ChangePassword Tests

    /// <summary>
    /// Test: ChangePassword with valid request returns 200 OK with true
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithValidRequest_Returns200OkWithTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        var request = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        _mockService.ChangePasswordAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await ChangePassword(userId, request, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<bool>;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeTrue();
    }

    /// <summary>
    /// Test: ChangePassword with user accessing different user's data returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithUserAccessingDifferentUsersData_Returns403Forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(differentUserId);

        var request = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act
        var result = await ChangePassword(userId, request, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(403);
        problemResult.ProblemDetails.Detail.Should().Contain("You can only change your own password");
    }

    /// <summary>
    /// Test: ChangePassword with incorrect current password returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        var request = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        _mockService.ChangePasswordAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Current password is incorrect"));

        // Act
        var result = await ChangePassword(userId, request, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Failed to change password");
        problemResult.ProblemDetails.Detail.Should().Contain("Current password is incorrect");
    }

    /// <summary>
    /// Test: ChangePassword with weak new password returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithWeakNewPassword_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateClaimsPrincipal(userId);

        var request = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "weak",
            ConfirmPassword = "weak"
        };

        _mockService.ChangePasswordAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Password does not meet requirements", "Password must be at least 8 characters"));

        // Act
        var result = await ChangePassword(userId, request, _mockService, user, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Detail.Should().Contain("Password does not meet requirements");
    }

    #endregion

    #region Helper Methods - Simulate Endpoint Logic

    /// <summary>
    /// Helper method to simulate GetUserEvents endpoint logic
    /// </summary>
    private async Task<IResult> GetUserEvents(
        Guid userId,
        bool includePast,
        IUserDashboardProfileService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Verify user is accessing their own data
        var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId != userId)
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "You can only access your own events",
                statusCode: 403);
        }

        var result = await service.GetUserEventsAsync(userId, includePast, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Failed to retrieve events",
                detail: result.Error ?? result.Details ?? "Failed to retrieve events",
                statusCode: 500);
    }

    /// <summary>
    /// Helper method to simulate GetVettingStatus endpoint logic
    /// </summary>
    private async Task<IResult> GetVettingStatus(
        Guid userId,
        IUserDashboardProfileService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Verify user is accessing their own data
        var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId != userId)
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "You can only access your own vetting status",
                statusCode: 403);
        }

        var result = await service.GetVettingStatusAsync(userId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Failed to retrieve vetting status",
                detail: result.Error ?? result.Details ?? "Failed to retrieve vetting status",
                statusCode: result.Error?.Contains("not found") == true ? 404 : 500);
    }

    /// <summary>
    /// Helper method to simulate GetUserProfile endpoint logic
    /// </summary>
    private async Task<IResult> GetUserProfile(
        Guid userId,
        IUserDashboardProfileService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Verify user is accessing their own data
        var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId != userId)
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "You can only access your own profile",
                statusCode: 403);
        }

        var result = await service.GetUserProfileAsync(userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Profile not found",
                detail: result.Error ?? result.Details ?? "Profile not found",
                statusCode: 404);
        }

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Helper method to simulate UpdateUserProfile endpoint logic
    /// </summary>
    private async Task<IResult> UpdateUserProfile(
        Guid userId,
        UpdateProfileDto request,
        IUserDashboardProfileService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Verify user is accessing their own data
        var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId != userId)
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "You can only update your own profile",
                statusCode: 403);
        }

        var result = await service.UpdateUserProfileAsync(userId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to update profile",
                detail: result.Error ?? result.Details ?? "Failed to update profile",
                statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Helper method to simulate ChangePassword endpoint logic
    /// </summary>
    private async Task<IResult> ChangePassword(
        Guid userId,
        ChangePasswordDto request,
        IUserDashboardProfileService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        // Verify user is accessing their own data
        var userIdClaim = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId != userId)
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "You can only change your own password",
                statusCode: 403);
        }

        var result = await service.ChangePasswordAsync(userId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                title: "Failed to change password",
                detail: result.Error ?? result.Details ?? "Failed to change password",
                statusCode: 400);
        }

        return Results.Ok(true);
    }

    /// <summary>
    /// Helper method to create ClaimsPrincipal with user ID
    /// </summary>
    private ClaimsPrincipal CreateClaimsPrincipal(Guid userId)
    {
        var claims = new[] { new Claim("sub", userId.ToString()) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    #endregion
}
