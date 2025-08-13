extern alias WitchCityRopeWeb;

using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Core.Enums;
using Microsoft.AspNetCore.Identity;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.IntegrationTests.Extensions
{
    /// <summary>
    /// Extension methods for HttpClient to support integration testing
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Authenticates the HttpClient as an admin user for testing API endpoints
        /// </summary>
        public static async Task AuthenticateAsAdminAsync(this HttpClient client, WebApplicationFactory<WitchCityRopeWeb::Program>? factory = null)
        {
            if (factory == null)
            {
                throw new ArgumentException("Factory is required for authentication", nameof(factory));
            }

            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
            
            // Create admin user if it doesn't exist
            var adminEmail = "testadmin@witchcityrope.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                adminUser = new WitchCityRopeUser(
                    encryptedLegalName: "ENCRYPTED_TestAdmin",
                    sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create("TestAdmin"),
                    email: WitchCityRope.Core.ValueObjects.EmailAddress.Create(adminEmail),
                    dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    role: UserRole.Administrator
                );
                adminUser.EmailConfirmed = true;
                
                var result = await userManager.CreateAsync(adminUser, "Test123!");
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            // Authenticate via login endpoint
            var loginRequest = new
            {
                Email = adminEmail,
                Password = "Test123!",
                RememberMe = false
            };

            var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Authentication failed: {response.StatusCode} - {errorContent}");
            }
        }

        /// <summary>
        /// Authenticates the HttpClient as a regular user for testing
        /// </summary>
        public static async Task AuthenticateAsUserAsync(this HttpClient client, WebApplicationFactory<WitchCityRopeWeb::Program> factory, UserRole role = UserRole.Member)
        {
            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
            
            // Create test user
            var userEmail = $"testuser{Guid.NewGuid():N}@witchcityrope.com";
            var testUser = new WitchCityRopeUser(
                encryptedLegalName: "ENCRYPTED_TestUser",
                sceneName: WitchCityRope.Core.ValueObjects.SceneName.Create("TestUser"),
                email: WitchCityRope.Core.ValueObjects.EmailAddress.Create(userEmail),
                dateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                role: role
            );
            testUser.EmailConfirmed = true;
            
            var result = await userManager.CreateAsync(testUser, "Test123!");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Authenticate via login endpoint
            var loginRequest = new
            {
                Email = userEmail,
                Password = "Test123!",
                RememberMe = false
            };

            var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Authentication failed: {response.StatusCode} - {errorContent}");
            }
        }

        /// <summary>
        /// Creates a user note via API
        /// </summary>
        public static async Task<HttpResponseMessage> CreateNoteAsync(this HttpClient client, Guid userId, string noteType = "General", string content = "Test note")
        {
            var createDto = new
            {
                NoteType = noteType,
                Content = content
            };

            return await client.PostAsJsonAsync($"/api/admin/users/{userId}/notes", createDto);
        }

        /// <summary>
        /// Gets user notes via API
        /// </summary>
        public static async Task<HttpResponseMessage> GetUserNotesAsync(this HttpClient client, Guid userId)
        {
            return await client.GetAsync($"/api/admin/users/{userId}/notes");
        }
    }
}