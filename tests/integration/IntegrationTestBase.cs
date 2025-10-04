using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
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
        /// </summary>
        public virtual async Task DisposeAsync()
        {
            try
            {
                Logger.LogInformation("Disposing integration test resources");

                // Additional cleanup if needed
                // The database reset happens in InitializeAsync for the next test

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
    }
}