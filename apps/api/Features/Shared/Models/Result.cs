namespace WitchCityRope.Api.Features.Shared.Models;

/// <summary>
/// Classifies a failed <see cref="Result"/> / <see cref="Result{T}"/> so that endpoint
/// handlers can map it to a correct HTTP status code via
/// <see cref="WitchCityRope.Api.Features.Shared.Extensions.ResultExtensions.ToProblem{T}(Result{T}, string, string?)"/>.
///
/// Values and their canonical HTTP mappings:
///   None            — success (never appears on a failed result).
///   BusinessRule    — 400. Caller-addressable domain violation (e.g. "cannot update a past event").
///   Validation      — 400. Request-shape / input validation failure. Semantically distinct from
///                     BusinessRule but shares the 400 status for client convenience.
///   NotFound        — 404. Referenced resource does not exist.
///   Conflict        — 409. State conflict (e.g. duplicate key, optimistic concurrency clash).
///   Unauthorized    — 401. Authentication required / failed.
///   Forbidden       — 403. Authenticated but lacks permission.
///   Infrastructure  — 500. Out-of-band failure (DB, disk, OUR code). Never leak the inner
///                     <c>ex.Message</c> in user-facing <see cref="Result.Error"/> — log the
///                     exception via <c>_logger.LogError(ex, ...)</c> and pass a static
///                     user-facing string here.
///   Upstream        — 502. External-service failure (PayPal, Authorize.Net, SendGrid, DigitalOcean
///                     Spaces, etc.). Distinguished from Infrastructure so the frontend can show
///                     a "third-party is down" message rather than "our system failed".
///
/// NEVER call <c>Results.Problem()</c> / <c>Results.BadRequest()</c> / <c>Results.NotFound()</c>
/// directly from an endpoint — go through <c>result.ToProblem(title)</c> so this mapping stays in
/// one place. An architectural test enforces this at build time.
/// </summary>
public enum ResultErrorKind
{
    None,
    BusinessRule,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Infrastructure,
    Upstream
}

/// <summary>
/// Simple result pattern for consistent error handling across all services.
/// Services return <see cref="Result{T}"/> / <see cref="Result"/>; endpoint handlers check
/// <see cref="IsSuccess"/> and call <c>result.ToProblem(title)</c> on failure to produce a
/// uniform RFC 7807 ProblemDetails response.
///
/// Backward compatibility: existing callers that use <see cref="Failure(string, string)"/>
/// get <see cref="ResultErrorKind.BusinessRule"/> (→ HTTP 400), preserving the behavior they
/// had before <see cref="ErrorKind"/> existed. Adopt the kind-specific factories
/// (<see cref="NotFound"/>, <see cref="Conflict"/>, <see cref="Upstream"/>, etc.) when
/// touching code so the HTTP status emitted downstream is correct.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string Error { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public ResultErrorKind ErrorKind { get; private set; } = ResultErrorKind.None;

    /// <summary>
    /// Backwards compatibility alias for Error property
    /// </summary>
    public string ErrorMessage => Error;

    private Result(bool isSuccess, T? value, string error, string details, ResultErrorKind errorKind)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Details = details;
        ErrorKind = errorKind;
    }

    /// <summary>Create successful result with value.</summary>
    public static Result<T> Success(T value)
        => new(true, value, string.Empty, string.Empty, ResultErrorKind.None);

    /// <summary>
    /// Create failed result with error message. Defaults to <see cref="ResultErrorKind.BusinessRule"/>
    /// (→ HTTP 400). Use one of the kind-specific factories below when the failure is not a plain
    /// domain-rule violation — e.g. <see cref="NotFound"/> when a lookup returns no rows so the
    /// endpoint emits 404 instead of 400.
    /// </summary>
    public static Result<T> Failure(string error, string details = "")
        => new(false, default, error, details, ResultErrorKind.BusinessRule);

    /// <summary>Request-shape / input validation failure (→ HTTP 400).</summary>
    public static Result<T> Validation(string error, string details = "")
        => new(false, default, error, details, ResultErrorKind.Validation);

    /// <summary>Referenced resource does not exist (→ HTTP 404).</summary>
    public static Result<T> NotFound(string error, string details = "")
        => new(false, default, error, details, ResultErrorKind.NotFound);

    /// <summary>State conflict — duplicate key, optimistic concurrency, etc. (→ HTTP 409).</summary>
    public static Result<T> Conflict(string error, string details = "")
        => new(false, default, error, details, ResultErrorKind.Conflict);

    /// <summary>Authentication required or failed (→ HTTP 401).</summary>
    public static Result<T> Unauthorized(string error, string details = "")
        => new(false, default, error, details, ResultErrorKind.Unauthorized);

    /// <summary>Authenticated but lacks required permission (→ HTTP 403).</summary>
    public static Result<T> Forbidden(string error, string details = "")
        => new(false, default, error, details, ResultErrorKind.Forbidden);

    /// <summary>
    /// Out-of-band / infrastructure failure — DB, disk, OUR code (→ HTTP 500).
    /// NEVER pass raw <c>ex.Message</c> as <paramref name="error"/>: it leaks implementation
    /// detail (SQL fragments, schema names, stack details) to end users. Log the exception at
    /// the catch site via <c>_logger.LogError(ex, ...)</c> and pass a static user-facing string
    /// here. For failures in EXTERNAL services (PayPal, SendGrid, DigitalOcean Spaces, etc.)
    /// use <see cref="Upstream"/>.
    /// </summary>
    public static Result<T> Infrastructure(string error, string details = "")
        => new(false, default, error, details, ResultErrorKind.Infrastructure);

    /// <summary>
    /// External-service failure — upstream API, cloud storage, etc. (→ HTTP 502 Bad Gateway).
    /// Prefer this over <see cref="Infrastructure"/> when the failure is clearly a third-party
    /// issue so the client can distinguish "they're down" from "we broke". Same ex.Message
    /// leakage rule as <see cref="Infrastructure"/>.
    /// </summary>
    public static Result<T> Upstream(string error, string details = "")
        => new(false, default, error, details, ResultErrorKind.Upstream);

    /// <summary>Implicit conversion to bool for easy success checking.</summary>
    public static implicit operator bool(Result<T> result) => result.IsSuccess;
}

/// <summary>
/// Non-generic result for operations that don't return data.
/// See <see cref="Result{T}"/> for the design rationale.
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public string Error { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public ResultErrorKind ErrorKind { get; private set; } = ResultErrorKind.None;

    /// <summary>
    /// Backwards compatibility alias for Error property
    /// </summary>
    public string ErrorMessage => Error;

    private Result(bool isSuccess, string error, string details, ResultErrorKind errorKind)
    {
        IsSuccess = isSuccess;
        Error = error;
        Details = details;
        ErrorKind = errorKind;
    }

    /// <summary>Create successful result.</summary>
    public static Result Success()
        => new(true, string.Empty, string.Empty, ResultErrorKind.None);

    /// <summary>Defaults to <see cref="ResultErrorKind.BusinessRule"/> (→ HTTP 400).</summary>
    public static Result Failure(string error, string details = "")
        => new(false, error, details, ResultErrorKind.BusinessRule);

    /// <summary>Request-shape / input validation failure (→ HTTP 400).</summary>
    public static Result Validation(string error, string details = "")
        => new(false, error, details, ResultErrorKind.Validation);

    /// <summary>Referenced resource does not exist (→ HTTP 404).</summary>
    public static Result NotFound(string error, string details = "")
        => new(false, error, details, ResultErrorKind.NotFound);

    /// <summary>State conflict — duplicate key, optimistic concurrency, etc. (→ HTTP 409).</summary>
    public static Result Conflict(string error, string details = "")
        => new(false, error, details, ResultErrorKind.Conflict);

    /// <summary>Authentication required or failed (→ HTTP 401).</summary>
    public static Result Unauthorized(string error, string details = "")
        => new(false, error, details, ResultErrorKind.Unauthorized);

    /// <summary>Authenticated but lacks required permission (→ HTTP 403).</summary>
    public static Result Forbidden(string error, string details = "")
        => new(false, error, details, ResultErrorKind.Forbidden);

    /// <summary>See <see cref="Result{T}.Infrastructure"/> for the ex.Message leakage warning.</summary>
    public static Result Infrastructure(string error, string details = "")
        => new(false, error, details, ResultErrorKind.Infrastructure);

    /// <summary>See <see cref="Result{T}.Upstream"/> — external-service failure → HTTP 502.</summary>
    public static Result Upstream(string error, string details = "")
        => new(false, error, details, ResultErrorKind.Upstream);

    /// <summary>Implicit conversion to bool for easy success checking.</summary>
    public static implicit operator bool(Result result) => result.IsSuccess;
}
