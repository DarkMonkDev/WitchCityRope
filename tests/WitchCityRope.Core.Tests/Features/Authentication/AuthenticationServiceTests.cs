using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Authentication.Models;
using WitchCityRope.Api.Features.Authentication.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Models.Auth;
using WitchCityRope.Api.Services;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Core.Tests.Features.Authentication;

/// <summary>
/// Tests for AuthenticationService using ACTUAL implemented API methods
/// Tests the real vertical slice architecture implementation with TestContainers
/// ACTUAL API: LoginAsync(LoginRequest), RegisterAsync(RegisterRequest), GetCurrentUserAsync(string), GetServiceTokenAsync(string, string)
/// All tests validate the working implementation against real PostgreSQL database
/// </summary>
[Collection("Database")]
public class AuthenticationServiceTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private Mock<SignInManager<ApplicationUser>> _mockSignInManager = null!;
    private Mock<IJwtService> _mockJwtService = null!;
    private Mock<ReturnUrlValidator> _mockReturnUrlValidator = null!;
    private Mock<ILogger<AuthenticationService>> _mockLogger = null!;
    private AuthenticationService _service = null!;

    public AuthenticationServiceTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        await _fixture.ResetDatabaseAsync();

        // Setup mocked dependencies for AuthenticationService
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            _mockUserManager.Object, contextAccessor.Object, userPrincipalFactory.Object, null!, null!, null!, null!);

        _mockJwtService = new Mock<IJwtService>();
        _mockReturnUrlValidator = new Mock<ReturnUrlValidator>();
        _mockLogger = new Mock<ILogger<AuthenticationService>>();

        _service = new AuthenticationService(
            _context,
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _mockJwtService.Object,
            _mockReturnUrlValidator.Object,
            _mockLogger.Object);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    /// <summary>
    /// Verify user registration with valid data succeeds
    /// Tests core business rule: Valid users should be able to register
    /// Uses ACTUAL RegisterAsync(RegisterRequest) method
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@witchcityrope.com",
            SceneName = "TestUser123",
            Password = "SecurePassword123!"
        };

        // Mock UserManager.CreateAsync to succeed
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var (success, response, error) = await _service.RegisterAsync(request);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Email.Should().Be(request.Email);
        response.SceneName.Should().Be(request.SceneName);
        error.Should().BeEmpty();

        // Verify UserManager.CreateAsync was called
        _mockUserManager.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u =>
            u.Email == request.Email && u.SceneName == request.SceneName), request.Password), Times.Once);
    }

    /// <summary>
    /// Verify user registration with duplicate email fails
    /// Tests business rule: Email addresses must be unique
    /// Uses real database to test duplicate constraint
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldFail()
    {
        // Arrange - Create existing user in database
        var existingEmail = "duplicate@witchcityrope.com";
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = existingEmail,
            UserName = existingEmail,
            SceneName = "ExistingUser",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = existingEmail,
            SceneName = "NewUser",
            Password = "SecurePassword123!"
        };

        // Act
        var (success, response, error) = await _service.RegisterAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("already registered");
    }

    /// <summary>
    /// Verify user registration with duplicate scene name fails
    /// Tests business rule: Scene names must be unique
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WithDuplicateSceneName_ShouldFail()
    {
        // Arrange - Create existing user in database
        var existingSceneName = "UniqueSceneName";
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "existing@witchcityrope.com",
            UserName = "existing@witchcityrope.com",
            SceneName = existingSceneName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = "new@witchcityrope.com",
            SceneName = existingSceneName, // Duplicate scene name
            Password = "SecurePassword123!"
        };

        // Act
        var (success, response, error) = await _service.RegisterAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("already taken");
    }

    /// <summary>
    /// Verify user registration with weak password fails
    /// Tests business rule: Passwords must meet security requirements
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WithWeakPassword_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@witchcityrope.com",
            SceneName = "TestUser",
            Password = "weak" // Weak password
        };

        // Mock UserManager.CreateAsync to fail with password error
        var identityError = new IdentityError { Code = "PasswordTooWeak", Description = "Password must be stronger" };
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var (success, response, error) = await _service.RegisterAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("stronger");
    }

    /// <summary>
    /// Verify user login with valid credentials succeeds
    /// Tests core business rule: Registered users should be able to login
    /// Uses ACTUAL LoginAsync(LoginRequest) method
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "logintest@witchcityrope.com",
            UserName = "logintest@witchcityrope.com",
            SceneName = "LoginTestUser",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = testUser.Email,
            Password = "SecurePassword123!"
        };

        // Mock dependencies
        _mockUserManager.Setup(x => x.FindByEmailAsync(testUser.Email))
            .ReturnsAsync(testUser);

        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(testUser, loginRequest.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var jwtToken = new WitchCityRope.Api.Models.Auth.JwtToken { Token = "test-jwt-token", ExpiresAt = DateTime.UtcNow.AddHours(1) };
        _mockJwtService.Setup(x => x.GenerateToken(testUser))
            .Returns(jwtToken);

        // Act
        var mockHttpContext = new Mock<HttpContext>().Object;
        var (success, response, error) = await _service.LoginAsync(loginRequest, mockHttpContext);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Token.Should().Be("test-jwt-token");
        response.User.Email.Should().Be(testUser.Email);
        error.Should().BeEmpty();

        // Verify last login time was updated in database
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == testUser.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Verify user login with invalid credentials fails
    /// Tests business rule: Invalid credentials should be rejected
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ShouldFail()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = "nonexistent@witchcityrope.com",
            Password = "WrongPassword"
        };

        // Mock UserManager to return null (user not found)
        _mockUserManager.Setup(x => x.FindByEmailAsync(loginRequest.EmailOrSceneName))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var mockHttpContext = new Mock<HttpContext>().Object;
        var (success, response, error) = await _service.LoginAsync(loginRequest, mockHttpContext);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Invalid email or password");
    }

    /// <summary>
    /// Verify user login with locked account fails
    /// Tests business rule: Locked accounts should be rejected
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithLockedAccount_ShouldFail()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "locked@witchcityrope.com",
            UserName = "locked@witchcityrope.com",
            SceneName = "LockedUser",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = testUser.Email,
            Password = "SecurePassword123!"
        };

        // Mock locked out sign-in result
        _mockUserManager.Setup(x => x.FindByEmailAsync(testUser.Email))
            .ReturnsAsync(testUser);

        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(testUser, loginRequest.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        // Act
        var mockHttpContext = new Mock<HttpContext>().Object;
        var (success, response, error) = await _service.LoginAsync(loginRequest, mockHttpContext);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("temporarily locked");
    }

    #region Email or Scene Name Login Tests

    /// <summary>
    /// Verify user can login with valid email address
    /// Tests new feature: Login with email OR scene name
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithValidEmail_ShouldSucceed()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "emailtest@witchcityrope.com",
            UserName = "emailtest@witchcityrope.com",
            SceneName = "EmailTestUser",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = testUser.Email, // Login with email
            Password = "SecurePassword123!"
        };

        // Mock dependencies - email lookup succeeds
        _mockUserManager.Setup(x => x.FindByEmailAsync(testUser.Email))
            .ReturnsAsync(testUser);

        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(testUser, loginRequest.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var jwtToken = new WitchCityRope.Api.Models.Auth.JwtToken { Token = "test-jwt-token", ExpiresAt = DateTime.UtcNow.AddHours(1) };
        _mockJwtService.Setup(x => x.GenerateToken(testUser))
            .Returns(jwtToken);

        // Act
        var mockHttpContext = new Mock<HttpContext>().Object;
        var (success, response, error) = await _service.LoginAsync(loginRequest, mockHttpContext);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Token.Should().Be("test-jwt-token");
        response.User.Email.Should().Be(testUser.Email);
        response.User.SceneName.Should().Be(testUser.SceneName);
        error.Should().BeEmpty();

        // Verify user lookup was done by email (primary path)
        _mockUserManager.Verify(x => x.FindByEmailAsync(testUser.Email), Times.Once);
    }

    /// <summary>
    /// Verify user can login with valid scene name
    /// Tests new feature: Login with email OR scene name (fallback lookup)
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithValidSceneName_ShouldSucceed()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "scenetest@witchcityrope.com",
            UserName = "scenetest@witchcityrope.com",
            SceneName = "SceneTestUser",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        // Add user to context for scene name lookup
        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = testUser.SceneName, // Login with scene name (not email)
            Password = "SecurePassword123!"
        };

        // Mock dependencies - email lookup fails (returns null), scene name lookup succeeds
        _mockUserManager.Setup(x => x.FindByEmailAsync(testUser.SceneName))
            .ReturnsAsync((ApplicationUser?)null); // Not an email

        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(testUser, loginRequest.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var jwtToken = new WitchCityRope.Api.Models.Auth.JwtToken { Token = "scene-jwt-token", ExpiresAt = DateTime.UtcNow.AddHours(1) };
        _mockJwtService.Setup(x => x.GenerateToken(testUser))
            .Returns(jwtToken);

        // Act
        var mockHttpContext = new Mock<HttpContext>().Object;
        var (success, response, error) = await _service.LoginAsync(loginRequest, mockHttpContext);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Token.Should().Be("scene-jwt-token");
        response.User.Email.Should().Be(testUser.Email);
        response.User.SceneName.Should().Be(testUser.SceneName);
        error.Should().BeEmpty();

        // Verify email lookup was tried first
        _mockUserManager.Verify(x => x.FindByEmailAsync(testUser.SceneName), Times.Once);
    }

    /// <summary>
    /// Verify login fails with invalid email or scene name
    /// Tests new feature: Generic error message (doesn't reveal which field failed)
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithInvalidEmailOrSceneName_ShouldFail()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = "nonexistent@example.com", // Neither email nor scene name exists
            Password = "AnyPassword123!"
        };

        // Mock both lookups returning null
        _mockUserManager.Setup(x => x.FindByEmailAsync(loginRequest.EmailOrSceneName))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var mockHttpContext = new Mock<HttpContext>().Object;
        var (success, response, error) = await _service.LoginAsync(loginRequest, mockHttpContext);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Invalid email/scene name or password");
        // Error message should be generic (security: don't reveal which field is wrong)
    }

    /// <summary>
    /// Verify login fails with valid email but wrong password
    /// Tests new feature: Password validation works with email login
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithValidEmailButWrongPassword_ShouldFail()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "wrongpassword@witchcityrope.com",
            UserName = "wrongpassword@witchcityrope.com",
            SceneName = "WrongPasswordUser",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = testUser.Email,
            Password = "WrongPassword123!"
        };

        // Mock user found but password check fails
        _mockUserManager.Setup(x => x.FindByEmailAsync(testUser.Email))
            .ReturnsAsync(testUser);

        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(testUser, loginRequest.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var mockHttpContext = new Mock<HttpContext>().Object;
        var (success, response, error) = await _service.LoginAsync(loginRequest, mockHttpContext);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Invalid email/scene name or password");
    }

    /// <summary>
    /// Verify login fails with valid scene name but wrong password
    /// Tests new feature: Password validation works with scene name login
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithValidSceneNameButWrongPassword_ShouldFail()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "scenewrongpass@witchcityrope.com",
            UserName = "scenewrongpass@witchcityrope.com",
            SceneName = "SceneWrongPassUser",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        // Add user to context for scene name lookup
        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = testUser.SceneName, // Login with scene name
            Password = "WrongPassword123!"
        };

        // Mock email lookup fails, password check fails
        _mockUserManager.Setup(x => x.FindByEmailAsync(testUser.SceneName))
            .ReturnsAsync((ApplicationUser?)null);

        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(testUser, loginRequest.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var mockHttpContext = new Mock<HttpContext>().Object;
        var (success, response, error) = await _service.LoginAsync(loginRequest, mockHttpContext);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Invalid email/scene name or password");
    }

    #endregion

    /// <summary>
    /// Verify GetCurrentUserAsync with valid user ID returns user
    /// Tests ACTUAL GetCurrentUserAsync(string) method
    /// </summary>
    [Fact]
    public async Task GetCurrentUserAsync_WithValidUserId_ShouldReturnUser()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "current@witchcityrope.com",
            UserName = "current@witchcityrope.com",
            SceneName = "CurrentUser",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow.AddMinutes(-30)
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.GetCurrentUserAsync(testUser.Id.ToString());

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Email.Should().Be(testUser.Email);
        response.SceneName.Should().Be(testUser.SceneName);
        error.Should().BeEmpty();
    }

    /// <summary>
    /// Verify GetCurrentUserAsync with invalid user ID fails
    /// Tests error handling for non-existent users
    /// </summary>
    [Fact]
    public async Task GetCurrentUserAsync_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid().ToString();

        // Act
        var (success, response, error) = await _service.GetCurrentUserAsync(nonExistentUserId);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("User not found");
    }

    /// <summary>
    /// Verify GetServiceTokenAsync with valid credentials succeeds
    /// Tests ACTUAL GetServiceTokenAsync(string, string) method
    /// </summary>
    [Fact]
    public async Task GetServiceTokenAsync_WithValidCredentials_ShouldGenerateToken()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "service@witchcityrope.com",
            UserName = "service@witchcityrope.com",
            SceneName = "ServiceUser",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        var jwtToken = new WitchCityRope.Api.Models.Auth.JwtToken { Token = "service-jwt-token", ExpiresAt = DateTime.UtcNow.AddHours(1) };
        _mockJwtService.Setup(x => x.GenerateToken(testUser))
            .Returns(jwtToken);

        // Act
        var (success, response, error) = await _service.GetServiceTokenAsync(testUser.Id.ToString(), testUser.Email);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Token.Should().Be("service-jwt-token");
        response.User.Email.Should().Be(testUser.Email);
        error.Should().BeEmpty();
    }

    /// <summary>
    /// Verify GetServiceTokenAsync with email mismatch fails
    /// Tests security validation for service token generation
    /// </summary>
    [Fact]
    public async Task GetServiceTokenAsync_WithEmailMismatch_ShouldFail()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "correct@witchcityrope.com",
            UserName = "correct@witchcityrope.com",
            SceneName = "TestUser",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Act - Use wrong email
        var (success, response, error) = await _service.GetServiceTokenAsync(testUser.Id.ToString(), "wrong@witchcityrope.com");

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("User not found or email mismatch");
    }

    /// <summary>
    /// Verify GetServiceTokenAsync with unconfirmed email fails
    /// Tests email confirmation requirement for service tokens
    /// </summary>
    [Fact]
    public async Task GetServiceTokenAsync_WithUnconfirmedEmail_ShouldFail()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "unconfirmed@witchcityrope.com",
            UserName = "unconfirmed@witchcityrope.com",
            SceneName = "UnconfirmedUser",
            EmailConfirmed = false, // Not confirmed
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        // Act
        var (success, response, error) = await _service.GetServiceTokenAsync(testUser.Id.ToString(), testUser.Email);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Account is not active or email not verified");
    }
}