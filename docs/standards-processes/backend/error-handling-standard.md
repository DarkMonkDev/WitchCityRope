# Error Handling Standard

**Last updated:** 2026-04-21
**Status:** ACTIVE — enforced by `EndpointErrorShapeTests` at build time
**Scope:** Every .NET API endpoint in this repository

This document defines how errors flow from a service through an endpoint to the HTTP client. It is intended to be copied verbatim into the Dark Monk ecosystem's other repositories (WitchCityRope, monkmail, etc.) so every DM service speaks the same wire format.

---

## Contract at a glance

| Layer | Type | Responsibility |
|-------|------|---------------|
| Service | `Result<T>` / `Result` with a `ResultErrorKind` | Classify the failure. Never throw for expected failures. Never put `ex.Message` in the user-facing `Error` string. |
| Endpoint | `result.ToProblem(title)` extension | Single statement produces an RFC 7807 `ProblemDetails` response. Do NOT call `Results.Problem`/`BadRequest`/`NotFound`/`Conflict` directly. |
| Middleware | `GlobalExceptionHandler` + `AddProblemDetails(...)` | Catches anything unhandled, attaches `correlationId`/`traceId` to every PD response. |
| Client (React) | `ApiError` / `ValidationError` classes parse ProblemDetails | Toast or per-field form error. |

**Wire format is always RFC 7807 `application/problem+json`**. No flat `{error: "..."}` envelopes, no ad-hoc shapes.

---

## Why RFC 7807 ProblemDetails (not a flat `{error}` envelope)

| Dimension | RFC 7807 (our choice) | Flat `{error}` (rejected) |
|-----------|----------------------|--------------------------|
| Standard | IETF RFC, widely adopted | Ad-hoc |
| Framework support | First-class: `Results.Problem()`, `Results.ValidationProblem()`, `AddProblemDetails()`, `IProblemDetailsService` all built into ASP.NET Core | Fights the framework; hand-rolled |
| OpenAPI / tooling | Swagger-codegen and openapi-typescript emit types natively | Requires custom schemas |
| Validation errors | Built-in `errors` dict drives per-field UI | Forces mixing two formats — worst outcome |
| Extensibility | RFC allows custom members (correlationId, traceId, retry-after, errorCode) | Ad-hoc |
| Content-type | `application/problem+json` is discoverable | Indistinguishable from success |

No tradeoff — RFC 7807 is both closer to the ASP.NET Core grain AND the industry-standard choice.

---

## The service layer: `Result<T>` with `ResultErrorKind`

Every service method that can fail returns `Result<T>` (or non-generic `Result` when no value is produced). The failure is classified via `ResultErrorKind`:

| `ResultErrorKind` | HTTP status | Factory method | When to use |
|-------------------|------------|----------------|-------------|
| `BusinessRule` | **400** | `Result.Failure(msg)` — the default | Domain-rule violation ("cannot update a completed PO") |
| `Validation` | **400** | `Result.Validation(msg)` | Semantic validation beyond FluentValidation's reach |
| `NotFound` | **404** | `Result.NotFound(msg)` | Referenced resource doesn't exist |
| `Conflict` | **409** | `Result.Conflict(msg)` | State conflict, duplicate key, optimistic concurrency clash |
| `Unauthorized` | **401** | `Result.Unauthorized(msg)` | Auth required / failed |
| `Forbidden` | **403** | `Result.Forbidden(msg)` | Authenticated, lacks permission |
| `Infrastructure` | **500** | `Result.Infrastructure(msg)` | DB, disk, OUR code |
| `Upstream` | **502** | `Result.Upstream(msg)` | External service failed (third-party API, cloud storage) |
| `None` | — | Only valid on `Success` | Never appears on a failed result |

### Rules for service code

**DO:**
```csharp
public async Task<Result<VendorResponse>> GetByIdAsync(Guid id, CancellationToken ct)
{
    var vendor = await _context.Vendors.FindAsync([id], ct);
    if (vendor is null)
        return Result<VendorResponse>.NotFound("Vendor not found");  // → 404

    if (vendor.IsArchived)
        return Result<VendorResponse>.Failure("Vendor is archived"); // → 400 (BusinessRule)

    return Result<VendorResponse>.Success(Map(vendor));
}
```

**DO NOT** use `Result.Failure` when a more specific kind applies — that's how `Integration` endpoints drifted into returning 400 for NotFound cases before we cleaned them up.

**DO NOT** put `ex.Message` in the user-facing `Error`:
```csharp
// WRONG — leaks DB schema, SQL fragments, etc. to the client
return Result<T>.Infrastructure($"Save failed: {ex.Message}");

// RIGHT — static string on the wire; full exception goes to the log
_logger.LogError(ex, "Save failed for item {ItemId}", id);
return Result<T>.Infrastructure("Failed to save changes. See server logs.");
```

Rationale: this rule is enforced repo-wide. An earlier sweep removed 117 `ex.Message` leaks. A new leak is a review blocker.

---

## The endpoint layer: `result.ToProblem(title)`

The ONLY sanctioned path from a failed `Result` to an `IResult` HTTP response is the `ToProblem(title, instance?)` extension method defined in `Features/Shared/Extensions/ResultExtensions.cs`.

### Canonical endpoint shape

```csharp
public static async Task<IResult> GetById(
    Guid id,
    IVendorService svc,
    CancellationToken ct)
{
    var result = await svc.GetByIdAsync(id, ct);

    if (!result.IsSuccess)
        return result.ToProblem("Vendor Not Found");

    return Results.Ok(result.Value);
}
```

### Why `ToProblem` exists as a single helper

Before this standard, every endpoint open-coded the `ResultErrorKind` → status mapping:
```csharp
// OLD — 113 sites of drift-prone ternaries
var statusCode = result.ErrorKind == ResultErrorKind.NotFound ? 404 : 400;
return Results.Problem(title: "X", detail: result.Error, statusCode: statusCode);
```

Problems this caused:
1. **Drift.** Integration endpoints dropped the ternary and hardcoded `statusCode: 400`, so NotFound failures returned 400 to the e-commerce caller. Nobody caught it until an audit.
2. **No place to add cross-cutting behavior.** Correlation IDs, retry-after headers, problem-type URIs all would have had to touch 113+ sites.
3. **Reviewer burden.** Each site had to be read to confirm the ternary was right. Reviewers miss things.

`ToProblem(title)` centralizes all three concerns. The mapping lives in one switch statement.

### Forbidden calls from endpoints

The arch test `EndpointErrorShapeTests.Endpoint_handlers_do_not_construct_error_responses_directly` fails the build if any file under `Features/*/Endpoints/` contains:

- `Results.Problem(`
- `Results.BadRequest(`
- `Results.NotFound(`
- `Results.Conflict(`
- `Results.UnprocessableEntity(`

without an `// ARCH-ALLOW: <reason>` comment on the same line.

### Allowed exits from an endpoint

- **Success paths:** `Results.Ok(...)`, `Results.Created(...)`, `Results.NoContent()`, `Results.Accepted(...)`, `Results.File(...)`, `Results.Stream(...)`.
- **Failed Result:** `result.ToProblem("Title")`.
- **FluentValidation failures:** `Results.ValidationProblem(validationResult.ToDictionary())` — produces the `errors` dict that the frontend's `ValidationError` class maps to per-field form errors.
- **Inline per-field guards** (missing query param, missing body field): `Results.ValidationProblem(new Dictionary<string, string[]> { ["fieldName"] = ["msg"] })`. Same shape as the FluentValidation path; still whitelisted by the arch test.
- **Genuine edge cases:** `// ARCH-ALLOW: <concrete reason>` on the forbidden-call line. Reviewers should push back unless the reason is concrete.

### Inline guard example

```csharp
// OLD — forbidden
if (!vendorId.HasValue || vendorId.Value == Guid.Empty)
    return Results.Problem(
        title: "vendorId required",
        detail: "vendorId query parameter is required.",
        statusCode: 400);

// NEW — uses ValidationProblem with a per-field dict (whitelisted)
if (!vendorId.HasValue || vendorId.Value == Guid.Empty)
    return Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["vendorId"] = ["vendorId query parameter is required."]
    });
```

Benefit: the frontend's existing `ValidationError` handling routes this straight to the form field. No special case.

---

## Global behavior wired in `Program.cs`

Two registrations make the standard work across every request, including unhandled exceptions.

### `AddProblemDetails` with correlation-ID extension

```csharp
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var correlationId =
            context.HttpContext.Response.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? context.HttpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(correlationId))
            context.ProblemDetails.Extensions["correlationId"] = correlationId;

        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
```

Effect: every `ProblemDetails` response — whether from `ToProblem`, `ValidationProblem`, or the global exception handler — carries a `correlationId` that ties back to the server log line.

### `AddExceptionHandler` + `UseExceptionHandler`

```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// ...
app.UseMiddleware<CorrelationIdMiddleware>(); // correlation ID set BEFORE the handler reads it
app.UseExceptionHandler();
```

`GlobalExceptionHandler` implements `IExceptionHandler`:
- Logs the full exception server-side via `_logger.LogError(ex, ...)`.
- Returns a generic 500 ProblemDetails. In Development, `detail` includes `"ExceptionType: message"` for fast iteration. In Staging/Production, `detail` is `"See server logs for details."` — the `ex.Message`-leakage rule holds for unhandled exceptions too.
- Delegates writing to the shared `IProblemDetailsService` so the `CustomizeProblemDetails` callback runs (correlationId attached).

Without this handler, an unhandled exception produced either the default ASP.NET HTML error page (Dev) or a plain empty 500 (Prod) — neither is parseable by the frontend's `ApiError` class.

### Pipeline order

The order in `Program.cs` matters:

```csharp
app.UseForwardedHeaders(...);
app.UseCors(...);
app.UseRateLimiter();
app.UseMiddleware<CorrelationIdMiddleware>();  // sets X-Correlation-Id on response
app.UseExceptionHandler();                      // ← reads correlation header when writing PD
app.UseSerilogRequestPipeline();                // logs the final 500 as a normal completion
app.UseAuthentication();
app.UseAuthorization();
// ...
```

Placing `UseExceptionHandler` AFTER `CorrelationIdMiddleware` guarantees the handler's ProblemDetails response carries a correlation ID. Placing it BEFORE Serilog's request pipeline means Serilog logs the final 500 as a clean pipeline completion rather than an aborted request.

---

## Frontend consumption

The React client at `src/.../web/src/lib/api/client.ts` already parses RFC 7807 correctly:

```typescript
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>; // populated only by ValidationProblem
}

export class ApiError extends Error {
  constructor(public status: number, public statusText: string, public problem?: ProblemDetails) {
    super(problem?.detail ?? problem?.title ?? `${status} ${statusText}`);
  }
}

export class ValidationError extends ApiError {
  public fieldErrors: Record<string, string[]>;
  // … extracts problem.errors for per-field Mantine form binding
}
```

- `detail` → toast message via the global mutation `onError` handler.
- `errors` dict → per-field form errors when present.
- `correlationId` extension → available on `problem.correlationId` (TypeScript allows it via index access). Future UX polish: surface this in the toast so users can read it to support.

No frontend change was required when the backend standard landed — the client was already RFC 7807-compliant.

---

## Enforcement

### Build-time architectural test

`tests/DarkMonk.InvPW.Tests/Features/Shared/EndpointErrorShapeTests.cs` enumerates every `.cs` file under `src/.../Features/*/Endpoints/` and grep-scans for forbidden patterns. The test fails the build with a file:line list of violations and a recovery hint.

The test uses `[CallerFilePath]` to resolve the repo root at compile time, so it works regardless of the test runner's working directory.

**When it fails:** either replace the forbidden call with `result.ToProblem(title)` (preferred) or add `// ARCH-ALLOW: <concrete reason>` to the line. If the reason is speculative, don't add ARCH-ALLOW — fix the code instead.

### Code review checklist

The `code-reviewer` agent's 13-point checklist (section 12, "API Endpoints") now explicitly requires:

- Error responses go through `result.ToProblem(title)`, not direct `Results.Problem/BadRequest/NotFound/Conflict`.
- Services use the RIGHT `ResultErrorKind` for the failure type. Common regression: `Result.Failure` for a not-found case → 400 instead of 404.
- New `// ARCH-ALLOW:` comments require concrete justification.
- Inline per-field guards use `Results.ValidationProblem(dict)`, not `Results.Problem`.

---

## Known allowed exceptions (documented `ARCH-ALLOW` sites)

| Site | Count | Reason |
|------|-------|--------|
| `AdminBackupEndpoints.cs` | 15 | Backup feature doesn't use `Result<T>` — every site is a `try/catch` around a Hangfire or Spaces call. Refactoring the backup service to return `Result<T>` is a separate pass. |
| `ItemEndpoints.RefreshUsedMonthly` | 1 | Explicit 500 with a custom "Refresh Failed" title for a long-running admin action. Would show "An unexpected error occurred." via the global handler, which is less useful to the admin. |
| `SalesHistoryImportEndpoints` flag-off branch | 1 | `Results.NotFound()` (no body) mimics "route doesn't exist" when the feature flag is off — prevents probing for feature state via this endpoint. No Result to convert. |

Every other forbidden-call site in endpoint code is a review-blocking violation.

---

## Migration / follow-up work

See `docs/technical-debt.md` TD-029 for the full resolved-entry. Deferred items:

1. **AdminBackup `Result<T>` refactor.** Collapse the 15 ARCH-ALLOW sites by restructuring `BackupOrchestrationService` and friends to return `Result<T>` instead of throwing. Larger change; re-evaluate when the backup feature gets substantial work.
2. **GlobalExceptionHandler integration test.** No test exercises the handler end-to-end. Would need a `TestServer` with a fake throwing endpoint. The handler is textbook .NET 10 code and the unit suite proves nothing else regressed, but a "throw, assert ProblemDetails body" test would close the last coverage gap.
3. **Frontend correlationId in toast.** The `correlationId` extension lands on every ProblemDetails but isn't shown to users today. Adding it to the error toast ("Error XYZ — ref a1b2c3") would make support tickets actionable.

---

## Reference implementation (this repo)

| Concern | File |
|---------|------|
| `Result` / `ResultErrorKind` | `src/DarkMonk.InvPW.Api/Features/Shared/Models/Result.cs` |
| `ToProblem` extension + status mapping | `src/DarkMonk.InvPW.Api/Features/Shared/Extensions/ResultExtensions.cs` |
| Global exception handler | `src/DarkMonk.InvPW.Api/Features/Shared/Logging/GlobalExceptionHandler.cs` |
| `AddProblemDetails` + `UseExceptionHandler` wiring | `src/DarkMonk.InvPW.Api/Program.cs` |
| Architectural test | `tests/DarkMonk.InvPW.Tests/Features/Shared/EndpointErrorShapeTests.cs` |
| CLAUDE.md quick-reference | `CLAUDE.md` — "Error responses" subsection under "Backend API Conventions" |
| Code-reviewer checklist | `.claude/agents/code-reviewer.md` §12 |

---

## Changelog

- **2026-04-21** — Initial version. Commit `4b53518` landed the standard end-to-end (foundation + sweep of 146 sites + enforcement + docs). See TD-029 in `docs/technical-debt.md` for the full audit, rationale, and follow-ups.
