using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using WitchCityRope.Api.Data;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests
{
    /// <summary>
    /// Integration Test Base Class
    /// Phase 2: Test Suite Integration - Enhanced Containerized Testing Infrastructure
    ///
    /// Features:
    /// - Inherits from IClassFixture<DatabaseTestFixture> for container sharing
    /// - Provides connection string and service provider access
    /// - Supports test isolation through database reset
    /// - Transaction rollback patterns for cleanup
    /// - Service configuration for integration scenarios
    /// - JWT token generation for API authentication testing
    /// </summary>
    [Collection("Database")]
    public abstract class IntegrationTestBase : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
    {
        protected readonly DatabaseTestFixture DatabaseFixture;
        protected readonly string ConnectionString;
        protected readonly IServiceProvider Services;
        protected readonly ILogger<IntegrationTestBase> Logger;

        // JWT configuration matching API Program.cs defaults
        protected const string JwtSecretKey = "DevSecret-JWT-WitchCityRope-AuthTest-2024-32CharMinimum!";
        protected const string JwtIssuer = "WitchCityRope-API";
        protected const string JwtAudience = "WitchCityRope-Services";
        protected const int JwtExpirationMinutes = 60;

        /// <summary>
        /// JSON serializer options matching API configuration.
        /// CRITICAL: API uses JsonStringEnumConverter, tests must use same options for deserialization.
        /// </summary>
        protected static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        protected IntegrationTestBase(DatabaseTestFixture fixture)
        {
            DatabaseFixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            ConnectionString = fixture.ConnectionString;
            Services = BuildServiceProvider();

            // Create logger for test operations
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            Logger = loggerFactory.CreateLogger<IntegrationTestBase>();
        }

        /// <summary>
        /// Builds a service provider configured for integration testing
        /// with the containerized PostgreSQL database
        /// </summary>
        private IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();
            
            // Configure Entity Framework with test database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(ConnectionString);
                
                // Enable detailed logging for debugging
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                
                // Disable connection pooling for integration tests
                options.EnableServiceProviderCaching(false);
                
                // Configure warnings for test environment
                options.ConfigureWarnings(warnings =>
                {
                    // Ignore pending model changes warning in test environment
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
                });
            });

            // Add logging services
            services.AddLogging(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));

            // Register common test services here
            // Add any additional services needed for integration testing
            
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Creates a fresh database context for test operations
        /// </summary>
        protected ApplicationDbContext CreateDbContext()
        {
            return DatabaseFixture.CreateDbContext();
        }

        /// <summary>
        /// Gets a scoped service from the test service provider
        /// </summary>
        protected T GetService<T>() where T : notnull
        {
            using var scope = Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Executes code within a database transaction scope
        /// Automatically rolls back on completion for test isolation
        /// </summary>
        protected async Task<T> ExecuteInTransactionAsync<T>(Func<ApplicationDbContext, Task<T>> operation)
        {
            await using var context = CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            
            try
            {
                var result = await operation(context);
                
                // Intentionally do not commit - let transaction roll back for isolation
                return result;
            }
            finally
            {
                // Transaction will automatically roll back when disposed
            }
        }

        /// <summary>
        /// Executes code within a database transaction scope (void return)
        /// Automatically rolls back on completion for test isolation
        /// </summary>
        protected async Task ExecuteInTransactionAsync(Func<ApplicationDbContext, Task> operation)
        {
            await using var context = CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            
            try
            {
                await operation(context);
                
                // Intentionally do not commit - let transaction roll back for isolation
            }
            finally
            {
                // Transaction will automatically roll back when disposed
            }
        }

        /// <summary>
        /// Initializes the test - called before each test method
        /// Resets the database to a clean state for test isolation
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            try
            {
                Logger.LogInformation("Initializing integration test - resetting database state");
                
                // Reset database to clean state for each test
                await DatabaseFixture.ResetDatabaseAsync();
                
                Logger.LogInformation("Integration test initialization completed");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize integration test");
                throw;
            }
        }

        /// <summary>
        /// Cleans up after test completion - called after each test method
        /// Enhanced with delay to ensure database connections close before next test
        /// </summary>
        public virtual async Task DisposeAsync()
        {
            try
            {
                Logger.LogInformation("Disposing integration test resources");

                // Additional cleanup if needed
                // The database reset happens in InitializeAsync for the next test

                // Allow time for connections to fully close before next test
                // This prevents database deadlocks during Respawn cleanup
                await Task.Delay(200);

                Logger.LogInformation("Integration test disposal completed");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error during integration test disposal");
                // Don't rethrow disposal exceptions to prevent masking test failures
            }
        }

        /// <summary>
        /// Generates a valid JWT token for test authentication.
        /// Matches the API's JWT configuration from Program.cs.
        /// </summary>
        /// <param name="userId">User ID to include in token</param>
        /// <param name="email">User email to include in token</param>
        /// <param name="role">User role for authorization (e.g., "Administrator", "Member")</param>
        /// <param name="sceneName">User scene name (optional)</param>
        /// <returns>JWT token string</returns>
        protected string GenerateJwtToken(Guid userId, string email, string? role = null, string? sceneName = null)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Add role claim if provided (CRITICAL for authorization)
            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add scene name if provided
            if (!string.IsNullOrEmpty(sceneName))
            {
                claims.Add(new Claim("scene_name", sceneName));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(JwtExpirationMinutes),
                Issuer = JwtIssuer,
                Audience = JwtAudience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Creates a test venue in the database.
        /// All Events must have a valid VenueId to satisfy foreign key constraints.
        /// </summary>
        /// <param name="name">Venue name (optional, defaults to unique test name)</param>
        /// <returns>The ID of the created venue</returns>
        protected async Task<int> CreateTestVenueAsync(string? name = null)
        {
            await using var context = CreateDbContext();

            var venue = new WitchCityRope.Api.Models.Venue
            {
                Name = name ?? $"Test Venue {Guid.NewGuid():N}"[..30],
                Directions = "Test directions",
                VenueInformation = "Test venue for integration testing",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Venues.Add(venue);
            await context.SaveChangesAsync();

            return venue.Id;
        }

        /// <summary>
        /// Fetches a CSRF token from the API for the given authenticated HttpClient.
        /// This must be called after authentication and before making state-changing requests.
        /// </summary>
        /// <param name="client">Authenticated HttpClient with Bearer token already set</param>
        /// <returns>CSRF token string extracted from XSRF-TOKEN cookie</returns>
        protected async Task<string> FetchCsrfTokenAsync(HttpClient client)
        {
            // Request CSRF token from antiforgery endpoint
            var tokenResponse = await client.GetAsync("/api/antiforgery/token");

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Failed to fetch CSRF token. Status: {tokenResponse.StatusCode}, Content: {errorContent}");
            }

            // Extract XSRF-TOKEN cookie from response
            if (!tokenResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                throw new InvalidOperationException("No Set-Cookie headers found in CSRF token response");
            }

            var xsrfCookie = cookies
                .Select(ParseXsrfTokenFromCookie)
                .FirstOrDefault(token => !string.IsNullOrEmpty(token));

            if (string.IsNullOrEmpty(xsrfCookie))
            {
                throw new InvalidOperationException(
                    $"XSRF-TOKEN cookie not found in response. Cookies: {string.Join(", ", cookies)}");
            }

            return xsrfCookie;
        }

        /// <summary>
        /// Parses XSRF-TOKEN value from a Set-Cookie header.
        /// </summary>
        private static string? ParseXsrfTokenFromCookie(string setCookieHeader)
        {
            // Cookie format: XSRF-TOKEN=value; path=/; samesite=strict
            if (!setCookieHeader.StartsWith("XSRF-TOKEN=", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var cookieValue = setCookieHeader["XSRF-TOKEN=".Length..];
            var semicolonIndex = cookieValue.IndexOf(';');

            return semicolonIndex > 0
                ? cookieValue[..semicolonIndex]
                : cookieValue;
        }

        /// <summary>
        /// Adds CSRF token to an HttpClient's default request headers.
        /// Call this after fetching the CSRF token and before making state-changing requests.
        /// </summary>
        /// <param name="client">HttpClient to add CSRF token header to</param>
        /// <param name="csrfToken">CSRF token value from FetchCsrfTokenAsync</param>
        protected void AddCsrfTokenHeader(HttpClient client, string csrfToken)
        {
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfToken);
        }

        /// <summary>
        /// Creates an authenticated HttpClient with CSRF token automatically configured.
        /// This is a convenience method that combines client creation, authentication, and CSRF token fetching.
        /// </summary>
        /// <param name="factory">WebApplicationFactory to create client from</param>
        /// <param name="bearerToken">JWT bearer token for authentication</param>
        /// <returns>Configured HttpClient ready for state-changing requests</returns>
        protected async Task<HttpClient> CreateAuthenticatedClientWithCsrfAsync(
            Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory,
            string bearerToken)
        {
            // Create client and set bearer token
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            // Fetch CSRF token
            var csrfToken = await FetchCsrfTokenAsync(client);

            // Add CSRF token to default headers
            AddCsrfTokenHeader(client, csrfToken);

            return client;
        }

        /// <summary>
        /// Creates a WebApplicationFactory configured to use the TestContainers database.
        /// CRITICAL: Overrides both the connection string (for Program.cs NpgsqlDataSource)
        /// AND the DbContext registration (for DI). Without the connection string override,
        /// Program.cs builds a NpgsqlDataSource with the wrong connection string at startup.
        /// </summary>
        /// <summary>
        /// No-op antiforgery implementation for integration tests.
        /// Validates all requests and returns dummy tokens.
        /// </summary>
        private class NoOpAntiforgery : Microsoft.AspNetCore.Antiforgery.IAntiforgery
        {
            public Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            {
                return new Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet("test-request-token", "test-cookie-token", "X-CSRF-TOKEN", "XSRF-TOKEN");
            }

            public Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetTokens(HttpContext httpContext)
            {
                return new Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet("test-request-token", "test-cookie-token", "X-CSRF-TOKEN", "XSRF-TOKEN");
            }

            public Task<bool> IsRequestValidAsync(HttpContext httpContext)
            {
                return Task.FromResult(true);
            }

            public void SetCookieTokenAndHeader(HttpContext httpContext) { }

            public Task ValidateRequestAsync(HttpContext httpContext)
            {
                return Task.CompletedTask;
            }
        }

        protected WebApplicationFactory<Program> CreateTestWebApplicationFactory()
        {
            return new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // Override connection string BEFORE Program.cs runs
                    // This ensures the NpgsqlDataSource built in Program.cs uses the test container
                    builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);

                    // Disable seed data - integration tests create their own data
                    builder.UseSetting("DatabaseInitialization:EnableSeedData", "false");

                    // Set test environment
                    builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Testing");

                    // Use mock PayPal service for integration tests
                    builder.UseSetting("USE_MOCK_PAYMENT_SERVICE", "true");

                    builder.ConfigureServices(services =>
                    {
                        // Remove the app's DbContext registration
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        // Build NpgsqlDataSource with EnableDynamicJson() for JSONB column support
                        // Without this, JSONB properties (e.g. EventAttendance.Metadata) fail on save
                        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(ConnectionString);
                        dataSourceBuilder.EnableDynamicJson();
                        var dataSource = dataSourceBuilder.Build();

                        // Add DbContext using the test container's data source
                        services.AddDbContext<ApplicationDbContext>(options =>
                        {
                            options.UseNpgsql(dataSource);
                            options.ConfigureWarnings(warnings =>
                            {
                                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
                            });
                        });

                        // Use in-memory Hangfire storage for tests
                        services.AddHangfire(config => config.UseMemoryStorage());

                        // Replace IAntiforgery with a no-op implementation for tests
                        // Integration tests focus on business logic, not CSRF protection
                        var antiforgeryDescriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(Microsoft.AspNetCore.Antiforgery.IAntiforgery));
                        if (antiforgeryDescriptor != null)
                        {
                            services.Remove(antiforgeryDescriptor);
                        }
                        services.AddSingleton<Microsoft.AspNetCore.Antiforgery.IAntiforgery, NoOpAntiforgery>();

                        // Replace real encryption with mock for tests
                        // Real EncryptionService requires properly encrypted values;
                        // MockEncryptionService passes through plaintext (e.g. "encrypted-capture-id")
                        var encryptionDescriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(WitchCityRope.Api.Features.Safety.Services.IEncryptionService));
                        if (encryptionDescriptor != null)
                        {
                            services.Remove(encryptionDescriptor);
                        }
                        services.AddScoped<WitchCityRope.Api.Features.Safety.Services.IEncryptionService,
                            WitchCityRope.Api.Features.Shared.Services.MockEncryptionService>();
                    });
                });
        }
    }
}