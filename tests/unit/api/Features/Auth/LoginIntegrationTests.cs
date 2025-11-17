using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using WitchCityRope.Api;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Authentication.Models;
using WitchCityRope.Api.Models;
using Xunit;

namespace WitchCityRope.UnitTests.Api.Features.Auth;

/// <summary>
/// Integration Tests: Login with Email or Scene Name Feature
/// Tests the real HTTP endpoints with real database for login functionality
/// Uses TestContainers with real PostgreSQL for true integration testing
///
/// Feature: Login with Email OR Scene Name
/// Backend Implementation: EmailOrSceneName field in LoginRequest
/// Service Logic: Try email lookup first, then scene name lookup as fallback
/// Created: 2025-10-27
/// </summary>
[Collection("Database")]
public class LoginIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _connectionString = null!;

    // Test user data
    private Guid _testUserWithEmailId;
    private const string TestUserEmail = "logintest@witchcityrope.com";
    private const string TestUserSceneName = "LoginTestUser";
    private const string TestUserPassword = "Test123!";

    public LoginIntegrationTests()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test_login")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        // Create test factory with container database
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add TestContainers PostgreSQL
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseNpgsql(_connectionString));

                    // Remove DatabaseInitializationService to prevent automatic seeding in tests
                    var initServiceDescriptor = services.FirstOrDefault(
                        d => d.ServiceType.Name == "DatabaseInitializationService");
                    if (initServiceDescriptor != null)
                    {
                        services.Remove(initServiceDescriptor);
                    }

                    // Remove hosted service that triggers database initialization
                    var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
                    foreach (var service in hostedServices)
                    {
                        if (service.ImplementationType?.Name.Contains("DatabaseInitialization") == true)
                        {
                            services.Remove(service);
                        }
                    }
                });
            });

        _client = _factory.CreateClient();

        // Seed test data
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.EnsureCreatedAsync();

        // Clean up any existing test users to ensure fresh state
        var existingUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == TestUserEmail || u.SceneName == TestUserSceneName);

        if (existingUser != null)
        {
            context.Users.Remove(existingUser);
            await context.SaveChangesAsync();
        }

        // Create test user with known email and scene name using UserManager for proper password hashing
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = TestUserEmail,
            UserName = TestUserEmail,
            SceneName = TestUserSceneName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Use UserManager to create user with properly hashed password
        var result = await userManager.CreateAsync(testUser, TestUserPassword);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _testUserWithEmailId = testUser.Id;
    }

    #region Email Login Tests

    /// <summary>
    /// Verify /api/auth/login endpoint accepts valid email address
    /// Tests: Email lookup path with valid credentials
    /// Expected: 200 OK with auth-token cookie and user data
    /// </summary>
    [Fact]
    public async Task LoginEndpoint_WithValidEmail_Returns200AndAuthCookie()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = TestUserEmail,
            Password = TestUserPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify response contains user data (BFF pattern - no token in response body)
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain(TestUserEmail);
        responseContent.Should().Contain(TestUserSceneName);
        responseContent.Should().Contain("\"success\":true", "BFF pattern returns success flag");

        // BFF Pattern: Token should NOT be in response body (security)
        responseContent.Should().NotContain("\"token\":", "BFF pattern does not expose tokens in response");

        // Verify auth-token cookie is set (BFF pattern - token only in httpOnly cookie)
        response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders).Should().BeTrue();
        cookieHeaders.Should().Contain(c => c.Contains("auth-token") && c.Contains("httponly"),
            "BFF pattern uses httpOnly cookies for tokens");
    }

    #endregion

    #region Scene Name Login Tests

    /// <summary>
    /// Verify /api/auth/login endpoint accepts valid scene name
    /// Tests: Scene name fallback lookup path with valid credentials
    /// Expected: 200 OK with auth-token cookie and user data
    /// </summary>
    [Fact]
    public async Task LoginEndpoint_WithValidSceneName_Returns200AndAuthCookie()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = TestUserSceneName, // Use scene name instead of email
            Password = TestUserPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify response contains user data (BFF pattern - no token in response body)
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain(TestUserEmail);
        responseContent.Should().Contain(TestUserSceneName);
        responseContent.Should().Contain("\"success\":true", "BFF pattern returns success flag");

        // BFF Pattern: Token should NOT be in response body (security)
        responseContent.Should().NotContain("\"token\":", "BFF pattern does not expose tokens in response");

        // Verify auth-token cookie is set (BFF pattern - token only in httpOnly cookie)
        response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders).Should().BeTrue();
        cookieHeaders.Should().Contain(c => c.Contains("auth-token") && c.Contains("httponly"),
            "BFF pattern uses httpOnly cookies for tokens");
    }

    #endregion

    #region Invalid Credentials Tests

    /// <summary>
    /// Verify /api/auth/login endpoint rejects non-existent email/scene name
    /// Tests: Both lookups fail (email and scene name)
    /// Expected: 401 Unauthorized with no auth cookie
    /// </summary>
    [Fact]
    public async Task LoginEndpoint_WithInvalidCredentials_Returns401()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = "nonexistent@example.com", // Neither email nor scene name exists
            Password = "AnyPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Verify no auth-token cookie is set (or if set, it's an expiration/delete cookie)
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            setCookieHeaders.Should().NotContain(c => c.Contains("auth-token") && !c.Contains("expires=Thu, 01 Jan 1970"),
                "Failed login should not set valid auth cookie");
        }

        // Verify error response contains generic message
        var errorResponse = await response.Content.ReadAsStringAsync();
        errorResponse.Should().Contain("Invalid");
    }

    /// <summary>
    /// Verify /api/auth/login endpoint rejects valid email with wrong password
    /// Tests: Password validation with email login path
    /// Expected: 401 Unauthorized with no auth cookie
    /// </summary>
    [Fact]
    public async Task LoginEndpoint_WithValidEmailButWrongPassword_Returns401()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = TestUserEmail,
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Verify no auth-token cookie is set (or if set, it's an expiration/delete cookie)
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            setCookieHeaders.Should().NotContain(c => c.Contains("auth-token") && !c.Contains("expires=Thu, 01 Jan 1970"),
                "Failed login should not set valid auth cookie");
        }
    }

    /// <summary>
    /// Verify /api/auth/login endpoint rejects valid scene name with wrong password
    /// Tests: Password validation with scene name login path
    /// Expected: 401 Unauthorized with no auth cookie
    /// </summary>
    [Fact]
    public async Task LoginEndpoint_WithValidSceneNameButWrongPassword_Returns401()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = TestUserSceneName, // Use scene name
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Verify no auth-token cookie is set (or if set, it's an expiration/delete cookie)
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            setCookieHeaders.Should().NotContain(c => c.Contains("auth-token") && !c.Contains("expires=Thu, 01 Jan 1970"),
                "Failed login should not set valid auth cookie");
        }
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Verify /api/auth/login endpoint requires emailOrSceneName field
    /// Tests: Validation for required field
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task LoginEndpoint_WithEmptyEmailOrSceneName_Returns400()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = "", // Empty identifier
            Password = "Test123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Verify /api/auth/login endpoint requires password field
    /// Tests: Validation for required password
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task LoginEndpoint_WithEmptyPassword_Returns400()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrSceneName = TestUserEmail,
            Password = "" // Empty password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
