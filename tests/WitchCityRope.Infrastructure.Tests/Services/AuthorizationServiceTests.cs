using System.Security.Claims;
using WitchCityRope.Infrastructure.Services;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.Services
{
    public class AuthorizationServiceTests
    {
        private readonly AuthorizationService _authorizationService;

        public AuthorizationServiceTests()
        {
            _authorizationService = new AuthorizationService();
        }

        private ClaimsPrincipal CreateUser(string role, bool isAuthenticated = true)
        {
            if (!isAuthenticated)
                return new ClaimsPrincipal();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, $"test{role}"),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public void Authentication_NotRequired_ForPublicEndpoints() // line 103
        {
            // Arrange
            var anonymousUser = new ClaimsPrincipal();
            var publicEndpoints = new[] { "/", "/events", "/about", "/contact", "/privacy", "/terms", "/login", "/register", "/resources" };

            // Act & Assert
            foreach (var endpoint in publicEndpoints)
            {
                var canAccess = _authorizationService.CanAccessEndpoint(anonymousUser, endpoint);
                Assert.True(canAccess, $"Public endpoint {endpoint} should be accessible without authentication");
            }

            // Specifically test that /events is public as mentioned in the requirement
            Assert.True(_authorizationService.IsPublicEndpoint("/events"));
        }

        [Fact]
        public void AuthenticatedUsers_CanAccess_RegularEndpoints() // line 81
        {
            // Arrange
            var authenticatedUser = CreateUser("Member");
            var regularEndpoints = new[] { "/profile", "/dashboard", "/logout" };

            // Act & Assert
            foreach (var endpoint in regularEndpoints)
            {
                var canAccess = _authorizationService.CanAccessEndpoint(authenticatedUser, endpoint);
                Assert.True(canAccess, $"Authenticated user should be able to access {endpoint}");
            }

            // Specifically test /profile as mentioned in the requirement
            Assert.True(_authorizationService.CanAccessEndpoint(authenticatedUser, "/profile"));
        }

        [Fact]
        public void AdminUsers_CanAccessAll_Endpoints() // line 141
        {
            // Arrange
            var adminUser = CreateUser("Admin");
            var allEndpoints = new[]
            {
                "/", "/events", "/about", "/contact", "/privacy", "/terms", "/login", "/register",
                "/profile", "/dashboard", "/logout",
                "/admin", "/admin/users", "/admin/events", "/admin/dashboard", "/admin/reports",
                "/events/create", "/events/edit", "/member", "/member/dashboard"
            };

            // Act & Assert
            foreach (var endpoint in allEndpoints)
            {
                var canAccess = _authorizationService.CanAccessEndpoint(adminUser, endpoint);
                Assert.True(canAccess, $"Admin user should be able to access {endpoint}");
            }
        }

        [Fact]
        public void Members_HaveCorrectAccess_Patterns() // line 163
        {
            // Arrange
            var memberUser = CreateUser("Member");

            // Act & Assert - Members should have access to these endpoints
            var allowedEndpoints = new[]
            {
                "/events/create", // Specifically mentioned in the requirement
                "/profile",
                "/member",
                "/member/dashboard",
                "/my-tickets"
            };

            foreach (var endpoint in allowedEndpoints)
            {
                var canAccess = _authorizationService.CanAccessEndpoint(memberUser, endpoint);
                Assert.True(canAccess, $"Member should be able to access {endpoint}");
            }

            // Members should NOT have access to admin endpoints
            var restrictedEndpoints = new[]
            {
                "/admin",
                "/admin/users",
                "/admin/events",
                "/admin/dashboard"
            };

            foreach (var endpoint in restrictedEndpoints)
            {
                var canAccess = _authorizationService.CanAccessEndpoint(memberUser, endpoint);
                Assert.False(canAccess, $"Member should NOT be able to access {endpoint}");
            }
        }

        [Fact]
        public void Organizers_HaveCorrectAccess_Patterns() // line 185
        {
            // Arrange
            var organizerUser = CreateUser("Organizer");

            // Act & Assert - Organizers should have access to these endpoints
            var allowedEndpoints = new[]
            {
                "/admin/users", // Specifically mentioned in the requirement
                "/admin/events",
                "/events/create",
                "/events/edit",
                "/events/delete"
            };

            foreach (var endpoint in allowedEndpoints)
            {
                var canAccess = _authorizationService.CanAccessEndpoint(organizerUser, endpoint);
                Assert.True(canAccess, $"Organizer should be able to access {endpoint}");
            }

            // Organizers should NOT have access to full admin endpoints
            var restrictedEndpoints = new[]
            {
                "/admin/dashboard",
                "/admin/reports",
                "/admin/settings"
            };

            foreach (var endpoint in restrictedEndpoints)
            {
                var canAccess = _authorizationService.CanAccessEndpoint(organizerUser, endpoint);
                Assert.False(canAccess, $"Organizer should NOT be able to access {endpoint}");
            }
        }

        [Fact]
        public void NonAuthenticatedUsers_CannotAccess_ProtectedEndpoints()
        {
            // Arrange
            var anonymousUser = new ClaimsPrincipal();
            var protectedEndpoints = new[]
            {
                "/profile",
                "/dashboard",
                "/member",
                "/member/dashboard",
                "/admin",
                "/admin/users",
                "/events/create"
            };

            // Act & Assert
            foreach (var endpoint in protectedEndpoints)
            {
                var canAccess = _authorizationService.CanAccessEndpoint(anonymousUser, endpoint);
                Assert.False(canAccess, $"Anonymous user should NOT be able to access {endpoint}");
            }
        }

        [Fact]
        public void IsAuthenticated_ReturnsCorrectValue()
        {
            // Arrange
            var authenticatedUser = CreateUser("Member");
            var anonymousUser = new ClaimsPrincipal();

            // Act & Assert
            Assert.True(_authorizationService.IsAuthenticated(authenticatedUser));
            Assert.False(_authorizationService.IsAuthenticated(anonymousUser));
        }

        [Fact]
        public void IsInRole_ReturnsCorrectValue()
        {
            // Arrange
            var adminUser = CreateUser("Admin");
            var memberUser = CreateUser("Member");

            // Act & Assert
            Assert.True(_authorizationService.IsInRole(adminUser, "Admin"));
            Assert.False(_authorizationService.IsInRole(adminUser, "Member"));
            Assert.True(_authorizationService.IsInRole(memberUser, "Member"));
            Assert.False(_authorizationService.IsInRole(memberUser, "Admin"));
        }
    }
}