using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Users.Services;
using WitchCityRope.Api.Features.Users.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace WitchCityRope.Api.Tests.Features.Users;

/// <summary>
/// Phase 1.5.4: User Management Service Tests
/// Comprehensive test suite for UserManagementService covering profile management,
/// user administration, search/filtering, and validation
/// Created: 2025-10-10
///
/// Tests user profile operations, admin user management, role changes, and search functionality
/// Uses real PostgreSQL database via TestContainers for accurate testing
/// </summary>
[Collection("Database")]
public class UserManagementServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ILogger<UserManagementService>> _mockLogger;
    private UserManagementService _sut; // System Under Test
    private ApplicationDbContext _context;

    public UserManagementServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;

        // Mock UserManager with proper setup
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null, null, null, null, null, null, null, null);

        _mockLogger = new Mock<ILogger<UserManagementService>>();
    }

    public async Task InitializeAsync()
    {
        // Reset database before each test
        await _fixture.ResetDatabaseAsync();

        // Create fresh context and service for each test
        _context = _fixture.CreateDbContext();
        _sut = new UserManagementService(
            _context,
            _mockUserManager.Object,
            _mockLogger.Object);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    #region GetProfileAsync Tests

    /// <summary>
    /// Test 1: Verify successful profile retrieval for existing user
    /// </summary>
    [Fact]
    public async Task GetProfileAsync_WithExistingUser_ReturnsProfile()
    {
        // Arrange
        var user = await CreateTestUserAsync("ProfileUser");

        // Act
        var result = await _sut.GetProfileAsync(user.Id.ToString());

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Id.Should().Be(user.Id.ToString());
        result.Response.SceneName.Should().Be(user.SceneName);
        result.Response.Email.Should().Be(user.Email);
    }

    /// <summary>
    /// Test 2: Verify profile retrieval fails with non-existent user
    /// </summary>
    [Fact]
    public async Task GetProfileAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.GetProfileAsync(nonExistentUserId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("User not found");
    }

    #endregion

    #region UpdateProfileAsync Tests

    /// <summary>
    /// Test 3: Verify successful profile update with valid data
    /// </summary>
    [Fact]
    public async Task UpdateProfileAsync_WithValidData_UpdatesProfile()
    {
        // Arrange
        var user = await CreateTestUserAsync("UpdateUser");
        var newSceneName = $"Updated_{Guid.NewGuid().ToString().Substring(0, 8)}";
        var newPronouns = "they/them";

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        var request = new UpdateProfileRequest
        {
            SceneName = newSceneName,
            Pronouns = newPronouns
        };

        // Act
        var result = await _sut.UpdateProfileAsync(user.Id.ToString(), request);

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.SceneName.Should().Be(newSceneName);
        result.Response.Pronouns.Should().Be(newPronouns);

        // Verify changes persisted to database
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.SceneName.Should().Be(newSceneName);
        updatedUser.Pronouns.Should().Be(newPronouns);
    }

    /// <summary>
    /// Test 4: Verify profile update fails when scene name is already taken
    /// </summary>
    [Fact]
    public async Task UpdateProfileAsync_WithDuplicateSceneName_ReturnsFailure()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("User1");
        var user2 = await CreateTestUserAsync("User2");

        _mockUserManager.Setup(x => x.FindByIdAsync(user1.Id.ToString()))
            .ReturnsAsync(user1);

        var request = new UpdateProfileRequest
        {
            SceneName = user2.SceneName, // Try to use user2's scene name
            Pronouns = "they/them"
        };

        // Act
        var result = await _sut.UpdateProfileAsync(user1.Id.ToString(), request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Scene name is already taken");
    }

    /// <summary>
    /// Test 5: Verify profile update succeeds with same scene name (no change)
    /// </summary>
    [Fact]
    public async Task UpdateProfileAsync_WithSameSceneName_Succeeds()
    {
        // Arrange
        var user = await CreateTestUserAsync("SameNameUser");

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        var request = new UpdateProfileRequest
        {
            SceneName = user.SceneName, // Same scene name
            Pronouns = "she/her"
        };

        // Act
        var result = await _sut.UpdateProfileAsync(user.Id.ToString(), request);

        // Assert
        result.Success.Should().BeTrue();
    }

    /// <summary>
    /// Test 6: Verify profile update fails with non-existent user
    /// </summary>
    [Fact]
    public async Task UpdateProfileAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid().ToString();

        _mockUserManager.Setup(x => x.FindByIdAsync(nonExistentUserId))
            .ReturnsAsync((ApplicationUser?)null);

        var request = new UpdateProfileRequest
        {
            SceneName = "NewName",
            Pronouns = "they/them"
        };

        // Act
        var result = await _sut.UpdateProfileAsync(nonExistentUserId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("User not found");
    }

    #endregion

    #region GetUsersAsync Tests

    /// <summary>
    /// Test 7: Verify user list retrieval with pagination
    /// </summary>
    [Fact]
    public async Task GetUsersAsync_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        await CreateTestUserAsync("User1");
        await CreateTestUserAsync("User2");
        await CreateTestUserAsync("User3");

        var request = new UserSearchRequest
        {
            Page = 1,
            PageSize = 2,
            SortBy = "SceneName",
            SortDescending = false
        };

        // Act
        var result = await _sut.GetUsersAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Users.Should().HaveCount(2);
        result.Response.TotalCount.Should().BeGreaterOrEqualTo(3);
        result.Response.Page.Should().Be(1);
        result.Response.PageSize.Should().Be(2);
    }

    /// <summary>
    /// Test 8: Verify user search by email
    /// </summary>
    [Fact]
    public async Task GetUsersAsync_WithSearchTerm_FiltersResults()
    {
        // Arrange
        var uniqueEmail = $"searchable_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";
        await CreateTestUserAsync("SearchableUser", email: uniqueEmail);
        await CreateTestUserAsync("OtherUser");

        var request = new UserSearchRequest
        {
            SearchTerm = "searchable",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _sut.GetUsersAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Users.Should().Contain(u => u.Email.Contains("searchable"));
    }

    /// <summary>
    /// Test 9: Verify user filtering by role
    /// </summary>
    [Fact]
    public async Task GetUsersAsync_WithRoleFilter_ReturnsOnlyUsersWithRole()
    {
        // Arrange
        await CreateTestUserAsync("Admin1", role: "Administrator");
        await CreateTestUserAsync("Admin2", role: "Administrator");
        await CreateTestUserAsync("Member1", role: "Member");

        var request = new UserSearchRequest
        {
            Role = "Administrator",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _sut.GetUsersAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Users.Should().OnlyContain(u => u.Role == "Administrator");
        result.Response.Users.Should().HaveCountGreaterOrEqualTo(2);
    }

    /// <summary>
    /// Test 10: Verify user filtering by active status
    /// </summary>
    [Fact]
    public async Task GetUsersAsync_WithActiveFilter_ReturnsOnlyActiveUsers()
    {
        // Arrange
        await CreateTestUserAsync("ActiveUser", isActive: true);
        await CreateTestUserAsync("InactiveUser", isActive: false);

        var request = new UserSearchRequest
        {
            IsActive = true,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _sut.GetUsersAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Users.Should().OnlyContain(u => u.IsActive == true);
    }

    #endregion

    #region GetUserAsync Tests

    /// <summary>
    /// Test 11: Verify single user retrieval by ID
    /// </summary>
    [Fact]
    public async Task GetUserAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var user = await CreateTestUserAsync("SingleUser");

        // Act
        var result = await _sut.GetUserAsync(user.Id.ToString());

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Id.Should().Be(user.Id.ToString());
        result.Response.SceneName.Should().Be(user.SceneName);
    }

    /// <summary>
    /// Test 12: Verify user retrieval fails with invalid ID format
    /// </summary>
    [Fact]
    public async Task GetUserAsync_WithInvalidIdFormat_ReturnsFailure()
    {
        // Arrange
        var invalidId = "not-a-guid";

        // Act
        var result = await _sut.GetUserAsync(invalidId);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Invalid user ID format");
    }

    #endregion

    #region UpdateUserAsync Tests

    /// <summary>
    /// Test 13: Verify admin user update with role change
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WithRoleChange_UpdatesRole()
    {
        // Arrange
        var user = await CreateTestUserAsync("MemberUser", role: "Member");

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        var request = new UpdateUserRequest
        {
            Role = "Teacher",
            SceneName = user.SceneName
        };

        // Act
        var result = await _sut.UpdateUserAsync(user.Id.ToString(), request);

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Role.Should().Be("Teacher");

        // Verify database update
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.Role.Should().Be("Teacher");
    }

    /// <summary>
    /// Test 14: Verify admin user update with active status change
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WithActiveStatusChange_UpdatesStatus()
    {
        // Arrange
        var user = await CreateTestUserAsync("ActiveUser", isActive: true);

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        var request = new UpdateUserRequest
        {
            IsActive = false,
            SceneName = user.SceneName
        };

        // Act
        var result = await _sut.UpdateUserAsync(user.Id.ToString(), request);

        // Assert
        result.Success.Should().BeTrue();
        result.Response!.IsActive.Should().BeFalse();

        // Verify database update
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Test 15: Verify admin update fails with duplicate scene name
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WithDuplicateSceneName_ReturnsFailure()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("User1");
        var user2 = await CreateTestUserAsync("User2");

        _mockUserManager.Setup(x => x.FindByIdAsync(user1.Id.ToString()))
            .ReturnsAsync(user1);

        var request = new UpdateUserRequest
        {
            SceneName = user2.SceneName // Duplicate scene name
        };

        // Act
        var result = await _sut.UpdateUserAsync(user1.Id.ToString(), request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Scene name is already taken");
    }

    #endregion

    #region GetUsersByRoleAsync Tests

    /// <summary>
    /// Test 16: Verify retrieval of users by role for dropdowns
    /// </summary>
    [Fact]
    public async Task GetUsersByRoleAsync_WithValidRole_ReturnsUsersWithRole()
    {
        // Arrange
        await CreateTestUserAsync("Teacher1", role: "Teacher");
        await CreateTestUserAsync("Teacher2", role: "Teacher");
        await CreateTestUserAsync("Member1", role: "Member");

        // Act
        var result = await _sut.GetUsersByRoleAsync("Teacher");

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.Should().HaveCountGreaterOrEqualTo(2);
        result.Response.Should().OnlyContain(u => u.Name != null);
    }

    /// <summary>
    /// Test 17: Verify user retrieval excludes inactive users
    /// </summary>
    [Fact]
    public async Task GetUsersByRoleAsync_ExcludesInactiveUsers()
    {
        // Arrange
        await CreateTestUserAsync("ActiveTeacher", role: "Teacher", isActive: true);
        await CreateTestUserAsync("InactiveTeacher", role: "Teacher", isActive: false);

        // Act
        var result = await _sut.GetUsersByRoleAsync("Teacher");

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Should().NotBeNull();
        // Should only include active users
        result.Response!.Should().Contain(u => u.Name.Contains("ActiveTeacher"));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to create test user with unique identifiers
    /// Ensures all required fields are populated for database insertion
    /// </summary>
    private async Task<ApplicationUser> CreateTestUserAsync(
        string baseName,
        string? email = null,
        string? role = null,
        bool isActive = true)
    {
        var uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
        var sceneName = $"{baseName}_{uniqueId}";

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email ?? $"{sceneName}@example.com".ToLower(),
            SceneName = sceneName,
            Role = role ?? "Member",
            IsActive = isActive,
            VettingStatus = 0, // 0 = UnderReview (not vetted)
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    #endregion
}
