using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WitchCityRope.Web.Middleware
{
    /// <summary>
    /// Middleware to handle authorization for Blazor Server protected routes
    /// </summary>
    public class BlazorAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string[] _protectedPaths = { "/member/dashboard", "/dashboard", "/my-tickets", "/profile", "/admin", "/member" };
        private readonly string[] _publicPaths = { "/", "/events", "/about", "/contact", "/apply", "/login", "/register", 
                                                    "/forgot-password", "/reset-password", "/privacy", "/terms", "/resources",
                                                    "/identity", "/account" };

        public BlazorAuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            
            // Skip authentication checks for static files and framework paths
            if (path.StartsWith("/_blazor") || path.StartsWith("/_framework") || 
                path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/images") ||
                path.StartsWith("/_content") || path.Contains("."))
            {
                await _next(context);
                return;
            }
            
            // Check if this is explicitly a public path
            var isPublicPath = _publicPaths.Any(p => path.Equals(p) || path.StartsWith(p + "/"));
            
            // Check if this is a protected path
            var isProtectedPath = _protectedPaths.Any(p => path.StartsWith(p.ToLowerInvariant()));
            
            // Only enforce authentication for protected paths
            if (isProtectedPath && !(context.User?.Identity?.IsAuthenticated ?? false))
            {
                // For non-browser requests or when auto-redirect is disabled, return 401
                if (context.Request.Headers.ContainsKey("X-Requested-With") ||
                    context.Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    context.Response.StatusCode = 401;
                    return;
                }
                
                // For browser requests to protected pages, redirect to login
                var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                context.Response.Redirect($"/login?returnUrl={returnUrl}");
                return;
            }

            // Continue processing for public paths and authenticated users
            await _next(context);
        }
    }
}