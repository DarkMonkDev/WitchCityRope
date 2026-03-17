using System.Security.Claims;
using Serilog.Context;

namespace WitchCityRope.Api.Features.Logging.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
        // Multi-role support: join all role claims for logging context
        var role = string.Join(",", context.User.FindAll(ClaimTypes.Role).Select(c => c.Value));

        var disposables = new List<IDisposable>();

        try
        {
            // Push UserId as Guid so the PostgreSQL sink can write to UUID column
            if (userId != null && Guid.TryParse(userId, out var userGuid))
                disposables.Add(LogContext.PushProperty("UserId", userGuid));
            if (email != null)
                disposables.Add(LogContext.PushProperty("UserEmail", email));
            if (!string.IsNullOrEmpty(role))
                disposables.Add(LogContext.PushProperty("UserRole", role));

            await _next(context);
        }
        finally
        {
            foreach (var disposable in disposables)
                disposable.Dispose();
        }
    }
}
