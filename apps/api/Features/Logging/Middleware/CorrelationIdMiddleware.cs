using Serilog.Context;

namespace WitchCityRope.Api.Features.Logging.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headerValue = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        var correlationId = Guid.TryParse(headerValue, out var parsed) ? parsed : Guid.NewGuid();

        context.Response.Headers[CorrelationIdHeader] = correlationId.ToString();

        // Push as Guid so the PostgreSQL sink can write to UUID column
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
