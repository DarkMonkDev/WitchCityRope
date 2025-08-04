using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace WitchCityRope.Web.Middleware
{
    /// <summary>
    /// Middleware to ensure proper Blazor initialization headers and responses
    /// </summary>
    public class BlazorInitializationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BlazorInitializationMiddleware> _logger;

        public BlazorInitializationMiddleware(RequestDelegate next, ILogger<BlazorInitializationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add headers to ensure proper Blazor circuit establishment
            if (context.Request.Path.StartsWithSegments("/_blazor"))
            {
                // Ensure proper content type for Blazor negotiation
                if (context.Request.Path.Value?.Contains("negotiate") == true)
                {
                    context.Response.Headers["Cache-Control"] = "no-cache, no-store";
                    context.Response.Headers["Pragma"] = "no-cache";
                }
                
                // Log Blazor circuit requests for debugging
                _logger.LogDebug("Blazor circuit request: {Path}", context.Request.Path);
            }
            
            // For the main app pages, ensure proper headers
            if (!context.Request.Path.StartsWithSegments("/api") && 
                !context.Request.Path.StartsWithSegments("/_") &&
                !context.Request.Path.StartsWithSegments("/css") &&
                !context.Request.Path.StartsWithSegments("/js") &&
                !context.Request.Path.StartsWithSegments("/lib") &&
                context.Request.Method == "GET")
            {
                // Add header to help E2E tests identify Blazor pages
                context.Response.Headers["X-Blazor-Server"] = "true";
            }

            await _next(context);
        }
    }
}