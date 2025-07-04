using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.IntegrationTests
{
    public class AuthenticationTests : IClassFixture<WebApplicationFactory<WitchCityRope.Web.Program>>
    {
        private readonly WebApplicationFactory<WitchCityRope.Web.Program> _factory;
        private readonly HttpClient _client;

        public AuthenticationTests(WebApplicationFactory<WitchCityRope.Web.Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<WitchCityRopeDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database for testing
                    services.AddDbContext<WitchCityRopeDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });

                    // Ensure database is created
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<WitchCityRopeDbContext>();
                    db.Database.EnsureCreated();
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Register_ValidUser_ReturnsSuccess()
        {
            // Arrange
            var registerRequest = new
            {
                Email = "test@example.com",
                Password = "StrongPassword123!",
                ConfirmPassword = "StrongPassword123!",
                Username = "testuser",
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("token");
            content.Should().Contain("userId");
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var email = "duplicate@example.com";
            var registerRequest = new
            {
                Email = email,
                Password = "StrongPassword123!",
                ConfirmPassword = "StrongPassword123!",
                Username = "user1",
                FirstName = "Test",
                LastName = "User"
            };

            // Register first user
            await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

            // Try to register second user with same email
            var duplicateRequest = new
            {
                Email = email,
                Password = "StrongPassword123!",
                ConfirmPassword = "StrongPassword123!",
                Username = "user2",
                FirstName = "Test",
                LastName = "User2"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", duplicateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("already exists");
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsTokenAndUserId()
        {
            // Arrange
            var email = "login@example.com";
            var password = "StrongPassword123!";

            // First register a user
            var registerRequest = new
            {
                Email = email,
                Password = password,
                ConfirmPassword = password,
                Username = "loginuser",
                FirstName = "Login",
                LastName = "User"
            };
            await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

            // Login request
            var loginRequest = new
            {
                Email = email,
                Password = password
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            loginResponse.Should().NotBeNull();
            loginResponse.Token.Should().NotBeNullOrWhiteSpace();
            loginResponse.UserId.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var email = "wrongpass@example.com";
            var correctPassword = "StrongPassword123!";
            var wrongPassword = "WrongPassword123!";

            // Register user
            var registerRequest = new
            {
                Email = email,
                Password = correctPassword,
                ConfirmPassword = correctPassword,
                Username = "wrongpassuser",
                FirstName = "Wrong",
                LastName = "Pass"
            };
            await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

            // Login with wrong password
            var loginRequest = new
            {
                Email = email,
                Password = wrongPassword
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_NonExistentUser_ReturnsUnauthorized()
        {
            // Arrange
            var loginRequest = new
            {
                Email = "nonexistent@example.com",
                Password = "AnyPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AuthorizedEndpoint_WithValidToken_ReturnsSuccess()
        {
            // Arrange
            // Register and login to get token
            var email = "authorized@example.com";
            var password = "StrongPassword123!";
            
            var registerRequest = new
            {
                Email = email,
                Password = password,
                ConfirmPassword = password,
                Username = "authuser",
                FirstName = "Auth",
                LastName = "User"
            };
            await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

            var loginRequest = new { Email = email, Password = password };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<LoginResponseDto>(loginContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Token;

            // Add token to request
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/user/profile");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task AuthorizedEndpoint_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/user/profile");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AuthorizedEndpoint_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.GetAsync("/api/user/profile");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("")]
        [InlineData("short")]
        [InlineData("no-uppercase-123!")]
        [InlineData("NO-LOWERCASE-123!")]
        [InlineData("NoSpecialChar123")]
        [InlineData("NoNumbers!")]
        public async Task Register_WeakPassword_ReturnsBadRequest(string weakPassword)
        {
            // Arrange
            var registerRequest = new
            {
                Email = $"{Guid.NewGuid()}@example.com",
                Password = weakPassword,
                ConfirmPassword = weakPassword,
                Username = "weakpassuser",
                FirstName = "Weak",
                LastName = "Pass"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("password");
        }

        [Fact]
        public async Task Register_PasswordMismatch_ReturnsBadRequest()
        {
            // Arrange
            var registerRequest = new
            {
                Email = "mismatch@example.com",
                Password = "StrongPassword123!",
                ConfirmPassword = "DifferentPassword123!",
                Username = "mismatchuser",
                FirstName = "Mismatch",
                LastName = "User"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("match");
        }

        private class LoginResponseDto
        {
            public string Token { get; set; }
            public Guid UserId { get; set; }
            public bool IsSuccess { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}