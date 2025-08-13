extern alias WitchCityRopeWeb;
extern alias WitchCityRopeApi;

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using WitchCityRope.IntegrationTests.Extensions;
using Xunit;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Identity;
using FluentAssertions;

namespace WitchCityRope.IntegrationTests.Admin;

/// <summary>
/// Integration tests for AdminUsersController endpoints
/// Tests real HTTP calls to admin/users API endpoints with authentication/authorization
/// </summary>
[Collection("PostgreSQL Integration Tests")]
public class AdminUsersControllerTests : IClassFixture<WebApplicationFactory<WitchCityRopeWeb::Program>>, IDisposable
{
    private readonly WebApplicationFactory<WitchCityRopeWeb::Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public AdminUsersControllerTests(WebApplicationFactory<WitchCityRopeWeb::Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _output = output;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUsers_WithAdminAuth_ReturnsPagedUserList()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);
        
        // Create test users for filtering
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
        
        var testUser1 = new WitchCityRopeUser(
            encryptedLegalName: "ENCRYPTED_TestUser1",
            sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create($"TestUser1_{Guid.NewGuid():N}"),
            email: WitchCityRope.Core.ValueObjects.EmailAddress.Create($"testuser1_{Guid.NewGuid():N}@example.com"),
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            role: UserRole.Member
        );
        
        var testUser2 = new WitchCityRopeUser(
            encryptedLegalName: "ENCRYPTED_TestUser2",
            sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create($"TestUser2_{Guid.NewGuid():N}"),
            email: WitchCityRope.Core.ValueObjects.EmailAddress.Create($"testuser2_{Guid.NewGuid():N}@example.com"),
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            role: UserRole.Organizer
        );

        await userManager.CreateAsync(testUser1, "Test123!");
        await userManager.CreateAsync(testUser2, "Test123!");

        // Act
        var response = await _client.GetAsync("api/admin/users?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedUserResult>();
        content.Should().NotBeNull();
        content!.Users.Should().NotBeEmpty();
        content.TotalCount.Should().BeGreaterThan(0);
        content.CurrentPage.Should().Be(1);
        content.PageSize.Should().Be(10);
        
        // Should contain our test users
        content.Users.Should().Contain(u => u.SceneName.Contains("TestUser1"));
        content.Users.Should().Contain(u => u.SceneName.Contains("TestUser2"));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUsers_WithSearchFilter_ReturnsFilteredResults()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);
        
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var searchableSceneName = $"SearchableUser_{uniqueId}";
        
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
        
        var searchableUser = new WitchCityRopeUser(
            encryptedLegalName: "ENCRYPTED_SearchableUser",
            sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create(searchableSceneName),
            email: WitchCityRope.Core.ValueObjects.EmailAddress.Create($"searchable_{uniqueId}@example.com"),
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            role: UserRole.Member
        );

        await userManager.CreateAsync(searchableUser, "Test123!");

        // Act - Search for the unique searchable user
        var response = await _client.GetAsync($"api/admin/users?searchTerm={uniqueId}&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedUserResult>();
        content.Should().NotBeNull();
        content!.Users.Should().HaveCount(1);
        content.Users.First().SceneName.Should().Be(searchableSceneName);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUsers_WithRoleFilter_ReturnsUsersOfSpecificRole()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);
        
        // Act - Filter by Organizer role
        var response = await _client.GetAsync($"api/admin/users?role={UserRole.Organizer}&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedUserResult>();
        content.Should().NotBeNull();
        content!.Users.Should().AllSatisfy(user => user.Role.Should().Be(UserRole.Organizer));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUsers_WithoutAdminAuth_ReturnsUnauthorized()
    {
        // Arrange - No authentication

        // Act
        var response = await _client.GetAsync("api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserById_WithValidId_ReturnsUserDetails()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);
        
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
        
        var testUser = new WitchCityRopeUser(
            encryptedLegalName: "ENCRYPTED_DetailedUser",
            sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create($"DetailedUser_{Guid.NewGuid():N}"),
            email: WitchCityRope.Core.ValueObjects.EmailAddress.Create($"detailed_{Guid.NewGuid():N}@example.com"),
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            role: UserRole.Member
        );

        testUser.UpdatePronouns("they/them");
        testUser.UpdatePronouncedName("Detailed User");

        await userManager.CreateAsync(testUser, "Test123!");

        // Act
        var response = await _client.GetAsync($"api/admin/users/{testUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<AdminUserDto>();
        content.Should().NotBeNull();
        content!.Id.Should().Be(testUser.Id);
        content.SceneName.Should().Be(testUser.SceneNameValue);
        content.Email.Should().Be(testUser.Email);
        content.Role.Should().Be(UserRole.Member);
        content.Pronouns.Should().Be("they/them");
        content.PronouncedName.Should().Be("Detailed User");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"api/admin/users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateUser_WithValidData_UpdatesUserSuccessfully()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);
        
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
        
        var testUser = new WitchCityRopeUser(
            encryptedLegalName: "ENCRYPTED_UpdateableUser",
            sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create($"UpdateableUser_{Guid.NewGuid():N}"),
            email: WitchCityRope.Core.ValueObjects.EmailAddress.Create($"updateable_{Guid.NewGuid():N}@example.com"),
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            role: UserRole.Member
        );

        await userManager.CreateAsync(testUser, "Test123!");

        var updateDto = new UpdateUserDto
        {
            SceneName = $"UpdatedUser_{Guid.NewGuid():N}",
            Role = UserRole.Organizer,
            IsActive = true,
            IsVetted = true,
            Pronouns = "she/her",
            PronouncedName = "Updated User",
            AdminNote = "Updated for integration test"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"api/admin/users/{testUser.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify the changes were persisted
        var updatedUser = await userManager.FindByIdAsync(testUser.Id.ToString());
        updatedUser.Should().NotBeNull();
        updatedUser!.Role.Should().Be(UserRole.Organizer);
        updatedUser.Pronouns.Should().Be("she/her");
        updatedUser.PronouncedName.Should().Be("Updated User");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateUser_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);
        
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
        
        var testUser = new WitchCityRopeUser(
            encryptedLegalName: "ENCRYPTED_InvalidUpdateUser",
            sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create($"InvalidUpdateUser_{Guid.NewGuid():N}"),
            email: WitchCityRope.Core.ValueObjects.EmailAddress.Create($"invalidupdate_{Guid.NewGuid():N}@example.com"),
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            role: UserRole.Member
        );

        await userManager.CreateAsync(testUser, "Test123!");

        var invalidUpdateDto = new UpdateUserDto
        {
            SceneName = "", // Invalid empty scene name
            Role = UserRole.Administrator // This might cause validation issues
        };

        // Act
        var response = await _client.PutAsJsonAsync($"api/admin/users/{testUser.Id}", invalidUpdateDto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ResetUserPassword_WithValidData_ResetsPasswordSuccessfully()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);
        
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
        
        var testUser = new WitchCityRopeUser(
            encryptedLegalName: "ENCRYPTED_PasswordResetUser",
            sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create($"PasswordResetUser_{Guid.NewGuid():N}"),
            email: WitchCityRope.Core.ValueObjects.EmailAddress.Create($"passwordreset_{Guid.NewGuid():N}@example.com"),
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            role: UserRole.Member
        );

        await userManager.CreateAsync(testUser, "OldPassword123!");

        var resetDto = new ResetUserPasswordDto
        {
            NewPassword = "NewSecurePassword123!",
            RequirePasswordChangeOnLogin = true,
            AdminNote = "Password reset for integration test"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"api/admin/users/{testUser.Id}/reset-password", resetDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify the password was changed by attempting to sign in with new password
        var updatedUser = await userManager.FindByIdAsync(testUser.Id.ToString());
        var passwordValid = await userManager.CheckPasswordAsync(updatedUser!, "NewSecurePassword123!");
        passwordValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ManageUserLockout_LockUser_LocksUserSuccessfully()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);
        
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
        
        var testUser = new WitchCityRopeUser(
            encryptedLegalName: "ENCRYPTED_LockoutUser",
            sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create($"LockoutUser_{Guid.NewGuid():N}"),
            email: WitchCityRope.Core.ValueObjects.EmailAddress.Create($"lockout_{Guid.NewGuid():N}@example.com"),
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            role: UserRole.Member
        );

        await userManager.CreateAsync(testUser, "Test123!");

        var lockoutDto = new UserLockoutDto
        {
            IsLocked = true,
            LockoutEnd = DateTime.UtcNow.AddDays(7),
            Reason = "Integration test lockout"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"api/admin/users/{testUser.Id}/lockout", lockoutDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify the user is locked out
        var lockedUser = await userManager.FindByIdAsync(testUser.Id.ToString());
        lockedUser.Should().NotBeNull();
        lockedUser!.LockoutEnd.Should().NotBeNull();
        lockedUser.LockoutEnd!.Value.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ManageUserLockout_UnlockUser_UnlocksUserSuccessfully()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);
        
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
        
        var testUser = new WitchCityRopeUser(
            encryptedLegalName: "ENCRYPTED_UnlockUser",
            sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create($"UnlockUser_{Guid.NewGuid():N}"),
            email: WitchCityRope.Core.ValueObjects.EmailAddress.Create($"unlock_{Guid.NewGuid():N}@example.com"),
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            role: UserRole.Member
        );

        await userManager.CreateAsync(testUser, "Test123!");
        
        // Lock the user first
        await userManager.SetLockoutEndDateAsync(testUser, DateTimeOffset.UtcNow.AddDays(7));

        var unlockDto = new UserLockoutDto
        {
            IsLocked = false,
            Reason = "Integration test unlock"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"api/admin/users/{testUser.Id}/lockout", unlockDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify the user is unlocked
        var unlockedUser = await userManager.FindByIdAsync(testUser.Id.ToString());
        unlockedUser.Should().NotBeNull();
        (unlockedUser!.LockoutEnd == null || unlockedUser.LockoutEnd <= DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserStats_WithAdminAuth_ReturnsStatistics()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);

        // Act
        var response = await _client.GetAsync("api/admin/users/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<UserStatsDto>();
        content.Should().NotBeNull();
        content!.TotalUsers.Should().BeGreaterThan(0);
        content.UsersByRole.Should().NotBeEmpty();
        content.CalculatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetRoles_WithAdminAuth_ReturnsAvailableRoles()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);

        // Act
        var response = await _client.GetAsync("api/admin/users/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<List<RoleDto>>();
        content.Should().NotBeNull();
        content!.Should().NotBeEmpty();
        content.Should().Contain(r => r.Name == "Member");
        content.Should().Contain(r => r.Name == "Organizer");
        content.Should().Contain(r => r.Name == "Administrator");
        
        // Verify roles are properly ordered by priority
        var priorities = content.Select(r => r.Priority).ToList();
        priorities.Should().BeInAscendingOrder();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteUserManagementWorkflow_CreateSearchUpdateDelete_WorksEndToEnd()
    {
        // Arrange
        await _client.AuthenticateAsAdminAsync(_factory);
        
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var testSceneName = $"WorkflowUser_{uniqueId}";
        var testEmail = $"workflow_{uniqueId}@example.com";
        
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
        
        // Step 1: Create a test user
        var testUser = new WitchCityRopeUser(
            encryptedLegalName: "ENCRYPTED_WorkflowUser",
            sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create(testSceneName),
            email: WitchCityRope.Core.ValueObjects.EmailAddress.Create(testEmail),
            dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            role: UserRole.Member
        );
        
        await userManager.CreateAsync(testUser, "Test123!");

        // Step 2: Search and find the user
        var searchResponse = await _client.GetAsync($"api/admin/users?searchTerm={uniqueId}");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var searchResult = await searchResponse.Content.ReadFromJsonAsync<PagedUserResult>();
        searchResult!.Users.Should().HaveCount(1);
        var foundUser = searchResult.Users.First();
        foundUser.SceneName.Should().Be(testSceneName);

        // Step 3: Get user details
        var detailResponse = await _client.GetAsync($"api/admin/users/{foundUser.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var userDetail = await detailResponse.Content.ReadFromJsonAsync<AdminUserDto>();
        userDetail!.Id.Should().Be(foundUser.Id);
        userDetail.Email.Should().Be(testEmail);

        // Step 4: Update the user
        var updateDto = new UpdateUserDto
        {
            Role = UserRole.Organizer,
            IsVetted = true,
            Pronouns = "he/him",
            AdminNote = "Promoted to organizer via integration test"
        };
        
        var updateResponse = await _client.PutAsJsonAsync($"api/admin/users/{foundUser.Id}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Step 5: Verify the update
        var verifyResponse = await _client.GetAsync($"api/admin/users/{foundUser.Id}");
        var updatedUser = await verifyResponse.Content.ReadFromJsonAsync<AdminUserDto>();
        updatedUser!.Role.Should().Be(UserRole.Organizer);
        updatedUser.IsVetted.Should().BeTrue();
        updatedUser.Pronouns.Should().Be("he/him");
        
        _output.WriteLine($"Successfully completed user management workflow for user {testSceneName}");
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}