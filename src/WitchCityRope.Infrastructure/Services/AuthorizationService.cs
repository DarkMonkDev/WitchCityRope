using System.Security.Claims;
using WitchCityRope.Core.Interfaces;

namespace WitchCityRope.Infrastructure.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly string[] _publicEndpoints = new[]
        {
            "/",
            "/events",
            "/about",
            "/contact",
            "/privacy",
            "/terms",
            "/login",
            "/register",
            "/resources"
        };

        private readonly Dictionary<string, string[]> _roleEndpoints = new()
        {
            ["Admin"] = new[]
            {
                "/admin",
                "/admin/users",
                "/admin/events",
                "/admin/dashboard",
                "/admin/reports",
                "/admin/settings"
            },
            ["Organizer"] = new[]
            {
                "/admin/users",
                "/admin/events",
                "/events/create",
                "/events/edit",
                "/events/delete"
            },
            ["Member"] = new[]
            {
                "/events/create",
                "/profile",
                "/member",
                "/member/dashboard",
                "/my-tickets"
            }
        };

        public bool IsAuthenticated(ClaimsPrincipal? user)
        {
            return user?.Identity?.IsAuthenticated ?? false;
        }

        public bool IsInRole(ClaimsPrincipal? user, string role)
        {
            if (user == null || !IsAuthenticated(user))
                return false;

            return user.IsInRole(role);
        }

        public bool CanAccessEndpoint(ClaimsPrincipal? user, string endpoint)
        {
            // Normalize endpoint
            endpoint = endpoint.ToLowerInvariant().TrimEnd('/');
            if (string.IsNullOrEmpty(endpoint))
                endpoint = "/";

            // Public endpoints are accessible to everyone
            if (IsPublicEndpoint(endpoint))
                return true;

            // Must be authenticated for non-public endpoints
            if (!IsAuthenticated(user))
                return false;

            // Admin can access everything
            if (IsInRole(user, "Admin"))
                return true;

            // Check role-specific endpoints
            foreach (var roleEndpoint in _roleEndpoints)
            {
                if (IsInRole(user, roleEndpoint.Key))
                {
                    foreach (var allowedEndpoint in roleEndpoint.Value)
                    {
                        if (endpoint.StartsWith(allowedEndpoint.ToLowerInvariant()))
                            return true;
                    }
                }
            }

            // Authenticated users can access some general endpoints
            var authenticatedEndpoints = new[]
            {
                "/profile",
                "/dashboard",
                "/logout"
            };

            foreach (var authenticatedEndpoint in authenticatedEndpoints)
            {
                if (endpoint.StartsWith(authenticatedEndpoint))
                    return true;
            }

            return false;
        }

        public bool IsPublicEndpoint(string endpoint)
        {
            // Normalize endpoint
            endpoint = endpoint.ToLowerInvariant().TrimEnd('/');
            if (string.IsNullOrEmpty(endpoint))
                endpoint = "/";

            // Check for exact matches first
            foreach (var publicEndpoint in _publicEndpoints)
            {
                if (endpoint == publicEndpoint.ToLowerInvariant())
                    return true;
            }

            // Special handling for /events - only the list page is public, not subpages like /events/create
            if (endpoint == "/events")
                return true;

            // For other endpoints, allow subdirectories (e.g., /about/team)
            var otherPublicPrefixes = new[] { "/about", "/contact", "/privacy", "/terms", "/resources" };
            foreach (var prefix in otherPublicPrefixes)
            {
                if (endpoint.StartsWith(prefix + "/"))
                    return true;
            }

            return false;
        }
    }
}