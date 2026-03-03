# Technology Research: Serilog Implementation for WitchCityRope .NET 10 API
<!-- Last Updated: 2026-03-02 -->
<!-- Version: 1.0 -->
<!-- Owner: Technology Researcher Agent -->
<!-- Status: Complete -->

## Executive Summary

**Decision Required**: How to implement structured logging with Serilog in the WitchCityRope .NET 10 Minimal API, including PostgreSQL persistence, frontend error capture, and production-ready configuration.

**Recommendation**: Implement Serilog with the `Serilog.AspNetCore` v10.0 package using two-stage initialization, `Serilog.Sinks.Postgresql.Alternative` v4.2.0 for PostgreSQL persistence (same database, separate schema), and custom middleware for user context enrichment.

**Confidence Level**: High (90%)

**Key Factors**:
1. Serilog is a drop-in replacement for `ILogger<T>` -- zero code changes to existing logging statements
2. `Serilog.Sinks.Postgresql.Alternative` v4.2.0 explicitly supports .NET 10 and uses efficient COPY-based batch inserts
3. The platform's moderate log volume (~600 members) makes same-database logging viable with proper batching

---

## 1. Serilog Setup for .NET 10

### Required NuGet Packages

**Core packages (mandatory)**:
```xml
<PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
<PackageReference Include="Serilog.Sinks.Postgresql.Alternative" Version="4.2.0" />
```

**Enricher packages (recommended)**:
```xml
<PackageReference Include="Serilog.Enrichers.Environment" Version="3.0.1" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="4.0.0" />
<PackageReference Include="Serilog.Enrichers.Sensitive" Version="2.1.0" />
```

**Package details**:
| Package | Version | .NET 10 Support | Downloads | Last Updated |
|---------|---------|---------------|-----------|--------------|
| Serilog.AspNetCore | 10.0.0 | Yes (explicit) | 638M total | 2025-11-28 |
| Serilog.Sinks.Postgresql.Alternative | 4.2.0 | Yes (explicit net10.0 target) | 1.9M total | 2025-03-24 |
| Serilog.Enrichers.Environment | 3.0.1 | Yes (.NET Standard 2.0) | Stable | Stable |
| Serilog.Enrichers.Thread | 4.0.0 | Yes (.NET Standard 2.0) | Stable | Stable |
| Serilog.Enrichers.Sensitive | 2.1.0 | Yes (.NET Standard 2.0) | Growing | Stable |

**What `Serilog.AspNetCore` 10.0.0 includes** (no separate install needed):
- Serilog (>= 4.3.0)
- Serilog.Extensions.Hosting (>= 10.0.0)
- Serilog.Formatting.Compact (>= 3.0.0)
- Serilog.Settings.Configuration (>= 10.0.0)
- Serilog.Sinks.Console (>= 6.1.1)
- Serilog.Sinks.Debug (>= 3.0.0)
- Serilog.Sinks.File (>= 7.0.0)

### Recommended Bootstrap Pattern: Two-Stage Initialization

The current recommended pattern for .NET 10 Minimal APIs uses `AddSerilog()` with two-stage initialization. This approach:
- Captures startup/configuration errors before the host is built
- Replaces the bootstrap logger with the fully-configured logger once configuration is loaded
- Supports DI-injected enrichers and sinks

```csharp
using Serilog;
using Serilog.Events;

// Stage 1: Bootstrap logger (captures startup errors)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting WitchCityRope API");

    var builder = WebApplication.CreateBuilder(args);

    // Stage 2: Full configuration from appsettings.json + services
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "WitchCityRope.Api")
        .WriteTo.Console()
        .WriteTo.PostgreSQL(
            connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
            tableName: "logs",
            schemaName: "logging",
            needAutoCreateTable: true,
            needAutoCreateSchema: true,
            useCopy: true,
            batchSizeLimit: 50,
            period: TimeSpan.FromSeconds(5)));

    // ... register services ...

    var app = builder.Build();

    // Request logging middleware (BEFORE route handlers)
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = EnrichFromRequest;
        options.GetLevel = ExcludeHealthChecks;
    });

    // ... map endpoints ...

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
```

### UseSerilog vs AddSerilog -- Current Recommendation

**`AddSerilog()` is the current recommended approach** (since Serilog.AspNetCore 8.x+):
- `builder.Services.AddSerilog()` -- redirects all `ILogger<T>` through Serilog
- Supports DI injection of services into enrichers and sinks via `ReadFrom.Services(services)`
- Replaces the older `builder.Host.UseSerilog()` pattern

**`UseSerilog()` (older pattern)** -- still works but less preferred:
- `builder.Host.UseSerilog()` -- configures at the host level
- Does not support `ReadFrom.Services()` for DI

### .NET 10 Changes

There are no breaking changes specific to .NET 10 for Serilog integration. The key consideration is version matching:
- Use Serilog.AspNetCore **10.0.0** which explicitly targets .NET 10
- The package also supports .NET 8, .NET 10, and .NET Standard 2.0/2.1

---

## 2. Serilog PostgreSQL Sink

### Sink Selection: Serilog.Sinks.Postgresql.Alternative

**Recommendation**: Use `Serilog.Sinks.Postgresql.Alternative` v4.2.0

**Why not the original `Serilog.Sinks.PostgreSQL`?**
The original package by b00ted is **no longer actively maintained**. The Alternative fork is actively maintained under `serilog-contrib` and is the community-recommended option.

| Criteria | Serilog.Sinks.PostgreSQL (original) | Serilog.Sinks.Postgresql.Alternative |
|----------|--------------------------------------|--------------------------------------|
| Maintained | No (abandoned) | Yes (active, last release 2025-03-24) |
| .NET 10 Support | No explicit support | Yes (explicit net10.0 target) |
| Version | 2.3.0 (stale) | 4.2.0 (current) |
| Npgsql Version | Old | >= 9.0.3 |
| COPY Support | Yes | Yes |
| Auto-create Table | Yes | Yes |
| Retention Config | No | Yes |

### Table Schema and Column Writers

The sink provides configurable column writers. Here is the **recommended schema for WitchCityRope**:

```csharp
var columnWriters = new Dictionary<string, ColumnWriterBase>
{
    { "id", new IdAutoIncrementColumnWriter() },
    { "timestamp", new TimestampColumnWriter(NpgsqlDbType.TimestampTz) },
    { "level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
    { "message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
    { "message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
    { "exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
    { "properties", new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
    { "log_event", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) }
};
```

**Available Column Writers**:
| Column Writer | Description | Recommended NpgsqlDbType |
|---------------|-------------|--------------------------|
| `IdAutoIncrementColumnWriter` | Auto-incrementing BigInt ID | BigInt |
| `TimestampColumnWriter` | Event timestamp with timezone | TimestampTz |
| `LevelColumnWriter` | Log level (text or integer) | Varchar (text=true) |
| `RenderedMessageColumnWriter` | Formatted message string | Text |
| `MessageTemplateColumnWriter` | Original template with placeholders | Text |
| `ExceptionColumnWriter` | Exception details | Text |
| `PropertiesColumnWriter` | Custom properties only | Jsonb |
| `LogEventSerializedColumnWriter` | Full serialized event (includes metadata) | Jsonb |
| `SinglePropertyColumnWriter` | Extract one specific property | Text/Varchar |

**Key difference**: `PropertiesColumnWriter` contains only custom properties (enrichers, LogContext). `LogEventSerializedColumnWriter` contains the full event including metadata. For querying, **use both**: `properties` for quick property queries and `log_event` for complete event reconstruction.

### Recommended Table Schema (SQL)

```sql
CREATE SCHEMA IF NOT EXISTS logging;

CREATE TABLE logging.logs (
    id BIGSERIAL PRIMARY KEY,
    timestamp TIMESTAMPTZ NOT NULL,
    level VARCHAR(20) NOT NULL,
    message TEXT NOT NULL,
    message_template TEXT,
    exception TEXT,
    properties JSONB,
    log_event JSONB
);

-- Performance indexes
CREATE INDEX idx_logs_timestamp ON logging.logs (timestamp DESC);
CREATE INDEX idx_logs_level ON logging.logs (level);
CREATE INDEX idx_logs_timestamp_level ON logging.logs (timestamp DESC, level);
CREATE INDEX idx_logs_properties_gin ON logging.logs USING gin (properties jsonb_path_ops);

-- For source context queries
CREATE INDEX idx_logs_source_context ON logging.logs
    ((properties->>'SourceContext'));

-- For user-specific log queries
CREATE INDEX idx_logs_user_id ON logging.logs
    ((properties->>'UserId'));

-- For correlation ID tracking
CREATE INDEX idx_logs_correlation_id ON logging.logs
    ((properties->>'CorrelationId'));
```

### Performance: Same Database vs Separate Database

**For WitchCityRope, same database is recommended** with the following rationale:

**Factors favoring same database**:
- ~600 members with moderate log volume (not a high-traffic SaaS platform)
- Simplified infrastructure (no additional database container)
- Easier querying for admin reporting (JOIN with application tables)
- Reduced operational complexity for volunteer-maintained platform

**Mitigations for same-database approach**:
1. **Separate schema** (`logging.logs` not `public.logs`) -- isolates log tables
2. **Batch inserts with COPY** -- `useCopy: true` uses PostgreSQL COPY command instead of individual INSERTs (14,000 rows/sec vs individual inserts)
3. **Configurable batching** -- buffer logs and write in batches
4. **Retention policy** -- auto-delete old logs to prevent table bloat
5. **Separate connection** -- use a dedicated connection string with lower pool size

**When to consider a separate database**:
- If log volume exceeds ~100,000 entries/day
- If log queries cause noticeable application slowdown
- If the database approaches storage limits

### Batching and Buffering Configuration

```csharp
.WriteTo.PostgreSQL(
    connectionString: connectionString,
    tableName: "logs",
    schemaName: "logging",
    columnOptions: columnWriters,
    needAutoCreateTable: true,
    needAutoCreateSchema: true,
    useCopy: true,              // Use COPY command for batch inserts (much faster)
    batchSizeLimit: 50,          // Max events per batch (default: 30)
    period: TimeSpan.FromSeconds(5), // Flush interval (default: 5 seconds)
    queueLimit: 10000            // Max queued events in memory (prevents OOM)
)
```

**Recommended settings for WitchCityRope**:
| Parameter | Recommended Value | Rationale |
|-----------|-------------------|-----------|
| `useCopy` | `true` | 3x faster than INSERT for batch operations |
| `batchSizeLimit` | 50 | Good balance for moderate volume |
| `period` | 5 seconds | Acceptable delay for non-real-time logs |
| `queueLimit` | 10,000 | Safety valve for memory protection |

### Retention and Cleanup Strategy

**Option 1: Built-in retention** (via sink parameter):
```csharp
retentionTime: TimeSpan.FromDays(90)  // Auto-delete entries older than 90 days
```

**Option 2: Scheduled PostgreSQL cleanup** (more reliable):
```sql
-- Run daily via pg_cron or application scheduled task
DELETE FROM logging.logs WHERE timestamp < NOW() - INTERVAL '90 days';

-- After bulk delete, reclaim space
VACUUM ANALYZE logging.logs;
```

**Option 3: Table partitioning** (for larger installations):
```sql
-- Partition by month for easy archival
CREATE TABLE logging.logs (
    -- columns as above
) PARTITION BY RANGE (timestamp);

CREATE TABLE logging.logs_2026_03 PARTITION OF logging.logs
    FOR VALUES FROM ('2026-03-01') TO ('2026-04-01');
```

**Recommended approach for WitchCityRope**: Option 1 (built-in) with Option 2 as backup. The 90-day retention period balances storage with investigation needs.

---

## 3. Structured Logging Best Practices

### Recommended Enrichers

```csharp
// In Program.cs Serilog configuration:
.Enrich.FromLogContext()           // REQUIRED: enables LogContext.PushProperty
.Enrich.WithMachineName()          // Container/host identification
.Enrich.WithEnvironmentName()      // Development/Staging/Production
.Enrich.WithThreadId()             // Thread identification for debugging
.Enrich.WithProperty("Application", "WitchCityRope.Api")  // Multi-app identification
.Enrich.WithSensitiveDataMasking(options =>  // PII protection
{
    options.MaskProperties.Add("password");
    options.MaskProperties.Add("token");
    options.MaskProperties.Add("creditCard");
    options.MaskProperties.Add("ssn");
    options.MaskValue = "***REDACTED***";
})
```

### User Context Enrichment Middleware

Create custom middleware to automatically add user identity to all log entries:

```csharp
// Middleware/SerilogUserContextMiddleware.cs
public class SerilogUserContextMiddleware
{
    private readonly RequestDelegate _next;

    public SerilogUserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract user claims from the authenticated user
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
            var role = context.User.FindFirst(ClaimTypes.Role)?.Value;

            // Push into LogContext -- available for ALL subsequent log calls
            using (LogContext.PushProperty("UserId", userId ?? "unknown"))
            using (LogContext.PushProperty("UserEmail", email ?? "unknown"))
            using (LogContext.PushProperty("UserRole", role ?? "anonymous"))
            {
                await _next(context);
            }
        }
        else
        {
            using (LogContext.PushProperty("UserId", "anonymous"))
            using (LogContext.PushProperty("UserRole", "guest"))
            {
                await _next(context);
            }
        }
    }
}

// Registration in Program.cs (AFTER authentication middleware):
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SerilogUserContextMiddleware>();
```

### Correlation ID Middleware

```csharp
// Middleware/CorrelationIdMiddleware.cs
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
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                           ?? Guid.NewGuid().ToString();

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        {
            await _next(context);
        }
    }
}

// Registration in Program.cs (BEFORE request logging):
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
```

### PII/Sensitive Data Handling

**Data that MUST be redacted in logs**:
- Passwords and authentication tokens
- Email addresses (use hashed or partial: `u***@example.com`)
- Full names (use initials or member IDs)
- Credit card numbers
- Session tokens
- httpOnly cookie values

**Implementation with Serilog.Enrichers.Sensitive**:
```csharp
.Enrich.WithSensitiveDataMasking(options =>
{
    // Mask specific properties by name (case-insensitive)
    options.MaskProperties.Add("password");
    options.MaskProperties.Add("token");
    options.MaskProperties.Add("secret");
    options.MaskProperties.Add("authorization");
    options.MaskProperties.Add("cookie");

    // Built-in operators for pattern-based masking
    // Automatically masks email addresses and IBANs

    // Custom mask text
    options.MaskValue = "***REDACTED***";

    // Exclude properties that look sensitive but are safe to log
    options.ExcludeProperties.Add("eventName");
    options.ExcludeProperties.Add("tokenType");
})
```

**WitchCityRope-specific consideration**: Given the platform's safety-critical nature, err on the side of over-redaction. Member identities in logs should use internal IDs, never email addresses or names in the message text. Use structured properties for identity data so the sensitive enricher can mask them.

### Log Level Guidelines

| Level | When to Use | WitchCityRope Examples |
|-------|-------------|------------------------|
| **Fatal** | Application cannot continue | Database connection permanently lost, critical config missing |
| **Error** | Operation failed, needs attention | Payment processing failure, auth service down, consent workflow broken |
| **Warning** | Unexpected but non-critical | Rate limiting triggered, deprecated endpoint called, slow query detected |
| **Information** | Significant business events | User registered, event created, session started, consent granted |
| **Debug** | Internal flow details | Service method entry/exit, cache hit/miss, query parameters |
| **Verbose** | Extremely detailed tracing | Raw HTTP request/response bodies, EF SQL generated |

**Default minimum levels**:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore.Hosting": "Warning",
        "Microsoft.AspNetCore.Mvc": "Warning",
        "Microsoft.AspNetCore.Routing": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Message Template Best Practices

```csharp
// CORRECT: Structured message templates with named properties
Log.Information("User {UserId} registered for event {EventId} session {SessionId}",
    userId, eventId, sessionId);

// CORRECT: Use @ for destructuring complex objects into structured data
Log.Information("Event created: {@EventDetails}", new { eventId, eventName, capacity });

// WRONG: String interpolation -- loses structure, prevents querying
Log.Information($"User {userId} registered for event {eventId}");  // DO NOT DO THIS

// WRONG: String concatenation -- same problem
Log.Information("User " + userId + " registered");  // DO NOT DO THIS

// CORRECT: Exceptions as first parameter
Log.Error(exception, "Failed to process registration for {UserId}", userId);

// Performance guard for expensive Debug/Verbose logging
if (Log.IsEnabled(LogEventLevel.Debug))
{
    Log.Debug("Detailed state: {@State}", expensiveStateCalculation());
}
```

---

## 4. ASP.NET Core Request Logging Middleware

### How UseSerilogRequestLogging Works

The `UseSerilogRequestLogging()` middleware replaces the default verbose ASP.NET Core request logging (which generates ~10 log events per request) with a **single, condensed log event** per request containing:
- HTTP method and path
- Status code
- Response time (elapsed milliseconds)
- Any enriched properties

### Configuration

```csharp
app.UseSerilogRequestLogging(options =>
{
    // Customize the message template
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    // Enrich with additional request data
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("RemoteIpAddress",
            httpContext.Connection.RemoteIpAddress?.ToString());

        // Add response content type and length
        if (httpContext.Response.HasStarted)
        {
            diagnosticContext.Set("ContentType", httpContext.Response.ContentType);
            diagnosticContext.Set("ContentLength", httpContext.Response.ContentLength);
        }
    };

    // Control log level based on status code and endpoint
    options.GetLevel = (httpContext, elapsed, exception) =>
    {
        // Errors always logged at Error level
        if (exception != null || httpContext.Response.StatusCode >= 500)
            return LogEventLevel.Error;

        // Client errors at Warning
        if (httpContext.Response.StatusCode >= 400)
            return LogEventLevel.Warning;

        // Health checks at Verbose (effectively suppressed)
        if (IsHealthCheckEndpoint(httpContext))
            return LogEventLevel.Verbose;

        // Slow requests at Warning
        if (elapsed > 1000) // > 1 second
            return LogEventLevel.Warning;

        return LogEventLevel.Information;
    };
});
```

### Excluding Health Check Endpoints

```csharp
private static bool IsHealthCheckEndpoint(HttpContext ctx)
{
    var endpoint = ctx.GetEndpoint();
    if (endpoint != null)
    {
        // Check by endpoint display name
        if (string.Equals(endpoint.DisplayName, "Health checks",
            StringComparison.Ordinal))
            return true;

        // Or check by path
        var path = ctx.Request.Path.Value;
        if (path != null && (path.StartsWith("/health") || path.StartsWith("/healthz")))
            return true;
    }
    return false;
}
```

### Replacing Default ASP.NET Core Logging

To prevent duplicate/verbose logging, suppress default ASP.NET Core log sources:

**In appsettings.json**:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "Microsoft.AspNetCore.Hosting": "Warning",
        "Microsoft.AspNetCore.Mvc": "Warning",
        "Microsoft.AspNetCore.Routing": "Warning",
        "Microsoft.AspNetCore.StaticFiles": "Warning"
      }
    }
  }
}
```

**Also remove** the `"Logging"` section from `appsettings.json` (it's for Microsoft's default logger, not Serilog).

### Middleware Order (Critical)

```csharp
var app = builder.Build();

// 1. Correlation ID (first, so all logs have it)
app.UseMiddleware<CorrelationIdMiddleware>();

// 2. Serilog request logging (captures timing for everything after it)
app.UseSerilogRequestLogging(/* options */);

// 3. Exception handling
app.UseExceptionHandler("/error");

// 4. Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

// 5. User context enrichment (after auth, so user is available)
app.UseMiddleware<SerilogUserContextMiddleware>();

// 6. Route handlers
app.MapEndpoints();
```

---

## 5. Log Querying and Reporting

### JSONB Column Strategy

**Use both `properties` (JSONB) and dedicated columns**:
- `timestamp`, `level`, `message`, `exception` -- dedicated columns for fast filtering
- `properties` -- JSONB column for structured property queries
- `log_event` -- JSONB column for complete event reconstruction (optional, increases storage)

### Querying Structured Logs in PostgreSQL

```sql
-- Find all errors in the last 24 hours
SELECT timestamp, level, message, exception,
       properties->>'UserId' as user_id,
       properties->>'CorrelationId' as correlation_id
FROM logging.logs
WHERE level = 'Error'
  AND timestamp > NOW() - INTERVAL '24 hours'
ORDER BY timestamp DESC;

-- Find all logs for a specific user
SELECT timestamp, level, message
FROM logging.logs
WHERE properties->>'UserId' = '123e4567-e89b-12d3-a456-426614174000'
ORDER BY timestamp DESC
LIMIT 100;

-- Find all logs for a specific correlation ID (trace a full request)
SELECT timestamp, level, message, properties->>'SourceContext' as source
FROM logging.logs
WHERE properties->>'CorrelationId' = 'abc-123-def-456'
ORDER BY timestamp ASC;

-- Aggregate error counts by source context
SELECT properties->>'SourceContext' as source,
       COUNT(*) as error_count
FROM logging.logs
WHERE level = 'Error'
  AND timestamp > NOW() - INTERVAL '7 days'
GROUP BY properties->>'SourceContext'
ORDER BY error_count DESC;

-- Find slow requests (using Serilog request logging)
SELECT timestamp, message,
       properties->>'Elapsed' as elapsed_ms,
       properties->>'RequestPath' as path,
       properties->>'StatusCode' as status_code
FROM logging.logs
WHERE properties->>'SourceContext' = 'Serilog.AspNetCore.RequestLoggingMiddleware'
  AND (properties->>'Elapsed')::float > 1000
ORDER BY (properties->>'Elapsed')::float DESC;

-- Log volume by level per day (for monitoring)
SELECT DATE(timestamp) as log_date, level, COUNT(*) as count
FROM logging.logs
WHERE timestamp > NOW() - INTERVAL '30 days'
GROUP BY DATE(timestamp), level
ORDER BY log_date DESC, level;
```

### Admin Reporting Endpoints

Build API endpoints that expose log data to the admin dashboard:

```csharp
// Features/Logging/Endpoints/LogEndpoints.cs
public static class LogEndpoints
{
    public static void MapLogEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin/logs")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/recent", GetRecentLogs);
        group.MapGet("/errors", GetRecentErrors);
        group.MapGet("/stats", GetLogStats);
        group.MapGet("/search", SearchLogs);
    }
}
```

### UI Tools for Serilog PostgreSQL Data

**Recommended**: Build a custom admin panel (since WitchCityRope already has admin UI).

**External alternatives** (if custom UI is too much effort):
- **pgAdmin** -- Direct SQL querying of the log table
- **Grafana** -- Connect to PostgreSQL as data source, create dashboards
- **Seq** -- Self-hosted structured log viewer (free for dev, paid for production). Can be used alongside PostgreSQL by adding a second Serilog sink.

---

## 6. Frontend Error Capture

### Recommended Pattern: React Error Boundary + Global Handlers + API Endpoint

**Architecture**:
```
React Frontend                              .NET API Backend
+---------------------------+              +---------------------------+
| Error Boundary (render)   |              | POST /api/client-logs     |
| window.onerror (sync)     | --HTTP POST->| Validate + sanitize       |
| unhandledrejection (async)|              | Log via Serilog           |
| Manual error logging      |              | Store in PostgreSQL       |
+---------------------------+              +---------------------------+
```

### Frontend Implementation

```typescript
// services/errorLogging.ts
interface ClientLogEntry {
  level: 'error' | 'warning' | 'info';
  message: string;
  source: string;
  stackTrace?: string;
  componentStack?: string;
  url: string;
  userAgent: string;
  timestamp: string;
  additionalData?: Record<string, unknown>;
}

const LOG_ENDPOINT = '/api/client-logs';

async function sendLogToServer(entry: ClientLogEntry): Promise<void> {
  try {
    await fetch(LOG_ENDPOINT, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include', // Include auth cookies
      body: JSON.stringify(entry),
    });
  } catch {
    // Fallback: log to console if server unreachable
    console.error('Failed to send log to server:', entry);
  }
}

// Global error handler (synchronous errors)
window.onerror = (message, source, lineno, colno, error) => {
  sendLogToServer({
    level: 'error',
    message: String(message),
    source: `${source}:${lineno}:${colno}`,
    stackTrace: error?.stack,
    url: window.location.href,
    userAgent: navigator.userAgent,
    timestamp: new Date().toISOString(),
  });
  return false; // Allow default browser handling
};

// Global promise rejection handler
window.addEventListener('unhandledrejection', (event) => {
  sendLogToServer({
    level: 'error',
    message: `Unhandled Promise Rejection: ${event.reason}`,
    source: 'unhandledrejection',
    stackTrace: event.reason?.stack,
    url: window.location.href,
    userAgent: navigator.userAgent,
    timestamp: new Date().toISOString(),
  });
});
```

### React Error Boundary Integration

```tsx
// components/ErrorBoundary.tsx
import { Component, type ErrorInfo, type ReactNode } from 'react';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
}

class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Send to backend via the logging service
    sendLogToServer({
      level: 'error',
      message: error.message,
      source: 'ReactErrorBoundary',
      stackTrace: error.stack,
      componentStack: errorInfo.componentStack ?? undefined,
      url: window.location.href,
      userAgent: navigator.userAgent,
      timestamp: new Date().toISOString(),
    });
  }

  render() {
    if (this.state.hasError) {
      return this.props.fallback ?? <div>Something went wrong.</div>;
    }
    return this.props.children;
  }
}
```

### Backend Endpoint for Client Logs

```csharp
// Features/ClientLogging/Endpoints/ClientLogEndpoints.cs
public static class ClientLogEndpoints
{
    public static void MapClientLogEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/client-logs", HandleClientLog)
            .AllowAnonymous()  // Allow unauthenticated error reporting
            .RequireRateLimiting("client-logs");  // Prevent abuse
    }

    private static IResult HandleClientLog(
        ClientLogRequest request,
        ILogger<ClientLogEndpoints> logger,
        HttpContext context)
    {
        // Sanitize input to prevent log injection
        var sanitizedMessage = SanitizeLogMessage(request.Message);
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        switch (request.Level?.ToLower())
        {
            case "error":
                logger.LogError(
                    "Client Error: {Message} | Source: {Source} | URL: {Url} | User: {UserId}",
                    sanitizedMessage, request.Source, request.Url, userId);
                break;
            case "warning":
                logger.LogWarning(
                    "Client Warning: {Message} | Source: {Source} | URL: {Url}",
                    sanitizedMessage, request.Source, request.Url);
                break;
            default:
                logger.LogInformation(
                    "Client Info: {Message} | Source: {Source}",
                    sanitizedMessage, request.Source);
                break;
        }

        return Results.Ok();
    }

    private static string SanitizeLogMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return "(empty)";
        // Prevent log injection by removing newlines and control characters
        return message
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ")
            [..Math.Min(message.Length, 2000)];  // Limit length
    }
}

public record ClientLogRequest(
    string? Level,
    string? Message,
    string? Source,
    string? StackTrace,
    string? ComponentStack,
    string? Url,
    string? UserAgent,
    string? Timestamp);
```

**Rate limiting configuration** (prevent abuse of the logging endpoint):
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("client-logs", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 30;  // Max 30 logs per minute per client
        opt.QueueLimit = 10;
    });
});
```

---

## 7. Docker/Container Considerations

### Console Logging Configuration

**Always keep console logging in Docker containers** -- Docker's log driver (json-file, fluentd, etc.) collects stdout/stderr. This provides a secondary log stream independent of the database.

```csharp
// Development: Console at Debug level
// Production: Console at Warning level only (blocking call, affects performance)
.WriteTo.Console(
    restrictedToMinimumLevel: builder.Environment.IsProduction()
        ? LogEventLevel.Warning
        : LogEventLevel.Debug,
    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
                    "{Properties:j}{NewLine}{Exception}")
```

**Why restrict console in production**: `Console.Write` is a **blocking synchronous call** in .NET. Under high load, console logging can significantly slow down the application. In production, use PostgreSQL as the primary sink and console only for errors.

### Docker-Specific Enrichers

```csharp
.Enrich.WithMachineName()           // Container ID in Docker
.Enrich.WithEnvironmentName()       // ASPNETCORE_ENVIRONMENT
.Enrich.WithProperty("Application", "WitchCityRope.Api")
.Enrich.WithProperty("ContainerName",
    Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown")
```

### Environment-Specific Configuration

**appsettings.Development.json**:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    },
    "WriteTo": [
      { "Name": "Console" }
    ]
  }
}
```

**appsettings.Production.json** (or Staging):
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "restrictedToMinimumLevel": "Warning"
        }
      },
      {
        "Name": "PostgreSQL",
        "Args": {
          "connectionString": "DefaultConnection",
          "tableName": "logs",
          "schemaName": "logging",
          "needAutoCreateTable": true,
          "needAutoCreateSchema": true
        }
      }
    ]
  }
}
```

### Docker Compose Logging Driver

```yaml
# docker-compose.yml
services:
  api:
    # ... existing config ...
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

---

## 8. Migration from ILogger

### Drop-In Replacement: Yes

**Serilog works as a drop-in replacement for `ILogger<T>`**. When you call `builder.Services.AddSerilog()`, it registers Serilog as the provider behind Microsoft's `ILoggerFactory`. All existing `ILogger<T>` injections continue to work identically.

```csharp
// This existing code works WITHOUT any changes:
public class EventService
{
    private readonly ILogger<EventService> _logger;

    public EventService(ILogger<EventService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<Event>> CreateEvent(CreateEventRequest request)
    {
        _logger.LogInformation("Creating event {EventName}", request.Name);
        // ^ This now goes through Serilog's pipeline automatically
    }
}
```

### What Changes Are Needed

**Changes required**:
1. Add Serilog NuGet packages
2. Configure Serilog in `Program.cs` (see Section 1)
3. Add middleware for request logging, correlation IDs, user context
4. Remove the `"Logging"` section from `appsettings.json` (replace with `"Serilog"` section)

**Changes NOT required**:
- No changes to any service or controller code
- No changes to `ILogger<T>` injection patterns
- No changes to log call syntax (`LogInformation`, `LogError`, etc.)
- No changes to message templates or parameters

### Gotchas and Migration Notes

**1. SourceContext property**:
Serilog captures the `ILogger<T>` category as a `SourceContext` property, but it is NOT included in the default console output template. Add `{SourceContext}` to your output template if you want to see it:
```csharp
outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
```

**2. String interpolation silently degrades**:
Existing code using `$"string {interpolation}"` works but loses structured properties. These should be refactored to use message templates when discovered, but it is not a blocking migration issue.

```csharp
// Works but loses structure (refactor when touched):
_logger.LogInformation($"Event {eventId} created");

// Correct structured logging:
_logger.LogInformation("Event {EventId} created", eventId);
```

**3. BeginScope behavior**:
If existing code uses `_logger.BeginScope(...)`, the scope values become Serilog properties. String-based scopes add a `Scope` property; dictionary-based scopes add individual properties. Both work but dictionary-based is preferred for structured logging.

**4. Log level filtering**:
After migration, log levels are controlled by the `"Serilog"` configuration section, NOT the `"Logging"` section. Remove the old `"Logging"` section to avoid confusion.

**5. Async/await considerations**:
`Log.CloseAndFlush()` should be called with `await Log.CloseAndFlushAsync()` to ensure all buffered events (especially PostgreSQL batches) are written before application shutdown.

---

## Complete appsettings.json Configuration Reference

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.Postgresql.Alternative",
      "Serilog.Enrichers.Environment",
      "Serilog.Enrichers.Thread",
      "Serilog.Enrichers.Sensitive"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore.Hosting": "Warning",
        "Microsoft.AspNetCore.Mvc": "Warning",
        "Microsoft.AspNetCore.Routing": "Warning",
        "Microsoft.AspNetCore.StaticFiles": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "System": "Warning"
      }
    },
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithEnvironmentName",
      "WithThreadId",
      {
        "Name": "WithProperty",
        "Args": { "name": "Application", "value": "WitchCityRope.Api" }
      },
      {
        "Name": "WithSensitiveDataMasking",
        "Args": {
          "options": {
            "MaskValue": "***REDACTED***",
            "MaskProperties": ["password", "token", "secret", "authorization", "cookie"]
          }
        }
      }
    ],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "PostgreSQL",
        "Args": {
          "connectionString": "DefaultConnection",
          "tableName": "logs",
          "schemaName": "logging",
          "needAutoCreateTable": true,
          "needAutoCreateSchema": true,
          "batchSizeLimit": 50,
          "period": "00:00:05",
          "queueLimit": 10000
        }
      }
    ]
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5434;Database=witchcityrope;Username=postgres;Password=..."
  }
}
```

---

## Complete Program.cs Reference

```csharp
using Serilog;
using Serilog.Events;

// Stage 1: Bootstrap logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting WitchCityRope API");

    var builder = WebApplication.CreateBuilder(args);

    // Stage 2: Full Serilog configuration
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // ... existing service registrations ...
    builder.Services.AddAuthentication(/* ... */);
    builder.Services.AddAuthorization(/* ... */);
    builder.Services.AddRateLimiter(/* ... */);

    var app = builder.Build();

    // Middleware pipeline (ORDER MATTERS)
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent",
                httpContext.Request.Headers.UserAgent.ToString());
        };

        options.GetLevel = (httpContext, elapsed, exception) =>
        {
            if (exception != null || httpContext.Response.StatusCode >= 500)
                return LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 400)
                return LogEventLevel.Warning;

            var endpoint = httpContext.GetEndpoint();
            if (endpoint?.DisplayName == "Health checks"
                || httpContext.Request.Path.StartsWithSegments("/health"))
                return LogEventLevel.Verbose;

            if (elapsed > 1000)
                return LogEventLevel.Warning;

            return LogEventLevel.Information;
        };
    });

    app.UseExceptionHandler("/error");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<SerilogUserContextMiddleware>();
    app.UseRateLimiter();

    // Map endpoints
    app.MapHealthEndpoints();
    app.MapAuthEndpoints();
    app.MapEventEndpoints();
    app.MapClientLogEndpoints();  // Frontend error capture
    app.MapLogEndpoints();        // Admin log viewing

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
```

---

## Risk Assessment

### High Risk
- **Log table growth without retention**: Without retention policy, the log table will grow unbounded and eventually impact database performance.
  - **Mitigation**: Configure `retentionTime: TimeSpan.FromDays(90)` and implement scheduled cleanup job.

### Medium Risk
- **PII in logs**: Developers might accidentally log sensitive user data in message templates.
  - **Mitigation**: Use `Serilog.Enrichers.Sensitive` with `MaskProperties` configuration. Code review guidelines to check for PII in log messages.

- **Console sink performance in production**: Blocking console writes under load.
  - **Mitigation**: Restrict console to Warning+ in production.

### Low Risk
- **PostgreSQL sink connection failure**: If the database is down, log events queue in memory.
  - **Monitoring**: The `queueLimit` parameter prevents OOM. Events exceeding the queue are silently dropped. Monitor queue depth if possible.

- **Migration disruption**: Changing from default logger to Serilog.
  - **Monitoring**: Since Serilog is a drop-in replacement, this is very low risk. Test in development first.

---

## Implementation Roadmap

### Phase 1: Core Setup (Immediate)
1. Add NuGet packages to API project
2. Configure two-stage initialization in Program.cs
3. Add appsettings.json Serilog configuration
4. Remove old `"Logging"` configuration section
5. Verify existing `ILogger<T>` calls work correctly

### Phase 2: PostgreSQL Sink (Next)
1. Add PostgreSQL sink configuration
2. Create log table schema (or let auto-create handle it)
3. Add recommended indexes
4. Configure retention policy
5. Test batching behavior

### Phase 3: Middleware and Enrichment
1. Add CorrelationIdMiddleware
2. Add SerilogUserContextMiddleware
3. Configure UseSerilogRequestLogging with health check exclusion
4. Add PII masking with Serilog.Enrichers.Sensitive
5. Verify enriched properties appear in PostgreSQL

### Phase 4: Frontend Error Capture
1. Create client-logs API endpoint with rate limiting
2. Implement React error boundary with server reporting
3. Add global window.onerror and unhandledrejection handlers
4. Test error capture flow end-to-end

### Phase 5: Admin Reporting
1. Create admin log viewing endpoints
2. Build admin UI for log browsing/searching
3. Add log statistics dashboard
4. Configure alerts for error spikes

---

## Research Sources

- [Serilog.AspNetCore GitHub Repository](https://github.com/serilog/serilog-aspnetcore)
- [Serilog.Sinks.Postgresql.Alternative GitHub](https://github.com/serilog-contrib/Serilog.Sinks.Postgresql.Alternative)
- [Serilog.Sinks.Postgresql.Alternative HowToUse](https://github.com/serilog-contrib/Serilog.Sinks.Postgresql.Alternative/blob/master/HowToUse.md)
- [Serilog.AspNetCore NuGet (v10.0.0)](https://www.nuget.org/packages/serilog.aspnetcore)
- [Serilog.Sinks.Postgresql.Alternative NuGet (v4.2.0)](https://www.nuget.org/packages/Serilog.Sinks.Postgresql.Alternative)
- [Serilog.Enrichers.Sensitive GitHub](https://github.com/serilog-contrib/Serilog.Enrichers.Sensitive)
- [Milan Jovanovic: 5 Serilog Best Practices](https://www.milanjovanovic.tech/blog/5-serilog-best-practices-for-better-structured-logging)
- [Nicholas Blumhardt: Serilog and .NET 8.0 Minimal APIs](https://nblumhardt.com/2024/04/serilog-net8-0-minimal/)
- [Andrew Lock: Excluding Health Checks from Serilog](https://andrewlock.net/using-serilog-aspnetcore-in-asp-net-core-3-excluding-health-check-endpoints-from-serilog-request-logging/)
- [Ben Foster: Serilog Best Practices](https://benfoster.io/blog/serilog-best-practices/)
- [PostSharp: Serilog Log Levels](https://blog.postsharp.net/serilog-log-levels)
- [Serilog Enrichment Wiki](https://github.com/serilog/serilog/wiki/Enrichment)
- [Code4IT: Serilog Correlation IDs](https://www.code4it.dev/blog/serilog-correlation-id/)
- [Serilog.Enrichers.Environment GitHub](https://github.com/serilog/serilog-enrichers-environment)
- [Serilog.Enrichers.Thread NuGet](https://www.nuget.org/packages/Serilog.Enrichers.Thread/)
