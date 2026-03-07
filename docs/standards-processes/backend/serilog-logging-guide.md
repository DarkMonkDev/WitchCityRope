# Serilog Logging Guide

**Purpose**: Project-specific logging patterns for WitchCityRope's Serilog infrastructure.
**When to Read**: Any feature that adds or modifies logging, new API endpoints, debugging log output.
**Related**: [Error Handling Patterns](./error-handling-patterns.md), [Service Layer Patterns](./service-layer-patterns.md)

## How Serilog Works in This Project

Serilog is a **drop-in replacement** for `Microsoft.Extensions.Logging`. Existing code using `ILogger<T>` works without changes. The key difference is what happens to log output: in staging/production, logs are persisted to PostgreSQL in addition to console.

**You do NOT need to:**
- Change how you inject `ILogger<T>`
- Change how you call `_logger.LogInformation(...)`, `_logger.LogWarning(...)`, etc.
- Add any Serilog-specific `using` statements in service or endpoint files

**Configuration lives in**: `Program.cs` (sink setup) + `appsettings.json` / `appsettings.Development.json` (levels and enrichers).

## Message Templates (Most Important Rule)

Use **named placeholders**, not string interpolation:

```csharp
// WRONG - defeats structured logging, properties become one opaque string
_logger.LogInformation($"User {userId} registered for event {eventId}");

// CORRECT - UserId and EventId are stored as queryable structured properties
_logger.LogInformation("User {UserId} registered for event {EventId}", userId, eventId);
```

Named placeholders flow into the `properties` JSONB column in PostgreSQL and are queryable with JSONB operators.

## Auto-Enriched Properties

The following properties are **automatically added** to every log entry by middleware. Do NOT add them manually in your log statements:

| Property | Source | Type | DB Column |
|----------|--------|------|-----------|
| `CorrelationId` | `CorrelationIdMiddleware` | Guid (UUID) | `correlation_id` |
| `UserId` | `UserContextMiddleware` (authenticated only) | Guid (UUID) | `user_id` |
| `UserEmail` | `UserContextMiddleware` (authenticated only) | string | `properties` JSONB |
| `UserRole` | `UserContextMiddleware` (authenticated only) | string | `properties` JSONB |
| `RequestPath` | Serilog request logging | string | `request_path` |
| `MachineName` | Serilog enricher | string | `machine_name` |
| `EnvironmentName` | Serilog enricher | string | `properties` JSONB |
| `ThreadId` | Serilog enricher | int | `properties` JSONB |

**CorrelationId** is generated per HTTP request. All log entries within the same request share the same ID. External callers can pass `X-Correlation-Id` header to chain requests.

## LogContext: Adding Custom Properties

Use `LogContext.PushProperty` when you need to add context that spans multiple log entries within a scope:

```csharp
using Serilog.Context;

using (LogContext.PushProperty("PaymentProvider", "AuthorizeNet"))
using (LogContext.PushProperty("TransactionId", transactionId))
{
    _logger.LogInformation("Processing payment for {Amount}", amount);
    // ... more code with additional log statements ...
    // Both PaymentProvider and TransactionId appear in all log entries within this scope
}
```

### UUID Column Rule

If you push a property that maps to a UUID column (`user_id` or `correlation_id`), you **must push a `Guid` value**, not a string:

```csharp
// WRONG - causes type mismatch at the PostgreSQL sink
LogContext.PushProperty("UserId", userId.ToString());

// CORRECT - push Guid for UUID columns
if (Guid.TryParse(userId, out var userGuid))
    LogContext.PushProperty("UserId", userGuid);
```

This only applies to middleware or custom code pushing to LogContext. Normal endpoint handler log statements using message templates are unaffected.

## Sensitive Data Masking

The following property names are **automatically masked** in all log output (console and database):

- `password`, `token`, `secret`, `key`
- `authorization`, `cookie`, `nonce`, `creditcard`

Matching is case-insensitive. If a log property name contains any of these words, the value is replaced with `***MASKED***`.

**Do not** rely on masking as a substitute for not logging sensitive data. Avoid including passwords, API keys, or PII in log messages when possible.

## Log Levels by Environment

| Level | Development | Staging/Production |
|-------|-------------|-------------------|
| Default | Debug | Information |
| Microsoft.AspNetCore | Warning | Warning |
| Microsoft.EntityFrameworkCore | Information | Error |
| Hangfire | Information | Warning |
| System.Net.Http.HttpClient | Warning | Warning |

**Guidance for your code:**
- `LogDebug` — Detailed diagnostic info. Only visible in Development console. Use for SQL queries, cache hits/misses, intermediate calculation results.
- `LogInformation` — Important business events: user registered, payment processed, event created. These persist to PostgreSQL.
- `LogWarning` — Unexpected but recoverable: validation failure, third-party API retry, capacity reached.
- `LogError` — Operation failed: exception caught, external service down, database constraint violation.
- `LogCritical` — System is unusable: startup failure, data corruption, unrecoverable state.

## PostgreSQL Sink

**Active in**: Staging and Production only. Development uses console-only logging.

| Setting | Value |
|---------|-------|
| Schema | `logging` |
| Table | `application_logs` |
| Batch size | 50 events |
| Flush interval | 5 seconds |
| COPY protocol | Disabled (`useCopy: false` — batch INSERTs are more reliable) |
| Auto-create table | Disabled (migration creates it) |

### Column Layout

| Column | Type | Content |
|--------|------|---------|
| `id` | BIGINT | Auto-increment PK |
| `timestamp` | TIMESTAMPTZ | When event occurred |
| `level` | SMALLINT | Numeric level (0-5) |
| `level_name` | VARCHAR(16) | "Debug", "Information", "Warning", "Error", "Fatal" |
| `message` | TEXT | Rendered message |
| `message_template` | TEXT | Original template for grouping similar messages |
| `exception` | TEXT | Full exception if present |
| `source_context` | VARCHAR(512) | Logger name (e.g., `WitchCityRope.Api.Features.Payments.Services.AuthorizeNetService`) |
| `properties` | JSONB | All structured properties not in dedicated columns |
| `user_id` | UUID | Extracted UserId for indexed lookups |
| `correlation_id` | UUID | Request correlation for tracing |
| `request_path` | VARCHAR(2048) | HTTP request path |
| `machine_name` | VARCHAR(256) | Container/host name |

### Retention

- **Raw logs**: 90 days (cleaned by `LogRetentionCleanupJob` at 3 AM UTC daily)
- **Daily summaries**: Indefinite (aggregated by `DailyLogSummaryJob` at 1 AM UTC daily)

## Frontend Error Capture

React errors are captured by the frontend error reporting service and sent to:

**`POST /api/client-errors`**
- `AllowAnonymous` — works for unauthenticated pages
- Rate limited: 20 requests/minute per IP
- Max payload: 100KB, max 10 errors per batch
- Returns `202 Accepted`

Frontend errors are logged with `SourceContext = "Frontend"`. To query them:
```sql
SELECT * FROM logging.application_logs
WHERE source_context = 'Frontend'
ORDER BY timestamp DESC;
```

## Admin Log Endpoints

All require Administrator role:

- `GET /api/admin/logs` — Query raw logs with filters (date range, level, source, user ID, correlation ID, text search). Paginated, max 90 days back.
- `GET /api/admin/logs/summaries` — Query daily summary aggregates.
- `GET /api/admin/logs/summaries/cc-failures` — Credit card failure breakdown by day and reason.

## Deployment Gotchas

Three issues discovered during staging deployment:

### 1. COPY Protocol Reliability
`useCopy: true` uses PostgreSQL's streaming COPY protocol, which can fail silently in certain pooling configurations. **Always use `useCopy: false`** in this project — batch INSERTs are reliable and performant enough for our log volume.

### 2. UUID Column Type Mismatch
`SinglePropertyColumnWriter` with `NpgsqlDbType.Uuid` requires the LogContext value to be a `Guid` CLR type. Pushing a string causes `InvalidCastException` and the entire batch fails. See "UUID Column Rule" above.

### 3. Table Must Pre-Exist via Migration
`needAutoCreateTable: false` is required because the `logging.application_logs` table is created by EF Core migration (`AddLoggingInfrastructure`). Setting this to `true` creates a differently-structured table.

## Key Files

| File | Purpose |
|------|---------|
| `apps/api/Program.cs` (lines 28-103) | Serilog bootstrap, column writers, sink config |
| `apps/api/appsettings.json` | Serilog level configuration |
| `apps/api/Features/Logging/Middleware/CorrelationIdMiddleware.cs` | Request correlation |
| `apps/api/Features/Logging/Middleware/UserContextMiddleware.cs` | User context enrichment |
| `apps/api/Features/Logging/Endpoints/ClientErrorEndpoints.cs` | Frontend error ingestion |
| `apps/api/Features/Logging/Endpoints/AdminLogEndpoints.cs` | Admin log queries |
| `apps/api/Features/Logging/Jobs/DailyLogSummaryJob.cs` | Daily aggregation |
| `apps/api/Features/Logging/Jobs/LogRetentionCleanupJob.cs` | 90-day retention cleanup |

---
**Created**: 2026-03-02
**Status**: Active
