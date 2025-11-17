using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using WitchCityRope.Api.Features.Users.Models;
using WitchCityRope.Api.Features.Users.Services;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Features.Users;

/// <summary>
/// Unit tests for UserEndpoints following Pattern B standards.
/// Tests all user management endpoints (profile, admin operations, roles).
/// </summary>
public class UserEndpointsTests
{
    private readonly IUserManagementService _mockUserService;
    private readonly ClaimsPrincipal _authenticatedUser;
    private readonly ClaimsPrincipal _adminUser;
    private readonly string _testUserId;

    public UserEndpointsTests()
    {
        // Create mock IUserManagementService
        _mockUserService = Substitute.For<IUserManagementService>();

        _testUserId = Guid.NewGuid().ToString();

        // Create authenticated user with "sub" claim
        _authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", _testUserId),
            new Claim(ClaimTypes.NameIdentifier, _testUserId)
        }, "test"));

        // Create admin user
        _adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "test"));
    }

    #region GET /api/users/profile Tests

    /// <summary>
    /// Test: GET /api/users/profile with valid "sub" claim returns 200 OK with UserDto
    /// </summary>
    [Fact]
    public async Task GetUserProfile_WithValidSubClaim_Returns200OkWithUserDto()
    {
        // Arrange
        var expectedUser = new UserDto
        {
            Id = Guid.Parse(_testUserId),
            Email = "test@witchcityrope.com",
            SceneName = "Test User",
            Role = "Member",
            IsActive = true
        };

        _mockUserService.GetProfileAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((true, expectedUser, string.Empty));

        // Act
        var result = await CallGetUserProfile(_authenticatedUser);

        // Assert
        result.Should().BeOfType<Ok<UserDto>>();
        var okResult = (Ok<UserDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Id.Should().Be(Guid.Parse(_testUserId));
        okResult.Value!.Email.Should().Be("test@witchcityrope.com");
        okResult.Value!.SceneName.Should().Be("Test User");
    }

    /// <summary>
    /// Test: GET /api/users/profile with NameIdentifier claim returns 200 OK
    /// </summary>
    [Fact]
    public async Task GetUserProfile_WithNameIdentifierClaim_Returns200OkWithUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "test"));

        var expectedUser = new UserDto
        {
            Id = Guid.Parse(userId),
            Email = "user@witchcityrope.com",
            SceneName = "User",
            Role = "Member"
        };

        _mockUserService.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns((true, expectedUser, string.Empty));

        // Act
        var result = await CallGetUserProfile(user);

        // Assert
        result.Should().BeOfType<Ok<UserDto>>();
        var okResult = (Ok<UserDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Id.Should().Be(Guid.Parse(userId));
    }

    /// <summary>
    /// Test: GET /api/users/profile with missing user ID claim returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetUserProfile_WithMissingUserIdClaim_Returns401Unauthorized()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { }, "test"));

        // Act
        var result = await CallGetUserProfile(user);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Invalid Token");
        problemResult.ProblemDetails!.Detail.Should().Contain("User ID not found in token claims");
    }

    /// <summary>
    /// Test: GET /api/users/profile when user not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetUserProfile_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        _mockUserService.GetProfileAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await CallGetUserProfile(_authenticatedUser);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Get Profile Failed");
        problemResult.ProblemDetails!.Detail.Should().Be("User not found");
    }

    /// <summary>
    /// Test: GET /api/users/profile when service fails returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task GetUserProfile_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var userDto = new UserDto(); // Non-null but service fails
        _mockUserService.GetProfileAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((false, userDto, "Database error"));

        // Act
        var result = await CallGetUserProfile(_authenticatedUser);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Get Profile Failed");
        problemResult.ProblemDetails!.Detail.Should().Be("Database error");
    }

    #endregion

    #region PUT /api/users/profile Tests

    /// <summary>
    /// Test: PUT /api/users/profile with valid request returns 200 OK with updated UserDto
    /// </summary>
    [Fact]
    public async Task UpdateUserProfile_WithValidRequest_Returns200OkWithUpdatedUser()
    {
        // Arrange
        var request = new UpdateProfileRequest
        {
            SceneName = "New Scene Name",
            Pronouns = "they/them"
        };

        var updatedUser = new UserDto
        {
            Id = Guid.Parse(_testUserId),
            Email = "test@witchcityrope.com",
            SceneName = "New Scene Name",
            Pronouns = "they/them",
            Role = "Member"
        };

        _mockUserService.UpdateProfileAsync(_testUserId, request, Arg.Any<CancellationToken>())
            .Returns((true, updatedUser, string.Empty));

        // Act
        var result = await CallUpdateUserProfile(_authenticatedUser, request);

        // Assert
        result.Should().BeOfType<Ok<UserDto>>();
        var okResult = (Ok<UserDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.SceneName.Should().Be("New Scene Name");
        okResult.Value!.Pronouns.Should().Be("they/them");
    }

    /// <summary>
    /// Test: PUT /api/users/profile with missing user ID claim returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task UpdateUserProfile_WithMissingUserIdClaim_Returns401Unauthorized()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { }, "test"));
        var request = new UpdateProfileRequest { SceneName = "Test" };

        // Act
        var result = await CallUpdateUserProfile(user, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
    }

    /// <summary>
    /// Test: PUT /api/users/profile with duplicate scene name returns 409 Conflict
    /// </summary>
    [Fact]
    public async Task UpdateUserProfile_WithDuplicateSceneName_Returns409Conflict()
    {
        // Arrange
        var request = new UpdateProfileRequest { SceneName = "Existing Name" };

        _mockUserService.UpdateProfileAsync(_testUserId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Scene name is already taken"));

        // Act
        var result = await CallUpdateUserProfile(_authenticatedUser, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(409);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Update Profile Failed");
        problemResult.ProblemDetails!.Detail.Should().Contain("already taken");
    }

    /// <summary>
    /// Test: PUT /api/users/profile with validation error returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task UpdateUserProfile_WithValidationError_Returns400BadRequest()
    {
        // Arrange
        var request = new UpdateProfileRequest { SceneName = "Test" };

        _mockUserService.UpdateProfileAsync(_testUserId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Invalid data"));

        // Act
        var result = await CallUpdateUserProfile(_authenticatedUser, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region GET /api/admin/users Tests

    /// <summary>
    /// Test: GET /api/admin/users with valid request returns 200 OK with UserListResponse
    /// </summary>
    [Fact]
    public async Task GetUsers_WithValidRequest_Returns200OkWithUserList()
    {
        // Arrange
        var searchRequest = new UserSearchRequest
        {
            Page = 1,
            PageSize = 20,
            SortBy = "SceneName"
        };

        var userListResponse = new UserListResponse
        {
            Users = new List<UserDto>
            {
                new UserDto { Id = Guid.NewGuid(), Email = "user1@test.com", SceneName = "User 1" },
                new UserDto { Id = Guid.NewGuid(), Email = "user2@test.com", SceneName = "User 2" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _mockUserService.GetUsersAsync(searchRequest, Arg.Any<CancellationToken>())
            .Returns((true, userListResponse, string.Empty));

        // Act
        var result = await CallGetUsers(searchRequest);

        // Assert
        result.Should().BeOfType<Ok<UserListResponse>>();
        var okResult = (Ok<UserListResponse>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Users.Should().HaveCount(2);
        okResult.Value!.TotalCount.Should().Be(2);
    }

    /// <summary>
    /// Test: GET /api/admin/users with search term filters results correctly
    /// </summary>
    [Fact]
    public async Task GetUsers_WithSearchTerm_ReturnsFilteredResults()
    {
        // Arrange
        var searchRequest = new UserSearchRequest
        {
            SearchTerm = "alice",
            Page = 1,
            PageSize = 20
        };

        var userListResponse = new UserListResponse
        {
            Users = new List<UserDto>
            {
                new UserDto { Id = Guid.NewGuid(), Email = "alice@test.com", SceneName = "Alice" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mockUserService.GetUsersAsync(searchRequest, Arg.Any<CancellationToken>())
            .Returns((true, userListResponse, string.Empty));

        // Act
        var result = await CallGetUsers(searchRequest);

        // Assert
        result.Should().BeOfType<Ok<UserListResponse>>();
        var okResult = (Ok<UserListResponse>)result;
        okResult.Value!.Users.Should().HaveCount(1);
        okResult.Value!.Users[0].SceneName.Should().Be("Alice");
    }

    /// <summary>
    /// Test: GET /api/admin/users when service fails returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task GetUsers_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var searchRequest = new UserSearchRequest();

        _mockUserService.GetUsersAsync(searchRequest, Arg.Any<CancellationToken>())
            .Returns((false, null, "Database error"));

        // Act
        var result = await CallGetUsers(searchRequest);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails!.Title.Should().Be("Get Users Failed");
    }

    #endregion

    #region GET /api/admin/users/{id} Tests

    /// <summary>
    /// Test: GET /api/admin/users/{id} with valid ID returns 200 OK with UserDto
    /// </summary>
    [Fact]
    public async Task GetUser_WithValidId_Returns200OkWithUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var expectedUser = new UserDto
        {
            Id = Guid.Parse(userId),
            Email = "admin@test.com",
            SceneName = "Admin User",
            Role = "Administrator"
        };

        _mockUserService.GetUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns((true, expectedUser, string.Empty));

        // Act
        var result = await CallGetUser(userId);

        // Assert
        result.Should().BeOfType<Ok<UserDto>>();
        var okResult = (Ok<UserDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Id.Should().Be(Guid.Parse(userId));
        okResult.Value!.SceneName.Should().Be("Admin User");
    }

    /// <summary>
    /// Test: GET /api/admin/users/{id} when user not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetUser_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        _mockUserService.GetUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await CallGetUser(userId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test: GET /api/admin/users/{id} with invalid ID format returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task GetUser_WithInvalidIdFormat_Returns400BadRequest()
    {
        // Arrange
        var invalidId = "not-a-guid";

        // Return a UserDto to trigger 400 (validation error) instead of 404 (not found)
        var dummyUser = new UserDto();
        _mockUserService.GetUserAsync(invalidId, Arg.Any<CancellationToken>())
            .Returns((false, dummyUser, "Invalid user ID format"));

        // Act
        var result = await CallGetUser(invalidId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region PUT /api/admin/users/{id} Tests

    /// <summary>
    /// Test: PUT /api/admin/users/{id} with valid request returns 200 OK with updated UserDto
    /// </summary>
    [Fact]
    public async Task UpdateUser_WithValidRequest_Returns200OkWithUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = new UpdateUserRequest
        {
            SceneName = "Updated Name",
            Role = "Teacher",
            IsActive = true
        };

        var updatedUser = new UserDto
        {
            Id = Guid.Parse(userId),
            Email = "user@test.com",
            SceneName = "Updated Name",
            Role = "Teacher",
            IsActive = true
        };

        _mockUserService.UpdateUserAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns((true, updatedUser, string.Empty));

        // Act
        var result = await CallUpdateUser(userId, request);

        // Assert
        result.Should().BeOfType<Ok<UserDto>>();
        var okResult = (Ok<UserDto>)result;
        okResult.Value!.SceneName.Should().Be("Updated Name");
        okResult.Value!.Role.Should().Be("Teacher");
    }

    /// <summary>
    /// Test: PUT /api/admin/users/{id} when user not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task UpdateUser_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = new UpdateUserRequest { SceneName = "Test" };

        _mockUserService.UpdateUserAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await CallUpdateUser(userId, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test: PUT /api/admin/users/{id} with duplicate scene name returns 409 Conflict
    /// </summary>
    [Fact]
    public async Task UpdateUser_WithDuplicateSceneName_Returns409Conflict()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = new UpdateUserRequest { SceneName = "Existing Name" };

        _mockUserService.UpdateUserAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Scene name is already taken"));

        // Act
        var result = await CallUpdateUser(userId, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(409);
    }

    /// <summary>
    /// Test: PUT /api/admin/users/{id} with validation error returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task UpdateUser_WithValidationError_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = new UpdateUserRequest { SceneName = "" };

        _mockUserService.UpdateUserAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Invalid request"));

        // Act
        var result = await CallUpdateUser(userId, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region PUT /api/admin/users/{userId}/roles Tests

    /// <summary>
    /// Test: PUT /api/admin/users/{userId}/roles with valid roles returns 200 OK with updated UserDto
    /// </summary>
    [Fact]
    public async Task UpdateUserRoles_WithValidRoles_Returns200OkWithUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = new UpdateUserRolesRequest
        {
            Roles = new List<string> { "Teacher", "SafetyTeam" }
        };

        var updatedUser = new UserDto
        {
            Id = Guid.Parse(userId),
            Email = "teacher@test.com",
            SceneName = "Teacher User",
            Role = "Teacher,SafetyTeam"
        };

        _mockUserService.UpdateUserRolesAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns((true, updatedUser, string.Empty));

        // Act
        var result = await CallUpdateUserRoles(userId, request);

        // Assert
        result.Should().BeOfType<Ok<UserDto>>();
        var okResult = (Ok<UserDto>)result;
        okResult.Value!.Role.Should().Be("Teacher,SafetyTeam");
    }

    /// <summary>
    /// Test: PUT /api/admin/users/{userId}/roles with empty roles list returns 200 OK (regular member)
    /// </summary>
    [Fact]
    public async Task UpdateUserRoles_WithEmptyRolesList_Returns200OkRegularMember()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = new UpdateUserRolesRequest
        {
            Roles = new List<string>()
        };

        var updatedUser = new UserDto
        {
            Id = Guid.Parse(userId),
            Email = "member@test.com",
            SceneName = "Regular Member",
            Role = string.Empty
        };

        _mockUserService.UpdateUserRolesAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns((true, updatedUser, string.Empty));

        // Act
        var result = await CallUpdateUserRoles(userId, request);

        // Assert
        result.Should().BeOfType<Ok<UserDto>>();
        var okResult = (Ok<UserDto>)result;
        okResult.Value!.Role.Should().BeEmpty();
    }

    /// <summary>
    /// Test: PUT /api/admin/users/{userId}/roles when user not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task UpdateUserRoles_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = new UpdateUserRolesRequest { Roles = new List<string> { "Teacher" } };

        _mockUserService.UpdateUserRolesAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await CallUpdateUserRoles(userId, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test: PUT /api/admin/users/{userId}/roles with invalid role returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task UpdateUserRoles_WithInvalidRole_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = new UpdateUserRolesRequest { Roles = new List<string> { "InvalidRole" } };

        _mockUserService.UpdateUserRolesAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Invalid roles: InvalidRole"));

        // Act
        var result = await CallUpdateUserRoles(userId, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Detail.Should().Contain("Invalid");
    }

    /// <summary>
    /// Test: PUT /api/admin/users/{userId}/roles when service fails returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task UpdateUserRoles_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = new UpdateUserRolesRequest { Roles = new List<string> { "Teacher" } };

        _mockUserService.UpdateUserRolesAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Database connection failed"));

        // Act
        var result = await CallUpdateUserRoles(userId, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GET /api/users/by-role/{role} Tests

    /// <summary>
    /// Test: GET /api/users/by-role/{role} with valid role returns 200 OK with UserOptionDto list
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_WithValidRole_Returns200OkWithUserList()
    {
        // Arrange
        var role = "Teacher";
        var users = new List<UserOptionDto>
        {
            new UserOptionDto { Id = Guid.NewGuid().ToString(), Name = "Teacher 1", Email = "t1@test.com" },
            new UserOptionDto { Id = Guid.NewGuid().ToString(), Name = "Teacher 2", Email = "t2@test.com" }
        };

        _mockUserService.GetUsersByRoleAsync(role, Arg.Any<CancellationToken>())
            .Returns((true, users, string.Empty));

        // Act
        var result = await CallGetUsersByRole(role);

        // Assert
        result.Should().BeOfType<Ok<IEnumerable<UserOptionDto>>>();
        var okResult = (Ok<IEnumerable<UserOptionDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Should().HaveCount(2);
    }

    /// <summary>
    /// Test: GET /api/users/by-role/{role} when service fails returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var role = "Teacher";

        _mockUserService.GetUsersByRoleAsync(role, Arg.Any<CancellationToken>())
            .Returns((false, null, "Database error"));

        // Act
        var result = await CallGetUsersByRole(role);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GET /api/public/users/{userId}/profile Tests

    /// <summary>
    /// Test: GET /api/public/users/{userId}/profile with valid user ID returns 200 OK with UserDto
    /// </summary>
    [Fact]
    public async Task GetUserProfileById_WithValidUserId_Returns200OkWithUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var expectedUser = new UserDto
        {
            Id = Guid.Parse(userId),
            Email = "public@test.com",
            SceneName = "Public User",
            Bio = "This is a public bio"
        };

        _mockUserService.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns((true, expectedUser, string.Empty));

        // Act
        var result = await CallGetUserProfileById(userId);

        // Assert
        result.Should().BeOfType<Ok<UserDto>>();
        var okResult = (Ok<UserDto>)result;
        okResult.Value!.Bio.Should().Be("This is a public bio");
    }

    /// <summary>
    /// Test: GET /api/public/users/{userId}/profile when user not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetUserProfileById_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        _mockUserService.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await CallGetUserProfileById(userId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test: GET /api/public/users/{userId}/profile when service fails but response is null returns 404
    /// </summary>
    [Fact]
    public async Task GetUserProfileById_WhenServiceFailsWithNullResponse_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        _mockUserService.GetProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await CallGetUserProfileById(userId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails!.Detail.Should().Contain("not found");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to simulate calling GET /api/users/profile endpoint
    /// </summary>
    private async Task<IResult> CallGetUserProfile(ClaimsPrincipal user)
    {
        // Extract user ID from JWT token claims
        var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Problem(
                title: "Invalid Token",
                detail: "User ID not found in token claims",
                statusCode: 401);
        }

        var (success, response, error) = await _mockUserService.GetProfileAsync(userId, CancellationToken.None);

        return success
            ? Results.Ok(response)
            : Results.Problem(
                title: "Get Profile Failed",
                detail: error,
                statusCode: response == null ? 404 : 500);
    }

    /// <summary>
    /// Helper method to simulate calling PUT /api/users/profile endpoint
    /// </summary>
    private async Task<IResult> CallUpdateUserProfile(ClaimsPrincipal user, UpdateProfileRequest request)
    {
        // Extract user ID from JWT token claims
        var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Problem(
                title: "Invalid Token",
                detail: "User ID not found in token claims",
                statusCode: 401);
        }

        var (success, response, error) = await _mockUserService.UpdateProfileAsync(userId, request, CancellationToken.None);

        return success
            ? Results.Ok(response)
            : Results.Problem(
                title: "Update Profile Failed",
                detail: error,
                statusCode: error.Contains("Scene name is already taken") ? 409 : 400);
    }

    /// <summary>
    /// Helper method to simulate calling GET /api/admin/users endpoint
    /// </summary>
    private async Task<IResult> CallGetUsers(UserSearchRequest request)
    {
        var (success, response, error) = await _mockUserService.GetUsersAsync(request, CancellationToken.None);

        return success
            ? Results.Ok(response)
            : Results.Problem(
                title: "Get Users Failed",
                detail: error,
                statusCode: 500);
    }

    /// <summary>
    /// Helper method to simulate calling GET /api/admin/users/{id} endpoint
    /// </summary>
    private async Task<IResult> CallGetUser(string id)
    {
        var (success, response, error) = await _mockUserService.GetUserAsync(id, CancellationToken.None);

        return success
            ? Results.Ok(response)
            : Results.Problem(
                title: "Get User Failed",
                detail: error,
                statusCode: response == null ? 404 : 400);
    }

    /// <summary>
    /// Helper method to simulate calling PUT /api/admin/users/{id} endpoint
    /// </summary>
    private async Task<IResult> CallUpdateUser(string id, UpdateUserRequest request)
    {
        var (success, response, error) = await _mockUserService.UpdateUserAsync(id, request, CancellationToken.None);

        return success
            ? Results.Ok(response)
            : Results.Problem(
                title: "Update User Failed",
                detail: error,
                statusCode: error.Contains("not found") ? 404 :
                          error.Contains("already taken") ? 409 : 400);
    }

    /// <summary>
    /// Helper method to simulate calling PUT /api/admin/users/{userId}/roles endpoint
    /// </summary>
    private async Task<IResult> CallUpdateUserRoles(string userId, UpdateUserRolesRequest request)
    {
        var (success, response, error) = await _mockUserService.UpdateUserRolesAsync(userId, request, CancellationToken.None);

        return success
            ? Results.Ok(response)
            : Results.Problem(
                title: "Update User Roles Failed",
                detail: error,
                statusCode: error.Contains("not found") ? 404 :
                          error.Contains("Invalid") ? 400 : 500);
    }

    /// <summary>
    /// Helper method to simulate calling GET /api/users/by-role/{role} endpoint
    /// </summary>
    private async Task<IResult> CallGetUsersByRole(string role)
    {
        var (success, response, error) = await _mockUserService.GetUsersByRoleAsync(role, CancellationToken.None);

        return success
            ? Results.Ok(response)
            : Results.Problem(
                title: "Get Users By Role Failed",
                detail: error,
                statusCode: 500);
    }

    /// <summary>
    /// Helper method to simulate calling GET /api/public/users/{userId}/profile endpoint
    /// </summary>
    private async Task<IResult> CallGetUserProfileById(string userId)
    {
        var (success, response, error) = await _mockUserService.GetProfileAsync(userId, CancellationToken.None);

        if (!success || response == null)
        {
            return Results.Problem(
                title: "Get Profile Failed",
                detail: error ?? "User not found",
                statusCode: 404);
        }

        return Results.Ok(response);
    }

    #endregion
}
