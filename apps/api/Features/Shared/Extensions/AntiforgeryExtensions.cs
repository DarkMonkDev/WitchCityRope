using Microsoft.AspNetCore.Antiforgery;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Shared.Extensions;

/// <summary>
/// Extension methods that wrap <see cref="IAntiforgery"/> validation in a
/// <see cref="Result"/> so endpoint handlers can route CSRF failures through the same
/// <see cref="ResultExtensions.ToProblem(Result, string, string?)"/> path as every other
/// failure — see <c>docs/standards-processes/backend/error-handling-standard.md</c>.
///
/// Before this helper, every state-changing endpoint repeated the same 4-line try/catch:
/// <code>
///   try { await antiforgery.ValidateRequestAsync(context); }
///   catch (AntiforgeryValidationException) {
///       return Results.Problem(title: "CSRF Validation Failed", detail: "...", statusCode: 400);
///   }
/// </code>
/// 30+ identical copies across 15 files. Extracting to one helper kills the duplication
/// (DRY) and gives us a single place to evolve CSRF error shape (e.g. switch to 403, add
/// retry-after, change the user-facing message).
///
/// Status mapping: a CSRF failure produces <see cref="Result.Failure(string, string)"/>,
/// which defaults to <see cref="ResultErrorKind.BusinessRule"/> → HTTP 400. This preserves
/// the legacy status (every prior site hardcoded <c>statusCode: 400</c>); changing to 403
/// would be more semantically accurate but is a wire-breaking change for any existing
/// frontend code that branches on 400 specifically.
/// </summary>
public static class AntiforgeryExtensions
{
    /// <summary>
    /// Validate the antiforgery token on the current request. Returns
    /// <see cref="Result.Success()"/> if valid, <see cref="Result.Failure(string, string)"/>
    /// (→ HTTP 400 via <see cref="ResultExtensions.ToProblem(Result, string, string?)"/>) if not.
    /// </summary>
    /// <example>
    /// <code>
    ///   var csrf = await antiforgery.ValidateAsync(context);
    ///   if (!csrf.IsSuccess)
    ///       return csrf.ToProblem("CSRF Validation Failed");
    /// </code>
    /// </example>
    public static async Task<Result> ValidateAsync(this IAntiforgery antiforgery, HttpContext context)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return Result.Success();
        }
        catch (AntiforgeryValidationException)
        {
            // Same wire message every prior call site emitted. Centralizing here means a future
            // change to "Your session has expired, please sign in again" lands in one place.
            return Result.Failure(
                "Antiforgery token validation failed. Please refresh the page and try again.");
        }
    }
}
