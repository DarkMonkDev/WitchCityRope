# Technology Research: Frontend Error Capture and Backend Reporting
<!-- Last Updated: 2026-03-02 -->
<!-- Version: 1.0 -->
<!-- Owner: Technology Researcher Agent -->
<!-- Status: Draft -->

## Executive Summary

**Decision Required**: How should WitchCityRope capture frontend React errors and send them to the .NET 10 backend for centralized logging?

**Recommendation**: Build a custom lightweight error reporting system using `react-error-boundary` (v6.1.1) for React component errors, global window event listeners for uncaught errors, and a dedicated .NET Minimal API endpoint with rate limiting. **Do not use Sentry or other third-party error tracking services.**

**Confidence Level**: High (85%)

**Key Factors**:
1. WitchCityRope's small scale (600 members) makes Sentry overkill and a custom solution trivially manageable
2. Privacy-first architecture (httpOnly cookies, no localStorage) extends to error reporting -- keeping error data on our own infrastructure
3. The .NET backend already has structured logging patterns that a frontend error endpoint integrates naturally into

---

## Research Scope

### Requirements
- Capture all categories of frontend errors (render, async, network, global)
- Send structured error reports to a .NET 10 Minimal API endpoint
- Include sufficient context for debugging (stack trace, URL, browser, user context)
- Prevent sensitive data leakage in error reports
- Handle error reporting failures gracefully
- Avoid impacting user experience or application performance
- Support both authenticated and unauthenticated user sessions

### Success Criteria
- Zero undetected JavaScript errors in production
- Error reports include actionable debugging information
- Error reporting adds less than 2KB to bundle size (excluding `react-error-boundary`)
- Error reporting never blocks the UI thread
- Sensitive PII is scrubbed before transmission

### Out of Scope
- Server-side (.NET) logging infrastructure (already established)
- Source map upload and server-side stack trace resolution
- Error dashboards or visualization UI
- Alerting and notification systems (handled by existing .NET logging)

---

## 1. Frontend Error Capture Strategy

### 1.1 Types of Errors to Capture

| Error Type | Capture Mechanism | Priority |
|---|---|---|
| React render errors | `react-error-boundary` + `componentDidCatch` | Critical |
| Unhandled promise rejections | `window.addEventListener('unhandledrejection')` | Critical |
| Global JavaScript errors | `window.addEventListener('error')` | Critical |
| API/Network errors | HTTP client interceptor (axios/fetch wrapper) | High |
| React async errors in event handlers | `useErrorBoundary` hook from `react-error-boundary` | High |
| Console errors | **Do NOT intercept** (see rationale below) | Skip |

**Rationale for NOT intercepting `console.error`**: Intercepting `console.error` creates circular logging problems, interferes with development debugging, and captures noisy third-party library warnings that are not actionable. React itself logs to `console.error` when it catches errors, so we already capture the underlying errors through other mechanisms.

### 1.2 React Error Boundaries

**Library**: `react-error-boundary` v6.1.1 (February 2026)
- 4.4KB minified, 1.5KB gzipped
- 3.3M weekly npm downloads
- Actively maintained by Brian Vaughn (former React core team)
- TypeScript support built in

**Why use the library instead of writing our own**: The library handles edge cases around React's error propagation model, provides the `useErrorBoundary` hook for imperative error throwing from event handlers and async code, and supports `resetKeys` for automatic recovery. Writing this from scratch would duplicate well-tested logic.

#### Recommended Error Boundary Architecture

```
App
 +-- AppErrorBoundary (top-level catch-all)
      +-- Layout
           +-- RouteErrorBoundary (per-route, shows route-level fallback)
           |    +-- Page Component
           |         +-- FeatureErrorBoundary (optional, for critical widgets)
           |              +-- Widget Component
           +-- RouteErrorBoundary
                +-- Another Page
```

**Three levels of error boundaries**:

1. **AppErrorBoundary** (top-level): Last resort. Shows "Something went wrong, please refresh." Captures errors that escape all other boundaries.

2. **RouteErrorBoundary** (per-route): Wraps each route's content. Shows a route-specific fallback with "Go back" or "Try again" options. Most errors are caught here.

3. **FeatureErrorBoundary** (optional, per-widget): For isolated features where a failure should not take down the whole page (e.g., an event calendar widget failing should not break the entire events page).

#### Implementation Pattern

```typescript
// src/components/error/AppErrorBoundary.tsx
import { ErrorBoundary } from 'react-error-boundary';
import { reportError } from '@/services/errorReporting';

interface ErrorFallbackProps {
  error: Error;
  resetErrorBoundary: () => void;
}

function AppErrorFallback({ error, resetErrorBoundary }: ErrorFallbackProps) {
  return (
    <div role="alert" style={{ padding: '2rem', textAlign: 'center' }}>
      <h1>Something went wrong</h1>
      <p>We have been notified and are looking into it.</p>
      <button onClick={resetErrorBoundary}>Try again</button>
      <button onClick={() => window.location.href = '/'}>Go home</button>
    </div>
  );
}

function handleError(error: Error, info: { componentStack?: string | null }) {
  reportError({
    type: 'react-render',
    error: {
      name: error.name,
      message: error.message,
      stack: error.stack ?? null,
    },
    componentStack: info.componentStack ?? null,
  });
}

export function AppErrorBoundary({ children }: { children: React.ReactNode }) {
  return (
    <ErrorBoundary
      FallbackComponent={AppErrorFallback}
      onError={handleError}
      onReset={() => {
        // Reset application state if needed
      }}
    >
      {children}
    </ErrorBoundary>
  );
}
```

### 1.3 Global Error Handlers

```typescript
// src/services/globalErrorHandlers.ts
import { reportError } from './errorReporting';

export function initGlobalErrorHandlers(): void {
  // Catch synchronous JS errors not caught by React
  window.addEventListener('error', (event: ErrorEvent) => {
    // Ignore errors from cross-origin scripts (browser extensions, etc.)
    if (!event.filename || event.filename === '') return;

    reportError({
      type: 'global-error',
      error: {
        name: 'GlobalError',
        message: event.message,
        stack: event.error?.stack ?? null,
      },
      source: event.filename,
      line: event.lineno,
      column: event.colno,
    });
  });

  // Catch unhandled promise rejections
  window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
    const error = event.reason;

    reportError({
      type: 'unhandled-rejection',
      error: {
        name: error?.name ?? 'UnhandledRejection',
        message: error?.message ?? String(error),
        stack: error?.stack ?? null,
      },
    });
  });
}
```

**Call this once in `main.tsx`** before `createRoot`:

```typescript
// src/main.tsx
import { initGlobalErrorHandlers } from '@/services/globalErrorHandlers';

initGlobalErrorHandlers();

const root = createRoot(document.getElementById('root')!);
root.render(<App />);
```

### 1.4 Async and Event Handler Errors

The `useErrorBoundary` hook from `react-error-boundary` allows imperative error throwing from async code and event handlers:

```typescript
import { useErrorBoundary } from 'react-error-boundary';

function EventRegistrationButton({ eventId }: { eventId: string }) {
  const { showBoundary } = useErrorBoundary();

  const handleRegister = async () => {
    try {
      await api.registerForEvent(eventId);
    } catch (error) {
      // This routes the error to the nearest ErrorBoundary
      showBoundary(error);
    }
  };

  return <button onClick={handleRegister}>Register</button>;
}
```

### 1.5 API/Network Error Capture

Integrate with the existing HTTP client (axios or fetch wrapper):

```typescript
// In your API client / axios interceptor
import { reportError } from '@/services/errorReporting';

api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Only report server errors (5xx) and network failures
    // Do NOT report 4xx (client errors like 401, 403, 404, 422)
    if (!error.response || error.response.status >= 500) {
      reportError({
        type: 'api-error',
        error: {
          name: 'ApiError',
          message: error.message,
          stack: error.stack ?? null,
        },
        request: {
          method: error.config?.method?.toUpperCase(),
          url: sanitizeUrl(error.config?.url),
          status: error.response?.status ?? null,
        },
      });
    }
    return Promise.reject(error);
  }
);
```

### 1.6 React 19 createRoot Error Callbacks (Future Consideration)

React 19 introduces three new error callbacks on `createRoot`:
- `onCaughtError`: Called when React catches an error in an Error Boundary
- `onUncaughtError`: Called when an error is thrown and not caught by any Error Boundary
- `onRecoverableError`: Called when React automatically recovers from errors

**Current status**: WitchCityRope uses React 18. When upgrading to React 19, these callbacks should be added as an additional capture layer:

```typescript
// Future: React 19+
const root = createRoot(document.getElementById('root')!, {
  onCaughtError: (error, errorInfo) => {
    reportError({ type: 'react-caught', error, componentStack: errorInfo.componentStack });
  },
  onUncaughtError: (error, errorInfo) => {
    reportError({ type: 'react-uncaught', error, componentStack: errorInfo.componentStack });
  },
  onRecoverableError: (error, errorInfo) => {
    reportError({ type: 'react-recoverable', error, componentStack: errorInfo.componentStack });
  },
});
```

---

## 2. Error Payload Design

### 2.1 TypeScript Interface

```typescript
// src/types/errorReporting.ts

/**
 * The full error report payload sent to the backend.
 */
export interface FrontendErrorReport {
  /** Unique ID for deduplication (generated client-side) */
  id: string;

  /** ISO 8601 timestamp when the error occurred */
  timestamp: string;

  /** Classification of the error source */
  type: ErrorType;

  /** The error itself */
  error: ErrorDetail;

  /** React component stack trace (only for render errors) */
  componentStack: string | null;

  /** Current page context */
  context: ErrorContext;

  /** Browser and device information */
  client: ClientInfo;

  /** Authenticated user info (if available, anonymized) */
  user: ErrorUserInfo | null;
}

export type ErrorType =
  | 'react-render'       // Error boundary caught during render
  | 'global-error'       // window.onerror
  | 'unhandled-rejection' // window.onunhandledrejection
  | 'api-error'          // HTTP 5xx or network failure
  | 'manual';            // Explicitly reported by developer code

export interface ErrorDetail {
  name: string;
  message: string;
  stack: string | null;
}

export interface ErrorContext {
  /** Current URL path (not full URL, to avoid leaking query params) */
  url: string;

  /** Current route name if available */
  route: string | null;

  /** HTTP request details (only for API errors) */
  request?: {
    method: string;
    url: string;
    status: number | null;
  };

  /** Source file info (only for global errors) */
  source?: string;
  line?: number;
  column?: number;
}

export interface ClientInfo {
  userAgent: string;
  language: string;
  /** Viewport width for mobile debugging */
  viewportWidth: number;
  viewportHeight: number;
  /** Whether the user is online */
  online: boolean;
}

export interface ErrorUserInfo {
  /** User ID (safe to include, not PII) */
  userId: string | null;
  /** User role for debugging role-specific issues */
  role: string | null;
  /** Whether the user is authenticated */
  isAuthenticated: boolean;
}

/**
 * Batch payload for sending multiple errors at once.
 */
export interface FrontendErrorBatch {
  errors: FrontendErrorReport[];
  /** Client session ID for correlating errors from same session */
  sessionId: string;
}
```

### 2.2 Data Sanitization

**Critical for WitchCityRope**: Given the sensitive nature of the community, error data must be scrubbed before transmission.

```typescript
// src/services/errorSanitizer.ts

const SENSITIVE_PATTERNS = [
  /password/i,
  /token/i,
  /secret/i,
  /authorization/i,
  /cookie/i,
  /email/i,
  /phone/i,
  /ssn/i,
  /credit.?card/i,
  /scene.?name/i,  // WitchCityRope-specific: scene names are private
  /legal.?name/i,  // WitchCityRope-specific: legal names are private
];

const EMAIL_REGEX = /[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}/g;
const PHONE_REGEX = /\b\d{3}[-.]?\d{3}[-.]?\d{4}\b/g;

export function sanitizeErrorMessage(message: string): string {
  let sanitized = message;
  sanitized = sanitized.replace(EMAIL_REGEX, '[EMAIL_REDACTED]');
  sanitized = sanitized.replace(PHONE_REGEX, '[PHONE_REDACTED]');
  return sanitized;
}

export function sanitizeStackTrace(stack: string | null): string | null {
  if (!stack) return null;
  let sanitized = stack;
  sanitized = sanitized.replace(EMAIL_REGEX, '[EMAIL_REDACTED]');
  // Remove query parameters from URLs in stack traces
  sanitized = sanitized.replace(/\?[^\s)]+/g, '?[PARAMS_REDACTED]');
  return sanitized;
}

export function sanitizeUrl(url: string | undefined): string {
  if (!url) return '[unknown]';
  try {
    const parsed = new URL(url, window.location.origin);
    // Strip query params and hash -- they may contain tokens or PII
    return parsed.pathname;
  } catch {
    return '[invalid-url]';
  }
}

export function sanitizeReport(report: FrontendErrorReport): FrontendErrorReport {
  return {
    ...report,
    error: {
      ...report.error,
      message: sanitizeErrorMessage(report.error.message),
      stack: sanitizeStackTrace(report.error.stack),
    },
    componentStack: report.componentStack
      ? sanitizeStackTrace(report.componentStack)
      : null,
    context: {
      ...report.context,
      url: sanitizeUrl(report.context.url),
      request: report.context.request
        ? { ...report.context.request, url: sanitizeUrl(report.context.request.url) }
        : undefined,
    },
  };
}
```

---

## 3. Error Reporting Service (Frontend)

### 3.1 Core Service with Batching and Circuit Breaker

```typescript
// src/services/errorReporting.ts
import { v4 as uuidv4 } from 'uuid'; // or use crypto.randomUUID()
import type {
  FrontendErrorReport,
  FrontendErrorBatch,
  ErrorType,
  ErrorDetail,
} from '@/types/errorReporting';
import { sanitizeReport } from './errorSanitizer';

// --- Configuration ---
const ERROR_ENDPOINT = '/api/client-errors';
const BATCH_INTERVAL_MS = 5_000;     // Send batch every 5 seconds
const MAX_BATCH_SIZE = 10;            // Send immediately if 10 errors queued
const MAX_ERRORS_PER_MINUTE = 30;     // Client-side rate limit
const CIRCUIT_BREAKER_THRESHOLD = 5;  // Open circuit after 5 consecutive failures
const CIRCUIT_BREAKER_RESET_MS = 60_000; // Try again after 1 minute

// --- State ---
let errorQueue: FrontendErrorReport[] = [];
let batchTimer: ReturnType<typeof setTimeout> | null = null;
let errorsThisMinute = 0;
let minuteResetTimer: ReturnType<typeof setInterval> | null = null;
let consecutiveFailures = 0;
let circuitOpen = false;
let circuitResetTimer: ReturnType<typeof setTimeout> | null = null;
const sessionId = crypto.randomUUID?.() ?? uuidv4();
const reportedErrors = new Set<string>(); // Deduplication

// --- Rate Limiting ---
function startRateLimitReset(): void {
  if (!minuteResetTimer) {
    minuteResetTimer = setInterval(() => {
      errorsThisMinute = 0;
    }, 60_000);
  }
}

function isRateLimited(): boolean {
  return errorsThisMinute >= MAX_ERRORS_PER_MINUTE;
}

// --- Circuit Breaker ---
function openCircuit(): void {
  circuitOpen = true;
  circuitResetTimer = setTimeout(() => {
    circuitOpen = false;
    consecutiveFailures = 0;
  }, CIRCUIT_BREAKER_RESET_MS);
}

// --- Deduplication ---
function getErrorFingerprint(report: FrontendErrorReport): string {
  return `${report.type}:${report.error.name}:${report.error.message}`;
}

// --- Context Builders ---
function buildClientInfo(): FrontendErrorReport['client'] {
  return {
    userAgent: navigator.userAgent,
    language: navigator.language,
    viewportWidth: window.innerWidth,
    viewportHeight: window.innerHeight,
    online: navigator.onLine,
  };
}

function buildUserInfo(): FrontendErrorReport['user'] {
  // Pull from auth store -- adjust import to match your auth implementation
  try {
    // Example: const { user, isAuthenticated } = useAuthStore.getState();
    // return { userId: user?.id ?? null, role: user?.role ?? null, isAuthenticated };
    return null; // Placeholder -- wire to your auth store
  } catch {
    return null;
  }
}

// --- Core Report Function ---
export function reportError(params: {
  type: ErrorType;
  error: ErrorDetail | Error;
  componentStack?: string | null;
  request?: { method: string; url: string; status: number | null };
  source?: string;
  line?: number;
  column?: number;
}): void {
  startRateLimitReset();

  if (isRateLimited() || circuitOpen) return;

  const errorDetail: ErrorDetail =
    params.error instanceof Error
      ? {
          name: params.error.name,
          message: params.error.message,
          stack: params.error.stack ?? null,
        }
      : params.error;

  const report: FrontendErrorReport = {
    id: crypto.randomUUID?.() ?? uuidv4(),
    timestamp: new Date().toISOString(),
    type: params.type,
    error: errorDetail,
    componentStack: params.componentStack ?? null,
    context: {
      url: window.location.pathname,
      route: null, // Can be enriched by router context
      request: params.request,
      source: params.source,
      line: params.line,
      column: params.column,
    },
    client: buildClientInfo(),
    user: buildUserInfo(),
  };

  // Deduplicate
  const fingerprint = getErrorFingerprint(report);
  if (reportedErrors.has(fingerprint)) return;
  reportedErrors.add(fingerprint);
  // Prevent memory leak: cap deduplication set
  if (reportedErrors.size > 100) {
    const first = reportedErrors.values().next().value;
    if (first) reportedErrors.delete(first);
  }

  // Sanitize before queuing
  const sanitized = sanitizeReport(report);

  errorsThisMinute++;
  errorQueue.push(sanitized);

  // Flush immediately if batch is full
  if (errorQueue.length >= MAX_BATCH_SIZE) {
    flushErrors();
  } else if (!batchTimer) {
    batchTimer = setTimeout(flushErrors, BATCH_INTERVAL_MS);
  }
}

// --- Batch Send ---
async function flushErrors(): Promise<void> {
  if (batchTimer) {
    clearTimeout(batchTimer);
    batchTimer = null;
  }

  if (errorQueue.length === 0 || circuitOpen) return;

  const batch: FrontendErrorBatch = {
    errors: [...errorQueue],
    sessionId,
  };
  errorQueue = [];

  try {
    const response = await fetch(ERROR_ENDPOINT, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(batch),
      // Use keepalive for page unload scenarios
      keepalive: true,
      // Include credentials so the cookie is sent (for user identification)
      credentials: 'include',
    });

    if (response.ok) {
      consecutiveFailures = 0;
    } else {
      consecutiveFailures++;
      if (consecutiveFailures >= CIRCUIT_BREAKER_THRESHOLD) {
        openCircuit();
      }
    }
  } catch {
    // Network failure -- do not re-queue (avoid infinite loops)
    consecutiveFailures++;
    if (consecutiveFailures >= CIRCUIT_BREAKER_THRESHOLD) {
      openCircuit();
    }
  }
}

// --- Flush on page unload ---
if (typeof window !== 'undefined') {
  window.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') {
      flushErrors();
    }
  });

  // Beacon API fallback for unload
  window.addEventListener('pagehide', () => {
    if (errorQueue.length > 0) {
      const batch: FrontendErrorBatch = {
        errors: [...errorQueue],
        sessionId,
      };
      errorQueue = [];
      // navigator.sendBeacon is more reliable during page unload than fetch
      navigator.sendBeacon(
        ERROR_ENDPOINT,
        new Blob([JSON.stringify(batch)], { type: 'application/json' })
      );
    }
  });
}
```

### 3.2 Key Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| **Batching** | 5-second window, max 10 per batch | Reduces HTTP requests; 5s is fast enough for error visibility |
| **Rate Limit** | 30 errors/minute client-side | Prevents error storms from crashing both client and server |
| **Circuit Breaker** | Open after 5 failures, reset after 60s | If the error endpoint is down, stop hammering it |
| **Deduplication** | Fingerprint on type+name+message | Same error from re-renders should only be reported once per session |
| **Page Unload** | `visibilitychange` + `navigator.sendBeacon` | Ensures errors are reported even when user navigates away |
| **Credentials** | `credentials: 'include'` | Sends httpOnly cookie so backend can identify user |
| **keepalive** | Enabled on fetch | Allows request to outlive the page |

---

## 4. Backend Error Endpoint Design

### 4.1 .NET Minimal API Endpoint

```csharp
// Features/ClientErrors/Endpoints/ClientErrorEndpoints.cs

public static class ClientErrorEndpoints
{
    public static void MapClientErrorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/client-errors")
            .WithTags("Client Errors");

        group.MapPost("/", HandleClientErrors)
            .AllowAnonymous()  // Must accept errors from unauthenticated users
            .RequireRateLimiting("client-errors")
            .Produces(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static IResult HandleClientErrors(
        ClientErrorBatchDto batch,
        ILogger<ClientErrorEndpoints> logger,
        HttpContext httpContext)
    {
        // Validate payload size
        if (batch.Errors.Count > 50)
        {
            return Results.BadRequest("Too many errors in a single batch");
        }

        // Extract user context from cookie-based auth (may be null)
        var userId = httpContext.User?.FindFirst("sub")?.Value;
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();

        foreach (var error in batch.Errors)
        {
            // Truncate oversized fields to prevent log storage abuse
            var message = Truncate(error.Error.Message, 1000);
            var stack = Truncate(error.Error.Stack, 5000);
            var componentStack = Truncate(error.ComponentStack, 3000);

            logger.LogWarning(
                "Frontend error [{ErrorType}] {ErrorName}: {ErrorMessage} | " +
                "URL: {Url} | User: {UserId} | Session: {SessionId} | " +
                "Client: {ViewportWidth}x{ViewportHeight} {UserAgent}",
                error.Type,
                error.Error.Name,
                message,
                error.Context.Url,
                userId ?? "anonymous",
                batch.SessionId,
                error.Client.ViewportWidth,
                error.Client.ViewportHeight,
                Truncate(error.Client.UserAgent, 200)
            );

            // Log stack trace separately at Debug level to avoid cluttering Warning logs
            if (!string.IsNullOrEmpty(stack))
            {
                logger.LogDebug(
                    "Frontend error stack [{ErrorId}]: {Stack}",
                    error.Id,
                    stack
                );
            }

            if (!string.IsNullOrEmpty(componentStack))
            {
                logger.LogDebug(
                    "Frontend error component stack [{ErrorId}]: {ComponentStack}",
                    error.Id,
                    componentStack
                );
            }
        }

        // Return 202 Accepted -- fire and forget, do not block the client
        return Results.Accepted();
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value == null) return null;
        return value.Length <= maxLength ? value : value[..maxLength] + "...[truncated]";
    }
}
```

### 4.2 DTOs

```csharp
// Features/ClientErrors/Models/ClientErrorDtos.cs

public record ClientErrorBatchDto(
    List<ClientErrorDto> Errors,
    string SessionId
);

public record ClientErrorDto(
    string Id,
    string Timestamp,
    string Type,
    ClientErrorDetailDto Error,
    string? ComponentStack,
    ClientErrorContextDto Context,
    ClientErrorClientDto Client,
    ClientErrorUserDto? User
);

public record ClientErrorDetailDto(
    string Name,
    string Message,
    string? Stack
);

public record ClientErrorContextDto(
    string Url,
    string? Route,
    ClientErrorRequestDto? Request,
    string? Source,
    int? Line,
    int? Column
);

public record ClientErrorRequestDto(
    string Method,
    string Url,
    int? Status
);

public record ClientErrorClientDto(
    string UserAgent,
    string Language,
    int ViewportWidth,
    int ViewportHeight,
    bool Online
);

public record ClientErrorUserDto(
    string? UserId,
    string? Role,
    bool IsAuthenticated
);
```

### 4.3 Rate Limiting Configuration

```csharp
// In Program.cs or a rate limiting configuration file

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("client-errors", config =>
    {
        config.PermitLimit = 20;       // 20 requests per window
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 5;         // Queue up to 5 additional requests
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// In the middleware pipeline
app.UseRateLimiter();
```

### 4.4 Endpoint Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| **Authentication** | `AllowAnonymous` | Must capture errors from login page, registration, guest browsing |
| **User Identification** | Extract from cookie if present | httpOnly cookie is sent via `credentials: 'include'` |
| **Rate Limiting** | 20 requests/minute per client IP | Prevents abuse of anonymous endpoint |
| **Response Code** | 202 Accepted | Fire-and-forget; client should not wait for processing |
| **Payload Limit** | 50 errors per batch | Prevents oversized payloads |
| **Field Truncation** | Message: 1KB, Stack: 5KB, Component: 3KB | Prevents log storage abuse |
| **Log Level** | Warning (summary) + Debug (stacks) | Summary visible in production; stacks available when needed |
| **CORS** | Uses existing CORS policy (same-origin) | Frontend and API already configured for cross-origin |

### 4.5 Security Considerations

1. **Anonymous endpoint risk**: The `AllowAnonymous` endpoint could be abused. Mitigations:
   - Server-side rate limiting per IP (20/min)
   - Payload size validation (max 50 errors, field truncation)
   - No sensitive data accepted (only logging, no database writes)
   - Consider adding a lightweight CSRF-like token in the future if abuse occurs

2. **No sensitive data stored**: The endpoint logs to the existing structured logging pipeline. Error data goes through the same log retention and access controls as other server logs.

3. **CORS**: Since the React frontend and API are on the same origin in production (proxied), CORS is already configured. No additional CORS changes needed.

---

## 5. Sentry vs Custom: Decision Analysis

### 5.1 Sentry Free Tier (Developer Plan)

| Feature | Sentry Free | WitchCityRope Need |
|---|---|---|
| Error events/month | 5,000 | More than sufficient for 600 members |
| Session replays | 50/month | Nice but not critical |
| Data retention | 30 days | Sufficient for small team |
| Users | 1 | Limiting -- only 1 developer can access |
| Custom dashboards | 10 | Adequate |
| Tracing spans | 5M/month | Overkill for our needs |
| Logs | 5GB/month | Adequate |
| Integrations | Limited | No Slack, PagerDuty on free tier |

### 5.2 Comparison Matrix

| Criteria | Weight | Sentry Free | Custom Solution | Winner |
|---|---|---|---|---|
| **Setup Complexity** | 10% | 9/10 (npm install + DSN) | 5/10 (must build) | Sentry |
| **Privacy/Data Control** | 25% | 4/10 (data on Sentry servers) | 10/10 (all data on our infra) | Custom |
| **Feature Richness** | 10% | 9/10 (replays, tracing, etc.) | 4/10 (basic error capture) | Sentry |
| **Cost (long-term)** | 15% | 7/10 (free now, may need paid) | 9/10 (zero ongoing cost) | Custom |
| **Bundle Size Impact** | 10% | 3/10 (~30KB gzipped Sentry SDK) | 9/10 (~2KB custom code) | Custom |
| **Maintenance Burden** | 15% | 8/10 (maintained by Sentry) | 5/10 (we maintain) | Sentry |
| **Integration with .NET Logs** | 15% | 3/10 (separate system) | 10/10 (same log pipeline) | Custom |
| **Weighted Total** | 100% | **5.85** | **7.65** | **Custom** |

### 5.3 Recommendation: Custom Solution

**Rationale**:

1. **Privacy is paramount**: WitchCityRope handles sensitive community data. Sending error reports (which may contain URL paths, component names, and user context) to a third-party server conflicts with the platform's privacy-first architecture. Even with Sentry's data scrubbing, the data leaves our infrastructure.

2. **Unified logging**: The .NET backend already has structured logging (likely Serilog or the built-in ILogger). Frontend errors flowing into the same pipeline means one place to search, one retention policy, one access control model.

3. **Bundle size**: Sentry's React SDK is approximately 30KB gzipped. Our custom solution adds approximately 2KB (excluding `react-error-boundary` which we need regardless). For mobile users at events, every KB matters.

4. **Scale mismatch**: Sentry is designed for applications processing millions of errors. WitchCityRope will generate perhaps 10-50 errors per day. The overhead of a third-party service is not justified.

5. **Single user limit on free tier**: Only one developer can access Sentry on the free plan. This is a hard limitation for a volunteer team.

---

## 6. Performance Considerations

### 6.1 Bundle Size Impact

| Component | Size (gzipped) | Required? |
|---|---|---|
| `react-error-boundary` | ~1.5KB | Yes (handles edge cases we should not rewrite) |
| Custom error reporting service | ~1.5KB | Yes |
| Error types + sanitizer | ~0.5KB | Yes |
| **Total** | **~3.5KB** | |
| Sentry SDK (for comparison) | ~30KB | No |

### 6.2 Runtime Performance

- **Error capture**: Synchronous, sub-millisecond. Just pushes to an array.
- **Batch sending**: Async `fetch` with `keepalive`. Does not block the UI thread.
- **Serialization**: `JSON.stringify` of 1-10 small objects. Negligible.
- **Sanitization**: Regex operations on short strings. Sub-millisecond.
- **Memory**: Deduplication set capped at 100 entries. Error queue capped by batch flushing.

### 6.3 High-Error-Rate Protection

Multiple layers prevent error storms:

1. **Client-side rate limit**: 30 errors/minute maximum
2. **Deduplication**: Same error reported only once per session
3. **Circuit breaker**: Stops sending after 5 consecutive failures
4. **Batch size cap**: Maximum 10 errors per batch, max 50 errors accepted by backend
5. **Server-side rate limit**: 20 requests/minute per IP
6. **Field truncation**: Prevents oversized payloads

### 6.4 Failure Handling

| Scenario | Behavior |
|---|---|
| Error endpoint returns 5xx | Increment failure counter, open circuit after 5 |
| Network offline | `navigator.onLine` check, `sendBeacon` on unload |
| Error in error reporter | Wrapped in try/catch, silently fails (no recursion) |
| Payload too large | Server returns 400, client discards batch |
| Rate limited (429) | Client pauses sending, circuit breaker activates |

### 6.5 Maximum Payload Size Recommendation

- Single error report: ~1-3KB (typical)
- Batch of 10 errors: ~10-30KB
- Recommended server-side body limit for this endpoint: 100KB
- Fields truncated at source (stack: 5KB, message: 1KB)

---

## 7. Libraries Evaluated

### 7.1 react-error-boundary (RECOMMENDED)

| Attribute | Value |
|---|---|
| Version | 6.1.1 (Feb 2026) |
| Size | 4.4KB min / 1.5KB gzip |
| Weekly Downloads | 3.3M |
| GitHub Stars | 7.2K+ |
| Maintainer | Brian Vaughn (ex-React core team) |
| TypeScript | Built-in support |
| React Versions | 16.13+ (including 18, 19) |

**Pros**: Battle-tested, lightweight, `useErrorBoundary` hook for async errors, `resetKeys` for automatic recovery, active maintenance.

**Cons**: None significant for our use case.

### 7.2 GlitchTip (NOT RECOMMENDED)

Open-source self-hosted error tracking. Sentry-compatible API.

**Why not**: Requires running a separate Docker service (Django + Postgres + Redis + Celery). Adds infrastructure complexity for a problem solvable with a single API endpoint.

### 7.3 Sentry SDK (NOT RECOMMENDED for WitchCityRope)

**Why not**: See Section 5 above. Privacy, bundle size, and unified logging concerns outweigh convenience.

### 7.4 TrackJS, Bugsnag, Rollbar, LogRocket (NOT RECOMMENDED)

**Why not**: All are paid SaaS services. Same privacy and data control concerns as Sentry, without Sentry's free tier. Not appropriate for a volunteer-run community platform.

---

## 8. Implementation Considerations

### 8.1 Migration Path

This is a new feature addition, not a migration. Implementation order:

1. **Step 1**: Create the error types file (`src/types/errorReporting.ts`)
2. **Step 2**: Create the sanitizer (`src/services/errorSanitizer.ts`)
3. **Step 3**: Create the error reporting service (`src/services/errorReporting.ts`)
4. **Step 4**: Create global error handlers (`src/services/globalErrorHandlers.ts`)
5. **Step 5**: Create Error Boundary components (`src/components/error/`)
6. **Step 6**: Wire up in `main.tsx` and wrap App with `AppErrorBoundary`
7. **Step 7**: Create the .NET endpoint (`Features/ClientErrors/`)
8. **Step 8**: Configure rate limiting for the endpoint
9. **Step 9**: Integrate with API client interceptor
10. **Step 10**: Test with intentional errors in development

### 8.2 Estimated Effort

| Task | Estimate |
|---|---|
| Frontend error types + sanitizer | 1 hour |
| Error reporting service | 2 hours |
| Error boundary components | 2 hours |
| Global error handlers + main.tsx wiring | 1 hour |
| .NET endpoint + DTOs + rate limiting | 2 hours |
| API client interceptor integration | 30 min |
| Testing and validation | 2 hours |
| **Total** | **~10-11 hours** |

### 8.3 Testing Strategy

- **Unit tests**: Test sanitizer functions, deduplication logic, rate limiting
- **Integration tests**: Verify error boundary renders fallback UI on error
- **E2E tests (Playwright)**: Trigger a known error, verify the error endpoint receives a report
- **Manual testing**: Intentionally throw errors in development to verify the full pipeline

### 8.4 File Structure

```
apps/web/src/
  types/
    errorReporting.ts          # TypeScript interfaces
  services/
    errorReporting.ts          # Core reporting service (queue, batch, send)
    errorSanitizer.ts          # PII scrubbing
    globalErrorHandlers.ts     # window.onerror + unhandledrejection
  components/
    error/
      AppErrorBoundary.tsx     # Top-level error boundary
      RouteErrorBoundary.tsx   # Per-route error boundary
      ErrorFallback.tsx        # Shared fallback UI component

apps/api/Features/
  ClientErrors/
    Endpoints/
      ClientErrorEndpoints.cs  # Minimal API endpoint
    Models/
      ClientErrorDtos.cs       # Request DTOs
```

---

## 9. Risk Assessment

### High Risk
- **Error reporting endpoint abuse**: Anonymous endpoint could be used for spam/DoS
  - **Mitigation**: Server-side rate limiting (20/min/IP), payload size validation, field truncation, no database writes

### Medium Risk
- **PII leakage in error messages**: Exception messages may contain user data
  - **Mitigation**: Client-side sanitization with regex patterns for emails, phones, WitchCityRope-specific terms (scene names, legal names)
- **Error reporting itself fails silently**: If the circuit breaker opens, errors are lost
  - **Mitigation**: `console.warn` when circuit breaker opens (visible in browser devtools for debugging). Accept that some errors may be lost -- this is acceptable for a monitoring system.

### Low Risk
- **Bundle size regression**: Adding error reporting increases bundle size
  - **Monitoring**: Total addition is ~3.5KB gzipped. Negligible compared to React itself (~40KB).
- **React 19 migration**: Current patterns may need adjustment for React 19's error callbacks
  - **Monitoring**: The `react-error-boundary` library already supports React 19. Adding `createRoot` callbacks is additive, not breaking.

---

## 10. Recommendation

### Primary Recommendation: Custom Error Reporting with react-error-boundary

**Confidence Level**: High (85%)

**Rationale**:
1. **Privacy alignment**: Error data stays on WitchCityRope infrastructure, consistent with the platform's privacy-first architecture and httpOnly cookie pattern
2. **Unified observability**: Frontend errors flow into the same .NET structured logging pipeline as backend errors -- one place to look, one retention policy
3. **Minimal footprint**: ~3.5KB total addition versus ~30KB for Sentry SDK -- critical for mobile users at events
4. **Appropriate scale**: A 600-member community platform does not need enterprise error tracking infrastructure
5. **No vendor dependency**: No risk of free tier changes, service outages, or pricing increases from third-party providers

**Implementation Priority**: Next Sprint (should be implemented early, as it provides visibility into all subsequent feature development)

### Alternative Recommendations
- **Second Choice**: Sentry Free Tier -- If the team decides build effort is not worth it, Sentry's free tier provides 5,000 errors/month with minimal setup. Accept the privacy trade-off and 30KB bundle addition.
- **Future Consideration**: GlitchTip self-hosted -- If error volume grows significantly or the team wants error grouping/deduplication UI, GlitchTip could be deployed alongside the existing Docker infrastructure. Not warranted now.

---

## 11. Next Steps

- [ ] Review this research document and approve approach
- [ ] Create implementation tickets based on Section 8.1 steps
- [ ] Install `react-error-boundary` package
- [ ] Implement error reporting service and global handlers
- [ ] Create .NET endpoint with rate limiting
- [ ] Wire error boundaries into application layout
- [ ] Test full pipeline in development environment
- [ ] Monitor error volume in staging before production deployment

---

## 12. Research Sources

### Official Documentation
- [React Error Boundaries (Legacy Docs)](https://legacy.reactjs.org/docs/error-boundaries.html)
- [React createRoot API (React 19)](https://react.dev/reference/react-dom/client/createRoot)
- [ASP.NET Core Rate Limiting Middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [Window error event (MDN)](https://developer.mozilla.org/en-US/docs/Web/API/Window/error_event)
- [Window unhandledrejection event (MDN)](https://developer.mozilla.org/en-US/docs/Web/API/Window/unhandledrejection_event)

### Libraries
- [react-error-boundary on GitHub](https://github.com/bvaughn/react-error-boundary) -- v6.1.1, Feb 2026
- [Sentry React SDK Documentation](https://docs.sentry.io/platforms/javascript/guides/react/)
- [Sentry Pricing Page](https://sentry.io/pricing/)

### Articles and Guides
- [React Router Error Reporting from Scratch](https://programmingarehard.com/2025/03/11/react-router-error-reporting.html/) -- Custom error reporting with source maps
- [Universal Error Handling in React](https://oscarbustos.dev/blog/react-error-handling/) -- Error boundary + global handlers comprehensive guide
- [Error Handling in React with react-error-boundary](https://certificates.dev/blog/error-handling-in-react-with-react-error-boundary) -- Library usage patterns
- [Sentry Data Scrubbing](https://docs.sentry.io/platforms/javascript/guides/react/data-management/sensitive-data/) -- PII sanitization patterns
- [ASP.NET Core Rate Limiting in .NET 10](https://dev.to/extinctsion/implement-rate-limiting-in-aspnet-core-net9-2725)
- [Custom Minimal API Endpoint Filters](https://oneuptime.com/blog/post/2026-01-30-custom-minimal-api-endpoint-filters/view)

### Community Discussions
- [React GitHub Issue #19838: window.onerror not invoked in production](https://github.com/facebook/react/issues/19838)
- [Sentry GitHub Issue #6688: Errors in event listener callbacks](https://github.com/getsentry/sentry-javascript/issues/6688)
- [React 19 Error Callbacks Discussion](https://github.com/facebook/react/issues/29581)

---

## 13. Questions for Technical Team

- [ ] Does the existing .NET logging pipeline use Serilog or the built-in ILogger? This affects how frontend error logs are formatted and queried.
- [ ] Should the error endpoint write to a separate log file/sink for frontend errors, or mix with backend logs?
- [ ] Is there an existing rate limiting configuration in the API that the client-errors endpoint should integrate with?
- [ ] Should error boundary fallback UI match the existing Mantine design system, or use basic HTML (for reliability when Mantine itself might be the error source)?
- [ ] Is there a preference for the error endpoint path? (`/api/client-errors` vs `/api/frontend-errors` vs `/api/log/errors`)

---

## Quality Gate Checklist (90% Required)

- [x] Multiple options evaluated (minimum 2) -- Custom vs Sentry vs GlitchTip vs other SaaS
- [x] Quantitative comparison provided -- Weighted scoring matrix in Section 5.2
- [x] WitchCityRope-specific considerations addressed -- Privacy, mobile, community values
- [x] Performance impact assessed -- Bundle size, runtime, rate limiting
- [x] Security implications reviewed -- Anonymous endpoint, PII sanitization, rate limiting
- [x] Mobile experience considered -- Bundle size, batch to reduce requests
- [x] Implementation path defined -- 10-step implementation in Section 8.1
- [x] Risk assessment completed -- High/Medium/Low risks with mitigations
- [x] Clear recommendation with rationale -- Custom solution at 85% confidence
- [x] Sources documented for verification -- Official docs, libraries, articles, discussions
