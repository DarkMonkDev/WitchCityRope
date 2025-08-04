using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using WitchCityRope.IntegrationTests;
using WitchCityRope.Infrastructure.Data;
using AngleSharp;
using AngleSharp.Html.Dom;

namespace WitchCityRope.Tests
{
    [Collection("PostgreSQL Integration Tests")]
    public class LoginDebugTest : IAsyncLifetime
    {
        private readonly PostgreSqlFixture _postgresFixture;
        private readonly TestWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;

        public LoginDebugTest(PostgreSqlFixture postgresFixture, ITestOutputHelper output)
        {
            _postgresFixture = postgresFixture;
            _factory = new TestWebApplicationFactory(postgresFixture.PostgresContainer);
            _output = output;
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _factory.DisposeAsync();
        }

        [Fact]
        public async Task DebugLoginFlow()
        {
            // Create test client
            var client = _factory.WithWebHostBuilder(builder => { }).CreateClient();
            
            // 1. Navigate to the login page
            _output.WriteLine("Navigating to login page...");
            var loginPageResponse = await client.GetAsync("/login");
            _output.WriteLine($"Login page status: {loginPageResponse.StatusCode}");
            
            if (!loginPageResponse.IsSuccessStatusCode)
            {
                _output.WriteLine($"Failed to load login page. Status: {loginPageResponse.StatusCode}");
                return;
            }

            var loginPageContent = await loginPageResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"Login page content length: {loginPageContent.Length}");

            // Parse the HTML
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(loginPageContent));

            // 2. Check what fields are present
            _output.WriteLine("\nChecking for form fields...");
            
            var emailField = document.QuerySelector("input[name='Input.Email']");
            var passwordField = document.QuerySelector("input[name='Input.Password']");
            var submitButton = document.QuerySelector("button[type='submit']");
            var form = document.QuerySelector("form#account");
            
            _output.WriteLine($"Email field found: {emailField != null}");
            _output.WriteLine($"Password field found: {passwordField != null}");
            _output.WriteLine($"Submit button found: {submitButton != null}");
            _output.WriteLine($"Form found: {form != null}");

            // Get the antiforgery token
            var antiforgeryToken = document.QuerySelector("input[name='__RequestVerificationToken']") as IHtmlInputElement;
            if (antiforgeryToken != null)
            {
                _output.WriteLine($"Antiforgery token found: {antiforgeryToken.Value?.Substring(0, 20)}...");
            }

            // 3. Try to login with admin credentials
            if (form != null && antiforgeryToken != null)
            {
                _output.WriteLine("\nAttempting to login with admin credentials...");
                
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Input.Email", "admin@witchcityrope.com"),
                    new KeyValuePair<string, string>("Input.Password", "Admin123!"),
                    new KeyValuePair<string, string>("Input.RememberMe", "false"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken.Value)
                });

                var loginResponse = await client.PostAsync("/login", formContent);
                _output.WriteLine($"Login response status: {loginResponse.StatusCode}");
                _output.WriteLine($"Login response headers:");
                foreach (var header in loginResponse.Headers)
                {
                    _output.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }

                var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
                
                // Check if we were redirected (successful login)
                if (loginResponse.StatusCode == System.Net.HttpStatusCode.Redirect || 
                    loginResponse.StatusCode == System.Net.HttpStatusCode.Found)
                {
                    _output.WriteLine($"Login successful! Redirected to: {loginResponse.Headers.Location}");
                }
                else
                {
                    // Parse response for errors
                    var responseDoc = await context.OpenAsync(req => req.Content(loginResponseContent));
                    var validationErrors = responseDoc.QuerySelector(".validation-errors");
                    if (validationErrors != null)
                    {
                        _output.WriteLine($"Validation errors found: {validationErrors.TextContent.Trim()}");
                    }
                    
                    var fieldErrors = responseDoc.QuerySelectorAll(".field-error");
                    foreach (var error in fieldErrors)
                    {
                        var errorText = error.TextContent.Trim();
                        if (!string.IsNullOrWhiteSpace(errorText))
                        {
                            _output.WriteLine($"Field error: {errorText}");
                        }
                    }

                    // Check if we're on the login page still
                    var pageTitle = responseDoc.QuerySelector("title")?.TextContent;
                    _output.WriteLine($"Response page title: {pageTitle}");
                }
            }

            // 4. Try alternate login paths
            _output.WriteLine("\nChecking for alternate login paths...");
            
            var loginResponse2 = await client.GetAsync("/login");
            _output.WriteLine($"/login - Status: {loginResponse2.StatusCode}");
            
            var loginResponse3 = await client.GetAsync("/Account/Login");
            _output.WriteLine($"/Account/Login - Status: {loginResponse3.StatusCode}");

            // 5. Check admin URLs
            _output.WriteLine("\nChecking admin URLs...");
            
            var adminUrls = new[] { "/admin", "/admin/events", "/events/create", "/Admin/Events" };
            foreach (var url in adminUrls)
            {
                var response = await client.GetAsync(url);
                _output.WriteLine($"{url} - Status: {response.StatusCode}");
                if (response.StatusCode == System.Net.HttpStatusCode.Redirect || 
                    response.StatusCode == System.Net.HttpStatusCode.Found)
                {
                    _output.WriteLine($"  Redirects to: {response.Headers.Location}");
                }
            }
        }

        [Fact]
        public async Task CheckAdminUserExists()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
            
            // Check if admin user exists
            var adminUser = dbContext.Users.FirstOrDefault(u => u.Email == "admin@witchcityrope.com");
            
            if (adminUser != null)
            {
                _output.WriteLine($"Admin user found:");
                _output.WriteLine($"  ID: {adminUser.Id}");
                _output.WriteLine($"  Email: {adminUser.Email}");
                _output.WriteLine($"  SceneName: {adminUser.SceneNameValue}");
                _output.WriteLine($"  Role: {adminUser.Role}");
                _output.WriteLine($"  IsActive: {adminUser.IsActive}");
                _output.WriteLine($"  IsVetted: {adminUser.IsVetted}");
                _output.WriteLine($"  EmailConfirmed: {adminUser.EmailConfirmed}");
                _output.WriteLine($"  LockoutEnabled: {adminUser.LockoutEnabled}");
                _output.WriteLine($"  LockoutEnd: {adminUser.LockoutEnd}");
            }
            else
            {
                _output.WriteLine("Admin user NOT found in database!");
                
                // List all users
                var allUsers = dbContext.Users.ToList();
                _output.WriteLine($"\nTotal users in database: {allUsers.Count}");
                foreach (var user in allUsers)
                {
                    _output.WriteLine($"  - {user.Email} (Role: {user.Role})");
                }
            }
        }
    }
}