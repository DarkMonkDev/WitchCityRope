# Kiosk Mode Implementation Guide
<!-- Last Updated: 2025-08-24 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Developer -->
<!-- Status: Implementation Guide Complete -->

## 1. Overview

### Why Kiosk Mode Exists
Event organizers need to delegate check-in duties to volunteers without providing admin access to the entire system. Traditional solutions requiring password sharing or elevated permissions create security risks and administrative overhead.

### The Security Problem It Solves
- **Privilege Escalation Risk**: Volunteers could access admin functions if logged in with admin accounts
- **Session Persistence Risk**: Check-in functionality must continue even if admin logs out
- **Device Handoff Risk**: Admin devices must be safely transferred to non-technical volunteers
- **Audit Trail Gap**: Actions must be traceable despite no user authentication for volunteers

### Reference Documentation
- **Business Requirements**: [Business Requirements](./business-requirements.md) - Section 7 (Check-In Security)
- **Security Analysis**: [Check-In Security Analysis](./check-in-security-analysis.md) - Solution 3 (Kiosk Mode)
- **Architecture Patterns**: [Coding Standards](/docs/standards-processes/CODING_STANDARDS.md) - Service implementation patterns

## 2. Architecture

### Session Token Generation and Validation
```
Admin Dashboard → Generate Kiosk Token → Kiosk Interface → API Validation
      ↓                    ↓                   ↓              ↓
  Full Admin          Scoped Token        Limited UI    Token Validation
  Privileges        (Event-Specific)    (Check-in Only)   Middleware
```

### POST-Based Security (Not GET)
**CRITICAL**: Tokens MUST be transmitted in POST request bodies, never in URL parameters.

```csharp
// ✅ CORRECT - Token in request body
[HttpPost("api/kiosk/validate")]
public async Task<IActionResult> ValidateSession([FromBody] KioskValidationRequest request)
{
    var token = request.SessionToken; // Secure transmission
    // Validation logic
}

// ❌ WRONG - Token in URL (security risk)
[HttpGet("api/kiosk/checkin/{sessionToken}")]
public async Task<IActionResult> CheckIn(string sessionToken) { } // NEVER DO THIS
```

### Device Fingerprinting Approach
Bind tokens to specific browser sessions to prevent URL sharing:

```javascript
// Generate device fingerprint
function generateDeviceFingerprint() {
    return {
        userAgent: navigator.userAgent,
        screenResolution: `${screen.width}x${screen.height}`,
        timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
        language: navigator.language,
        platform: navigator.platform,
        cookieEnabled: navigator.cookieEnabled
    };
}
```

### Token Storage (httpOnly Cookies, Not localStorage)
**MANDATORY**: Use httpOnly cookies to prevent XSS attacks.

```csharp
// Set kiosk session cookie
var cookieOptions = new CookieOptions
{
    HttpOnly = true,           // Prevents JavaScript access (XSS protection)
    Secure = true,            // HTTPS only
    SameSite = SameSiteMode.Strict,
    Expires = DateTimeOffset.Now.AddHours(sessionDurationHours),
    Path = "/kiosk"           // Scope to kiosk routes only
};

Response.Cookies.Append("KioskSession", sessionToken, cookieOptions);
```

### Session Expiration Handling
Implement graceful degradation and clear user messaging:

```typescript
interface KioskSession {
    expiresAt: Date;
    eventId: number;
    stationName: string;
    permissions: string[];
}

// Check session validity before each operation
const validateSession = async (): Promise<boolean> => {
    const session = await getKioskSession();
    if (!session || new Date() > session.expiresAt) {
        showSessionExpiredDialog();
        return false;
    }
    return true;
};
```

## 3. Backend Implementation (.NET API)

### Endpoint: POST /api/events/{eventId}/kiosk/generate

```csharp
[ApiController]
[Route("api/events/{eventId}/kiosk")]
[Authorize(Roles = "Admin,EventOrganizer")]
public class KioskController : ControllerBase
{
    private readonly IKioskSessionService _kioskService;
    private readonly IEventService _eventService;
    private readonly ILogger<KioskController> _logger;

    public KioskController(
        IKioskSessionService kioskService, 
        IEventService eventService,
        ILogger<KioskController> logger)
    {
        _kioskService = kioskService;
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a secure kiosk mode session for event check-in.
    /// Creates time-limited, event-scoped access tokens for volunteer use.
    /// 
    /// Security features:
    /// - Cryptographically signed tokens with HMAC-SHA256
    /// - Device fingerprint binding to prevent URL sharing
    /// - Event-specific scope limiting access to single event
    /// - Configurable expiration (2-12 hours)
    /// - Real-time revocation capability
    /// </summary>
    /// <param name="eventId">Event to create kiosk session for</param>
    /// <param name="request">Kiosk session configuration</param>
    /// <returns>Secure session token and configuration</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(KioskSessionResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<IActionResult> GenerateKioskSession(
        [FromRoute] int eventId,
        [FromBody] GenerateKioskSessionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // Validate event exists and user has permission
            var eventResult = await _eventService.GetEventAsync(eventId, ct);
            if (!eventResult.IsSuccess)
            {
                return NotFound($"Event {eventId} not found");
            }

            // Validate session parameters
            if (request.ExpirationHours < 1 || request.ExpirationHours > 12)
            {
                return BadRequest("Session expiration must be between 1 and 12 hours");
            }

            // Generate kiosk session
            var sessionResult = await _kioskService.GenerateSessionAsync(new CreateKioskSessionRequest
            {
                EventId = eventId,
                StationName = request.StationName,
                ExpirationHours = request.ExpirationHours,
                Permissions = request.Permissions ?? new[] { "checkin:read", "checkin:write" },
                DeviceFingerprint = request.DeviceFingerprint,
                CreatedByUserId = User.GetUserId(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            }, ct);

            if (!sessionResult.IsSuccess)
            {
                return BadRequest(sessionResult.Error);
            }

            _logger.LogInformation(
                "Kiosk session generated for event {EventId} by user {UserId}. Session ID: {SessionId}",
                eventId, User.GetUserId(), sessionResult.Value.SessionId);

            return Ok(sessionResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating kiosk session for event {EventId}", eventId);
            return StatusCode(500, "Failed to generate kiosk session");
        }
    }
}
```

### Token Generation with Cryptographic Signing

```csharp
public interface IKioskSessionService
{
    Task<Result<KioskSessionResponse>> GenerateSessionAsync(CreateKioskSessionRequest request, CancellationToken ct = default);
    Task<Result<KioskSession>> ValidateSessionAsync(string sessionToken, CancellationToken ct = default);
    Task<Result> RevokeSessionAsync(string sessionId, CancellationToken ct = default);
    Task<Result<List<ActiveKioskSession>>> GetActiveSessionsAsync(int eventId, CancellationToken ct = default);
}

public class KioskSessionService : IKioskSessionService
{
    private readonly ICacheService _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KioskSessionService> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;

    public KioskSessionService(
        ICacheService cache,
        IConfiguration configuration,
        ILogger<KioskSessionService> logger,
        IDateTimeProvider dateTimeProvider)
    {
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<KioskSessionResponse>> GenerateSessionAsync(
        CreateKioskSessionRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            // Generate cryptographically secure session ID
            var sessionId = GenerateSecureSessionId();
            var sessionToken = GenerateSecureSessionToken(sessionId, request);

            // Create session object
            var session = new KioskSession
            {
                SessionId = sessionId,
                SessionToken = sessionToken,
                EventId = request.EventId,
                StationName = request.StationName,
                Permissions = request.Permissions,
                DeviceFingerprint = request.DeviceFingerprint,
                CreatedAt = _dateTimeProvider.UtcNow,
                ExpiresAt = _dateTimeProvider.UtcNow.AddHours(request.ExpirationHours),
                CreatedByUserId = request.CreatedByUserId,
                IpAddress = request.IpAddress,
                IsActive = true
            };

            // Store in Redis with expiration
            var cacheKey = $"kiosk_session:{sessionId}";
            var cacheExpiration = TimeSpan.FromHours(request.ExpirationHours);
            await _cache.SetAsync(cacheKey, session, cacheExpiration, ct);

            // Store in active sessions index for management
            var activeSessionsKey = $"kiosk_active:{request.EventId}";
            var activeSessions = await _cache.GetAsync<List<string>>(activeSessionsKey, ct) ?? new List<string>();
            activeSessions.Add(sessionId);
            await _cache.SetAsync(activeSessionsKey, activeSessions, TimeSpan.FromDays(1), ct);

            return Result<KioskSessionResponse>.Success(new KioskSessionResponse
            {
                SessionId = sessionId,
                SessionToken = sessionToken,
                ExpiresAt = session.ExpiresAt,
                StationName = session.StationName,
                EventId = session.EventId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate kiosk session for event {EventId}", request.EventId);
            return Result<KioskSessionResponse>.Failure("Failed to generate kiosk session");
        }
    }

    private string GenerateSecureSessionId()
    {
        // Generate 32-byte random ID, base64 encoded
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string GenerateSecureSessionToken(string sessionId, CreateKioskSessionRequest request)
    {
        // Create JWT-like token with HMAC-SHA256 signature
        var payload = new
        {
            session_id = sessionId,
            event_id = request.EventId,
            station = request.StationName,
            permissions = request.Permissions,
            device_fp = request.DeviceFingerprint,
            created_by = request.CreatedByUserId,
            exp = DateTimeOffset.UtcNow.AddHours(request.ExpirationHours).ToUnixTimeSeconds(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

        // Sign with HMAC-SHA256
        var secret = _configuration["KioskMode:SigningSecret"];
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException("Kiosk signing secret not configured");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
        var signatureBase64 = Convert.ToBase64String(signature);

        return $"{payloadBase64}.{signatureBase64}";
    }
}
```

### Session Storage in Redis/Memory Cache

```csharp
/// <summary>
/// Kiosk session data model for secure storage and validation.
/// Contains all information needed for session validation and audit logging.
/// </summary>
public class KioskSession
{
    public string SessionId { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string[] Permissions { get; set; } = Array.Empty<string>();
    public string DeviceFingerprint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int AccessCount { get; set; }
}

// Redis storage configuration
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
    options.InstanceName = "WitchCityRope_Kiosk";
});
```

### Device Fingerprinting Data to Collect

```csharp
public class DeviceFingerprint
{
    /// <summary>
    /// Browser user agent string for basic device identification
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Screen resolution to help identify unique devices
    /// </summary>
    public string ScreenResolution { get; set; } = string.Empty;

    /// <summary>
    /// System timezone for additional uniqueness
    /// </summary>
    public string Timezone { get; set; } = string.Empty;

    /// <summary>
    /// Browser language settings
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Platform information (Windows, macOS, Linux, etc.)
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Whether cookies are enabled (basic capability check)
    /// </summary>
    public bool CookieEnabled { get; set; }

    /// <summary>
    /// Generate hash for comparison (don't store raw fingerprint data)
    /// </summary>
    public string ToHash()
    {
        var combined = $"{UserAgent}|{ScreenResolution}|{Timezone}|{Language}|{Platform}|{CookieEnabled}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hashBytes);
    }
}
```

### Validation Middleware for Kiosk Routes

```csharp
public class KioskAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IKioskSessionService _kioskService;
    private readonly ILogger<KioskAuthenticationMiddleware> _logger;

    public KioskAuthenticationMiddleware(
        RequestDelegate next,
        IKioskSessionService kioskService,
        ILogger<KioskAuthenticationMiddleware> logger)
    {
        _next = next;
        _kioskService = kioskService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to kiosk routes
        if (!context.Request.Path.StartsWithSegments("/kiosk"))
        {
            await _next(context);
            return;
        }

        // Skip authentication for static assets and initial kiosk page
        if (IsStaticAsset(context.Request.Path) || context.Request.Path == "/kiosk")
        {
            await _next(context);
            return;
        }

        try
        {
            // Extract session token from cookie
            var sessionToken = context.Request.Cookies["KioskSession"];
            if (string.IsNullOrEmpty(sessionToken))
            {
                await HandleUnauthorized(context, "No kiosk session found");
                return;
            }

            // Validate session
            var validationResult = await _kioskService.ValidateSessionAsync(sessionToken);
            if (!validationResult.IsSuccess)
            {
                await HandleUnauthorized(context, validationResult.Error);
                return;
            }

            // Add session info to context for controllers
            context.Items["KioskSession"] = validationResult.Value;

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating kiosk session");
            await HandleUnauthorized(context, "Session validation failed");
        }
    }

    private static async Task HandleUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        
        var response = new { error = "Unauthorized", message };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static bool IsStaticAsset(PathString path)
    {
        return path.Value?.Contains(".css") == true ||
               path.Value?.Contains(".js") == true ||
               path.Value?.Contains(".ico") == true ||
               path.Value?.Contains(".png") == true ||
               path.Value?.Contains(".jpg") == true ||
               path.Value?.Contains(".gif") == true;
    }
}

// Register middleware in Program.cs
app.UseMiddleware<KioskAuthenticationMiddleware>();
```

### Automatic Expiration and Cleanup

```csharp
public class KioskSessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KioskSessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(15);

    public KioskSessionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<KioskSessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
                
                await CleanupExpiredSessions(cache, stoppingToken);
                
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during kiosk session cleanup");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry after 5 minutes
            }
        }
    }

    private async Task CleanupExpiredSessions(ICacheService cache, CancellationToken ct)
    {
        // Implementation would scan for expired sessions and remove them
        // This is handled automatically by Redis TTL, but we might want
        // additional cleanup for audit logging and active session tracking
        
        _logger.LogDebug("Kiosk session cleanup completed");
    }
}

// Register in Program.cs
services.AddHostedService<KioskSessionCleanupService>();
```

## 4. Frontend Implementation (React)

### Kiosk Route Protection

```typescript
// KioskRouteGuard.tsx
import React, { useEffect, useState } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { validateKioskSession } from '../services/kioskService';

interface KioskRouteGuardProps {
  children: React.ReactNode;
}

export const KioskRouteGuard: React.FC<KioskRouteGuardProps> = ({ children }) => {
  const [isValidating, setIsValidating] = useState(true);
  const [isValid, setIsValid] = useState(false);
  const location = useLocation();

  useEffect(() => {
    const validateSession = async () => {
      try {
        const result = await validateKioskSession();
        setIsValid(result.isValid);
        
        if (!result.isValid) {
          // Clear any existing kiosk data
          localStorage.removeItem('kioskSession');
        }
      } catch (error) {
        console.error('Session validation error:', error);
        setIsValid(false);
      } finally {
        setIsValidating(false);
      }
    };

    validateSession();
  }, [location.pathname]);

  if (isValidating) {
    return (
      <div className="kiosk-loading">
        <div className="spinner" />
        <p>Validating session...</p>
      </div>
    );
  }

  if (!isValid) {
    return <Navigate to="/kiosk/expired" replace />;
  }

  return <>{children}</>;
};
```

### Navigation Prevention (No Back Button, No URL Bar Access)

```typescript
// KioskSecurityProvider.tsx
import React, { useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';

interface KioskSecurityProviderProps {
  children: React.ReactNode;
}

export const KioskSecurityProvider: React.FC<KioskSecurityProviderProps> = ({ children }) => {
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    // Prevent back button navigation
    const preventBackNavigation = (event: PopStateEvent) => {
      event.preventDefault();
      // Force stay on current page
      window.history.pushState(null, '', location.pathname);
    };

    // Prevent common keyboard shortcuts that could expose admin functions
    const preventKeyboardShortcuts = (event: KeyboardEvent) => {
      // Prevent F12 (Developer Tools)
      if (event.key === 'F12') {
        event.preventDefault();
        return false;
      }

      // Prevent Ctrl+Shift+I (Developer Tools)
      if (event.ctrlKey && event.shiftKey && event.key === 'I') {
        event.preventDefault();
        return false;
      }

      // Prevent Ctrl+Shift+J (Console)
      if (event.ctrlKey && event.shiftKey && event.key === 'J') {
        event.preventDefault();
        return false;
      }

      // Prevent Ctrl+U (View Source)
      if (event.ctrlKey && event.key === 'u') {
        event.preventDefault();
        return false;
      }

      // Prevent Ctrl+Shift+C (Element Inspector)
      if (event.ctrlKey && event.shiftKey && event.key === 'C') {
        event.preventDefault();
        return false;
      }

      // Prevent Alt+Tab (window switching - browser limitation)
      if (event.altKey && event.key === 'Tab') {
        event.preventDefault();
        return false;
      }
    };

    // Prevent right-click context menu
    const preventContextMenu = (event: MouseEvent) => {
      event.preventDefault();
      return false;
    };

    // Initialize security measures
    window.history.pushState(null, '', location.pathname);
    window.addEventListener('popstate', preventBackNavigation);
    document.addEventListener('keydown', preventKeyboardShortcuts);
    document.addEventListener('contextmenu', preventContextMenu);

    // Set document title to indicate kiosk mode
    document.title = `WitchCityRope - Kiosk Mode - ${getStationName()}`;

    // Hide URL bar on mobile devices (if supported)
    if ('standalone' in window.navigator && !(window.navigator as any).standalone) {
      // Suggest adding to home screen for full-screen experience
      showAddToHomeScreenHint();
    }

    return () => {
      window.removeEventListener('popstate', preventBackNavigation);
      document.removeEventListener('keydown', preventKeyboardShortcuts);
      document.removeEventListener('contextmenu', preventContextMenu);
    };
  }, [location]);

  return <>{children}</>;
};

const getStationName = (): string => {
  const session = localStorage.getItem('kioskSession');
  if (session) {
    try {
      const parsed = JSON.parse(session);
      return parsed.stationName || 'Check-in Station';
    } catch {
      return 'Check-in Station';
    }
  }
  return 'Check-in Station';
};

const showAddToHomeScreenHint = () => {
  // Show a subtle hint to add to home screen for better kiosk experience
  console.log('Consider adding to home screen for full-screen kiosk experience');
};
```

### Keyboard Shortcut Blocking

```typescript
// kioskSecurity.ts
export const initializeKioskSecurity = (): (() => void) => {
  const securityConfig = {
    blockF12: true,
    blockCtrlShiftI: true,
    blockCtrlShiftJ: true,
    blockCtrlU: true,
    blockCtrlShiftC: true,
    blockRightClick: true,
    preventTextSelection: true,
    preventDragDrop: true
  };

  const handleKeyDown = (event: KeyboardEvent) => {
    // F12 - Developer Tools
    if (securityConfig.blockF12 && event.key === 'F12') {
      event.preventDefault();
      showSecurityWarning('Developer tools are disabled in kiosk mode');
      return false;
    }

    // Ctrl combinations
    if (event.ctrlKey) {
      // Ctrl+Shift+I - Developer Tools
      if (securityConfig.blockCtrlShiftI && event.shiftKey && event.key === 'I') {
        event.preventDefault();
        showSecurityWarning('Developer tools are disabled in kiosk mode');
        return false;
      }

      // Ctrl+Shift+J - Console
      if (securityConfig.blockCtrlShiftJ && event.shiftKey && event.key === 'J') {
        event.preventDefault();
        showSecurityWarning('Console access is disabled in kiosk mode');
        return false;
      }

      // Ctrl+U - View Source
      if (securityConfig.blockCtrlU && event.key === 'u') {
        event.preventDefault();
        showSecurityWarning('View source is disabled in kiosk mode');
        return false;
      }

      // Ctrl+Shift+C - Element Inspector
      if (securityConfig.blockCtrlShiftC && event.shiftKey && event.key === 'C') {
        event.preventDefault();
        showSecurityWarning('Element inspector is disabled in kiosk mode');
        return false;
      }
    }
  };

  const handleContextMenu = (event: MouseEvent) => {
    if (securityConfig.blockRightClick) {
      event.preventDefault();
      showSecurityWarning('Right-click is disabled in kiosk mode');
      return false;
    }
  };

  const handleSelectStart = (event: Event) => {
    if (securityConfig.preventTextSelection) {
      event.preventDefault();
      return false;
    }
  };

  const handleDragStart = (event: DragEvent) => {
    if (securityConfig.preventDragDrop) {
      event.preventDefault();
      return false;
    }
  };

  // Apply security measures
  document.addEventListener('keydown', handleKeyDown);
  document.addEventListener('contextmenu', handleContextMenu);
  document.addEventListener('selectstart', handleSelectStart);
  document.addEventListener('dragstart', handleDragStart);

  // Return cleanup function
  return () => {
    document.removeEventListener('keydown', handleKeyDown);
    document.removeEventListener('contextmenu', handleContextMenu);
    document.removeEventListener('selectstart', handleSelectStart);
    document.removeEventListener('dragstart', handleDragStart);
  };
};

const showSecurityWarning = (message: string) => {
  // Show a brief, non-intrusive warning
  const toast = document.createElement('div');
  toast.className = 'kiosk-security-warning';
  toast.textContent = message;
  toast.style.cssText = `
    position: fixed;
    top: 20px;
    right: 20px;
    background: #f44336;
    color: white;
    padding: 12px 20px;
    border-radius: 4px;
    z-index: 10000;
    font-size: 14px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.2);
  `;

  document.body.appendChild(toast);

  setTimeout(() => {
    document.body.removeChild(toast);
  }, 3000);
};
```

### Session Timer Display and Auto-Logout

```typescript
// SessionTimer.tsx
import React, { useState, useEffect } from 'react';
import { useKioskSession } from '../hooks/useKioskSession';
import { formatTimeRemaining } from '../utils/timeUtils';

export const SessionTimer: React.FC = () => {
  const { session, refreshSession, logout } = useKioskSession();
  const [timeRemaining, setTimeRemaining] = useState<number>(0);
  const [showWarning, setShowWarning] = useState(false);

  useEffect(() => {
    if (!session) return;

    const updateTimer = () => {
      const now = Date.now();
      const expiresAt = new Date(session.expiresAt).getTime();
      const remaining = Math.max(0, expiresAt - now);
      
      setTimeRemaining(remaining);

      // Show warning when 5 minutes or less remaining
      setShowWarning(remaining <= 5 * 60 * 1000);

      // Auto-logout when expired
      if (remaining <= 0) {
        logout('Session expired');
      }
    };

    updateTimer();
    const interval = setInterval(updateTimer, 1000);

    return () => clearInterval(interval);
  }, [session, logout]);

  const handleExtendSession = async () => {
    try {
      await refreshSession();
      setShowWarning(false);
    } catch (error) {
      console.error('Failed to extend session:', error);
    }
  };

  if (!session) return null;

  return (
    <div className={`session-timer ${showWarning ? 'warning' : ''}`}>
      <div className="timer-content">
        <span className="time-display">
          {formatTimeRemaining(timeRemaining)}
        </span>
        <span className="station-name">
          {session.stationName}
        </span>
      </div>
      
      {showWarning && (
        <div className="session-warning">
          <p>Session expires in {formatTimeRemaining(timeRemaining)}</p>
          <button 
            onClick={handleExtendSession}
            className="extend-session-btn"
          >
            Extend Session
          </button>
        </div>
      )}
    </div>
  );
};
```

### Error Handling for Expired Sessions

```typescript
// kioskService.ts
export interface KioskSessionInfo {
  sessionId: string;
  eventId: number;
  stationName: string;
  expiresAt: string;
  permissions: string[];
}

export interface KioskApiError {
  code: 'SESSION_EXPIRED' | 'PERMISSION_DENIED' | 'NETWORK_ERROR' | 'UNKNOWN_ERROR';
  message: string;
  canRetry: boolean;
}

export class KioskService {
  private baseUrl: string;

  constructor(baseUrl: string = '/api') {
    this.baseUrl = baseUrl;
  }

  async validateSession(): Promise<{ isValid: boolean; session?: KioskSessionInfo; error?: KioskApiError }> {
    try {
      const response = await fetch(`${this.baseUrl}/kiosk/validate`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include', // Include httpOnly cookies
        body: JSON.stringify({
          deviceFingerprint: generateDeviceFingerprint()
        })
      });

      if (response.ok) {
        const session = await response.json();
        return { isValid: true, session };
      }

      if (response.status === 401) {
        return {
          isValid: false,
          error: {
            code: 'SESSION_EXPIRED',
            message: 'Your session has expired. Please ask an organizer to generate a new kiosk session.',
            canRetry: false
          }
        };
      }

      if (response.status === 403) {
        return {
          isValid: false,
          error: {
            code: 'PERMISSION_DENIED',
            message: 'Your session does not have permission for this operation.',
            canRetry: false
          }
        };
      }

      throw new Error(`Unexpected response: ${response.status}`);

    } catch (error) {
      return {
        isValid: false,
        error: {
          code: 'NETWORK_ERROR',
          message: 'Unable to validate session. Please check your internet connection.',
          canRetry: true
        }
      };
    }
  }

  async checkInAttendee(attendeeId: string): Promise<{ success: boolean; error?: KioskApiError }> {
    try {
      const response = await fetch(`${this.baseUrl}/kiosk/checkin`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',
        body: JSON.stringify({
          attendeeId,
          checkInTime: new Date().toISOString(),
          deviceFingerprint: generateDeviceFingerprint()
        })
      });

      if (response.ok) {
        return { success: true };
      }

      // Handle specific error cases
      const errorData = await response.json().catch(() => ({}));
      
      if (response.status === 401) {
        return {
          success: false,
          error: {
            code: 'SESSION_EXPIRED',
            message: errorData.message || 'Session expired',
            canRetry: false
          }
        };
      }

      return {
        success: false,
        error: {
          code: 'UNKNOWN_ERROR',
          message: errorData.message || 'Check-in failed',
          canRetry: true
        }
      };

    } catch (error) {
      return {
        success: false,
        error: {
          code: 'NETWORK_ERROR',
          message: 'Network error during check-in',
          canRetry: true
        }
      };
    }
  }
}

function generateDeviceFingerprint(): Record<string, any> {
  return {
    userAgent: navigator.userAgent,
    screenResolution: `${screen.width}x${screen.height}`,
    timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    language: navigator.language,
    platform: navigator.platform,
    cookieEnabled: navigator.cookieEnabled
  };
}
```

### QR Code Generation for Mobile Setup

```typescript
// QRCodeGenerator.tsx
import React, { useEffect, useState } from 'react';
import QRCode from 'qrcode';

interface QRCodeGeneratorProps {
  sessionUrl: string;
  stationName: string;
}

export const QRCodeGenerator: React.FC<QRCodeGeneratorProps> = ({ sessionUrl, stationName }) => {
  const [qrCodeUrl, setQrCodeUrl] = useState<string>('');
  const [error, setError] = useState<string>('');

  useEffect(() => {
    const generateQR = async () => {
      try {
        // Generate QR code with high error correction for reliability
        const qrDataUrl = await QRCode.toDataURL(sessionUrl, {
          errorCorrectionLevel: 'H', // High error correction
          type: 'image/png',
          quality: 0.92,
          margin: 1,
          color: {
            dark: '#000000',
            light: '#FFFFFF'
          },
          width: 256
        });

        setQrCodeUrl(qrDataUrl);
        setError('');
      } catch (err) {
        setError('Failed to generate QR code');
        console.error('QR Code generation error:', err);
      }
    };

    if (sessionUrl) {
      generateQR();
    }
  }, [sessionUrl]);

  if (error) {
    return (
      <div className="qr-code-error">
        <p>Unable to generate QR code</p>
        <p className="error-details">{error}</p>
      </div>
    );
  }

  return (
    <div className="qr-code-container">
      <h3>Mobile Access</h3>
      <div className="qr-code-wrapper">
        {qrCodeUrl ? (
          <img 
            src={qrCodeUrl} 
            alt="Kiosk Session QR Code" 
            className="qr-code-image"
          />
        ) : (
          <div className="qr-code-loading">Generating QR Code...</div>
        )}
      </div>
      <div className="qr-code-info">
        <p><strong>Station:</strong> {stationName}</p>
        <p>Scan with mobile device for portable check-in access</p>
        <details>
          <summary>Manual URL</summary>
          <code className="session-url">{sessionUrl}</code>
        </details>
      </div>
    </div>
  );
};
```

## 5. Security Checklist

### Token Security
- [x] **Tokens are cryptographically signed with HMAC-SHA256**
  - Use secure random generation for session IDs (32 bytes)
  - Sign tokens with secret key separate from JWT signing key
  - Include expiration time in token payload

### Device and Session Security
- [x] **Device fingerprinting implemented**
  - Collect browser, screen, timezone, language, platform data
  - Hash fingerprint data for comparison (don't store raw data)
  - Validate fingerprint on each request to prevent URL sharing

- [x] **POST validation on all kiosk endpoints**
  - Never pass tokens in URL parameters (security risk)
  - Use request bodies for token transmission
  - Validate tokens server-side before processing requests

- [x] **httpOnly cookies for session storage**
  - Set httpOnly flag to prevent JavaScript access
  - Use Secure flag for HTTPS-only transmission
  - Set SameSite=Strict to prevent CSRF attacks
  - Scope cookies to /kiosk path only

### Data Protection
- [x] **No sensitive data in URLs**
  - All tokens transmitted in request bodies or cookies
  - No personally identifiable information in URLs
  - No session data exposed in client-side code

- [x] **Audit logging for all actions**
  - Log session creation, validation, and expiration
  - Log all check-in actions with session attribution
  - Include IP address, timestamp, and user context
  - Store audit logs separately from session data

### Session Management
- [x] **Automatic session cleanup**
  - Redis TTL automatically expires sessions
  - Background service removes stale session references
  - Memory cache cleanup prevents resource leaks
  - Proper cleanup on session revocation

- [x] **Rate limiting on token generation**
  - Limit token generation to prevent abuse
  - Implement per-user and per-IP rate limits
  - Monitor for suspicious generation patterns
  - Alert on excessive token generation requests

## 6. Testing Requirements

### Unit Tests for Token Generation/Validation

```csharp
[TestClass]
public class KioskSessionServiceTests
{
    private IKioskSessionService _service;
    private Mock<ICacheService> _mockCache;
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<ILogger<KioskSessionService>> _mockLogger;
    private Mock<IDateTimeProvider> _mockDateTimeProvider;

    [TestInitialize]
    public void Setup()
    {
        _mockCache = new Mock<ICacheService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<KioskSessionService>>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();

        _mockConfiguration.Setup(c => c["KioskMode:SigningSecret"])
            .Returns("test-secret-key-for-signing-tokens-minimum-32-characters");
        
        _mockDateTimeProvider.Setup(d => d.UtcNow)
            .Returns(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        _service = new KioskSessionService(
            _mockCache.Object,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockDateTimeProvider.Object);
    }

    [TestMethod]
    public async Task GenerateSessionAsync_WithValidRequest_ShouldReturnSuccessResult()
    {
        // Arrange
        var request = new CreateKioskSessionRequest
        {
            EventId = 123,
            StationName = "Front Desk",
            ExpirationHours = 4,
            Permissions = new[] { "checkin:read", "checkin:write" },
            DeviceFingerprint = "test-fingerprint-hash",
            CreatedByUserId = "admin-user-id",
            IpAddress = "192.168.1.100"
        };

        _mockCache.Setup(c => c.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<KioskSession>(), 
            It.IsAny<TimeSpan>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GenerateSessionAsync(request);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.IsNotNull(result.Value.SessionId);
        Assert.IsNotNull(result.Value.SessionToken);
        Assert.AreEqual(request.EventId, result.Value.EventId);
        Assert.AreEqual(request.StationName, result.Value.StationName);
        
        // Verify token structure
        var tokenParts = result.Value.SessionToken.Split('.');
        Assert.AreEqual(2, tokenParts.Length, "Token should have payload and signature parts");
    }

    [TestMethod]
    public async Task ValidateSessionAsync_WithValidToken_ShouldReturnValidSession()
    {
        // Arrange
        var sessionId = "test-session-id";
        var storedSession = new KioskSession
        {
            SessionId = sessionId,
            EventId = 123,
            StationName = "Front Desk",
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            IsActive = true
        };

        _mockCache.Setup(c => c.GetAsync<KioskSession>(
            $"kiosk_session:{sessionId}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedSession);

        // Create a valid token
        var generateRequest = new CreateKioskSessionRequest
        {
            EventId = 123,
            StationName = "Front Desk",
            ExpirationHours = 4,
            DeviceFingerprint = "test-fingerprint"
        };
        
        var generateResult = await _service.GenerateSessionAsync(generateRequest);
        var validToken = generateResult.Value.SessionToken;

        // Act
        var result = await _service.ValidateSessionAsync(validToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(storedSession.SessionId, result.Value.SessionId);
        Assert.AreEqual(storedSession.EventId, result.Value.EventId);
    }

    [TestMethod]
    public async Task ValidateSessionAsync_WithExpiredSession_ShouldReturnFailure()
    {
        // Arrange
        var expiredSession = new KioskSession
        {
            SessionId = "expired-session",
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired 1 hour ago
            IsActive = true
        };

        _mockCache.Setup(c => c.GetAsync<KioskSession>(
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredSession);

        // Act
        var result = await _service.ValidateSessionAsync("some-token");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Error.Contains("expired"), "Error should indicate session is expired");
    }

    [TestMethod]
    public async Task GenerateSessionAsync_WithInvalidExpirationHours_ShouldReturnFailure()
    {
        // Arrange
        var invalidRequest = new CreateKioskSessionRequest
        {
            EventId = 123,
            ExpirationHours = 15 // Invalid - exceeds maximum
        };

        // Act
        var result = await _service.GenerateSessionAsync(invalidRequest);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Error.Contains("expiration"), "Should validate expiration hours");
    }
}
```

### Integration Tests for Kiosk Workflow

```csharp
[TestClass]
public class KioskWorkflowIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task CompleteKioskWorkflow_FromSessionGenerationToCheckin_ShouldWork()
    {
        // Arrange - Create test event and admin user
        var testEvent = await CreateTestEventAsync();
        var adminUser = await CreateTestUserAsync(UserRole.Admin);
        await AuthenticateAsAsync(adminUser);

        // Act 1 - Generate kiosk session
        var generateRequest = new GenerateKioskSessionRequest
        {
            StationName = "Test Station",
            ExpirationHours = 2,
            Permissions = new[] { "checkin:read", "checkin:write" }
        };

        var generateResponse = await _client.PostAsync(
            $"/api/events/{testEvent.Id}/kiosk/generate",
            CreateJsonContent(generateRequest));

        // Assert 1 - Session generation succeeded
        generateResponse.EnsureSuccessStatusCode();
        var sessionResult = await DeserializeAsync<KioskSessionResponse>(generateResponse);
        Assert.IsNotNull(sessionResult.SessionId);
        Assert.IsNotNull(sessionResult.SessionToken);

        // Act 2 - Validate session works
        var validateResponse = await _client.PostAsync(
            "/api/kiosk/validate",
            CreateJsonContent(new { deviceFingerprint = CreateTestFingerprint() }));

        // Assert 2 - Session validation succeeded
        validateResponse.EnsureSuccessStatusCode();

        // Act 3 - Perform check-in using kiosk session
        var attendee = await CreateTestAttendeeAsync(testEvent.Id);
        var checkinResponse = await _client.PostAsync(
            "/api/kiosk/checkin",
            CreateJsonContent(new 
            { 
                attendeeId = attendee.Id,
                checkInTime = DateTime.UtcNow,
                deviceFingerprint = CreateTestFingerprint()
            }));

        // Assert 3 - Check-in succeeded
        checkinResponse.EnsureSuccessStatusCode();

        // Verify check-in was recorded
        var attendeeStatus = await GetAttendeeStatusAsync(attendee.Id);
        Assert.IsTrue(attendeeStatus.IsCheckedIn);
        Assert.IsNotNull(attendeeStatus.CheckInTime);
    }

    [TestMethod]
    public async Task KioskSession_WhenRevoked_ShouldRejectSubsequentRequests()
    {
        // Arrange
        var testEvent = await CreateTestEventAsync();
        var adminUser = await CreateTestUserAsync(UserRole.Admin);
        await AuthenticateAsAsync(adminUser);

        var sessionResult = await GenerateTestKioskSessionAsync(testEvent.Id);

        // Act 1 - Revoke session
        var revokeResponse = await _client.PostAsync(
            $"/api/kiosk/revoke",
            CreateJsonContent(new { sessionId = sessionResult.SessionId }));

        revokeResponse.EnsureSuccessStatusCode();

        // Act 2 - Try to use revoked session
        var validateResponse = await _client.PostAsync(
            "/api/kiosk/validate",
            CreateJsonContent(new { deviceFingerprint = CreateTestFingerprint() }));

        // Assert - Request should be unauthorized
        Assert.AreEqual(HttpStatusCode.Unauthorized, validateResponse.StatusCode);
    }

    private async Task<KioskSessionResponse> GenerateTestKioskSessionAsync(int eventId)
    {
        var request = new GenerateKioskSessionRequest
        {
            StationName = "Test Station",
            ExpirationHours = 2
        };

        var response = await _client.PostAsync(
            $"/api/events/{eventId}/kiosk/generate",
            CreateJsonContent(request));

        response.EnsureSuccessStatusCode();
        return await DeserializeAsync<KioskSessionResponse>(response);
    }

    private Dictionary<string, object> CreateTestFingerprint()
    {
        return new Dictionary<string, object>
        {
            ["userAgent"] = "Mozilla/5.0 Test Browser",
            ["screenResolution"] = "1920x1080",
            ["timezone"] = "America/New_York",
            ["language"] = "en-US",
            ["platform"] = "Win32"
        };
    }
}
```

### Security Tests for Bypass Attempts

```csharp
[TestClass]
public class KioskSecurityTests : IntegrationTestBase
{
    [TestMethod]
    public async Task KioskEndpoints_WithoutValidSession_ShouldReturnUnauthorized()
    {
        // Arrange - No authentication setup

        // Act & Assert - All kiosk endpoints should be protected
        var protectedEndpoints = new[]
        {
            "/api/kiosk/checkin",
            "/api/kiosk/attendees",
            "/api/kiosk/event-info"
        };

        foreach (var endpoint in protectedEndpoints)
        {
            var response = await _client.PostAsync(endpoint, CreateJsonContent(new { }));
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
                $"Endpoint {endpoint} should require authentication");
        }
    }

    [TestMethod]
    public async Task KioskSession_WithTamperedToken_ShouldRejectRequest()
    {
        // Arrange - Generate valid session then tamper with token
        var validSession = await GenerateTestKioskSessionAsync();
        var tamperedToken = validSession.SessionToken.Replace('a', 'b'); // Simple tampering

        // Set tampered token in cookie
        _client.DefaultRequestHeaders.Add("Cookie", $"KioskSession={tamperedToken}");

        // Act
        var response = await _client.PostAsync(
            "/api/kiosk/validate",
            CreateJsonContent(new { deviceFingerprint = CreateTestFingerprint() }));

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task KioskSession_WithWrongDeviceFingerprint_ShouldRejectRequest()
    {
        // Arrange - Generate session with specific fingerprint
        var originalFingerprint = CreateTestFingerprint();
        var session = await GenerateTestKioskSessionAsync(fingerprint: originalFingerprint);

        // Act - Try to use session with different device fingerprint
        var differentFingerprint = CreateTestFingerprint();
        differentFingerprint["userAgent"] = "Different Browser";

        var response = await _client.PostAsync(
            "/api/kiosk/validate",
            CreateJsonContent(new { deviceFingerprint = differentFingerprint }));

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task KioskSession_AccessingWrongEvent_ShouldRejectRequest()
    {
        // Arrange - Generate session for Event A
        var eventA = await CreateTestEventAsync();
        var eventB = await CreateTestEventAsync();
        var session = await GenerateTestKioskSessionAsync(eventA.Id);

        // Act - Try to access Event B's data with Event A's session
        var response = await _client.GetAsync($"/api/kiosk/event/{eventB.Id}/attendees");

        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task KioskTokenGeneration_WithoutServiceSecret_ShouldRejectRequest()
    {
        // Arrange - Remove service secret header
        _client.DefaultRequestHeaders.Remove("X-Service-Secret");

        // Act
        var response = await _client.PostAsync(
            "/api/auth/service-token",
            CreateJsonContent(new { userId = "test", email = "test@test.com" }));

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task KioskSession_ExcessiveGenerationRequests_ShouldRateLimit()
    {
        // Arrange
        var eventId = (await CreateTestEventAsync()).Id;
        var adminUser = await CreateTestUserAsync(UserRole.Admin);
        await AuthenticateAsAsync(adminUser);

        // Act - Generate many sessions rapidly
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(_client.PostAsync(
                $"/api/events/{eventId}/kiosk/generate",
                CreateJsonContent(new GenerateKioskSessionRequest
                {
                    StationName = $"Station {i}",
                    ExpirationHours = 2
                })));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - Some requests should be rate limited
        var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.IsTrue(rateLimitedCount > 0, "Rate limiting should prevent excessive session generation");
    }
}
```

### Load Tests for Concurrent Kiosk Sessions

```csharp
[TestClass]
public class KioskLoadTests : LoadTestBase
{
    [TestMethod]
    public async Task KioskSessions_MultipleConcurrentSessions_ShouldHandleLoad()
    {
        // Arrange - Create multiple events and kiosk sessions
        var events = await CreateMultipleTestEventsAsync(5);
        var sessions = new List<KioskSessionResponse>();

        // Generate sessions for each event
        foreach (var evt in events)
        {
            var session = await GenerateTestKioskSessionAsync(evt.Id);
            sessions.Add(session);
        }

        // Act - Simulate concurrent check-in operations
        var concurrentTasks = new List<Task<TimeSpan>>();
        var attendeesPerEvent = 50;

        foreach (var session in sessions)
        {
            for (int i = 0; i < attendeesPerEvent; i++)
            {
                concurrentTasks.Add(SimulateKioskCheckinAsync(session));
            }
        }

        var results = await Task.WhenAll(concurrentTasks);

        // Assert - Performance requirements
        var averageResponseTime = results.Average(r => r.TotalMilliseconds);
        var maxResponseTime = results.Max(r => r.TotalMilliseconds);

        Assert.IsTrue(averageResponseTime < 200, 
            $"Average response time should be < 200ms, was {averageResponseTime}ms");
        Assert.IsTrue(maxResponseTime < 1000, 
            $"Max response time should be < 1000ms, was {maxResponseTime}ms");

        // Verify all check-ins were successful
        var failedCheckins = results.Count(r => r == TimeSpan.Zero); // Zero indicates failure
        Assert.AreEqual(0, failedCheckins, "All concurrent check-ins should succeed");
    }

    [TestMethod]
    public async Task KioskSessions_MemoryUsage_ShouldStayWithinLimits()
    {
        // Arrange - Generate many concurrent sessions
        var sessionCount = 100;
        var sessions = new List<KioskSessionResponse>();

        // Act - Generate sessions and measure memory
        var initialMemory = GC.GetTotalMemory(true);

        for (int i = 0; i < sessionCount; i++)
        {
            var evt = await CreateTestEventAsync();
            var session = await GenerateTestKioskSessionAsync(evt.Id);
            sessions.Add(session);
        }

        var peakMemory = GC.GetTotalMemory(false);
        var memoryIncrease = peakMemory - initialMemory;

        // Assert - Memory increase should be reasonable
        var memoryIncreasePerSessionMB = (memoryIncrease / sessionCount) / (1024 * 1024);
        
        Assert.IsTrue(memoryIncreasePerSessionMB < 1, 
            $"Memory per session should be < 1MB, was {memoryIncreasePerSessionMB}MB");
    }

    private async Task<TimeSpan> SimulateKioskCheckinAsync(KioskSessionResponse session)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Create test attendee
            var attendee = await CreateTestAttendeeAsync(session.EventId);
            
            // Perform check-in
            var response = await _client.PostAsync("/api/kiosk/checkin",
                CreateJsonContent(new 
                { 
                    attendeeId = attendee.Id,
                    checkInTime = DateTime.UtcNow
                }));

            stopwatch.Stop();
            
            return response.IsSuccessStatusCode ? stopwatch.Elapsed : TimeSpan.Zero;
        }
        catch
        {
            return TimeSpan.Zero; // Indicate failure
        }
    }
}
```

### Edge Cases (Expired Tokens, Device Changes)

```csharp
[TestClass]
public class KioskEdgeCaseTests : IntegrationTestBase
{
    [TestMethod]
    public async Task KioskSession_ExpiredDuringOperation_ShouldHandleGracefully()
    {
        // Arrange - Generate session with very short expiration
        var session = await GenerateTestKioskSessionAsync(expirationMinutes: 1);
        
        // Wait for session to expire
        await Task.Delay(TimeSpan.FromMinutes(1.1));

        // Act - Try to use expired session
        var response = await _client.PostAsync(
            "/api/kiosk/validate",
            CreateJsonContent(new { deviceFingerprint = CreateTestFingerprint() }));

        // Assert - Should get clear expiration message
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        
        var errorContent = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(errorContent.Contains("expired"), 
            "Error message should clearly indicate session expiration");
    }

    [TestMethod]
    public async Task KioskSession_DeviceChangeMidSession_ShouldRejectNewDevice()
    {
        // Arrange - Generate session with specific device
        var originalFingerprint = CreateTestFingerprint();
        var session = await GenerateTestKioskSessionAsync(fingerprint: originalFingerprint);

        // Verify session works with original device
        var validResponse = await _client.PostAsync(
            "/api/kiosk/validate",
            CreateJsonContent(new { deviceFingerprint = originalFingerprint }));
        
        validResponse.EnsureSuccessStatusCode();

        // Act - Try to use session from different device
        var newFingerprint = CreateTestFingerprint();
        newFingerprint["userAgent"] = "Completely Different Browser";
        newFingerprint["platform"] = "Different OS";

        var invalidResponse = await _client.PostAsync(
            "/api/kiosk/validate",
            CreateJsonContent(new { deviceFingerprint = newFingerprint }));

        // Assert - New device should be rejected
        Assert.AreEqual(HttpStatusCode.Unauthorized, invalidResponse.StatusCode);
    }

    [TestMethod]
    public async Task KioskSession_BrowserRestartWithCookies_ShouldContinueWorking()
    {
        // Arrange - Generate session and get cookie
        var session = await GenerateTestKioskSessionAsync();
        var kioskCookie = ExtractCookie("KioskSession");

        // Simulate browser restart by creating new client with same cookie
        var newClient = CreateHttpClient();
        newClient.DefaultRequestHeaders.Add("Cookie", $"KioskSession={kioskCookie}");

        // Act - Try to use session after "browser restart"
        var response = await newClient.PostAsync(
            "/api/kiosk/validate",
            CreateJsonContent(new { deviceFingerprint = CreateTestFingerprint() }));

        // Assert - Session should still work
        response.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task KioskSession_NetworkInterruption_ShouldRetryGracefully()
    {
        // Arrange - Generate working session
        var session = await GenerateTestKioskSessionAsync();
        var attendee = await CreateTestAttendeeAsync(session.EventId);

        // Simulate network interruption by using invalid endpoint momentarily
        _client.BaseAddress = new Uri("http://invalid-server/");

        // Act - Try operation with network issue
        var failedResponse = await _client.PostAsync(
            "/api/kiosk/checkin",
            CreateJsonContent(new { attendeeId = attendee.Id }));

        // Restore valid endpoint
        _client.BaseAddress = new Uri("http://localhost:5653/");

        // Retry the operation
        var retryResponse = await _client.PostAsync(
            "/api/kiosk/checkin",
            CreateJsonContent(new { attendeeId = attendee.Id }));

        // Assert - Retry should succeed after network restoration
        Assert.IsFalse(failedResponse.IsSuccessStatusCode, "First attempt should fail due to network");
        retryResponse.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task KioskSession_MaxSessionsPerEvent_ShouldEnforceLimit()
    {
        // Arrange - Create event and admin user
        var testEvent = await CreateTestEventAsync();
        var adminUser = await CreateTestUserAsync(UserRole.Admin);
        await AuthenticateAsAsync(adminUser);

        // Act - Try to create more sessions than allowed (assume limit is 5)
        var sessions = new List<KioskSessionResponse>();
        var maxSessions = 5;

        for (int i = 0; i <= maxSessions; i++)
        {
            var generateRequest = new GenerateKioskSessionRequest
            {
                StationName = $"Station {i}",
                ExpirationHours = 2
            };

            var response = await _client.PostAsync(
                $"/api/events/{testEvent.Id}/kiosk/generate",
                CreateJsonContent(generateRequest));

            if (response.IsSuccessStatusCode)
            {
                var session = await DeserializeAsync<KioskSessionResponse>(response);
                sessions.Add(session);
            }
            else
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
                    "Should return BadRequest when session limit exceeded");
                break;
            }
        }

        // Assert - Should not exceed maximum sessions
        Assert.IsTrue(sessions.Count <= maxSessions, 
            $"Should not create more than {maxSessions} concurrent sessions per event");
    }
}
```

## 7. Deployment Considerations

### Environment Variables for Token Secrets

```yaml
# docker-compose.prod.yml
version: '3.8'
services:
  api:
    environment:
      # Kiosk Mode Configuration
      - KioskMode__SigningSecret=${KIOSK_SIGNING_SECRET}
      - KioskMode__MaxSessionsPerEvent=10
      - KioskMode__DefaultExpirationHours=4
      - KioskMode__MaxExpirationHours=12
      - KioskMode__CleanupIntervalMinutes=15
      
      # Service Authentication
      - ServiceAuth__Secret=${SERVICE_AUTH_SECRET}
      
      # Redis Configuration
      - ConnectionStrings__Redis=${REDIS_CONNECTION_STRING}
      
      # Rate Limiting
      - RateLimit__KioskGeneration__RequestsPerMinute=5
      - RateLimit__KioskGeneration__BurstSize=10
    secrets:
      - kiosk_signing_secret
      - service_auth_secret

secrets:
  kiosk_signing_secret:
    external: true
  service_auth_secret:
    external: true
```

```bash
# .env.production (example - use proper secret management)
KIOSK_SIGNING_SECRET=prod-kiosk-secret-key-minimum-64-characters-for-production-security-2024
SERVICE_AUTH_SECRET=prod-service-auth-secret-minimum-64-characters-for-production-security-2024
REDIS_CONNECTION_STRING=redis://production-redis-server:6379
```

### Redis Configuration for Distributed Sessions

```csharp
// Program.cs - Production configuration
public static void Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure Redis for session storage
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "WitchCityRope_Kiosk_Prod";
        
        // Production-specific settings
        options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
        {
            EndPoints = { builder.Configuration.GetConnectionString("Redis") },
            AbortOnConnectFail = false,
            ConnectRetry = 3,
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            AsyncTimeout = 5000,
            KeepAlive = 180,
            DefaultDatabase = 2, // Use dedicated database for kiosk sessions
        };
    });

    // Configure distributed session storage
    builder.Services.Configure<RedisCacheOptions>(options =>
    {
        options.ConfigurationOptions.ClientName = "WitchCityRope_API";
    });

    var app = builder.Build();

    // Verify Redis connection on startup
    var cache = app.Services.GetRequiredService<IDistributedCache>();
    try
    {
        await cache.SetStringAsync("startup_test", DateTime.UtcNow.ToString());
        var test = await cache.GetStringAsync("startup_test");
        app.Logger.LogInformation("Redis connection verified successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Redis connection failed - kiosk sessions will not work");
        throw; // Fail fast if Redis is not available
    }

    app.Run();
}
```

### Monitoring and Alerting Setup

```csharp
// KioskMonitoringService.cs
public class KioskMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KioskMonitoringService> _logger;
    private readonly IMetrics _metrics;
    private readonly TimeSpan _monitoringInterval = TimeSpan.FromMinutes(5);

    public KioskMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<KioskMonitoringService> logger,
        IMetrics metrics)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorKioskSessions(stoppingToken);
                await Task.Delay(_monitoringInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during kiosk monitoring");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task MonitorKioskSessions(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        // Monitor active sessions
        var activeSessionsMetrics = await CollectActiveSessionsMetrics(cache, ct);
        
        // Record metrics
        _metrics.Gauge("kiosk_active_sessions_total").Set(activeSessionsMetrics.TotalActiveSessions);
        _metrics.Gauge("kiosk_sessions_by_event").Set(activeSessionsMetrics.SessionsByEvent.Count);
        _metrics.Gauge("kiosk_sessions_expiring_soon").Set(activeSessionsMetrics.ExpiringSoon);

        // Check for alerts
        await CheckForAlerts(activeSessionsMetrics);
    }

    private async Task CheckForAlerts(ActiveSessionsMetrics metrics)
    {
        // Alert if too many sessions are active (potential abuse)
        if (metrics.TotalActiveSessions > 50)
        {
            _logger.LogWarning(
                "High number of active kiosk sessions detected: {Count}. Check for potential abuse.",
                metrics.TotalActiveSessions);
        }

        // Alert if sessions are failing validation frequently
        if (metrics.FailedValidationsLastHour > 100)
        {
            _logger.LogWarning(
                "High number of session validation failures: {Count} in last hour. Check for attacks.",
                metrics.FailedValidationsLastHour);
        }

        // Alert if many sessions are expiring without being used
        var unusedExpirationRate = metrics.UnusedExpiredSessions / (double)metrics.TotalExpiredSessions;
        if (unusedExpirationRate > 0.8)
        {
            _logger.LogWarning(
                "High rate of unused session expiration: {Rate:P1}. Sessions may be abandoned.",
                unusedExpirationRate);
        }
    }
}

public class ActiveSessionsMetrics
{
    public int TotalActiveSessions { get; set; }
    public Dictionary<int, int> SessionsByEvent { get; set; } = new();
    public int ExpiringSoon { get; set; }
    public int FailedValidationsLastHour { get; set; }
    public int UnusedExpiredSessions { get; set; }
    public int TotalExpiredSessions { get; set; }
}
```

### Backup Admin Override Capabilities

```csharp
[ApiController]
[Route("api/admin/kiosk")]
[Authorize(Roles = "Admin")]
public class KioskAdminController : ControllerBase
{
    private readonly IKioskSessionService _kioskService;
    private readonly ILogger<KioskAdminController> _logger;

    /// <summary>
    /// Emergency endpoint to revoke all kiosk sessions for an event.
    /// Used when sessions need to be terminated immediately due to security concerns.
    /// </summary>
    [HttpPost("emergency-revoke/{eventId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> EmergencyRevokeAllSessions(
        [FromRoute] int eventId,
        [FromBody] EmergencyRevokeRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogWarning(
                "Emergency revocation requested for event {EventId} by admin {UserId}. Reason: {Reason}",
                eventId, User.GetUserId(), request.Reason);

            var result = await _kioskService.RevokeAllSessionsForEventAsync(eventId, ct);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Emergency revocation completed for event {EventId}. {Count} sessions revoked.",
                    eventId, result.Value);

                return Ok(new { message = $"Revoked {result.Value} sessions", eventId });
            }

            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform emergency revocation for event {EventId}", eventId);
            return StatusCode(500, "Emergency revocation failed");
        }
    }

    /// <summary>
    /// Get detailed information about all active kiosk sessions.
    /// Used for monitoring and troubleshooting session issues.
    /// </summary>
    [HttpGet("sessions/active")]
    [ProducesResponseType(typeof(List<KioskSessionInfo>), 200)]
    public async Task<IActionResult> GetActiveSessions(CancellationToken ct = default)
    {
        try
        {
            var sessions = await _kioskService.GetAllActiveSessionsAsync(ct);
            return Ok(sessions.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active kiosk sessions");
            return StatusCode(500, "Failed to retrieve session information");
        }
    }

    /// <summary>
    /// Override session expiration for a specific session.
    /// Used when volunteers need extended access due to event delays.
    /// </summary>
    [HttpPost("sessions/{sessionId}/extend")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ExtendSession(
        [FromRoute] string sessionId,
        [FromBody] ExtendSessionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _kioskService.ExtendSessionAsync(
                sessionId, 
                request.AdditionalHours, 
                ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Session {SessionId} extended by {Hours} hours by admin {UserId}",
                    sessionId, request.AdditionalHours, User.GetUserId());

                return Ok(new { message = "Session extended successfully" });
            }

            return NotFound(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extend session {SessionId}", sessionId);
            return StatusCode(500, "Failed to extend session");
        }
    }
}

public class EmergencyRevokeRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class ExtendSessionRequest
{
    public int AdditionalHours { get; set; }
}

public class KioskSessionInfo
{
    public string SessionId { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public int AccessCount { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public bool IsActive { get; set; }
}
```

## 8. Common Pitfalls to Avoid

### ❌ Don't Store Tokens in localStorage

```typescript
// ❌ WRONG - Vulnerable to XSS attacks
localStorage.setItem('kioskToken', sessionToken);

// ❌ WRONG - Also vulnerable to XSS
sessionStorage.setItem('kioskToken', sessionToken);

// ✅ CORRECT - Use httpOnly cookies set by server
// Token automatically included in requests, not accessible to JavaScript
```

### ❌ Don't Use GET Requests for Sensitive Operations

```csharp
// ❌ WRONG - Token exposed in URL and server logs
[HttpGet("checkin/{sessionToken}/{attendeeId}")]
public async Task<IActionResult> CheckIn(string sessionToken, string attendeeId) { }

// ✅ CORRECT - Token in request body
[HttpPost("checkin")]
public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request) 
{
    // request.SessionToken is safely transmitted in body
}
```

### ❌ Don't Forget Device Fingerprinting

```typescript
// ❌ WRONG - No device binding, sessions can be shared via URL
const generateSession = async (eventId: number) => {
    return await fetch('/api/kiosk/generate', {
        method: 'POST',
        body: JSON.stringify({ eventId })
    });
};

// ✅ CORRECT - Include device fingerprint
const generateSession = async (eventId: number) => {
    const fingerprint = generateDeviceFingerprint();
    return await fetch('/api/kiosk/generate', {
        method: 'POST',
        body: JSON.stringify({ 
            eventId, 
            deviceFingerprint: fingerprint 
        })
    });
};
```

### ❌ Don't Allow Navigation From Kiosk Mode

```typescript
// ❌ WRONG - Users can navigate away from kiosk interface
const KioskInterface = () => {
    return (
        <div>
            <nav>
                <Link to="/admin">Admin Panel</Link> {/* Security risk! */}
                <Link to="/events">All Events</Link> {/* Security risk! */}
            </nav>
            <CheckInForm />
        </div>
    );
};

// ✅ CORRECT - Locked kiosk interface
const KioskInterface = () => {
    return (
        <div className="kiosk-mode-locked">
            <KioskHeader stationName="Front Desk" />
            <CheckInForm />
            {/* No navigation links to other parts of the application */}
        </div>
    );
};
```

### ❌ Don't Skip Token Expiration Validation

```csharp
// ❌ WRONG - No expiration check
public async Task<bool> ValidateKioskToken(string token)
{
    var session = await GetSessionFromCache(token);
    return session != null && session.IsActive;
}

// ✅ CORRECT - Validate expiration
public async Task<bool> ValidateKioskToken(string token)
{
    var session = await GetSessionFromCache(token);
    return session != null && 
           session.IsActive && 
           session.ExpiresAt > DateTime.UtcNow;
}
```

### ❌ Don't Hardcode Security Configuration

```csharp
// ❌ WRONG - Hardcoded secrets and limits
public class KioskService
{
    private const string SigningSecret = "hardcoded-secret";
    private const int MaxSessions = 5;
}

// ✅ CORRECT - Configuration-driven security
public class KioskService
{
    private readonly string _signingSecret;
    private readonly int _maxSessionsPerEvent;

    public KioskService(IConfiguration configuration)
    {
        _signingSecret = configuration["KioskMode:SigningSecret"] 
            ?? throw new InvalidOperationException("Kiosk signing secret not configured");
        _maxSessionsPerEvent = configuration.GetValue<int>("KioskMode:MaxSessionsPerEvent", 10);
    }
}
```

### ❌ Don't Ignore Audit Logging

```csharp
// ❌ WRONG - No audit trail for kiosk operations
public async Task CheckInAttendee(int attendeeId)
{
    await _repository.MarkAttendeeCheckedIn(attendeeId);
}

// ✅ CORRECT - Comprehensive audit logging
public async Task CheckInAttendee(int attendeeId, string sessionId)
{
    var attendee = await _repository.GetAttendeeAsync(attendeeId);
    var session = await _cache.GetKioskSessionAsync(sessionId);
    
    await _repository.MarkAttendeeCheckedIn(attendeeId);
    
    _logger.LogInformation(
        "Attendee {AttendeeId} checked in via kiosk session {SessionId} at station {StationName} for event {EventId}",
        attendeeId, sessionId, session.StationName, session.EventId);
    
    // Store audit record
    await _auditService.LogKioskActionAsync(new KioskAuditRecord
    {
        SessionId = sessionId,
        Action = "CheckIn",
        AttendeeId = attendeeId,
        EventId = session.EventId,
        StationName = session.StationName,
        Timestamp = DateTime.UtcNow,
        IpAddress = _httpContext.Connection.RemoteIpAddress?.ToString()
    });
}
```

---

## Summary

This implementation guide provides a complete, secure solution for kiosk mode functionality that:

- **Eliminates security risks** through cryptographic token signing and device binding
- **Maintains audit compliance** with comprehensive logging and session tracking  
- **Provides excellent user experience** for volunteers with simple, locked interfaces
- **Scales for production use** with Redis caching, monitoring, and admin controls
- **Includes comprehensive testing** to ensure reliability and security

The solution balances the competing requirements of security (protecting admin functions) and usability (easy volunteer operation) while providing the operational flexibility needed for diverse community events.

Key security features ensure that volunteers cannot access admin functions, tokens cannot be shared between devices, and all actions are fully audited for compliance and troubleshooting.