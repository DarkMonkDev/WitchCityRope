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

        public BlazorAuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            
            // Check if this is a protected path
            var isProtectedPath = _protectedPaths.Any(p => path.StartsWith(p.ToLowerInvariant()));
            
            if (isProtectedPath && !(context.User?.Identity?.IsAuthenticated ?? false))
            {
                // For non-browser requests or when auto-redirect is disabled, return 401
                if (context.Request.Headers.ContainsKey("X-Requested-With") ||
                    context.Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    context.Response.StatusCode = 401;
                    return;
                }
                
                // For browser requests, redirect to login
                var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                context.Response.Redirect($"/auth/login?returnUrl={returnUrl}");
                return;
            }

            await _next(context);
        }
    }
}