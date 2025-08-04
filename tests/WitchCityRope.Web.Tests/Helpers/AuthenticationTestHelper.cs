using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Web.Models;
using WitchCityRope.Web.Services;

namespace WitchCityRope.Web.Tests.Helpers;

/// <summary>
/// Helper class for setting up authentication in tests
/// </summary>
public static class AuthenticationTestHelper
{
    /// <summary>
    /// Configures services for an authenticated user
    /// </summary>
    public static void AddAuthentication(TestServiceProvider services, 
        ClaimsPrincipal? user = null, 
        bool isAuthenticated = true)
    {
        var authStateProvider = new Mock<AuthenticationStateProvider>();
        var authState = new AuthenticationState(user ?? CreateTestUser(isAuthenticated));
        
        authStateProvider
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        services.AddSingleton(authStateProvider.Object);
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();
    }

    /// <summary>
    /// Creates a test user with specified authentication status
    /// </summary>
    public static ClaimsPrincipal CreateTestUser(bool isAuthenticated = true, 
        string? userId = null, 
        string? email = null,
        string[]? roles = null)
    {
        if (!isAuthenticated)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId ?? Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, email ?? "test@example.com"),
            new(ClaimTypes.Name, email ?? "test@example.com")
        };

        if (roles != null)
        {
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Creates a test user with admin role
    /// </summary>
    public static ClaimsPrincipal CreateAdminUser(string? userId = null, string? email = null)
    {
        return CreateTestUser(true, userId, email, new[] { "Admin" });
    }

    /// <summary>
    /// Creates a test user with member role
    /// </summary>
    public static ClaimsPrincipal CreateMemberUser(string? userId = null, string? email = null)
    {
        return CreateTestUser(true, userId, email, new[] { "Member" });
    }

    /// <summary>
    /// Creates a mock IAuthService
    /// </summary>
    public static Mock<IAuthService> CreateAuthServiceMock(
        bool isAuthenticated = true,
        UserDto? currentUser = null)
    {
        var mock = new Mock<IAuthService>();
        
        mock.Setup(x => x.IsAuthenticatedAsync()).ReturnsAsync(isAuthenticated);
        mock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);

        return mock;
    }

    /// <summary>
    /// Creates a UserDto for testing
    /// </summary>
    public static UserDto CreateTestUserDto(
        Guid? id = null,
        string? email = null,
        string? displayName = null,
        string? sceneName = null,
        string[]? roles = null)
    {
        return new UserDto
        {
            Id = id ?? Guid.NewGuid(),
            Email = email ?? "test@example.com",
            DisplayName = displayName ?? "Test User",
            SceneName = sceneName ?? "TestScene",
            Roles = roles?.ToList() ?? new List<string>(),
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Extension method to add authenticated context to TestContext
    /// </summary>
    public static TestContext AddTestAuthentication(this TestContext context, 
        ClaimsPrincipal? user = null,
        bool isAuthenticated = true)
    {
        AddAuthentication(context.Services, user, isAuthenticated);
        return context;
    }

    /// <summary>
    /// Extension method to add admin authentication to TestContext
    /// </summary>
    public static TestContext AddAdminAuthentication(this TestContext context)
    {
        var adminUser = CreateAdminUser();
        AddAuthentication(context.Services, adminUser, true);
        return context;
    }

    /// <summary>
    /// Extension method to add member authentication to TestContext
    /// </summary>
    public static TestContext AddMemberAuthentication(this TestContext context)
    {
        var memberUser = CreateMemberUser();
        AddAuthentication(context.Services, memberUser, true);
        return context;
    }

    /// <summary>
    /// Extension method to add unauthenticated context to TestContext
    /// </summary>
    public static TestContext AddUnauthenticated(this TestContext context)
    {
        AddAuthentication(context.Services, null, false);
        return context;
    }
}