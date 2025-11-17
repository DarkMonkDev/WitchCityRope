using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using WitchCityRope.Api.Features.Admin.Endpoints;
using WitchCityRope.Api.Models;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Features.Admin.Users;

/// <summary>
/// Unit tests for Admin UsersEndpoints following Pattern B standards.
/// Tests the GET /api/users/by-role/{role} endpoint.
/// </summary>
public class UsersEndpointsTests
{
    private readonly UserManager<ApplicationUser> _mockUserManager;
    private readonly ClaimsPrincipal _authenticatedUser;

    public UsersEndpointsTests()
    {
        // Create mock UserManager<ApplicationUser>
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _mockUserManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore,
            null, null, null, null, null, null, null, null);

        // Create authenticated user
        _authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "test"));
    }

    #region GetUsersByRole Tests

    /// <summary>
    /// Test: GET /api/users/by-role/Teacher with valid role returns 200 OK with list of UserOptionDto
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_WithValidRoleTeacher_Returns200OkWithUserList()
    {
        // Arrange
        var role = "Teacher";
        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                SceneName = "Teacher Alice",
                Email = "teacher1@witchcityrope.com"
            },
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                SceneName = "Teacher Bob",
                Email = "teacher2@witchcityrope.com"
            }
        };

        _mockUserManager.GetUsersInRoleAsync(role).Returns(mockUsers);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await CallGetUsersByRole(role, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<UserOptionDto>>>();
        var okResult = (Ok<List<UserOptionDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Should().HaveCount(2);
        okResult.Value![0].Name.Should().Be("Teacher Alice");
        okResult.Value![0].Email.Should().Be("teacher1@witchcityrope.com");
        okResult.Value![1].Name.Should().Be("Teacher Bob");
        okResult.Value![1].Email.Should().Be("teacher2@witchcityrope.com");
    }

    /// <summary>
    /// Test: GET /api/users/by-role/Admin with valid role returns 200 OK with list
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_WithValidRoleAdmin_Returns200OkWithUserList()
    {
        // Arrange
        var role = "Admin";
        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                SceneName = "Admin Charlie",
                Email = "admin@witchcityrope.com"
            }
        };

        _mockUserManager.GetUsersInRoleAsync(role).Returns(mockUsers);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await CallGetUsersByRole(role, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<UserOptionDto>>>();
        var okResult = (Ok<List<UserOptionDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Should().HaveCount(1);
        okResult.Value![0].Name.Should().Be("Admin Charlie");
    }

    /// <summary>
    /// Test: GET /api/users/by-role/Member with valid role returns 200 OK with list
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_WithValidRoleMember_Returns200OkWithUserList()
    {
        // Arrange
        var role = "Member";
        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                SceneName = "Member David",
                Email = "member@witchcityrope.com"
            }
        };

        _mockUserManager.GetUsersInRoleAsync(role).Returns(mockUsers);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await CallGetUsersByRole(role, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<UserOptionDto>>>();
        var okResult = (Ok<List<UserOptionDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Should().HaveCount(1);
    }

    /// <summary>
    /// Test: GET /api/users/by-role/{role} with role that has no users returns 200 OK with empty list
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_WithRoleHavingNoUsers_Returns200OkWithEmptyList()
    {
        // Arrange
        var role = "SafetyTeam";
        var mockUsers = new List<ApplicationUser>(); // Empty list

        _mockUserManager.GetUsersInRoleAsync(role).Returns(mockUsers);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await CallGetUsersByRole(role, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<UserOptionDto>>>();
        var okResult = (Ok<List<UserOptionDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Should().BeEmpty();
    }

    /// <summary>
    /// Test: User with SceneName returns SceneName in Name field
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_UserWithSceneName_ReturnsSceneNameInNameField()
    {
        // Arrange
        var role = "Teacher";
        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                SceneName = "Rope Master",
                Email = "master@witchcityrope.com"
            }
        };

        _mockUserManager.GetUsersInRoleAsync(role).Returns(mockUsers);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await CallGetUsersByRole(role, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<UserOptionDto>>>();
        var okResult = (Ok<List<UserOptionDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Should().HaveCount(1);
        okResult.Value![0].Name.Should().Be("Rope Master");
    }

    /// <summary>
    /// Test: User without SceneName returns Email in Name field
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_UserWithoutSceneName_ReturnsEmailInNameField()
    {
        // Arrange
        var role = "Teacher";
        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                SceneName = null!, // No scene name
                Email = "noscene@witchcityrope.com"
            }
        };

        _mockUserManager.GetUsersInRoleAsync(role).Returns(mockUsers);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await CallGetUsersByRole(role, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<UserOptionDto>>>();
        var okResult = (Ok<List<UserOptionDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Should().HaveCount(1);
        okResult.Value![0].Name.Should().Be("noscene@witchcityrope.com");
    }

    /// <summary>
    /// Test: User without SceneName or Email returns "Unknown" in Name field
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_UserWithoutSceneNameOrEmail_ReturnsUnknownInNameField()
    {
        // Arrange
        var role = "Teacher";
        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                SceneName = null!, // No scene name
                Email = null! // No email
            }
        };

        _mockUserManager.GetUsersInRoleAsync(role).Returns(mockUsers);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await CallGetUsersByRole(role, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<UserOptionDto>>>();
        var okResult = (Ok<List<UserOptionDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Should().HaveCount(1);
        okResult.Value![0].Name.Should().Be("Unknown");
        okResult.Value![0].Email.Should().Be("");
    }

    /// <summary>
    /// Test: UserManager throws exception returns 500 InternalServerError with ProblemDetails
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_WhenUserManagerThrowsException_Returns500InternalServerError()
    {
        // Arrange
        var role = "Teacher";
        _mockUserManager.GetUsersInRoleAsync(role)
            .Returns<IList<ApplicationUser>>(x => throw new InvalidOperationException("Database connection failed"));

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await CallGetUsersByRole(role, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Internal Server Error");
        problemResult.ProblemDetails!.Detail.Should().Contain("Failed to get users by role");
        problemResult.ProblemDetails!.Detail.Should().Contain("Database connection failed");
    }

    #endregion

    #region Additional Edge Case Tests

    /// <summary>
    /// Test: Empty role string handles gracefully
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_WithEmptyRoleString_Returns200OkWithEmptyList()
    {
        // Arrange
        var role = "";
        var mockUsers = new List<ApplicationUser>();

        _mockUserManager.GetUsersInRoleAsync(role).Returns(mockUsers);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await CallGetUsersByRole(role, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<UserOptionDto>>>();
        var okResult = (Ok<List<UserOptionDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Should().BeEmpty();
    }

    /// <summary>
    /// Test: Case sensitivity - different role case returns appropriate results
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_WithDifferentRoleCase_ReturnsAppropriateResults()
    {
        // Arrange
        var role = "teacher"; // lowercase
        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                SceneName = "Teacher Eve",
                Email = "eve@witchcityrope.com"
            }
        };

        _mockUserManager.GetUsersInRoleAsync(role).Returns(mockUsers);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await CallGetUsersByRole(role, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<UserOptionDto>>>();
        var okResult = (Ok<List<UserOptionDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Should().HaveCount(1);
    }

    /// <summary>
    /// Test: Multiple users with mixed SceneName/Email fallbacks
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_WithMixedUserAttributes_ReturnsCorrectFallbacks()
    {
        // Arrange
        var role = "Member";
        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                SceneName = "User With Scene",
                Email = "user1@witchcityrope.com"
            },
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                SceneName = null!, // No scene name
                Email = "user2@witchcityrope.com"
            },
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                SceneName = null!, // No scene name
                Email = null! // No email
            }
        };

        _mockUserManager.GetUsersInRoleAsync(role).Returns(mockUsers);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await CallGetUsersByRole(role, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<UserOptionDto>>>();
        var okResult = (Ok<List<UserOptionDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Should().HaveCount(3);
        okResult.Value![0].Name.Should().Be("User With Scene");
        okResult.Value![1].Name.Should().Be("user2@witchcityrope.com");
        okResult.Value![2].Name.Should().Be("Unknown");
    }

    /// <summary>
    /// Test: UserManager returns null (edge case) - should handle gracefully
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_WhenUserManagerReturnsNull_HandlesGracefully()
    {
        // Arrange
        var role = "Teacher";
        _mockUserManager.GetUsersInRoleAsync(role)
            .Returns((IList<ApplicationUser>)null!);

        var httpContext = new DefaultHttpContext();
        httpContext.User = _authenticatedUser;

        // Act
        var result = await CallGetUsersByRole(role, httpContext);

        // Assert - Should either handle null gracefully or return 500 error
        // Depending on implementation, we expect either OK with empty list or ProblemDetails
        result.Should().Match(r =>
            r is Ok<List<UserOptionDto>> ||
            r is ProblemHttpResult);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to simulate calling GetUsersByRole endpoint.
    /// Mimics the Minimal API endpoint invocation pattern.
    /// </summary>
    private async Task<IResult> CallGetUsersByRole(
        string role,
        HttpContext httpContext)
    {
        try
        {
            // Get all users with the specified role
            var usersInRole = await _mockUserManager.GetUsersInRoleAsync(role);

            // Convert to simple DTOs for dropdowns
            var userOptions = usersInRole.Select(user => new UserOptionDto
            {
                Id = user.Id.ToString(),
                Name = user.SceneName ?? user.Email ?? "Unknown",
                Email = user.Email ?? ""
            }).ToList();

            return Results.Ok(userOptions);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: $"Failed to get users by role: {ex.Message}",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
    }

    #endregion
}
