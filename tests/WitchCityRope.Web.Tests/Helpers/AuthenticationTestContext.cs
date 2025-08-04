using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Bunit;
using Bunit.TestDoubles;
using Moq;
using WitchCityRope.Web.Services;
using WitchCityRope.Core.DTOs;

namespace WitchCityRope.Web.Tests.Helpers
{
    /// <summary>
    /// Helper class for setting up authentication state in tests
    /// </summary>
    public static class AuthenticationTestContext
    {
        /// <summary>
        /// Sets up an authenticated user context for testing
        /// </summary>
        public static TestAuthorizationContext SetupAuthenticatedUser(TestContext context, 
            string userId = "test-user-id",
            string email = "test@example.com",
            string sceneName = "TestUser",
            string[] roles = null)
        {
            roles ??= new[] { "Member" };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, sceneName)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            var authContext = context.AddTestAuthorization();
            authContext.SetAuthorized(sceneName);

            return authContext;
        }

        /// <summary>
        /// Sets up an anonymous (not authenticated) user context
        /// </summary>
        public static TestAuthorizationContext SetupAnonymousUser(TestContext context)
        {
            var authContext = context.AddTestAuthorization();
            authContext.SetNotAuthorized();
            return authContext;
        }

        /// <summary>
        /// Creates a mock IAuthService with common setup
        /// </summary>
        public static Mock<IAuthService> CreateMockAuthService(bool isAuthenticated = true)
        {
            var mock = new Mock<IAuthService>();
            
            mock.Setup(x => x.IsAuthenticatedAsync())
                .ReturnsAsync(isAuthenticated);

            if (isAuthenticated)
            {
                mock.Setup(x => x.GetCurrentUserAsync())
                    .ReturnsAsync(new UserDto
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Email = "test@example.com",
                        SceneName = "TestUser",
                        Roles = new List<string> { "Member" }
                    });
            }

            return mock;
        }

        /// <summary>
        /// Creates a custom AuthenticationStateProvider for testing
        /// </summary>
        public static AuthenticationStateProvider CreateAuthenticationStateProvider(bool isAuthenticated = true)
        {
            var authStateMock = new Mock<AuthenticationStateProvider>();
            
            if (isAuthenticated)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                    new Claim(ClaimTypes.Email, "test@example.com"),
                    new Claim(ClaimTypes.Name, "TestUser"),
                    new Claim(ClaimTypes.Role, "Member")
                };

                var identity = new ClaimsIdentity(claims, "test");
                var principal = new ClaimsPrincipal(identity);
                var authState = Task.FromResult(new AuthenticationState(principal));
                
                authStateMock.Setup(x => x.GetAuthenticationStateAsync())
                    .Returns(authState);
            }
            else
            {
                var authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
                authStateMock.Setup(x => x.GetAuthenticationStateAsync())
                    .Returns(authState);
            }

            return authStateMock.Object;
        }
    }
}