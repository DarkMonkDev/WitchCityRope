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
        var role = context.User.FindFirst(ClaimTypes.Role)?.Value;

        var disposables = new List<IDisposable>();

        try
        {
            if (userId != null)
                disposables.Add(LogContext.PushProperty("UserId", userId));
            if (email != null)
                disposables.Add(LogContext.PushProperty("UserEmail", email));
            if (role != null)
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
