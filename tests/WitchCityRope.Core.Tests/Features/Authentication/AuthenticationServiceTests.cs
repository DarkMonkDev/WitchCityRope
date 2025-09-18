using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Authentication.Models;
using WitchCityRope.Api.Features.Authentication.Services;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Core.Tests.Features.Authentication;

/// <summary>
/// Tests for AuthenticationService following Vertical Slice Architecture patterns
/// All tests are marked as [Skip] since Authentication feature is not yet implemented
/// These tests preserve critical business logic requirements for future implementation
/// </summary>
public class AuthenticationServiceTests
{
    /// <summary>
    /// Mock database context for testing (tests are skipped, so mock setup is minimal)
    /// </summary>
    private readonly Mock<ApplicationDbContext> MockDbContext = new();

    /// <summary>
    /// Verify user registration with valid data succeeds
    /// Tests core business rule: Valid users should be able to register
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task RegisterUserAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        var request = new RegisterUserRequestBuilder()
            .WithEmail("newuser@witchcityrope.com")
            .WithPassword("SecurePassword123!")
            .Build();

        // Act
        var (success, response, error) = await authService.RegisterUserAsync(request);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Email.Should().Be(request.Email);
        error.Should().BeNull();

        // Verify user was saved to database
        var userExists = await MockDbContext.Object.Users.AnyAsync(u => u.Email == request.Email);
        userExists.Should().BeTrue();
    }

    /// <summary>
    /// Verify user registration with duplicate email fails
    /// Tests business rule: Email addresses must be unique
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task RegisterUserAsync_WithDuplicateEmail_ShouldFail()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        var existingEmail = "duplicate@witchcityrope.com";

        // First registration
        await authService.RegisterUserAsync(new RegisterUserRequestBuilder()
            .WithEmail(existingEmail)
            .Build());

        // Act - Second registration with same email
        var (success, response, error) = await authService.RegisterUserAsync(
            new RegisterUserRequestBuilder()
                .WithEmail(existingEmail)
                .Build()
        );

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("email");
        error.Should().Contain("already exists");
    }

    /// <summary>
    /// Verify user registration with weak password fails
    /// Tests business rule: Passwords must meet security requirements
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task RegisterUserAsync_WithWeakPassword_ShouldFail()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        var request = new RegisterUserRequestBuilder()
            .WithPassword("weak")
            .Build();

        // Act
        var (success, response, error) = await authService.RegisterUserAsync(request);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("password");
    }

    /// <summary>
    /// Verify user registration with invalid email format fails
    /// Tests business rule: Email must be in valid format
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task RegisterUserAsync_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        var request = new RegisterUserRequestBuilder()
            .WithEmail("notanemail")
            .Build();

        // Act
        var (success, response, error) = await authService.RegisterUserAsync(request);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("email");
        error.Should().Contain("format");
    }

    /// <summary>
    /// Verify user login with valid credentials succeeds
    /// Tests core business rule: Registered users should be able to login
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task LoginAsync_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        var email = "logintest@witchcityrope.com";
        var password = "SecurePassword123!";

        // Register user first
        await authService.RegisterUserAsync(new RegisterUserRequestBuilder()
            .WithEmail(email)
            .WithPassword(password)
            .Build());

        var loginRequest = new LoginRequestBuilder()
            .WithEmail(email)
            .WithPassword(password)
            .Build();

        // Act
        var (success, response, error) = await authService.LoginAsync(loginRequest);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Email.Should().Be(email);
        response.Token.Should().NotBeEmpty();
        error.Should().BeNull();
    }

    /// <summary>
    /// Verify user login with invalid credentials fails
    /// Tests business rule: Invalid credentials should be rejected
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task LoginAsync_WithInvalidCredentials_ShouldFail()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        var loginRequest = new LoginRequestBuilder()
            .WithEmail("nonexistent@witchcityrope.com")
            .WithPassword("WrongPassword")
            .Build();

        // Act
        var (success, response, error) = await authService.LoginAsync(loginRequest);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("invalid");
    }

    /// <summary>
    /// Verify user profile update with valid data succeeds
    /// Tests business rule: Users should be able to update their profiles
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task UpdateUserProfileAsync_WithValidData_ShouldUpdate()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        // Register user first
        var (registerSuccess, registerResponse, _) = await authService.RegisterUserAsync(
            new RegisterUserRequestBuilder().Build()
        );

        registerSuccess.Should().BeTrue();

        var updateRequest = new UpdateUserProfileRequestBuilder()
            .WithSceneName("UpdatedSceneName")
            .WithBio("Updated bio information")
            .Build();

        // Act
        var (success, response, error) = await authService.UpdateUserProfileAsync(
            registerResponse!.Id,
            updateRequest
        );

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.SceneName.Should().Be("UpdatedSceneName");
        response.Bio.Should().Be("Updated bio information");
        error.Should().BeNull();
    }

    /// <summary>
    /// Verify password change with correct old password succeeds
    /// Tests business rule: Users should be able to change their passwords
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task ChangePasswordAsync_WithCorrectOldPassword_ShouldSucceed()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        var originalPassword = "OriginalPassword123!";
        var newPassword = "NewSecurePassword456!";

        // Register user first
        var (registerSuccess, registerResponse, _) = await authService.RegisterUserAsync(
            new RegisterUserRequestBuilder()
                .WithPassword(originalPassword)
                .Build()
        );

        registerSuccess.Should().BeTrue();

        var changePasswordRequest = new ChangePasswordRequestBuilder()
            .WithCurrentPassword(originalPassword)
            .WithNewPassword(newPassword)
            .Build();

        // Act
        var (success, error) = await authService.ChangePasswordAsync(
            registerResponse!.Id,
            changePasswordRequest
        );

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();

        // Verify can login with new password
        var loginResult = await authService.LoginAsync(new LoginRequestBuilder()
            .WithEmail(registerResponse.Email)
            .WithPassword(newPassword)
            .Build());

        loginResult.success.Should().BeTrue();
    }

    /// <summary>
    /// Verify password change with incorrect old password fails
    /// Tests business rule: Current password must be verified before change
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task ChangePasswordAsync_WithIncorrectOldPassword_ShouldFail()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        // Register user first
        var (registerSuccess, registerResponse, _) = await authService.RegisterUserAsync(
            new RegisterUserRequestBuilder().Build()
        );

        registerSuccess.Should().BeTrue();

        var changePasswordRequest = new ChangePasswordRequestBuilder()
            .WithCurrentPassword("WrongCurrentPassword")
            .WithNewPassword("NewPassword123!")
            .Build();

        // Act
        var (success, error) = await authService.ChangePasswordAsync(
            registerResponse!.Id,
            changePasswordRequest
        );

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("current password");
    }

    /// <summary>
    /// Verify user deactivation with valid user succeeds
    /// Tests business rule: Users should be able to deactivate their accounts
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task DeactivateUserAsync_WithValidUser_ShouldDeactivate()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        // Register user first
        var (registerSuccess, registerResponse, _) = await authService.RegisterUserAsync(
            new RegisterUserRequestBuilder().Build()
        );

        registerSuccess.Should().BeTrue();

        // Act
        var (success, error) = await authService.DeactivateUserAsync(registerResponse!.Id);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();

        // Verify user cannot login after deactivation
        var loginResult = await authService.LoginAsync(new LoginRequestBuilder()
            .WithEmail(registerResponse.Email)
            .WithPassword("password") // Default from builder
            .Build());

        loginResult.success.Should().BeFalse();
        loginResult.error.Should().Contain("deactivated");
    }

    /// <summary>
    /// Verify account activation via email token succeeds
    /// Tests business rule: Users should activate accounts via email verification
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task ActivateAccountAsync_WithValidToken_ShouldActivate()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        // Register user (should create with pending activation)
        var (registerSuccess, registerResponse, _) = await authService.RegisterUserAsync(
            new RegisterUserRequestBuilder().Build()
        );

        registerSuccess.Should().BeTrue();

        // Act
        var (success, error) = await authService.ActivateAccountAsync(
            registerResponse!.Id,
            "valid-activation-token"
        );

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();

        // Verify user can now login
        var loginResult = await authService.LoginAsync(new LoginRequestBuilder()
            .WithEmail(registerResponse.Email)
            .Build());

        loginResult.success.Should().BeTrue();
    }

    /// <summary>
    /// Verify role assignment with valid permissions succeeds
    /// Tests business rule: Admins should be able to assign roles to users
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task AssignRoleAsync_WithValidPermissions_ShouldAssignRole()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        // Register user first
        var (registerSuccess, registerResponse, _) = await authService.RegisterUserAsync(
            new RegisterUserRequestBuilder().Build()
        );

        registerSuccess.Should().BeTrue();

        // Act
        var (success, error) = await authService.AssignRoleAsync(
            registerResponse!.Id,
            "Teacher",
            "admin-user-id"
        );

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();

        // Verify role was assigned
        var user = await MockDbContext.Object.Users
            .FirstOrDefaultAsync(u => u.Id == registerResponse.Id);
        user.Should().NotBeNull();
        user!.Roles.Should().Contain("Teacher");
    }

    /// <summary>
    /// Verify password reset request generates token
    /// Tests business rule: Users should be able to request password resets
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task RequestPasswordResetAsync_WithValidEmail_ShouldGenerateToken()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        var email = "resettest@witchcityrope.com";

        // Register user first
        await authService.RegisterUserAsync(new RegisterUserRequestBuilder()
            .WithEmail(email)
            .Build());

        // Act
        var (success, token, error) = await authService.RequestPasswordResetAsync(email);

        // Assert
        success.Should().BeTrue();
        token.Should().NotBeEmpty();
        error.Should().BeNull();
    }

    /// <summary>
    /// Verify password reset with valid token succeeds
    /// Tests business rule: Valid reset tokens should allow password changes
    /// </summary>
    [Fact(Skip = "Authentication feature pending implementation")]
    public async Task ResetPasswordAsync_WithValidToken_ShouldResetPassword()
    {
        // Arrange
        var authService = new AuthenticationService(
            MockDbContext.Object,
            Mock.Of<ILogger<AuthenticationService>>()
        );

        var email = "resettest2@witchcityrope.com";
        var newPassword = "NewResetPassword123!";

        // Register user and request reset
        await authService.RegisterUserAsync(new RegisterUserRequestBuilder()
            .WithEmail(email)
            .Build());

        var (tokenSuccess, token, _) = await authService.RequestPasswordResetAsync(email);
        tokenSuccess.Should().BeTrue();

        // Act
        var (success, error) = await authService.ResetPasswordAsync(token, newPassword);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();

        // Verify can login with new password
        var loginResult = await authService.LoginAsync(new LoginRequestBuilder()
            .WithEmail(email)
            .WithPassword(newPassword)
            .Build());

        loginResult.success.Should().BeTrue();
    }
}