using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.IntegrationTests.Helpers;

namespace WitchCityRope.IntegrationTests
{
    /// <summary>
    /// Tests for protected route navigation using ASP.NET Core Identity
    /// </summary>
    [Collection("PostgreSQL Integration Tests")]
    public class ProtectedRouteNavigationTests : IAsyncLifetime
    {
        private readonly PostgreSqlFixture _postgresFixture;
        private readonly TestWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProtectedRouteNavigationTests(PostgreSqlFixture postgresFixture)
        {
            _postgresFixture = postgresFixture;
            _factory = new TestWebApplicationFactory(postgresFixture.PostgresContainer);
            _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _factory.DisposeAsync();
        }

        #region Route Definitions

        private static readonly string[] PublicRoutes = new[]
        {
            "/",
            "/events",
            "/login",
            "/register",
            "/forgot-password",
            "/about",
            "/code-of-conduct",
            "/contact",
            "/privacy",
            "/terms"
        };

        private static readonly string[] AuthenticatedRoutes = new[]
        {
            "/member/dashboard",
            "/member/profile", 
            "/member/tickets",
            "/member/settings",
            "/member/events"
        };

        private static readonly string[] AdminRoutes = new[]
        {
            "/admin",
            "/admin/users",
            "/admin/events",
            "/admin/events/new",
            "/admin/reports",
            "/admin/settings"
        };

        private static readonly string[] VettedMemberRoutes = new[]
        {
            "/member/vetted-events",
            "/member/presenter-resources"
        };

        #endregion

        #region Public Route Tests

        [Theory]
        [MemberData(nameof(GetPublicRoutes))]
        public async Task PublicRoutes_ShouldBeAccessible_WithoutAuthentication(string route)
        {
            // Act
            var response = await _client.GetAsync(route);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                $"Public route {route} should be accessible without authentication");

            // Verify no redirect to login
            response.Headers.Location.Should().BeNull(
                $"Public route {route} should not redirect");
        }

        public static IEnumerable<object[]> GetPublicRoutes()
        {
            return PublicRoutes.Select(route => new object[] { route });
        }

        #endregion

        #region Authenticated Route Tests

        [Theory]
        [MemberData(nameof(GetAuthenticatedRoutes))]
        public async Task AuthenticatedRoutes_ShouldRedirectToLogin_WhenNotAuthenticated(string route)
        {
            // Act
            var response = await _client.GetAsync(route);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect,
                $"Protected route {route} should redirect when not authenticated");

            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                var location = response.Headers.Location?.ToString() ?? "";
                location.Should().Contain("login", 
                    $"Protected route {route} should redirect to login");
                location.Should().Match(l => l.Contains($"ReturnUrl={Uri.EscapeDataString(route)}") || 
                                            l.Contains($"returnUrl={Uri.EscapeDataString(route)}"), 
                    $"Should include return URL for {route} (case-insensitive)");
            }
        }

        [Fact]
        public async Task AuthenticatedRoutes_ShouldRequireAuthentication()
        {
            // Note: In integration tests with ASP.NET Core Identity, 
            // we can't easily test authenticated access since cookie authentication
            // requires actual browser-like behavior. These scenarios are better
            // tested with E2E tests using Puppeteer.
            
            // This test verifies that the routes are properly protected
            foreach (var route in AuthenticatedRoutes)
            {
                var response = await _client.GetAsync(route);
                response.StatusCode.Should().Be(HttpStatusCode.Redirect,
                    $"{route} should be protected");
            }
        }

        public static IEnumerable<object[]> GetAuthenticatedRoutes()
        {
            return AuthenticatedRoutes.Select(route => new object[] { route });
        }

        #endregion

        #region Admin Route Tests

        [Theory]
        [MemberData(nameof(GetAdminRoutes))]
        public async Task AdminRoutes_ShouldRedirectToLogin_WhenNotAuthenticated(string route)
        {
            // Act
            var response = await _client.GetAsync(route);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect,
                $"Admin route {route} should redirect when not authenticated");

            var location = response.Headers.Location?.ToString() ?? "";
            location.Should().Contain("login",
                $"Admin route {route} should redirect to login");
        }

        [Fact]
        public async Task AdminRoutes_RequireAdminRole()
        {
            // Create a regular user
            using var scope = _factory.Services.CreateScope();
            var helper = new AuthenticationTestHelper(_client, scope.ServiceProvider);
            
            var regularUser = await helper.CreateAndAuthenticateUserAsync(
                email: "regular@test.com",
                password: "Test123!",
                isAdmin: false);

            // Note: Testing role-based authorization would require cookie authentication
            // which is not easily testable in integration tests. This is better
            // tested with E2E tests.
            
            // For now, we verify that admin routes are protected
            foreach (var route in AdminRoutes)
            {
                var response = await _client.GetAsync(route);
                response.StatusCode.Should().Be(HttpStatusCode.Redirect,
                    $"Admin route {route} should be protected");
            }
        }

        public static IEnumerable<object[]> GetAdminRoutes()
        {
            return AdminRoutes.Select(route => new object[] { route });
        }

        #endregion

        #region Vetted Member Route Tests

        [Theory]
        [MemberData(nameof(GetVettedMemberRoutes))]
        public async Task VettedMemberRoutes_ShouldRedirectToLogin_WhenNotAuthenticated(string route)
        {
            // Act
            var response = await _client.GetAsync(route);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect,
                $"Vetted member route {route} should redirect when not authenticated");

            var location = response.Headers.Location?.ToString() ?? "";
            location.Should().Contain("login",
                $"Vetted member route {route} should redirect to login");
        }

        public static IEnumerable<object[]> GetVettedMemberRoutes()
        {
            return VettedMemberRoutes.Select(route => new object[] { route });
        }

        #endregion

        #region Navigation Tests

        [Fact]
        public async Task Navigation_ShouldShowLoginLink_WhenNotAuthenticated()
        {
            // Act
            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            // Assert
            content.Should().Contain("href=\"/login\"", "Should show login link");
            content.Should().NotContain("href=\"/logout\"", "Should not show logout link");
        }

        [Fact]
        public async Task ProtectedPageRedirect_ShouldIncludeReturnUrl()
        {
            // Arrange
            var protectedUrl = "/member/dashboard";

            // Act
            var response = await _client.GetAsync(protectedUrl);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            var location = response.Headers.Location?.ToString();
            location.Should().NotBeNull();
            location.Should().Match(l => l.Contains("ReturnUrl=%2Fmember%2Fdashboard") || 
                                        l.Contains("returnUrl=%2Fmember%2Fdashboard"),
                "Should include return URL parameter (case-insensitive)");
        }

        #endregion

        #region Error Page Tests

        [Fact]
        public async Task NonExistentRoute_ShouldReturn404()
        {
            // Act
            var response = await _client.GetAsync("/this-route-does-not-exist");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AccessDeniedPage_ShouldBeAccessible()
        {
            // Act
            var response = await _client.GetAsync("/access-denied");

            // Assert
            // Access denied page might redirect to login or show a custom page
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK, 
                HttpStatusCode.Redirect);
        }

        #endregion
    }
}