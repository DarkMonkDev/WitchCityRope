using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        await context.Database.EnsureCreatedAsync();

        // Create test user with known email and scene name
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
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PasswordHash = "AQAAAAIAAYagAAAAEJ7F3z+9c3l6z8Wz+5J7b4Q==" // Placeholder - will be set by Identity
        };

        context.Users.Add(testUser);
        await context.SaveChangesAsync();

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

        // Verify response contains user data
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.User.Should().NotBeNull();
        loginResponse.User.Email.Should().Be(TestUserEmail);
        loginResponse.User.SceneName.Should().Be(TestUserSceneName);

        // Verify JWT token is present
        loginResponse.Token.Should().NotBeNullOrEmpty();
        loginResponse.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // Verify auth-token cookie is set
        var cookies = response.Headers.GetValues("Set-Cookie");
        cookies.Should().Contain(c => c.Contains("auth-token"));
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

        // Verify response contains user data (same user, different login method)
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.User.Should().NotBeNull();
        loginResponse.User.Email.Should().Be(TestUserEmail);
        loginResponse.User.SceneName.Should().Be(TestUserSceneName);

        // Verify JWT token is present
        loginResponse.Token.Should().NotBeNullOrEmpty();

        // Verify auth-token cookie is set
        var cookies = response.Headers.GetValues("Set-Cookie");
        cookies.Should().Contain(c => c.Contains("auth-token"));
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

        // Verify no auth-token cookie is set
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
        setCookieHeaders.Should().NotContain(c => c.Contains("auth-token") && !c.Contains("expires="));

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

        // Verify no auth-token cookie is set
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
        setCookieHeaders.Should().NotContain(c => c.Contains("auth-token") && !c.Contains("expires="));
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

        // Verify no auth-token cookie is set
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
        setCookieHeaders.Should().NotContain(c => c.Contains("auth-token") && !c.Contains("expires="));
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
