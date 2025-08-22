using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using WitchCityRope.Web.Models;
using WitchCityRope.Web.Services;
using Xunit;

namespace WitchCityRope.Web.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<ApiClient> _mockApiClient;
    private readonly Mock<AuthenticationService> _mockAuthenticationService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockApiClient = new Mock<ApiClient>();
        
        // Create mock for AuthenticationService dependencies
        var mockHttpClient = new Mock<HttpClient>();
        var mockLocalStorage = new Mock<ILocalStorageService>();
        var mockLogger = new Mock<ILogger<AuthenticationService>>();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        _mockAuthenticationService = new Mock<AuthenticationService>(
            mockHttpClient.Object,
            mockLocalStorage.Object,
            mockLogger.Object,
            mockHttpContextAccessor.Object
        );
        
        _authService = new AuthService(_mockApiClient.Object, _mockAuthenticationService.Object);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenNotAuthenticated_ReturnsNull()
    {
        // Arrange
        var anonymousState = new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity())
        );
        _mockAuthenticationService.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(anonymousState);

        // Act
        var result = await _authService.GetCurrentUserAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenAuthenticated_ReturnsUserDto()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "VettedMember")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authState = new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(principal);
        
        _mockAuthenticationService.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);
        
        var userInfo = new UserInfo
        {
            Id = "test-user-id",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<string> { "Admin", "VettedMember" }
        };
        
        _mockAuthenticationService.Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(userInfo);

        // Act
        var result = await _authService.GetCurrentUserAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal("Test", result.SceneName);
        Assert.True(result.IsAdmin);
        Assert.True(result.IsVetted);
        Assert.Contains("Admin", result.Roles);
        Assert.Contains("VettedMember", result.Roles);
    }

    [Fact]
    public async Task LoginAsync_WhenSuccessful_RaisesAuthenticationStateChangedEvent()
    {
        // Arrange
        var loginResult = new LoginResult { Success = true };
        _mockAuthenticationService.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(loginResult);
        
        bool eventRaised = false;
        _authService.AuthenticationStateChanged += (sender, isAuthenticated) =>
        {
            eventRaised = true;
            Assert.True(isAuthenticated);
        };

        // Act
        await _authService.LoginAsync("test@example.com", "password");

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public async Task LogoutAsync_RaisesAuthenticationStateChangedEvent()
    {
        // Arrange
        _mockAuthenticationService.Setup(x => x.LogoutAsync())
            .Returns(Task.CompletedTask);
        
        bool eventRaised = false;
        _authService.AuthenticationStateChanged += (sender, isAuthenticated) =>
        {
            eventRaised = true;
            Assert.False(isAuthenticated);
        };

        // Act
        await _authService.LogoutAsync();

        // Assert
        Assert.True(eventRaised);
    }
}