# Logging & Observability - Site-Wide Infrastructure Project

**Status**: Not Started - Research Complete
**Priority**: High (Foundational - blocks reporting capabilities)
**Created**: 2026-03-02
**Business Domain**: Cross-cutting infrastructure

## Problem Statement

WitchCityRope has **no centralized, queryable logging infrastructure**. The application uses bare-minimum `Microsoft.Extensions.Logging` (ILogger) with all 1,157 log statements going to **console/stdout only**. In production, logs are effectively lost when containers restart. There is no ability to search logs, create reports, set up alerts, or track errors across the application.

The immediate trigger was discovering that Authorize.net credit card payment failures are not being persisted anywhere queryable - but that's a symptom of the larger problem: **the entire site lacks a proper logging strategy**.

## Current State Audit (2026-03-02)

### Logging Framework

- **Framework**: Built-in `Microsoft.Extensions.Logging` (ILogger<T>) via dependency injection
- **NO third-party logging packages**: No Serilog, NLog, Application Insights, Seq, Sentry, or any other logging library
- **NO structured logging sinks**: Logs go to console provider only
- **NO log persistence**: Logs are lost on container restart

### Configuration

**appsettings.json** (Base):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**appsettings.Development.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**Production (docker-compose.prod.yml environment variables)**:
```yaml
Logging__LogLevel__Default: Warning
Logging__LogLevel__Microsoft: Error
Logging__LogLevel__WitchCityRope: Information
```

**PostgreSQL Development Logging**:
- `log_statement=all` - logs all SQL statements
- `log_min_duration_statement=0` - logs query duration

### Where Logs Go Today

| Environment | Destination | Persistence | Queryable |
|-------------|-------------|-------------|-----------|
| Development | Docker container stdout | Until container removed | `docker compose logs -f` only |
| Production | Docker container stdout | Until container removed | NO |
| Production | `/app/logs` tmpfs volume (512MB) | Until container restart (RAM-based) | NO |
| Production | Fluent Bit (fluent/fluent-bit:2.2) | **Destination unknown/unconfigured** | NO |
| Production (nginx) | `/var/log/nginx` volume | Persisted but no rotation | Manual only |

**Critical**: Fluent Bit is running in production docker-compose but the configuration file destination is unclear. Logs may be aggregated and then discarded.

### Log Statement Distribution (1,157 total)

| Level | Count | Percentage | Notes |
|-------|-------|-----------|-------|
| LogInformation | 503 | 43.5% | Normal operations |
| LogError | 304 | 26.3% | Exception handling |
| LogWarning | 235 | 20.3% | Declined payments, validation failures |
| LogDebug | 110 | 9.5% | Development tracing |
| LogCritical | 5 | 0.4% | Payment charged but finalization failed |

### Existing Database Audit Tables (10 separate custom tables)

The codebase has 10 domain-specific audit log tables, each designed independently with different schemas:

| Table | Entity File | Purpose | Has IP/UserAgent |
|-------|------------|---------|------------------|
| PaymentAuditLog | `Features/Payments/Entities/PaymentAuditLog.cs` | Payment operations, refunds, status changes | Yes |
| VettingAuditLog | `Features/Vetting/Entities/VettingAuditLog.cs` | Vetting application workflow changes | No (UserId only) |
| CheckInAuditLog | `Features/CheckIn/Entities/CheckInAuditLog.cs` | Event check-in, manual entry, overrides | Yes |
| EmailTriggerLog | `Features/EmailTemplates/Entities/EmailTriggerLog.cs` | Email template trigger events | No |
| VettingEmailLog | `Features/Vetting/Entities/VettingEmailLog.cs` | Vetting email sending | No |
| VettingBulkOperationLog | `Features/Vetting/Entities/VettingBulkOperationLog.cs` | Bulk vetting operations | No |
| SentAdHocEmail | `Features/EmailTemplates/Entities/SentAdHocEmail.cs` | Ad-hoc email sending with recipients/body | No |
| AttendanceHistory | `Features/Participation/Entities/AttendanceHistory.cs` | Attendance changes | No |
| ParticipationHistory | `Features/Participation/Entities/ParticipationHistory.cs` | Event participation changes | No |
| IncidentAuditLog | `Features/Safety/Entities/IncidentAuditLog.cs` | Safety incident modifications | No |

**Problems with current audit tables:**
- No shared base class or interface
- Inconsistent schemas (some have IP/UserAgent, most don't)
- No unified querying capability
- No retention policy
- No indexing strategy for reporting
- Each table was designed in isolation

### What IS Being Logged (via ILogger)

**Authentication & Security:**
- Login attempts (success/failure) - `AuthenticationService.cs` lines 118-142
- Duplicate scene name login attempts
- Unverified email blocks
- JWT token validation (Debug level) - `Program.cs` lines 207-256
- Authentication failures
- CSRF validation failures on logout
- Token blacklisting

**Payments:**
- Payment initiation, approval, decline - `AuthorizeNetService.cs`
- Payment processing stages (4-stage checkout correlation) - `CheckoutEndpoints.cs`
- Refund operations - `RefundService.cs`
- PayPal webhook events - `PayPalWebhookProcessingService.cs`
- Cash payment recording - `KioskPaymentEndpoints.cs`

**Email:**
- Email sending success/failure - `EmailService.cs`
- Template operations - `EmailTemplateService.cs`
- Personalized email sending with per-recipient status

**Operations:**
- Check-in operations
- Vetting workflow changes
- Event CRUD operations
- Database seeding
- Docker/container startup

### What is NOT Being Logged

| Missing Log Category | Risk Level | Notes |
|---------------------|-----------|-------|
| Password changes | High | No audit trail for credential changes |
| Role/permission assignments | High | Admin can change roles with no log |
| Failed authorization attempts (403) | High | Cannot detect permission probing |
| Account creation details | Medium | Only partial logging in AuthenticationService |
| User profile changes | Medium | Only vetting-related changes tracked |
| Admin user management actions | High | Create/delete/modify users untracked |
| API request/response correlation | High | Cannot trace a request end-to-end |
| Frontend errors in production | High | Completely invisible |
| Database query performance | Medium | Dev only (EF logging) |
| Authorize.net CC failure details | High | Error codes/messages lost after container restart |

### Frontend Logging

- **Error Boundary**: `RootErrorBoundary.tsx` catches route errors, displays stack trace in dev mode only
- **API Error Handler**: `errors.ts` - RFC 9457 Problem Details parsing, `console.error()` only
- **30+ locations** use `console.log/console.error` - all browser-only, nothing sent to server
- **NO error tracking service** (no Sentry, no Rollbar, no Application Insights)
- **NO analytics pipeline**
- **Production frontend errors are completely invisible to the team**

### Error Handling Architecture

- **NO centralized exception middleware** - each endpoint/service has individual try-catch
- **NO global exception filter**
- **NO request/response logging middleware**
- **NO correlation IDs** (except checkout which creates its own 12-char GUID prefix)
- **Error responses use RFC 9457 Problem Details** format (good practice, should be preserved)

## Key Files Reference

### Backend Configuration
- `apps/api/Program.cs` - Main app setup, JWT event logging (lines 207-256), logging config (lines 348-349)
- `apps/api/appsettings.json` - Base logging config
- `apps/api/appsettings.Development.json` - Dev logging config
- `apps/api/WitchCityRope.Api.csproj` - Package references (NO logging packages)

### Docker/Production
- `docker-compose.prod.yml` - Production log levels (lines 151-154), Fluent Bit config (lines 413-440), tmpfs volumes
- `config/fluent-bit/fluent-bit.conf` - Fluent Bit aggregation config (destination TBD)

### Authentication Logging
- `apps/api/Features/Authentication/Services/AuthenticationService.cs` - Login/registration logging (lines 63-144)
- `apps/api/Features/Authentication/Endpoints/AuthenticationEndpoints.cs` - Logout/CSRF logging

### Payment Logging
- `apps/api/Features/Payments/Services/AuthorizeNetService.cs` - CC payment logging (474 lines)
- `apps/api/Features/Payments/Endpoints/CheckoutEndpoints.cs` - 4-stage checkout logging (465 lines)
- `apps/api/Features/Payments/Endpoints/CreditCardEndpoints.cs` - Standalone CC endpoint logging (154 lines)
- `apps/api/Features/Payments/Services/PaymentService.cs` - Payment service with `LogPaymentFailureAsync()` (331 lines)

### Audit Entities (all under apps/api/Features/)
- `Payments/Entities/PaymentAuditLog.cs` - Most complete audit entity (JSONB before/after, IP, UserAgent)
- `Payments/Configuration/PaymentAuditLogConfiguration.cs` - EF configuration with GIN indexes
- `Vetting/Entities/VettingAuditLog.cs`
- `CheckIn/Entities/CheckInAuditLog.cs`
- `Safety/Entities/IncidentAuditLog.cs`
- `Participation/Entities/AttendanceHistory.cs`
- `Participation/Entities/ParticipationHistory.cs`
- `EmailTemplates/Entities/EmailTriggerLog.cs`
- `Vetting/Entities/VettingEmailLog.cs`
- `Vetting/Entities/VettingBulkOperationLog.cs`
- `EmailTemplates/Entities/SentAdHocEmail.cs`

### Frontend Error Handling
- `apps/web/src/lib/api/utils/errors.ts` - ApiErrorHandler class
- `apps/web/src/components/errors/RootErrorBoundary.tsx` - Route error boundary
- `apps/web/src/lib/debug.ts` - Conditional logging utility

## Decisions Needed

### 1. Logging Framework
**Recommendation: Serilog** - Industry standard for .NET, supports structured logging, 100+ sinks, enrichers for correlation IDs and user context.

**Alternatives to evaluate:**
- Serilog (most popular, richest ecosystem)
- NLog (similar capabilities, XML config)
- OpenTelemetry (newer, cloud-native, but more complex)

### 2. Log Storage / Sink Strategy
**Options to evaluate:**

| Option | Pros | Cons | Cost |
|--------|------|------|------|
| PostgreSQL (same DB) | Simple, no new infra, SQL queries | Can bloat main DB, performance impact | Free |
| PostgreSQL (separate DB) | Isolated, won't affect main app | Extra DB to manage | ~$15/mo DigitalOcean |
| Seq (self-hosted) | Purpose-built for .NET/Serilog, amazing UI | Another container to run | Free (single user) |
| Grafana Loki + Grafana | Industry standard, visual dashboards | Complex setup | Free (self-hosted) |
| Datadog/New Relic | Full APM, alerting, dashboards | Expensive, vendor lock-in | $15-50+/mo |
| File-based with rotation | Simple, reliable | Hard to query, no real-time | Free |

### 3. What Should Be Logged (Minimum)
- All authentication events (login, logout, registration, password changes, failures)
- All admin actions (role changes, user management, content changes)
- All payment events (initiation, success, failure with full error details)
- All API errors (4xx and 5xx with request context)
- Frontend errors (sent to backend endpoint)
- Request/response correlation (trace IDs)
- Performance metrics (slow queries, slow endpoints)

### 4. Existing Audit Tables
- Keep as-is? (domain-specific business audit trails)
- Consolidate into unified structure?
- Replace with Serilog structured events?
- Use both? (Serilog for operational logs, audit tables for business compliance)

### 5. Frontend Error Capture
- Sentry (purpose-built, excellent React integration)
- Custom error endpoint that logs to same Serilog pipeline
- Both?

### 6. Retention Policy
- How long to keep operational logs?
- How long to keep audit/compliance logs?
- Different tiers for different log levels?

## Technology Stack Context

- **Backend**: .NET 10 Minimal API + Controller-based endpoints (mixed)
- **Frontend**: React 18 + TypeScript + Vite
- **Database**: PostgreSQL (DigitalOcean managed)
- **Hosting**: DigitalOcean Droplet (Docker containers)
- **Current DI**: Standard Microsoft DI container
- **Current middleware**: CORS, Authentication, Authorization, Antiforgery, Static Files
- **Background jobs**: Hangfire (PostgreSQL-backed, admin dashboard at /hangfire)

## Scope of Work (Estimated)

### Phase 1: Serilog Foundation
- Add Serilog packages to API project
- Configure in Program.cs (replace default logging)
- Add structured logging enrichers (correlation ID, user context)
- Add request/response logging middleware
- Configure sinks (console + chosen persistent storage)
- Verify all 1,157 existing log statements work with Serilog (they should - ILogger is abstracted)

### Phase 2: Enhanced Logging
- Add global exception middleware
- Add security event logging (auth, admin actions)
- Add CC failure detail capture (Authorize.net response codes, AVS/CVV)
- Add missing log points identified in audit

### Phase 3: Frontend Error Capture
- Add error reporting endpoint or Sentry integration
- Update React error boundary to send errors to backend
- Add API error reporting from frontend

### Phase 4: Reporting & Alerting
- Admin reporting endpoints for log queries
- Dashboard for payment failures, errors, security events
- Alert configuration for critical errors

### Phase 5: Audit Table Decision
- Evaluate whether to keep, consolidate, or supplement existing 10 audit tables
- Implement chosen strategy
- Add retention policy

## Related Issues
- Payment entity consolidation (separate project - see `/docs/functional-areas/payment-entity-consolidation/`)
- CC failure reporting was the original trigger for this investigation
- Fluent Bit production configuration needs review regardless of Serilog decision
