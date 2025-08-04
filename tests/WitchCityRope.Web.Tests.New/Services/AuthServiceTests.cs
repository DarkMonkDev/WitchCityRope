using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Web.Models;
using WitchCityRope.Web.Services;
using WitchCityRope.Web.Tests.New.Helpers;
using WitchCityRope.Tests.Common.Identity;
using WitchCityRope.Core.Enums;
using Xunit;

namespace WitchCityRope.Web.Tests.New.Services;

/// <summary>
/// Tests for AuthService implementation focusing on service logic
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<UserManager<WitchCityRopeUser>> _userManagerMock;
    private readonly Mock<SignInManager<WitchCityRopeUser>> _signInManagerMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<WitchCityRopeUser>>();
        _userManagerMock = new Mock<UserManager<WitchCityRopeUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Setup SignInManager mock
        _signInManagerMock = MockIdentityFactory.CreateSignInManagerMock(_userManagerMock.Object);
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenNotAuthenticated_ReturnsNull()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.Context)
            .Returns((Microsoft.AspNetCore.Http.HttpContext)null!);

        // Act
        var result = await _authService.GetCurrentUserAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenAuthenticated_ReturnsUserDto()
    {
        // Arrange
        var user = MockIdentityFactory.CreateTestUser("TestUser", "test@example.com");
        var httpContext = MockIdentityFactory.CreateAuthenticatedHttpContext(user);
        
        _signInManagerMock.Setup(x => x.Context)
            .Returns(httpContext);
        
        _userManagerMock.Setup(x => x.GetUserAsync(httpContext.User))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetCurrentUserAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.SceneName.Should().Be(user.SceneName.Value);
    }

    [Fact]
    public async Task IsAuthenticated_WhenNotSignedIn_ReturnsFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _signInManagerMock.Setup(x => x.Context)
            .Returns(httpContext);

        // Act
        var result = await _authService.IsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAuthenticated_WhenSignedIn_ReturnsTrue()
    {
        // Arrange
        var user = MockIdentityFactory.CreateTestUser("TestUser", "test@example.com");
        var httpContext = MockIdentityFactory.CreateAuthenticatedHttpContext(user);
        
        _signInManagerMock.Setup(x => x.Context)
            .Returns(httpContext);
        
        _signInManagerMock.Setup(x => x.IsSignedIn(httpContext.User))
            .Returns(true);

        // Act
        var result = await _authService.IsAuthenticated();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUserId_WhenAuthenticated_ReturnsUserId()
    {
        // Arrange
        var user = MockIdentityFactory.CreateTestUser("TestUser", "test@example.com");
        var httpContext = MockIdentityFactory.CreateAuthenticatedHttpContext(user);
        
        _signInManagerMock.Setup(x => x.Context)
            .Returns(httpContext);
        
        _userManagerMock.Setup(x => x.GetUserId(httpContext.User))
            .Returns(user.Id.ToString());

        // Act
        var result = await _authService.GetCurrentUserId();

        // Assert
        result.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetCurrentUserId_WhenNotAuthenticated_ReturnsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _signInManagerMock.Setup(x => x.Context)
            .Returns(httpContext);
        
        _userManagerMock.Setup(x => x.GetUserId(httpContext.User))
            .Returns((string)null!);

        // Act
        var result = await _authService.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task IsInRole_WhenUserNotFound_ReturnsFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _signInManagerMock.Setup(x => x.Context)
            .Returns(httpContext);
        
        _userManagerMock.Setup(x => x.GetUserAsync(httpContext.User))
            .ReturnsAsync((WitchCityRopeUser)null!);

        // Act
        var result = await _authService.IsInRole("Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsInRole_WhenUserInRole_ReturnsTrue()
    {
        // Arrange
        var user = MockIdentityFactory.CreateTestUser("AdminUser", "admin@example.com", UserRole.Administrator);
        var httpContext = MockIdentityFactory.CreateAuthenticatedHttpContext(user);
        
        _signInManagerMock.Setup(x => x.Context)
            .Returns(httpContext);
        
        _userManagerMock.Setup(x => x.GetUserAsync(httpContext.User))
            .ReturnsAsync(user);
        
        _userManagerMock.Setup(x => x.IsInRoleAsync(user, "Admin"))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.IsInRole("Admin");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsInRole_WhenUserNotInRole_ReturnsFalse()
    {
        // Arrange
        var user = MockIdentityFactory.CreateTestUser("MemberUser", "member@example.com", UserRole.Member);
        var httpContext = MockIdentityFactory.CreateAuthenticatedHttpContext(user);
        
        _signInManagerMock.Setup(x => x.Context)
            .Returns(httpContext);
        
        _userManagerMock.Setup(x => x.GetUserAsync(httpContext.User))
            .ReturnsAsync(user);
        
        _userManagerMock.Setup(x => x.IsInRoleAsync(user, "Admin"))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.IsInRole("Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Test123!";
        var user = MockIdentityFactory.CreateTestUser("TestUser", email);
        
        _userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);
        
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
                user, 
                password, 
                It.IsAny<bool>(), 
                It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _authService.LoginAsync(email, password, rememberMe: true);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.RequiresTwoFactor.Should().BeFalse();
        result.IsLockedOut.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var email = "test@example.com";
        var password = "WrongPassword";
        var user = MockIdentityFactory.CreateTestUser("TestUser", email);
        
        _userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);
        
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
                user, 
                password, 
                It.IsAny<bool>(), 
                It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "Test123!";
        
        _userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((WitchCityRopeUser)null!);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WithLockedOutUser_ReturnsLockedOut()
    {
        // Arrange
        var email = "locked@example.com";
        var password = "Test123!";
        var user = MockIdentityFactory.CreateTestUser("LockedUser", email);
        
        _userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);
        
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
                user, 
                password, 
                It.IsAny<bool>(), 
                It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.IsLockedOut.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_RequiringTwoFactor_ReturnsTwoFactorRequired()
    {
        // Arrange
        var email = "2fa@example.com";
        var password = "Test123!";
        var user = MockIdentityFactory.CreateTestUser("TwoFactorUser", email);
        
        _userManagerMock.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);
        
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
                user, 
                password, 
                It.IsAny<bool>(), 
                It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.RequiresTwoFactor.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_CallsSignOut()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _authService.LogoutAsync();

        // Assert
        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task RefreshSignInAsync_WithValidUser_CallsRefreshSignIn()
    {
        // Arrange
        var user = MockIdentityFactory.CreateTestUser("TestUser", "test@example.com");
        var httpContext = MockIdentityFactory.CreateAuthenticatedHttpContext(user);
        
        _signInManagerMock.Setup(x => x.Context)
            .Returns(httpContext);
        
        _userManagerMock.Setup(x => x.GetUserAsync(httpContext.User))
            .ReturnsAsync(user);
        
        _signInManagerMock.Setup(x => x.RefreshSignInAsync(user))
            .Returns(Task.CompletedTask);

        // Act
        await _authService.RefreshSignInAsync();

        // Assert
        _signInManagerMock.Verify(x => x.RefreshSignInAsync(user), Times.Once);
    }

    [Fact]
    public async Task RefreshSignInAsync_WithNoUser_DoesNothing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        
        _signInManagerMock.Setup(x => x.Context)
            .Returns(httpContext);
        
        _userManagerMock.Setup(x => x.GetUserAsync(httpContext.User))
            .ReturnsAsync((WitchCityRopeUser)null!);

        // Act
        await _authService.RefreshSignInAsync();

        // Assert
        _signInManagerMock.Verify(x => x.RefreshSignInAsync(It.IsAny<WitchCityRopeUser>()), Times.Never);
    }
}