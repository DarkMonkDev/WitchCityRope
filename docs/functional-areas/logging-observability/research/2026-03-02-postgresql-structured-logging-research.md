# Technology Research: PostgreSQL Structured Log Storage with Serilog
<!-- Last Updated: 2026-03-02 -->
<!-- Version: 1.0 -->
<!-- Owner: Technology Researcher Agent -->
<!-- Status: Complete -->

## Executive Summary
**Decision Required**: How to store Serilog structured logs in PostgreSQL for a .NET 10 application
**Recommendation**: Use `Serilog.Sinks.Postgresql.Alternative` with a hybrid schema (dedicated columns + JSONB properties), monthly partitioning via `pg_partman`, BRIN index on timestamp, and GIN index on JSONB properties. Use the same DigitalOcean managed PostgreSQL instance with a separate schema and dedicated connection string.
**Confidence Level**: High (85%)
**Key Factors**: (1) Low-traffic site makes same-database approach viable (2) DigitalOcean supports pg_partman and pg_cron (3) BRIN indexes are purpose-built for append-only timestamp data

---

## 1. Table Design

### Recommended Schema

```sql
-- Create a dedicated schema for logging
CREATE SCHEMA IF NOT EXISTS logging;

-- Create the partitioned log table
CREATE TABLE logging.application_logs (
    id              BIGINT GENERATED ALWAYS AS IDENTITY,
    timestamp       TIMESTAMPTZ     NOT NULL,
    level           SMALLINT        NOT NULL,  -- Serilog numeric level (0=Verbose..5=Fatal)
    level_name      VARCHAR(16)     NOT NULL,  -- Human-readable: 'Information', 'Error', etc.
    message         TEXT            NOT NULL,  -- Rendered message
    message_template TEXT           NULL,       -- Serilog message template
    exception       TEXT            NULL,       -- Full exception text (stack trace)
    properties      JSONB           NOT NULL DEFAULT '{}', -- All structured properties
    machine_name    VARCHAR(128)    NULL,
    environment     VARCHAR(32)     NULL,       -- 'Development', 'Staging', 'Production'
    source_context  VARCHAR(512)    NULL,       -- Logger source (namespace.class)
    request_id      VARCHAR(64)     NULL,       -- HTTP request correlation
    user_id         UUID            NULL,       -- Extracted for fast querying
    correlation_id  UUID            NULL,       -- Cross-service correlation
    PRIMARY KEY (timestamp, id)               -- Partition key must be in PK
) PARTITION BY RANGE (timestamp);

-- Set LZ4 compression for large text columns (PostgreSQL 14+)
ALTER TABLE logging.application_logs
    ALTER COLUMN exception SET COMPRESSION lz4,
    ALTER COLUMN message SET COMPRESSION lz4,
    ALTER COLUMN message_template SET COMPRESSION lz4;
```

### Design Rationale

**Dedicated Columns (for frequently queried fields)**:
| Column | Type | Rationale |
|--------|------|-----------|
| `timestamp` | `TIMESTAMPTZ` | Partition key, range queries, always queried |
| `level` / `level_name` | `SMALLINT` / `VARCHAR(16)` | Numeric for filtering efficiency, text for readability |
| `message` | `TEXT` | Always displayed in log viewers |
| `exception` | `TEXT` | Large text, needs TOAST compression, filtered on NULL/NOT NULL |
| `source_context` | `VARCHAR(512)` | Common filter: "show me all logs from AuthService" |
| `user_id` | `UUID` | Critical for WitchCityRope: "what happened for this user?" |
| `correlation_id` | `UUID` | Cross-service request tracing |
| `request_id` | `VARCHAR(64)` | HTTP request-level correlation |
| `environment` | `VARCHAR(32)` | Filter by deployment environment |

**JSONB Column (for flexible/infrequent properties)**:
| Use Case | Example Properties |
|----------|-------------------|
| HTTP details | `{"RequestPath": "/api/events", "StatusCode": 200, "Method": "GET"}` |
| Event context | `{"EventId": "abc-123", "SessionId": "sess-456"}` |
| Custom enrichment | `{"ThreadId": 42, "ProcessId": 1234}` |

**Why this hybrid approach?**
- Frequently queried fields as dedicated columns avoid JSONB extraction overhead
- JSONB stores the "long tail" of properties without schema changes
- Promotion rule: If you query a property in >30% of log searches, make it a dedicated column

### Single Table with Partitioning (Not Multiple Tables)

Do NOT create separate tables per log level. Instead, use a single partitioned table:
- Simpler application code (one INSERT target)
- Partitioning handles data lifecycle management
- Partial indexes handle level-specific query optimization
- Serilog sink writes to one table regardless

### Partitioning Strategy: Monthly Range Partitions

For WitchCityRope's traffic (~100-500 DAU), monthly partitions are the right granularity:

```sql
-- Create partitions (pg_partman automates this, but manual example shown)
CREATE TABLE logging.application_logs_2026_01
    PARTITION OF logging.application_logs
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

CREATE TABLE logging.application_logs_2026_02
    PARTITION OF logging.application_logs
    FOR VALUES FROM ('2026-02-01') TO ('2026-03-01');

-- ... pg_partman creates these automatically
```

**Why monthly?**
- At ~100-500 DAU with typical logging, expect 5,000-50,000 log entries/day
- Monthly = ~150K-1.5M rows per partition (well within optimal range)
- Daily would create too many partitions for this traffic volume
- Quarterly would make retention cleanup too coarse-grained
- Monthly aligns with natural retention periods (keep 3 months, 6 months, etc.)

**Estimated storage**: At ~500 bytes/row average (with JSONB), 1.5M rows/month = ~750MB/month before compression. With TOAST compression, expect ~300-400MB/month.

---

## 2. Indexing Strategy

### Recommended Indexes

```sql
-- 1. BRIN index on timestamp (primary query pattern: date range)
--    BRIN is ideal for append-only tables with naturally ordered timestamps
--    ~1000x smaller than B-tree equivalent
CREATE INDEX idx_logs_timestamp_brin
    ON logging.application_logs
    USING BRIN (timestamp)
    WITH (pages_per_range = 32);

-- 2. B-tree index on level for filtering by severity
--    Small cardinality (6 values), frequently combined with timestamp
CREATE INDEX idx_logs_level
    ON logging.application_logs (level);

-- 3. Partial index for Error+ only (most common investigation query)
--    Only indexes ~5-10% of rows, dramatically smaller
CREATE INDEX idx_logs_errors_only
    ON logging.application_logs (timestamp, source_context)
    WHERE level >= 3;  -- Warning(3), Error(4), Fatal(5)

-- 4. B-tree index on user_id for "what happened to this user?" queries
--    Critical for WitchCityRope safety investigations
CREATE INDEX idx_logs_user_id
    ON logging.application_logs (user_id)
    WHERE user_id IS NOT NULL;

-- 5. B-tree index on correlation_id for request tracing
CREATE INDEX idx_logs_correlation_id
    ON logging.application_logs (correlation_id)
    WHERE correlation_id IS NOT NULL;

-- 6. GIN index on JSONB properties for ad-hoc searches
--    Use jsonb_path_ops for better performance (supports @> operator)
CREATE INDEX idx_logs_properties_gin
    ON logging.application_logs
    USING GIN (properties jsonb_path_ops);

-- 7. B-tree on source_context for filtering by component
CREATE INDEX idx_logs_source_context
    ON logging.application_logs (source_context);
```

### Index Strategy Rationale

**BRIN vs B-tree for Timestamp**:
| Characteristic | BRIN | B-tree |
|---------------|------|--------|
| Index size (1M rows) | ~24 KB | ~21 MB |
| Works for append-only? | Perfectly | Yes, but wasteful |
| Range query performance | Excellent for large ranges | Better for pinpoint lookups |
| Write overhead | Minimal | Moderate |
| **Recommendation** | **Use this** for log timestamps | Overkill for this use case |

BRIN is purpose-built for append-only tables with naturally correlated data. Log tables are the canonical use case. The 1000x size reduction keeps the index in memory at all times.

**GIN Index Considerations**:
- Use `jsonb_path_ops` (not default `jsonb_ops`): Smaller index, faster `@>` containment queries
- GIN indexes have higher write overhead than B-tree, but logging is write-once
- Monitor GIN index bloat; periodically `REINDEX CONCURRENTLY` if needed
- GIN supports `@>` containment operator but NOT `->>` extraction

**Partial Indexes**:
- Error-level partial index only stores ~5-10% of rows
- Dramatically faster for the most common investigation query: "show me recent errors"
- `WHERE user_id IS NOT NULL` avoids indexing the majority of rows that have no user context

**IMPORTANT**: All indexes are automatically created per-partition by PostgreSQL. When using `pg_partman`, indexes on the parent table are inherited by child partitions.

---

## 3. Performance

### Same Database vs. Separate Database

**Recommendation: Same database, separate schema (`logging`)**

For WitchCityRope's scale (100-500 DAU), a separate database is unnecessary overhead:

| Factor | Same DB, Same Schema | Same DB, Separate Schema | Separate DB |
|--------|---------------------|-------------------------|-------------|
| Operational complexity | Low | Low | High (two managed instances) |
| Cost | None | None | 2x database cost |
| Connection pool impact | Shared pool | Separate pool possible | Isolated |
| Transaction interference | Possible | Minimal with async writes | None |
| Backup/restore | Logs in same backup | Logs in same backup | Independent |
| **Recommendation** | No | **Yes** | Overkill at this scale |

**Key insight**: With async batched writes (see below), log inserts are decoupled from request processing. The application never waits for a log INSERT to complete before responding to a user.

### Capacity Analysis

**Estimated daily log volume** (100-500 DAU community site):

| Log Level | Events/Day (low traffic) | Events/Day (event night) |
|-----------|-------------------------|------------------------|
| Verbose/Debug | 0 (disabled in prod) | 0 |
| Information | 3,000-5,000 | 10,000-20,000 |
| Warning | 50-200 | 200-500 |
| Error | 10-50 | 30-100 |
| Fatal | 0-2 | 0-5 |
| **Total** | **~5,000/day** | **~20,000/day** |

PostgreSQL can comfortably handle 100,000+ inserts/day on a modest instance. WitchCityRope's volume is 10-20x below any concern threshold.

**At peak**: 20,000 events/day = ~14 events/minute = 1 batch insert every ~2 minutes (at batch size 30). This is negligible load.

### Batched/Async Insert Configuration

**Recommended Serilog configuration**:

```csharp
// Batch configuration
var batchSizeLimit = 30;          // Events per batch (default, good for this volume)
var period = TimeSpan.FromSeconds(5); // Flush interval
var queueLimit = 10_000;          // In-memory queue before dropping

// Use COPY command for batch inserts (faster than individual INSERTs)
var useCopy = true;
```

**How batching works**:
1. Application logs an event -> goes into in-memory queue (instant, non-blocking)
2. Timer fires every 5 seconds (or when batch size reached)
3. Sink writes batch of up to 30 events via PostgreSQL `COPY` command
4. `COPY` is 2-5x faster than equivalent individual `INSERT` statements

**Queue behavior**:
- If PostgreSQL is temporarily unavailable, events queue in memory (up to `queueLimit`)
- If queue fills, oldest events are dropped (logging should never crash the app)
- On application shutdown, `Log.CloseAndFlush()` writes remaining queued events

### Connection Pool Considerations

**Use a dedicated connection string for logging** with a small pool:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Database=witchcityrope;...;Maximum Pool Size=20;",
    "LoggingConnection": "Host=db;Database=witchcityrope;...;Maximum Pool Size=3;Application Name=Serilog"
  }
}
```

**Why separate connection strings?**
- Npgsql pools connections by connection string
- Logging pool (3 connections) is isolated from application pool (20 connections)
- If logging has issues, it cannot exhaust the application's connection pool
- `Application Name=Serilog` makes it easy to identify logging connections in `pg_stat_activity`

---

## 4. Retention and Cleanup

### Retention Strategy

**Recommended retention periods by level**:

| Log Level | Retention Period | Rationale |
|-----------|-----------------|-----------|
| Fatal | 12 months | Rare, critical for post-mortem analysis |
| Error | 6 months | Important for trend analysis and debugging |
| Warning | 3 months | Useful for recent investigation, high volume |
| Information | 1 month | Highest volume, most routine |
| Debug/Verbose | 0 (disabled in prod) | Never enable in production without explicit need |

**Simplified approach (recommended for WitchCityRope)**: Keep ALL levels for 3 months, then drop entire monthly partitions. This is simpler to implement and at the estimated storage (~300-400MB/month), 3 months = ~1.2GB total. Not a concern.

### Automated Cleanup with pg_partman + pg_cron

DigitalOcean managed PostgreSQL supports both `pg_partman` and `pg_cron`:

```sql
-- Step 1: Install extensions
CREATE EXTENSION IF NOT EXISTS pg_partman;
-- pg_cron is typically pre-installed on managed PostgreSQL

-- Step 2: Configure pg_partman for the log table
SELECT partman.create_parent(
    p_parent_table := 'logging.application_logs',
    p_control := 'timestamp',
    p_type := 'native',            -- Use PostgreSQL native partitioning
    p_interval := '1 month',       -- Monthly partitions
    p_premake := 3,                -- Create 3 months of future partitions
    p_start_partition := '2026-01-01'
);

-- Step 3: Set retention policy (3 months)
UPDATE partman.part_config
SET retention = '3 months',
    retention_keep_table = false,   -- Drop old partitions entirely
    retention_keep_index = false,   -- Drop indexes too
    infinite_time_partitions = true -- Always create future partitions
WHERE parent_table = 'logging.application_logs';

-- Step 4: Schedule pg_partman maintenance via pg_cron
-- Runs daily at 3 AM: creates new partitions, drops expired ones
SELECT cron.schedule(
    'log-partition-maintenance',
    '0 3 * * *',  -- Daily at 3:00 AM
    $$SELECT partman.run_maintenance_proc()$$
);
```

**How it works**:
1. `pg_partman` automatically creates new monthly partitions 3 months ahead
2. `pg_cron` runs maintenance daily at 3 AM
3. Maintenance creates any needed partitions and drops partitions older than 3 months
4. Dropping a partition is instant (unlike `DELETE` which generates WAL and triggers vacuum)

### Why Partitioning Beats DELETE for Retention

| Approach | Time to Remove 1M Rows | WAL Generated | Vacuum Needed | Table Bloat |
|----------|----------------------|---------------|---------------|-------------|
| `DELETE WHERE timestamp < X` | Minutes to hours | Massive | Yes, heavy | Yes, significant |
| `DROP PARTITION` | Milliseconds | Minimal | No | No |

**There is no comparison**. For log retention, partition dropping is the only sane approach.

### Archiving Before Deletion (Optional)

If you want to archive logs before dropping partitions:

```sql
-- Export a partition to CSV before dropping
COPY logging.application_logs_2025_12 TO '/tmp/logs_2025_12.csv' WITH CSV HEADER;

-- Or detach without dropping (for investigation access)
ALTER TABLE logging.application_logs
    DETACH PARTITION logging.application_logs_2025_12;
-- Table still exists, just not part of the partitioned set
```

For WitchCityRope, archiving is likely unnecessary at this scale. If needed later, add a step to the pg_cron job that exports to CSV before dropping.

### Autovacuum Considerations

With partitioning, autovacuum impact is minimal:
- Log tables are INSERT-only (no dead tuples from updates)
- PostgreSQL 13+ has `autovacuum_vacuum_insert_threshold` for insert-heavy tables
- Partitions keep table sizes small, so vacuum runs quickly
- Dropping partitions avoids vacuum entirely for retention cleanup

**Per-table tuning** (optional, only if you see issues):

```sql
ALTER TABLE logging.application_logs
SET (
    autovacuum_vacuum_insert_scale_factor = 0.1,  -- Vacuum after 10% inserts (vs 20% default)
    autovacuum_analyze_scale_factor = 0.05          -- Analyze sooner for better query plans
);
```

---

## 5. Querying Patterns

### Common Queries

**Query 1: Recent errors with date range** (most common investigation query)

```sql
-- Uses: partial index (idx_logs_errors_only) + BRIN timestamp index
SELECT timestamp, level_name, message, exception, source_context, properties
FROM logging.application_logs
WHERE timestamp >= NOW() - INTERVAL '24 hours'
  AND level >= 4  -- Error and Fatal only
ORDER BY timestamp DESC
LIMIT 100;
```

**Query 2: All logs for a specific user** (safety investigation)

```sql
-- Uses: idx_logs_user_id partial index
SELECT timestamp, level_name, message, source_context, properties
FROM logging.application_logs
WHERE user_id = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'
  AND timestamp >= NOW() - INTERVAL '7 days'
ORDER BY timestamp DESC;
```

**Query 3: Search JSONB properties by specific value** (ad-hoc investigation)

```sql
-- Uses: GIN index with @> containment operator
-- Find all logs for a specific HTTP request path
SELECT timestamp, level_name, message, properties
FROM logging.application_logs
WHERE properties @> '{"RequestPath": "/api/events/register"}'::jsonb
  AND timestamp >= NOW() - INTERVAL '1 day'
ORDER BY timestamp DESC;

-- IMPORTANT: This pattern uses the GIN index:
--   WHERE properties @> '{"key": "value"}'::jsonb    -- GIN-compatible
-- This pattern does NOT use the GIN index:
--   WHERE properties->>'key' = 'value'                -- Requires seq scan or expression index
```

**Query 4: Trace a request across services** (correlation)

```sql
-- Uses: idx_logs_correlation_id
SELECT timestamp, level_name, source_context, message, properties
FROM logging.application_logs
WHERE correlation_id = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'
ORDER BY timestamp ASC;
```

**Query 5: Error count by day** (trend reporting)

```sql
-- Uses: BRIN timestamp + partial index on level
SELECT
    DATE_TRUNC('day', timestamp) AS day,
    level_name,
    COUNT(*) AS error_count
FROM logging.application_logs
WHERE timestamp >= NOW() - INTERVAL '30 days'
  AND level >= 3  -- Warning+
GROUP BY day, level_name
ORDER BY day DESC, level_name;
```

**Query 6: Top error sources** (identify problematic components)

```sql
SELECT
    source_context,
    COUNT(*) AS error_count,
    COUNT(DISTINCT DATE_TRUNC('day', timestamp)) AS days_affected
FROM logging.application_logs
WHERE timestamp >= NOW() - INTERVAL '7 days'
  AND level >= 4  -- Error+
GROUP BY source_context
ORDER BY error_count DESC
LIMIT 20;
```

**Query 7: Errors per category (exception type)**

```sql
SELECT
    SUBSTRING(exception FROM '^[^\n:]+') AS exception_type,
    COUNT(*) AS occurrence_count,
    MIN(timestamp) AS first_seen,
    MAX(timestamp) AS last_seen
FROM logging.application_logs
WHERE timestamp >= NOW() - INTERVAL '30 days'
  AND level >= 4
  AND exception IS NOT NULL
GROUP BY exception_type
ORDER BY occurrence_count DESC
LIMIT 20;
```

### Should You Create Materialized Views?

**No, not at WitchCityRope's scale.** Here is why:

| Factor | Regular Query | Materialized View |
|--------|--------------|-------------------|
| Setup complexity | Zero | Moderate (create, refresh schedule, unique index) |
| Query time (1.5M rows/month) | < 100ms with proper indexes | < 10ms |
| Maintenance burden | None | Refresh schedule, monitoring, index on MV |
| Data freshness | Real-time | Stale until refresh |

At 1.5M rows/month, the direct queries above with proper indexes will return in under 100ms. Materialized views are worth it at 10M+ rows or when query time exceeds 1-2 seconds.

**Reconsider if**: Log volume grows significantly (10x+) or you build a real-time dashboard that hammers the same aggregation query.

---

## 6. PostgreSQL-Specific Considerations

### DigitalOcean Managed PostgreSQL

Confirmed extensions available:
- **pg_partman**: Available on PostgreSQL 13-18
- **pg_cron**: Available on PostgreSQL 13-18
- **pg_stat_statements**: Available for query performance monitoring

**DigitalOcean limitation**: pg_cron can only be used on the `defaultdb` database. Ensure your application uses `defaultdb` or plan accordingly.

### BRIN Index Tuning

```sql
-- Default pages_per_range = 128
-- For log tables, smaller values (32) improve narrow time range queries
-- Larger values reduce index size further but scan more blocks
CREATE INDEX idx_logs_timestamp_brin
    ON logging.application_logs
    USING BRIN (timestamp)
    WITH (pages_per_range = 32);
```

**pages_per_range = 32 recommendation**:
- Good balance between index size and query precision
- At ~500 bytes/row and 8KB pages, 32 pages = ~500 rows per range entry
- Narrow queries (last 1 hour) scan fewer unnecessary blocks
- Index remains tiny (~50-100KB for millions of rows)

### TOAST Compression for Large Text

PostgreSQL TOAST automatically compresses text values over ~2KB. For exception stack traces:

```sql
-- Use LZ4 compression (PostgreSQL 14+) for faster compression
-- LZ4: ~5x faster than pglz, ~7% less compression ratio
-- For write-heavy log tables, speed wins over marginal space savings
ALTER TABLE logging.application_logs
    ALTER COLUMN exception SET COMPRESSION lz4;
ALTER TABLE logging.application_logs
    ALTER COLUMN message SET COMPRESSION lz4;
```

**Impact**: Stack traces are often 2-10KB. LZ4 compresses these ~2x with 5x faster compression than default pglz. For a write-heavy table, this is the right tradeoff.

### PostgreSQL Settings for Log Workload

For DigitalOcean managed PostgreSQL, you can tune some parameters via their control panel:

```
-- These are suggestions; managed PostgreSQL may limit some:
-- Increase checkpoint distance for write-heavy workloads
checkpoint_completion_target = 0.9    -- Default, already good
wal_buffers = 16MB                     -- Helps with batch inserts

-- Connection pool (managed by DigitalOcean)
-- Ensure you have enough connections for app + logging pools
max_connections = 50                   -- Check DigitalOcean plan limits
```

Most tuning is handled by DigitalOcean's managed service. The main optimization levers available to you are:
1. Schema design (covered above)
2. Index strategy (covered above)
3. Partitioning (covered above)
4. Client-side batching (covered below)

---

## 7. Serilog Implementation

### NuGet Package

```xml
<PackageReference Include="Serilog.Sinks.Postgresql.Alternative" Version="4.2.0" />
```

**Why `Serilog.Sinks.Postgresql.Alternative` over `Serilog.Sinks.PostgreSQL`?**
- Actively maintained (last update 2023, but stable and production-tested)
- Supports `COPY` command for batch inserts
- Better column writer API
- Auto-create table and schema support
- Built on Serilog's `PeriodicBatching` infrastructure

### C# Configuration

```csharp
using Serilog;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using NpgsqlTypes;

// Define column mappings
var columnWriters = new Dictionary<string, ColumnWriterBase>
{
    { "timestamp",        new TimestampColumnWriter(NpgsqlDbType.TimestampTz) },
    { "level",            new LevelColumnWriter(renderAsText: false, NpgsqlDbType.Smallint) },
    { "level_name",       new LevelColumnWriter(renderAsText: true, NpgsqlDbType.Varchar) },
    { "message",          new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
    { "message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
    { "exception",        new ExceptionColumnWriter(NpgsqlDbType.Text) },
    { "properties",       new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
    { "machine_name",     new SinglePropertyColumnWriter(
                              "MachineName", PropertyWriteMethod.ToString,
                              NpgsqlDbType.Varchar) },
    { "environment",      new SinglePropertyColumnWriter(
                              "Environment", PropertyWriteMethod.ToString,
                              NpgsqlDbType.Varchar) },
    { "source_context",   new SinglePropertyColumnWriter(
                              "SourceContext", PropertyWriteMethod.ToString,
                              NpgsqlDbType.Varchar) },
    { "request_id",       new SinglePropertyColumnWriter(
                              "RequestId", PropertyWriteMethod.ToString,
                              NpgsqlDbType.Varchar) },
    { "user_id",          new SinglePropertyColumnWriter(
                              "UserId", PropertyWriteMethod.ToString,
                              NpgsqlDbType.Varchar) },
    { "correlation_id",   new SinglePropertyColumnWriter(
                              "CorrelationId", PropertyWriteMethod.ToString,
                              NpgsqlDbType.Varchar) }
};

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.PostgreSQL(
        connectionString: builder.Configuration.GetConnectionString("LoggingConnection"),
        tableName: "application_logs",
        columnOptions: columnWriters,
        schemaName: "logging",
        needAutoCreateTable: true,
        needAutoCreateSchema: true,
        batchSizeLimit: 30,
        period: TimeSpan.FromSeconds(5),
        queueLimit: 10_000,
        useCopy: true              // Use COPY command for batch inserts
    )
    .WriteTo.Console()             // Always keep console for container logs
    .CreateLogger();
```

### Enricher Configuration for Structured Properties

```csharp
// In Program.cs or middleware, push properties to LogContext
app.Use(async (context, next) =>
{
    var userId = context.User?.FindFirst("sub")?.Value;
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();

    using (LogContext.PushProperty("UserId", userId))
    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
    {
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await next();
    }
});
```

### appsettings.json Configuration (Alternative)

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Postgresql.Alternative"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "PostgreSQL",
        "Args": {
          "connectionString": "LoggingConnection",
          "tableName": "application_logs",
          "schemaName": "logging",
          "needAutoCreateTable": true,
          "needAutoCreateSchema": true,
          "batchSizeLimit": 30,
          "period": "00:00:05",
          "queueLimit": 10000,
          "useCopy": true
        }
      },
      {
        "Name": "Console"
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithEnvironmentName"]
  }
}
```

### Critical: Application Shutdown

```csharp
// In Program.cs, ensure logs are flushed on shutdown
var app = builder.Build();

// ... configure app ...

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();  // CRITICAL: Flushes remaining batched events
}
```

---

## 8. Risk Assessment

### Low Risk
- **PostgreSQL capacity exceeded by logs**
  - At 5,000-20,000 events/day, this is nowhere near a concern
  - **Monitoring**: Check `pg_stat_user_tables` row counts monthly

### Low Risk
- **Connection pool contention**
  - Mitigated by separate connection string with dedicated 3-connection pool
  - Batch writes only open connections every 5 seconds
  - **Monitoring**: Watch `pg_stat_activity` for Serilog connections

### Low Risk
- **GIN index bloat on JSONB column**
  - Log tables are insert-only (no updates = minimal bloat)
  - Partitions are dropped monthly, which removes old indexes entirely
  - **Monitoring**: Check index sizes periodically; `REINDEX CONCURRENTLY` if needed

### Medium Risk
- **useCopy UTF-8 encoding issues**
  - The `COPY` command is stricter about encoding than `INSERT`
  - Some users report `invalid byte sequence for encoding "UTF8"` errors
  - **Mitigation**: If you encounter this, set `useCopy: false` to fall back to INSERT statements. Performance impact is negligible at this volume.

### Medium Risk
- **pg_partman/pg_cron configuration on DigitalOcean**
  - pg_cron has limitations on DigitalOcean (defaultdb only)
  - **Mitigation**: Use `defaultdb` for the application database, or implement partition maintenance as a Hangfire job in the application instead

---

## 9. Implementation Checklist

### Phase 1: Schema and Table Setup
- [ ] Create `logging` schema
- [ ] Create partitioned `application_logs` table
- [ ] Set LZ4 compression on text columns
- [ ] Install `pg_partman` extension
- [ ] Configure pg_partman for monthly partitions with 3-month retention
- [ ] Install and configure `pg_cron` for daily maintenance

### Phase 2: Index Creation
- [ ] Create BRIN index on timestamp
- [ ] Create B-tree index on level
- [ ] Create partial index for Error+ levels
- [ ] Create partial index on user_id (WHERE NOT NULL)
- [ ] Create partial index on correlation_id (WHERE NOT NULL)
- [ ] Create GIN index on properties (jsonb_path_ops)
- [ ] Create B-tree index on source_context

### Phase 3: Serilog Integration
- [ ] Add `Serilog.Sinks.Postgresql.Alternative` NuGet package
- [ ] Configure column writers matching table schema
- [ ] Set up dedicated logging connection string
- [ ] Configure batch settings (size=30, period=5s, queue=10K)
- [ ] Add enrichers (UserId, CorrelationId, RequestId)
- [ ] Ensure `Log.CloseAndFlush()` on shutdown
- [ ] Keep Console sink alongside PostgreSQL sink

### Phase 4: Verification
- [ ] Verify log entries appear in PostgreSQL
- [ ] Verify JSONB properties are queryable with `@>` operator
- [ ] Test error-level partial index with EXPLAIN ANALYZE
- [ ] Test user_id and correlation_id lookups
- [ ] Verify partitions are being created by pg_partman
- [ ] Confirm pg_cron maintenance job runs successfully

---

## 10. Research Sources

- [Serilog.Sinks.Postgresql.Alternative - GitHub](https://github.com/serilog-contrib/Serilog.Sinks.Postgresql.Alternative)
- [Serilog.Sinks.Postgresql.Alternative - HowToUse](https://github.com/serilog-contrib/Serilog.Sinks.Postgresql.Alternative/blob/master/HowToUse.md)
- [Serilog.Sinks.PostgreSQL (original) - GitHub](https://github.com/b00ted/serilog-sinks-postgresql)
- [PostgreSQL Documentation: Table Partitioning](https://www.postgresql.org/docs/current/ddl-partitioning.html)
- [PostgreSQL Documentation: Partial Indexes](https://www.postgresql.org/docs/current/indexes-partial.html)
- [Crunchy Data: When Does BRIN Win?](https://www.crunchydata.com/blog/postgres-indexing-when-does-brin-win)
- [Crunchy Data: Indexing JSONB in Postgres](https://www.crunchydata.com/blog/indexing-jsonb-in-postgres)
- [pganalyze: Understanding GIN Indexes](https://pganalyze.com/blog/gin-index)
- [Data Egret: Data Archiving and Retention in PostgreSQL](https://dataegret.com/2025/05/data-archiving-and-retention-in-postgresql-best-practices-for-large-datasets/)
- [Crunchy Data: Auto-archiving with pg_partman](https://www.crunchydata.com/blog/auto-archiving-and-data-retention-management-in-postgres-with-pg_partman)
- [DigitalOcean: Supported PostgreSQL Extensions](https://docs.digitalocean.com/products/databases/postgresql/details/supported-extensions/)
- [Sequin Stream: Time-based Retention Strategies in Postgres](https://blog.sequinstream.com/time-based-retention-strategies-in-postgres/)
- [Simple Thread: Drop Partitions, Not Performance](https://www.simplethread.com/beyond-delete/)
- [CYBERTEC: B-tree vs BRIN in Data Warehouses](https://www.cybertec-postgresql.com/en/btree-vs-brin-2-options-for-indexing-in-postgresql-data-warehouses/)
- [Tiger Data: PostgreSQL Compression pglz vs LZ4](https://www.tigerdata.com/blog/optimizing-postgresql-performance-compression-pglz-vs-lz4)
- [PostgreSQL Fastware: LZ4 TOAST Compression](https://www.postgresql.fastware.com/blog/what-is-the-new-lz4-toast-compression-in-postgresql-14)
- [CYBERTEC: Tuning Autovacuum for PostgreSQL](https://www.cybertec-postgresql.com/en/tuning-autovacuum-postgresql/)
- [EDB: Autovacuum Tuning Basics](https://www.enterprisedb.com/blog/autovacuum-tuning-basics)
