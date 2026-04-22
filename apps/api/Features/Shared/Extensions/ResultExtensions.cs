using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Shared.Extensions;

/// <summary>
/// Endpoint-layer glue that turns a failed <see cref="Result"/> / <see cref="Result{T}"/>
/// into a uniform RFC 7807 ProblemDetails HTTP response. This is the ONLY sanctioned way
/// for an endpoint handler to emit an error response from a service Result — the
/// architectural test <c>EndpointErrorShapeTests</c> fails the build if any endpoint
/// calls <c>Results.Problem()</c>, <c>Results.BadRequest()</c>, <c>Results.NotFound()</c>,
/// or <c>Results.Conflict()</c> directly.
///
/// Why a dedicated helper:
///   1. Keeps the <see cref="ResultErrorKind"/> → HTTP status mapping in one place. Prior to
///      this helper the mapping was open-coded at ~480 endpoint call sites with drift between
///      them — e.g. EventEndpoints string-matched the error text ("not found" → 404, "date" →
///      400) to pick a status, so rephrasing an error message in the service silently
///      changed the HTTP status.
///   2. Makes it mechanically obvious which endpoint is returning what. Reviewers scan for
///      <c>ToProblem(</c> in the diff rather than reasoning through ternaries.
///   3. Gives us a single place to add new cross-cutting behavior (correlation ID, retry-after
///      headers, problem type URIs) without touching every endpoint.
///
/// Call shape from an endpoint handler:
/// <code>
///   var result = await service.DoSomethingAsync(...);
///   if (!result.IsSuccess)
///       return result.ToProblem("Failed to Do Something");
///   return Results.Ok(result.Value);
/// </code>
///
/// <para>
/// FluentValidation failures are intentionally NOT routed through this helper — use
/// <c>Results.ValidationProblem(validationResult.ToDictionary())</c> directly because it
/// populates the <c>errors</c> dict in the RFC 7807 response. The frontend's ApiError /
/// ProblemDetails handling (see <c>apps/web/src/lib/api/client.ts</c> and <c>utils/errors.ts</c>)
/// already parses that dict for per-field form error display when present.
/// </para>
///
/// <para>
/// Inline per-field validation guards (missing query param, missing body field) should use
/// <c>Results.ValidationProblem(new Dictionary&lt;string, string[]&gt; { ["fieldName"] = ["msg"] })</c>
/// — same shape as FluentValidation, whitelisted by the arch test, routes to per-field error
/// display automatically when the frontend is extended to read the errors dict.
/// </para>
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Produce an <see cref="IResult"/> ProblemDetails response from a failed generic
    /// <see cref="Result{T}"/>. Throws if called on a successful result — an endpoint that
    /// reaches this method has already checked <c>!result.IsSuccess</c>.
    /// </summary>
    public static IResult ToProblem<T>(this Result<T> result, string title, string? instance = null)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException(
                "ToProblem called on a successful Result<T>. Check IsSuccess before calling.");
        return BuildProblem(result.ErrorKind, result.Error, title, instance);
    }

    /// <summary>
    /// Produce an <see cref="IResult"/> ProblemDetails response from a failed non-generic
    /// <see cref="Result"/>. Throws if called on a successful result.
    /// </summary>
    public static IResult ToProblem(this Result result, string title, string? instance = null)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException(
                "ToProblem called on a successful Result. Check IsSuccess before calling.");
        return BuildProblem(result.ErrorKind, result.Error, title, instance);
    }

    /// <summary>
    /// Central ErrorKind → HTTP status mapping. Any new <see cref="ResultErrorKind"/>
    /// value needs a branch here (the switch is explicit, not a fallthrough, so adding a
    /// value without updating this switch will not silently map to a wrong status code).
    /// </summary>
    private static IResult BuildProblem(ResultErrorKind kind, string detail, string title, string? instance)
    {
        var statusCode = kind switch
        {
            ResultErrorKind.BusinessRule => StatusCodes.Status400BadRequest,
            ResultErrorKind.Validation => StatusCodes.Status400BadRequest,
            ResultErrorKind.NotFound => StatusCodes.Status404NotFound,
            ResultErrorKind.Conflict => StatusCodes.Status409Conflict,
            ResultErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ResultErrorKind.Infrastructure => StatusCodes.Status500InternalServerError,
            ResultErrorKind.Upstream => StatusCodes.Status502BadGateway,
            // None on a failed result is a bug in the caller — default to 500 so it surfaces.
            ResultErrorKind.None => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
        return Results.Problem(title: title, detail: detail, statusCode: statusCode, instance: instance);
    }
}
