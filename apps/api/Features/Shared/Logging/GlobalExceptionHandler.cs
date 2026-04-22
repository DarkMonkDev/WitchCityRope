using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WitchCityRope.Api.Features.Shared.Logging;

/// <summary>
/// Catches every exception that bubbles to the top of the request pipeline and serializes
/// it as a uniform RFC 7807 ProblemDetails response. Without this handler, an unhandled
/// exception produces either (a) the default ASP.NET 500 HTML page in Development or
/// (b) a plain empty 500 in Production — neither is parseable by the frontend's
/// error-extraction logic (see <c>apps/web/src/lib/api/client.ts</c>), so the user sees
/// "500 Internal Server Error" rather than the friendly error toast we render for every
/// other failure path.
///
/// <para>
/// The handler intentionally does NOT leak the exception message to the wire in
/// non-Development environments. Inner-exception detail is captured server-side via
/// <c>_logger.LogError(ex, …)</c>; the client gets a generic "See server logs for details."
/// Detail is included in Development because dev-container logs are local and exposing the
/// message makes iteration faster. This matches the rule that prohibits <c>ex.Message</c>
/// from appearing in service-layer <c>Result.Failure</c> calls — see
/// <c>Result.cs</c> XML docs on <see cref="Models.Result.Infrastructure(string, string)"/>.
/// </para>
///
/// <para>
/// Correlation-ID propagation is handled by <c>CorrelationIdMiddleware</c>
/// (<c>Features/Logging/Middleware/CorrelationIdMiddleware.cs</c>) and the
/// <c>CustomizeProblemDetails</c> callback registered in <c>Program.cs</c>: every
/// ProblemDetails response — including the one this handler produces — picks up the
/// correlation ID as a top-level <c>correlationId</c> extension. Support can reference
/// that value to tie a user-reported error to the matching server log line.
/// </para>
///
/// <para>
/// Registered via <c>builder.Services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;()</c>
/// and activated by <c>app.UseExceptionHandler()</c> in the middleware pipeline. The
/// <c>UseExceptionHandler()</c> call must sit AFTER <c>UseMiddleware&lt;CorrelationIdMiddleware&gt;()</c>
/// (so the correlation-ID header is set when this handler writes the response) and BEFORE
/// <c>UseSerilogRequestLogging()</c> (so the 500 is logged as a clean request completion
/// rather than an aborted pipeline).
/// </para>
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment env,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _env = env;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Full stack + message server-side only. Same rule as the Result.Infrastructure()
        // guidance: ex.Message never hits the wire in non-Dev builds because it can reveal
        // schema names, SQL fragments, etc.
        _logger.LogError(
            exception,
            "Unhandled exception during {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Title = "An unexpected error occurred.",
            Detail = _env.IsDevelopment()
                ? $"{exception.GetType().Name}: {exception.Message}"
                : "See server logs for details.",
            Instance = httpContext.Request.Path
        };

        // Delegate writing to the registered IProblemDetailsService so that the
        // CustomizeProblemDetails callback (correlation ID, traceId) runs against this
        // response the same way it runs against every other ProblemDetails in the system.
        // Returning true tells the ExceptionHandlerMiddleware that we've handled it; false
        // would let the default HTML error page run.
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
