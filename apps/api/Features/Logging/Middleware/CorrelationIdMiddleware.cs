using Serilog.Context;

namespace WitchCityRope.Api.Features.Logging.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    // HttpContext.Items key used by CustomizeProblemDetails (in Program.cs) as a reliable
    // fallback. When an unhandled exception bubbles through the pipeline, ASP.NET Core resets
    // HttpContext.Response.Headers before GlobalExceptionHandler writes its response body —
    // so the X-Correlation-Id header we set here is gone by the time CustomizeProblemDetails
    // runs. HttpContext.Items survives the reset because it lives on HttpContext, not
    // HttpContext.Response. This is what guarantees every ProblemDetails response — even 500s
    // from GlobalExceptionHandler — carries the correlationId extension.
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headerValue = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        var correlationId = Guid.TryParse(headerValue, out var parsed) ? parsed : Guid.NewGuid();
        var correlationIdString = correlationId.ToString();

        // Set the response header (for the happy path — clients read this to correlate requests).
        context.Response.Headers[CorrelationIdHeader] = correlationIdString;

        // ALSO stash in HttpContext.Items so it survives the response-header reset that
        // happens when an exception propagates to GlobalExceptionHandler. See comment on
        // CorrelationIdItemKey above.
        context.Items[CorrelationIdItemKey] = correlationIdString;

        // Push as Guid so the PostgreSQL sink can write to UUID column
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
