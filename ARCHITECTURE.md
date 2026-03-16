# WitchCityRope Architecture Documentation

<!-- Last Updated: 2026-03-16 -->
<!-- Version: 3.0 -->
<!-- Owner: Engineering Team -->
<!-- Status: Active -->

> **For developer configuration details, test accounts, and quick commands, see [CLAUDE.md](./CLAUDE.md).**

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Architecture Diagram](#architecture-diagram)
3. [Services](#services)
4. [Port Configuration](#port-configuration)
5. [Authentication and Security](#authentication-and-security)
6. [Data Protection](#data-protection)
7. [Database](#database)
8. [Feature Areas](#feature-areas)
9. [Background Jobs](#background-jobs)
10. [Frontend Architecture](#frontend-architecture)
11. [API Architecture](#api-architecture)
12. [Logging and Observability](#logging-and-observability)
13. [Deployment Architecture](#deployment-architecture)
14. [Development Environment](#development-environment)
15. [Key Files](#key-files)
16. [History](#history)

---

## Architecture Overview

WitchCityRope is a membership and event management platform built with a **Web+API microservices architecture**. The React frontend communicates with a .NET Minimal API backend via HTTP, with PostgreSQL for persistence. Background job processing is handled by Hangfire, email delivery by SendGrid, and payments by PayPal and Authorize.Net.

---

## Architecture Diagram

```
                                    Internet
                                       |
                              ┌────────┴────────┐
                              │   Nginx Reverse  │
                              │      Proxy       │
                              │  (staging/prod)  │
                              └───┬──────────┬───┘
                                  │          │
                    ┌─────────────┘          └─────────────┐
                    ▼                                      ▼
          ┌─────────────────┐                    ┌─────────────────┐
          │  React Frontend │    HTTP/JSON        │   API Service   │
          │  (apps/web)     │ ──────────────────► │   (apps/api)    │
          │                 │                     │                 │
          │  React 18       │ ◄────────────────── │  .NET 10        │
          │  TypeScript     │    JSON + Cookies    │  Minimal API    │
          │  Vite           │                     │                 │
          │  Mantine v7     │                     │  Hangfire       │
          └─────────────────┘                     └────────┬────────┘
                                                           │
                          ┌────────────────────────────────┤
                          │                │               │
                          ▼                ▼               ▼
                 ┌──────────────┐  ┌─────────────┐  ┌──────────┐
                 │  PostgreSQL  │  │   SendGrid   │  │  Payment │
                 │  Database    │  │   (Email)    │  │Processors│
                 │              │  └─────────────┘  │          │
                 │  Dev: Docker │                    │  PayPal  │
                 │  Stg/Prod:   │                    │Auth.Net  │
                 │  DO Managed  │                    └──────────┘
                 └──────────────┘
                        │
                 ┌──────┴──────┐
                 │  DO Spaces  │
                 │  (Backups)  │
                 └─────────────┘
```

---

## Services

### React Frontend (`apps/web`)

- **Technology**: React 18 + TypeScript + Vite
- **UI Framework**: Mantine v7 (WCAG compliant, TypeScript-first)
- **State Management**: Zustand (auth, CSRF), TanStack Query (server state)
- **Rich Text**: @mantine/tiptap (Tiptap v2)
- **Database Access**: None -- all data via API calls
- **Port**: 5173 (development), 80 (production container, behind Nginx)

### API Service (`apps/api`)

- **Technology**: ASP.NET Core Minimal API on .NET 10
- **Pattern**: Vertical slice architecture under `Features/`
- **Database Access**: Full access via Entity Framework Core with `ApplicationDbContext`
- **Port**: 8080 (internal container), 5655 (development host), 5001/5002 (production/staging host)

### PostgreSQL Database

- **Development**: PostgreSQL 16 Alpine in Docker container
- **Staging/Production**: DigitalOcean Managed PostgreSQL (port 25060)
- **Connection**: Direct NpgsqlDataSource with explicit pool sizing (no PgBouncer)

### Redis

- **Purpose**: Available in staging/production compose files
- **Image**: redis:7-alpine
- **Configuration**: 256MB max memory, LRU eviction, AOF persistence

---

## Port Configuration

### Development

| Service    | Host Port | Container Port | URL                       |
|------------|-----------|----------------|---------------------------|
| React Web  | 5173      | 5173           | http://localhost:5173      |
| API        | 5655      | 8080           | http://localhost:5655      |
| PostgreSQL | **5434**  | 5432           | localhost:5434             |
| HMR WS     | 24678     | 24678          | --                        |
| .NET Debug | 40000     | 40000          | --                        |

**Note**: PostgreSQL uses port **5434** (not 5432 or 5433) to avoid conflicts with other local PostgreSQL instances.

### Staging

| Service    | Host Port | Container Port | URL                              |
|------------|-----------|----------------|----------------------------------|
| API        | 5002      | 8080           | https://staging.notfai.com/api   |
| Web        | 3002      | 80             | https://staging.notfai.com       |
| Redis      | --        | 6379           | Internal only                    |
| PostgreSQL | --        | 25060          | DigitalOcean Managed             |

### Production

| Service    | Host Port | Container Port | URL                              |
|------------|-----------|----------------|----------------------------------|
| API        | 5001      | 8080           | https://witchcityrope.com/api    |
| Web        | 3001      | 80             | https://witchcityrope.com        |
| Redis      | --        | 6379           | Internal only                    |
| PostgreSQL | --        | 25060          | DigitalOcean Managed             |

---

## Authentication and Security

### Architecture: BFF with httpOnly Cookies

The application uses a Backend-for-Frontend authentication pattern. JWT tokens are stored in httpOnly cookies, never in localStorage or JavaScript-accessible storage.

**Flow**:
1. User submits credentials to `/api/auth/login`
2. API validates credentials, generates JWT, sets `auth-token` httpOnly cookie
3. All subsequent requests automatically include the cookie
4. API extracts JWT from cookie (or Bearer header) via `OnMessageReceived` event
5. Token refresh via `/api/auth/refresh` (silent, cookie-based)
6. Logout via `/api/auth/logout` deletes the cookie server-side

**Client-Side State**: Zustand store (`authStore.ts`) manages authentication state. TanStack Query mutations handle login/logout/register operations.

### CSRF Protection

- Antiforgery tokens via ASP.NET Core (`X-CSRF-TOKEN` header)
- Token cookie (`XSRF-TOKEN`) is JavaScript-readable; validation cookie (`.AspNetCore.Antiforgery`) is httpOnly
- Token endpoint (`/api/antiforgery/token`) is anonymous -- tokens are fetched before login
- CSRF store (`csrfStore.ts`) manages token initialization

### Authorization Roles

| Role              | Description                                |
|-------------------|--------------------------------------------|
| Administrator     | Full system access, Hangfire dashboard     |
| Teacher           | Event management, content creation         |
| VettedMember      | Full event access, community features      |
| GeneralMember     | Basic event access                         |
| Guest             | Public event viewing                       |
| SafetyTeam        | Safety incident management                 |

### JWT Configuration

- Issuer/Audience configurable per environment
- Signing key via `Jwt:SecretKey` (user secrets / environment variable)
- Token lifetime: 60 minutes (production), 480 minutes (development)
- Clock skew: 5 minutes
- Token blacklist service for logout invalidation

### Rate Limiting

- Client error endpoint rate limited: 20 requests per minute per IP (fixed window)

---

## Data Protection

ASP.NET Core Data Protection keys are persisted to PostgreSQL via Entity Framework Core (`DataProtectionKeys` table). This was a critical architectural decision -- without persistence, every container restart or deployment invalidated all outstanding password reset and email confirmation tokens.

**Configuration**:
- `SetApplicationName("WitchCityRope")` ensures consistent key isolation across Docker image rebuilds (without it, the discriminator derives from the content root path, which changes per build)
- Keys auto-rotate every 90 days; old keys are retained indefinitely for validation
- Token lifespan extended to 72 hours (from default 24h) for bulk welcome emails
- Storage: XML blobs in `DataProtectionKeys` table
- DigitalOcean Managed PostgreSQL provides encryption at rest at the storage level

---

## Database

### Context

`ApplicationDbContext` extends `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` and implements `IDataProtectionKeyContext`. It manages all business entities and ASP.NET Core Identity tables.

Identity tables are mapped to the `public` schema with custom names: `Users`, `Roles`, `UserRoles`, `UserClaims`, `UserLogins`, `UserTokens`, `RoleClaims`.

### Entities (DbSets)

**Core/Identity**: DataProtectionKeys, Users (ApplicationUser), Settings, UserNotes

**Events**: Events, Venues, Sessions, TicketTypes, TicketPurchases, VolunteerPositions, VolunteerSignups

**Check-In**: EventAttendees, CheckIns, CheckInAuditLogs, OfflineSyncQueues, CheckInSessionTokens, CheckInSessionTokenSessions

**Participation**: EventAttendances, AttendanceHistory

**Safety**: SafetyIncidents, IncidentAuditLogs, IncidentNotifications, IncidentNotes

**Vetting**: VettingApplications, VettingAuditLogs, VettingEmailLogs, VettingNotifications, VettingBulkOperations, VettingBulkOperationItems, VettingBulkOperationLogs

**Payments**: PaymentRefunds

**CMS**: ContentPages, ContentRevisions

**Email**: GlobalEmailTemplates, EventEmailTemplates, SentAdHocEmails, AdHocEmailTemplates, EmailTriggerLogs

**Logging**: ApplicationLogs (written by Serilog), DailyLogSummaries (written by Hangfire)

### Migrations

- Location: `/apps/api/Migrations/`
- Auto-applied on startup via `DatabaseInitializationService` (BackgroundService pattern with exponential backoff retry)
- Single consolidated initial migration (~2674 lines) as of October 2025; subsequent migrations are incremental
- Command: `dotnet ef migrations add MigrationName` (from `/apps/api` directory)

### Connection Pooling

Three separate Npgsql connection pools to prevent resource exhaustion on the shared managed database cluster:

| Pool         | MaxPoolSize | MinPoolSize | Purpose                                    |
|--------------|-------------|-------------|--------------------------------------------|
| EF Core      | 20 (dev) / 5 (prod) | 5 (dev) / 1 (prod) | Application queries             |
| Hangfire     | 3           | 1           | Background job processing                  |
| Health Check | 2           | 0           | Database health monitoring                 |
| Serilog      | 2           | 0           | Log writing (batched every 5s)             |

### Seed Data

- `DatabaseInitializationService` runs migrations, then `SeedDataService` / `SeedCoordinator` populates test data
- 7 test accounts with predefined roles (see CLAUDE.md for credentials)
- 12 sample events for development
- 24 email templates across 5 categories (Vetting, Events, Admin, Incident, AdHoc)
- 12+ CMS pages
- Production environments use a different admin email (`ropemaster@witchcityrope.com`)

---

## Feature Areas

The API uses a vertical slice architecture. Each feature area is self-contained under `apps/api/Features/`:

| Feature            | Description                                                        |
|--------------------|--------------------------------------------------------------------|
| **Admin**          | Admin dashboard aggregation endpoints                              |
| **Authentication** | Login, register, password reset, email verification, JWT issuance  |
| **Backup**         | Automated database backups to DigitalOcean Spaces (S3-compatible)  |
| **CheckIn**        | Session tokens, QR codes, kiosk mode, attendance tracking          |
| **Cms**            | Content pages, revision history, dynamic routing by slug           |
| **Dashboard**      | User dashboard data aggregation                                    |
| **EmailTemplates** | Global and event-specific templates, ad-hoc bulk email, scheduling |
| **Events**         | Event CRUD, sessions, ticket types, venue management               |
| **Health**         | Health check endpoints (`/health-check`, `/api/health/database`)   |
| **Logging**        | Structured logging infrastructure, log viewer, daily summaries     |
| **Metadata**       | Application metadata endpoints                                     |
| **Participation**  | RSVPs, ticket purchases, attendance records, cancellation          |
| **Payments**       | PayPal integration, Authorize.Net credit cards, unified checkout, refunds |
| **Safety**         | Incident reporting, investigation notes, notifications, audit logs |
| **Shared**         | Cross-cutting extensions, base services, feature registration      |
| **Users**          | Member profiles, admin member management                           |
| **Venues**         | Venue CRUD                                                         |
| **Vetting**        | Member vetting applications, bulk operations, approval workflow    |
| **VettingHold**    | Vetting hold management                                            |
| **Volunteers**     | Volunteer positions, shift sign-ups, scheduling                    |
| **Webhooks**       | PayPal webhook signature verification and processing               |

Features register their own services and endpoints via `AddFeatureServices()` and `MapFeatureEndpoints()` extension methods.

---

## Background Jobs

Hangfire processes background jobs with PostgreSQL storage (schema: `hangfire`). Configuration: 1 worker, 30-second heartbeat.

### Recurring Jobs

| Job ID                  | Schedule       | Implementation            | Description                                  |
|-------------------------|----------------|---------------------------|----------------------------------------------|
| `daily-backup`          | 2:00 AM local  | `BackupJob`               | Database backup to DigitalOcean Spaces       |
| `daily-log-summary`     | 1:00 AM UTC    | `DailyLogSummaryJob`      | Aggregate application logs into daily stats  |
| `log-retention-cleanup` | 3:00 AM UTC    | `LogRetentionCleanupJob`  | Delete logs older than 90 days               |
| `event-email-scheduler` | Hourly (0 min) | `EmailSchedulerJob`       | Event reminders and thank-you emails         |

### Fire-and-Forget Jobs

- **Ad-hoc email sends** (`AdHocEmailSendJob`): Bulk email sends are enqueued as fire-and-forget jobs when administrators send ad-hoc emails

### Dashboard

- Accessible at `/hangfire` (admin-only, protected by `HangfireAuthorizationFilter`)
- Requires authenticated user with `Administrator` role

---

## Frontend Architecture

### Technology Stack

| Library                         | Version | Purpose                              |
|---------------------------------|---------|--------------------------------------|
| React                           | 18.x    | UI framework                         |
| TypeScript                      | 5.x     | Type safety                          |
| Vite                            | 5.x     | Build tool and dev server            |
| Mantine                         | 7.x     | UI component library                 |
| TanStack Query                  | 5.x     | Server state management              |
| Zustand                         | 5.x     | Client state management              |
| React Router                    | 7.x     | Client-side routing                  |
| @mantine/form                   | 7.x     | Form management (Mantine integration)|
| React Hook Form + Zod           | 7.x/4.x | Form management and validation       |
| @mantine/tiptap                 | 7.x     | Rich text editing                    |
| @paypal/react-paypal-js         | 8.x     | PayPal button integration            |
| Framer Motion                   | 12.x    | Animations                           |
| Tailwind CSS                    | 4.x     | Utility CSS                          |
| qrcode.react                    | 4.x     | QR code generation for check-in      |

### Project Structure

```
apps/web/src/
  features/           # Feature-based modules
    admin/            # Admin panel components
    auth/             # Authentication UI
    checkin/          # Kiosk check-in interface
    cms/              # Dynamic CMS pages
    events/           # Event browsing and management
    members/          # Member profiles
    payments/         # Payment forms and confirmation
    safety/           # Incident reporting
    vetting/          # Vetting application form
    volunteers/       # Volunteer management
  stores/             # Zustand stores (authStore, csrfStore)
  routes/             # React Router config, loaders, guards
  components/         # Shared layout, forms, errors
  hooks/              # Custom React hooks
  lib/api/            # API client, hooks (TanStack Query), services
  utils/              # Utility functions
  types/              # TypeScript type definitions
```

### Auto-Generated Types

TypeScript interfaces are generated from the API's OpenAPI specification using `openapi-typescript`. The `@witchcityrope/shared-types` package (monorepo workspace dependency) provides these types.

**Generation**: `cd packages/shared-types && npm run generate`
**Usage**: `import type { components } from '@witchcityrope/shared-types'`

Manually creating TypeScript interfaces for API data is prohibited. All types must come from the generated package.

### Routing

React Router v7 with `createBrowserRouter` provides:
- Public routes (home, events, login, register, safety report, vetting application, CMS pages)
- Protected routes with `authLoader` (dashboard, profile, payments, my reports)
- Admin routes with `adminLoader` validating `Administrator` role
- Kiosk check-in routes outside the main layout (session-token auth, not user login)
- Dynamic CMS catch-all route (`:slug`) for database-driven pages
- `LowercaseUrlRedirect` wrapper normalizes URLs to prevent case-sensitivity 404s

### Build Optimization

Vite is configured with manual chunk splitting for cache efficiency:
- `vendor`: react, react-dom
- `router`: react-router-dom
- `ui`: @mantine/core, @mantine/notifications
- `query`: @tanstack/react-query
- `forms`: react-hook-form, zod

---

## API Architecture

### Endpoint Pattern

Routes use the `/api/` prefix (no version segment). Most endpoints use the Minimal API pattern. PayPal webhook handling uses a controller (`MapControllers()`).

### OpenAPI Documentation

- Microsoft native OpenAPI support (.NET 10) with `AddOpenApi()`
- NSwag provides Swagger UI in development at `/swagger`
- Custom schema transformers: `BearerSecuritySchemeTransformer`, `NumericSchemaTransformer`
- OpenAPI spec exported post-build for frontend type generation

### CORS

Environment-aware CORS policies:
- **Development**: Allows `localhost:5173`, `localhost:3000`, `127.0.0.1:5173`, `localhost:8080`
- **Production/Staging**: Configured via `CORS:AllowedOrigins` environment variable

### Validation

FluentValidation for request validation with dependency injection integration.

### Content Sanitization

HtmlSanitizer for user-submitted HTML content (CMS, email templates).

---

## Logging and Observability

### Serilog

Two-stage initialization:
1. **Bootstrap logger**: Console-only, captures startup errors before DI is available
2. **Full configuration**: Console + PostgreSQL sink (non-development environments)

PostgreSQL sink writes to `logging.application_logs` table with custom columns:
- timestamp, level, message, exception, source_context, properties (JSONB)
- user_id, correlation_id, request_path, machine_name

### Middleware Pipeline

1. `CorrelationIdMiddleware` -- assigns correlation ID to all requests (before auth)
2. `UserContextMiddleware` -- enriches logs with authenticated user info (after auth)
3. `UseSerilogRequestLogging` -- structured request/response logging (after both)

### Sensitive Data

Serilog enricher masks: password, token, secret, key, authorization, cookie, nonce, creditcard.

### Health Checks

- `/health-check` -- ASP.NET Core health check (database connectivity via `AddNpgSql`)
- Feature-specific health endpoints under `/api/health/`

---

## Deployment Architecture

### Infrastructure

- **Host**: DigitalOcean Droplet
- **Database**: DigitalOcean Managed PostgreSQL (shared cluster across multiple apps)
- **Object Storage**: DigitalOcean Spaces (S3-compatible) for database backups
- **Container Registry**: DigitalOcean Container Registry (`registry.digitalocean.com/witchcityrope/`)
- **Reverse Proxy**: System-level Nginx with Let's Encrypt SSL
- **Deployment Automation**: Custom skills (`staging-deploy`, `production-deploy`)

### Environments

| Environment | Domain                    | Database                   | Deployment Path              |
|-------------|---------------------------|----------------------------|------------------------------|
| Development | localhost:5173            | Docker PostgreSQL (local)  | `./dev.sh`                   |
| Staging     | staging.notfai.com        | DO Managed (witchcityrope_staging) | `/opt/witchcityrope/staging/` |
| Production  | witchcityrope.com         | DO Managed (witchcityrope_production) | `/opt/witchcityrope/production/` |

### Container Images

| Image                                                         | Environment |
|---------------------------------------------------------------|-------------|
| `registry.digitalocean.com/witchcityrope/staging-api-witchcityrope`  | Staging     |
| `registry.digitalocean.com/witchcityrope/staging-web-witchcityrope`  | Staging     |
| `registry.digitalocean.com/witchcityrope/production-api-witchcityrope` | Production  |
| `registry.digitalocean.com/witchcityrope/production-web-witchcityrope` | Production  |

### Resource Limits (Staging/Production)

| Service | Memory Limit | CPU Limit | Memory Reserved |
|---------|-------------|-----------|-----------------|
| API     | 2 GB        | 2.0 cores | 512 MB          |
| Web     | 512 MB      | 0.5 cores | 128 MB          |
| Redis   | 256 MB      | 0.25 cores| --              |

### Maintenance Mode

Deployment skills support maintenance mode during deployments. Nginx serves a static maintenance page while containers are being updated.

---

## Development Environment

### Docker-Only Development

Local dev servers are **disabled** to prevent confusion between Docker and non-Docker environments. `npm run dev` will fail with an error message directing developers to use `./dev.sh`.

### Starting Development

```bash
# Start all services (database, API with hot reload, React with HMR)
./dev.sh

# Access points:
# React:      http://localhost:5173
# API:        http://localhost:5655
# Swagger:    http://localhost:5655/swagger (development only)
# Database:   localhost:5434 (postgres/devpass123)
```

### Hot Reload

- **React**: Vite HMR with polling-based file watching (required for Docker volume mounts)
- **API**: `dotnet watch run` with polling file watcher

### Test Accounts

Seven test accounts are seeded automatically. See [CLAUDE.md](./CLAUDE.md) for full credentials.

### Testing

- **React unit tests**: Vitest + Testing Library
- **E2E tests**: Playwright (in `tests/playwright/`)
- **Backend unit tests**: xUnit
- **Backend integration tests**: xUnit with Docker database

---

## Key Files

### Frontend

| File | Purpose |
|------|---------|
| `apps/web/src/App.tsx` | Root component: auth check, CSRF init, router |
| `apps/web/src/routes/router.tsx` | All route definitions with loaders/guards |
| `apps/web/src/stores/authStore.ts` | Zustand auth state management |
| `apps/web/src/stores/csrfStore.ts` | CSRF token management |
| `apps/web/vite.config.ts` | Vite config: proxy, HMR, build optimization |
| `apps/web/package.json` | Frontend dependencies |
| `packages/shared-types/` | Auto-generated TypeScript types from API |

### Backend

| File | Purpose |
|------|---------|
| `apps/api/Program.cs` | API bootstrap: DI, middleware pipeline, Hangfire, auth |
| `apps/api/Data/ApplicationDbContext.cs` | EF Core context, all DbSets, entity configuration |
| `apps/api/Services/DatabaseInitializationService.cs` | Auto-migration on startup |
| `apps/api/Services/SeedDataService.cs` | Development seed data |
| `apps/api/Features/Shared/Extensions/` | Feature service/endpoint registration |
| `apps/api/WitchCityRope.Api.csproj` | Backend dependencies |

### Infrastructure

| File | Purpose |
|------|---------|
| `docker-compose.yml` | Base service definitions |
| `docker-compose.dev.yml` | Development overrides (ports, hot reload, volumes) |
| `deployment/docker-compose.staging.yml` | Staging deployment config |
| `deployment/docker-compose.production.yml` | Production deployment config |
| `dev.sh` | Development environment management script |
| `apps/api/Dockerfile` | Multi-stage API Dockerfile |
| `apps/web/Dockerfile` | Multi-stage Web Dockerfile |

### Documentation

| File | Purpose |
|------|---------|
| `CLAUDE.md` | Developer configuration, test accounts, quick commands |
| `PROGRESS.md` | Current development status |
| `DOCKER_DEV_GUIDE.md` | Docker development workflow |
| `docs/architecture/functional-area-master-index.md` | Documentation navigation |

---

## History

WitchCityRope was originally built as a Blazor Server application. In August-September 2025, the frontend was migrated to React + TypeScript + Vite while the backend was rewritten as a .NET Minimal API with vertical slice architecture. The migration was completed in September 2025, and the legacy Blazor code has been archived. The platform reached production readiness in late 2025 and continues to receive feature enhancements.
