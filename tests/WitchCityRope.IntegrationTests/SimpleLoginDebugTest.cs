using System;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;
using AngleSharp;
using AngleSharp.Html.Dom;

namespace WitchCityRope.IntegrationTests
{
    [Collection("PostgreSQL Integration Tests")]
    public class SimpleLoginDebugTest : IAsyncLifetime
    {
        private readonly PostgreSqlFixture _postgresFixture;
        private readonly ITestOutputHelper _output;
        private TestWebApplicationFactory _factory = null!;
        private HttpClient _client = null!;

        public SimpleLoginDebugTest(PostgreSqlFixture postgresFixture, ITestOutputHelper output)
        {
            _postgresFixture = postgresFixture;
            _output = output;
        }

        public async Task InitializeAsync()
        {
            _factory = new TestWebApplicationFactory(_postgresFixture.PostgresContainer);
            
            // Ensure database is seeded
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<SimpleLoginDebugTest>>();
                
                // Initialize database with seed data
                // Note: DbInitializer expects WitchCityRopeDbContext, but we're using WitchCityRopeIdentityDbContext
                // Skip the DbInitializer for now - the database is seeded via migrations
            }
            
            _client = _factory.CreateClient();
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            await _factory.DisposeAsync();
        }

        [Fact]
        public async Task CheckDatabaseState()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
            
            // Check users
            var users = dbContext.Users.ToList();
            _output.WriteLine($"Total users in database: {users.Count}");
            
            foreach (var user in users)
            {
                _output.WriteLine($"User: {user.Email}, Role: {user.Role}, SceneName: {user.SceneNameValue}, Active: {user.IsActive}, Vetted: {user.IsVetted}");
            }
            
            // Check events
            var events = dbContext.Events.ToList();
            _output.WriteLine($"\nTotal events in database: {events.Count}");
        }

        [Fact]
        public async Task TestLoginWithSeededUser()
        {
            // 1. Navigate to the login page
            _output.WriteLine("Navigating to login page...");
            var loginPageResponse = await _client.GetAsync("/login");
            _output.WriteLine($"Login page status: {loginPageResponse.StatusCode}");
            
            if (!loginPageResponse.IsSuccessStatusCode)
            {
                _output.WriteLine($"Failed to load login page. Status: {loginPageResponse.StatusCode}");
                return;
            }

            var loginPageContent = await loginPageResponse.Content.ReadAsStringAsync();
            
            // Parse the HTML
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(loginPageContent));

            // Get the antiforgery token
            var antiforgeryToken = document.QuerySelector("input[name='__RequestVerificationToken']") as IHtmlInputElement;
            if (antiforgeryToken == null)
            {
                _output.WriteLine("Antiforgery token not found!");
                return;
            }

            // 2. Try to login with correct password from seeder
            _output.WriteLine("\nAttempting to login with admin@witchcityrope.com / Test123!");
            
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Input.Email", "admin@witchcityrope.com"),
                new KeyValuePair<string, string>("Input.Password", "Test123!"),
                new KeyValuePair<string, string>("Input.RememberMe", "false"),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken.Value)
            });

            var loginResponse = await _client.PostAsync("/login", formContent);
            _output.WriteLine($"Login response status: {loginResponse.StatusCode}");

            // Check if we were redirected (successful login)
            if (loginResponse.StatusCode == System.Net.HttpStatusCode.Redirect || 
                loginResponse.StatusCode == System.Net.HttpStatusCode.Found)
            {
                _output.WriteLine($"Login successful! Redirected to: {loginResponse.Headers.Location}");
                
                // Follow the redirect
                if (loginResponse.Headers.Location != null)
                {
                    var redirectResponse = await _client.GetAsync(loginResponse.Headers.Location);
                    _output.WriteLine($"Redirect response status: {redirectResponse.StatusCode}");
                }
            }
            else
            {
                var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
                
                // Parse response for errors
                var responseDoc = await context.OpenAsync(req => req.Content(loginResponseContent));
                var validationErrors = responseDoc.QuerySelector(".validation-errors");
                if (validationErrors != null)
                {
                    _output.WriteLine($"Validation errors found: {validationErrors.TextContent.Trim()}");
                }
            }
        }

        [Fact]
        public async Task TestAdminAccessAfterLogin()
        {
            // First login
            await LoginAsAdmin();
            
            // Try to access admin pages
            _output.WriteLine("\nTesting access to admin pages...");
            
            var adminUrls = new[] 
            { 
                "/admin", 
                "/admin/events", 
                "/admin/dashboard",
                "/Admin/Events/Index" 
            };
            
            foreach (var url in adminUrls)
            {
                var response = await _client.GetAsync(url);
                _output.WriteLine($"{url} - Status: {response.StatusCode}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Redirect || 
                    response.StatusCode == System.Net.HttpStatusCode.Found)
                {
                    _output.WriteLine($"  Redirects to: {response.Headers.Location}");
                }
                else if (response.IsSuccessStatusCode)
                {
                    _output.WriteLine($"  Success! Can access {url}");
                }
            }
        }

        [Fact]
        public async Task FindEventCreationUrl()
        {
            // First login
            await LoginAsAdmin();
            
            // Get the admin events page
            _output.WriteLine("\nLooking for event creation URL...");
            var eventsResponse = await _client.GetAsync("/admin/events");
            
            if (eventsResponse.IsSuccessStatusCode)
            {
                var content = await eventsResponse.Content.ReadAsStringAsync();
                
                // Parse the HTML
                var config = Configuration.Default;
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(req => req.Content(content));
                
                // Look for "Create" or "New" buttons/links
                var createLinks = document.QuerySelectorAll("a").Where(a => 
                    a.TextContent.Contains("Create") ||
                    a.TextContent.Contains("New") ||
                    a.TextContent.Contains("Add"));
                
                foreach (var link in createLinks)
                {
                    var href = link.GetAttribute("href");
                    _output.WriteLine($"Found link: '{link.TextContent.Trim()}' -> {href}");
                }
                
                // Also look for buttons
                var createButtons = document.QuerySelectorAll("button").Where(b => 
                    b.TextContent.Contains("Create") ||
                    b.TextContent.Contains("New") ||
                    b.TextContent.Contains("Add"));
                
                foreach (var button in createButtons)
                {
                    _output.WriteLine($"Found button: '{button.TextContent.Trim()}'");
                }
            }
        }

        private async Task LoginAsAdmin()
        {
            // Get login page
            var loginPageResponse = await _client.GetAsync("/login");
            var loginPageContent = await loginPageResponse.Content.ReadAsStringAsync();
            
            // Parse and get token
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(loginPageContent));
            var antiforgeryToken = document.QuerySelector("input[name='__RequestVerificationToken']") as IHtmlInputElement;
            
            // Login
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Input.Email", "admin@witchcityrope.com"),
                new KeyValuePair<string, string>("Input.Password", "Test123!"),
                new KeyValuePair<string, string>("Input.RememberMe", "false"),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken!.Value)
            });
            
            await _client.PostAsync("/login", formContent);
        }
    }
}