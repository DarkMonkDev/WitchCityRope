using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;
using System.Linq;

namespace WitchCityRope.IntegrationTests.Helpers
{
    /// <summary>
    /// Helper class for authentication-related operations in integration tests
    /// </summary>
    public class AuthenticationTestHelper
    {
        private readonly HttpClient _client;
        private readonly IServiceProvider _serviceProvider;

        public AuthenticationTestHelper(HttpClient client, IServiceProvider serviceProvider)
        {
            _client = client;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Creates and authenticates a test user using ASP.NET Core Identity
        /// </summary>
        public async Task<AuthenticatedUser> CreateAndAuthenticateUserAsync(
            string email = "testuser@example.com",
            string password = "StrongPassword123!",
            string sceneName = "TestUser",
            string legalName = "Test User",
            bool isAdmin = false,
            bool isVetted = false)
        {
            // Create user directly in the database using UserManager
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<WitchCityRopeRole>>();
            
            // Ensure roles exist
            if (isAdmin && !await roleManager.RoleExistsAsync("Administrator"))
            {
                await roleManager.CreateAsync(new WitchCityRopeRole("Administrator") { Description = "System administrator" });
            }
            
            // Create the user
            var user = new WitchCityRopeUser(
                encryptedLegalName: $"ENCRYPTED_{legalName}",
                sceneName: Core.ValueObjects.SceneName.Create(sceneName),
                email: Core.ValueObjects.EmailAddress.Create(email),
                dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                role: isAdmin ? Core.Enums.UserRole.Administrator : Core.Enums.UserRole.Attendee
            );
            
            user.UserName = email;
            user.EmailConfirmed = true;
            
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
            
            // Add roles
            if (isAdmin)
            {
                await userManager.AddToRoleAsync(user, "Administrator");
            }
            
            // Set vetting status
            if (isVetted)
            {
                user.MarkAsVetted();
                var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
                await dbContext.SaveChangesAsync();
            }
            
            return new AuthenticatedUser
            {
                UserId = user.Id,
                Email = email,
                SceneName = sceneName,
                Token = "N/A", // Integration tests use cookie authentication set up by the test infrastructure
                IsAdmin = isAdmin,
                IsVetted = isVetted
            };
        }

        /// <summary>
        /// Performs logout through the UI
        /// </summary>
        public async Task LogoutAsync()
        {
            // Navigate to logout page which will clear the authentication cookie
            var response = await _client.GetAsync("/logout");
            
            // Follow any redirects
            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    await _client.GetAsync(location);
                }
            }
        }
        

        /// <summary>
        /// Creates multiple test users with different roles
        /// </summary>
        public async Task<TestUsersSet> CreateTestUsersAsync()
        {
            var regularUser = await CreateAndAuthenticateUserAsync(
                email: $"regular_{Guid.NewGuid():N}@test.com",
                sceneName: $"RegularUser_{Guid.NewGuid():N}");

            var adminUser = await CreateAndAuthenticateUserAsync(
                email: $"admin_{Guid.NewGuid():N}@test.com",
                sceneName: $"AdminUser_{Guid.NewGuid():N}",
                isAdmin: true);

            var vettedUser = await CreateAndAuthenticateUserAsync(
                email: $"vetted_{Guid.NewGuid():N}@test.com",
                sceneName: $"VettedUser_{Guid.NewGuid():N}",
                isVetted: true);

            var vettedAdmin = await CreateAndAuthenticateUserAsync(
                email: $"vettedadmin_{Guid.NewGuid():N}@test.com",
                sceneName: $"VettedAdmin_{Guid.NewGuid():N}",
                isAdmin: true,
                isVetted: true);

            return new TestUsersSet
            {
                RegularUser = regularUser,
                AdminUser = adminUser,
                VettedUser = vettedUser,
                VettedAdminUser = vettedAdmin
            };
        }

    }

    /// <summary>
    /// Represents an authenticated test user
    /// </summary>
    public class AuthenticatedUser
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string SceneName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsVetted { get; set; }
    }

    /// <summary>
    /// A set of test users with different roles
    /// </summary>
    public class TestUsersSet
    {
        public AuthenticatedUser RegularUser { get; set; } = null!;
        public AuthenticatedUser AdminUser { get; set; } = null!;
        public AuthenticatedUser VettedUser { get; set; } = null!;
        public AuthenticatedUser VettedAdminUser { get; set; } = null!;
    }
}