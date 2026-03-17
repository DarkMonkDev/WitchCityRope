# Technology Research: Persistent Login / "Remember Me" Implementation
<!-- Last Updated: 2026-03-16 -->
<!-- Version: 1.0 -->
<!-- Owner: Technology Researcher Agent -->
<!-- Status: Draft -->

## Executive Summary
**Decision Required**: How to implement persistent login / "remember me" for WitchCityRope with JWT + httpOnly cookies in a BFF pattern.
**Recommendation**: Dual-duration refresh tokens stored server-side (database), with short-lived JWT access tokens in httpOnly cookies, and TanStack Query `visibilitychange`-based session validation.
**Confidence Level**: High (85%)
**Key Factors**: Security (httpOnly-only architecture), UX (community members at events), maintainability (volunteer dev team)

---

## 1. JWT + Remember Me Best Practices

### Recommended Pattern: Separate Refresh Token, NOT Longer JWTs

**Do NOT extend JWT access token lifetime for "remember me."** This is the consensus across all authoritative sources.

#### Token Durations

| Token Type | Remember Me UNCHECKED | Remember Me CHECKED |
|---|---|---|
| **JWT Access Token** | 15 minutes | 15 minutes (SAME) |
| **Refresh Token** | Session-only (no persistence) | 14 days |
| **Cookie (access)** | Session cookie (no `Expires`) | Session cookie (no `Expires`) |
| **Cookie (refresh)** | Session cookie (no `Expires`) | Persistent cookie, 14 days `Expires` |

**Key insight**: The access token duration NEVER changes based on "remember me." Only the refresh token duration and cookie persistence change.

#### Rationale
- **Short access tokens (15 min)**: Limits damage window if token is compromised. This is the industry standard per Auth0, OWASP, and multiple security experts.
- **Session vs persistent cookies**: When "remember me" is unchecked, cookies are session-only (cleared on browser close). When checked, the refresh token cookie persists on disk with an `Expires` attribute.
- **14-day refresh token**: Balances UX with security. OWASP ASVS Level 1 allows up to 30 days, but for a platform handling safety-sensitive content, 14 days is more appropriate.

#### Why NOT Longer JWTs
- JWTs are stateless -- you cannot revoke them once issued
- A 30-day JWT means 30 days of access even after the user's account is disabled
- Refresh tokens stored server-side CAN be revoked immediately
- Every security authority recommends this pattern

### Implementation Pattern (ASP.NET Core)

```csharp
// Login endpoint
app.MapPost("/auth/login", async (LoginRequest request,
    UserManager<ApplicationUser> userManager,
    IRefreshTokenService refreshTokenService,
    HttpContext context) =>
{
    // ... validate credentials ...

    var accessToken = GenerateJwtAccessToken(user, roles); // 15 min expiry

    // Access token cookie -- always a session cookie
    context.Response.Cookies.Append("access_token", accessToken, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        IsEssential = true
        // NO Expires = session cookie
    });

    // Refresh token -- persistent only if "remember me"
    var refreshToken = await refreshTokenService.CreateRefreshTokenAsync(user.Id);
    var refreshCookieOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        IsEssential = true,
        Path = "/auth/refresh" // Restrict to refresh endpoint only
    };

    if (request.RememberMe)
    {
        refreshCookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(14);
    }
    // else: session cookie (cleared on browser close)

    context.Response.Cookies.Append("refresh_token", refreshToken, refreshCookieOptions);

    return Results.Ok(new { user = MapToUserDto(user) });
});
```

---

## 2. sessionStorage vs localStorage for Auth State Cache (Zustand)

### Recommendation: sessionStorage with Zustand persist middleware

Since WitchCityRope uses httpOnly cookies for ALL authentication (tokens never touch JavaScript), the Zustand auth store only holds **display data** (user name, roles, avatar URL). This is NOT sensitive security data.

#### Decision Matrix

| Factor | sessionStorage | localStorage | No Persistence |
|---|---|---|---|
| **Survives page refresh** | Yes | Yes | No |
| **Survives browser close** | No | Yes | No |
| **XSS exposure** | Tab-scoped | All tabs, all time | None |
| **"Remember me" compatible** | Partial | Yes | No |
| **Recommended** | **Default (no remember me)** | **With remember me** | **Not recommended** |

#### Recommendation: Conditional Storage Based on Remember Me

```typescript
import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

interface AuthState {
  user: UserDisplayData | null;
  isAuthenticated: boolean;
  rememberMe: boolean;
  setUser: (user: UserDisplayData | null) => void;
  setRememberMe: (remember: boolean) => void;
  clear: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      isAuthenticated: false,
      rememberMe: false,
      setUser: (user) => set({ user, isAuthenticated: !!user }),
      setRememberMe: (remember) => set({ rememberMe: remember }),
      clear: () => set({ user: null, isAuthenticated: false }),
    }),
    {
      name: 'wcr-auth',
      // Use sessionStorage by default for security
      // The /auth/me endpoint will re-populate on new sessions
      storage: createJSONStorage(() => sessionStorage),
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
        // Do NOT persist rememberMe preference
      }),
    }
  )
);
```

#### Why sessionStorage is Preferred Even With "Remember Me"

Even when a user checks "remember me," the **httpOnly cookie handles session persistence**. The Zustand store is just a UI cache:

1. User opens new browser session (after "remember me")
2. Zustand store is empty (sessionStorage was cleared)
3. App calls `/auth/me` endpoint on startup
4. Server validates the persistent refresh token cookie, issues new access token
5. Response populates the Zustand store with user display data

This means **sessionStorage is always sufficient** because the httpOnly cookie is the true persistence mechanism. The Zustand store is rebuilt from the server on every new session.

#### Security Analysis
- **No tokens in JavaScript**: Zustand store NEVER contains JWTs or refresh tokens
- **Display data only**: Name, scene name, roles, avatar URL -- not security-sensitive
- **sessionStorage scoping**: Data is tab-scoped, reducing XSS blast radius
- **Server is source of truth**: Even if Zustand data is tampered with, the server validates the cookie on every API call

---

## 3. Token Refresh on Wake/Resume

### Recommended Pattern: TanStack Query `refetchOnWindowFocus` + Custom `visibilitychange` Handler

#### How TanStack Query v5 Handles This

**Important change in v5**: TanStack Query now uses `visibilitychange` event (not `focus` event) by default for `refetchOnWindowFocus`. This means it naturally handles the "wake from sleep" scenario because:

- When a laptop wakes from sleep, the browser fires `visibilitychange` with `document.visibilityState === 'visible'`
- When a user switches back to a tab, same event fires
- TanStack Query v5 catches both automatically

#### Recommended Implementation

```typescript
// auth-query.ts
export const useAuthSession = () => {
  return useQuery({
    queryKey: ['auth', 'session'],
    queryFn: async () => {
      const response = await fetch('/auth/me', { credentials: 'include' });
      if (response.status === 401) {
        // Try refresh
        const refreshResponse = await fetch('/auth/refresh', {
          method: 'POST',
          credentials: 'include',
        });
        if (!refreshResponse.ok) {
          // Refresh failed -- user is logged out
          useAuthStore.getState().clear();
          return null;
        }
        // Retry /auth/me with new access token
        const retryResponse = await fetch('/auth/me', { credentials: 'include' });
        if (!retryResponse.ok) return null;
        return retryResponse.json();
      }
      if (!response.ok) return null;
      return response.json();
    },
    // Refetch when tab becomes visible (handles sleep/wake)
    refetchOnWindowFocus: true,
    // Only refetch if data is stale (prevent unnecessary calls)
    staleTime: 5 * 60 * 1000, // 5 minutes
    // Don't retry auth failures
    retry: false,
    // Keep previous data while refetching
    placeholderData: (previousData) => previousData,
  });
};
```

#### Enhanced Wake Detection (Optional)

For cases where `visibilitychange` is insufficient (e.g., desktop app stays visible but network reconnects after sleep):

```typescript
// In QueryClient configuration
import { focusManager, onlineManager } from '@tanstack/react-query';

// TanStack Query v5 already handles visibilitychange
// Add online manager for network reconnection after sleep
onlineManager.setEventListener((setOnline) => {
  const onlineHandler = () => setOnline(true);
  const offlineHandler = () => setOnline(false);

  window.addEventListener('online', onlineHandler);
  window.addEventListener('offline', offlineHandler);

  return () => {
    window.removeEventListener('online', onlineHandler);
    window.removeEventListener('offline', offlineHandler);
  };
});
```

#### Why This Works for WitchCityRope

- **Mobile users at events**: Phone screens turn off/on frequently. `visibilitychange` fires each time.
- **Tab switching**: Community members may have multiple tabs open. Session check happens on return.
- **Network reconnection**: `onlineManager` handles WiFi reconnection at event venues.
- **No unnecessary requests**: `staleTime: 5 minutes` prevents rapid-fire auth checks.

---

## 4. Cookie Expiration vs JWT Expiration

### Recommendation: Cookie Expiration Should MATCH or SLIGHTLY EXCEED Token Expiration

#### Access Token Cookie
| Setting | Value | Rationale |
|---|---|---|
| Cookie Type | Session cookie (no `Expires`) | Cleared on browser close |
| JWT `exp` claim | 15 minutes | Short-lived for security |
| Sliding expiration | **NO** | Use refresh token pattern instead |

**Why no sliding expiration on access tokens**: Sliding expiration on JWTs is an anti-pattern because JWTs are immutable once signed. You would need to issue a NEW JWT (effectively a refresh), which is exactly what the refresh token pattern does more cleanly.

#### Refresh Token Cookie

| Setting | Remember Me OFF | Remember Me ON |
|---|---|---|
| Cookie `Expires` | Not set (session) | `DateTimeOffset.UtcNow.AddDays(14)` |
| DB token expiry | 24 hours | 14 days |
| Alignment | Cookie is session-scoped, DB enforces actual expiry | Cookie and DB both expire at ~14 days |

#### Why Cookie and Token Expiry Should Align

1. **Cookie expires BEFORE token**: User gets logged out even though refresh token is still valid. Wastes server resources on unused tokens. Poor UX.
2. **Cookie expires AFTER token**: Browser sends expired token. Server rejects it. User sees unexpected auth failure. Confusing UX.
3. **Cookie expires WITH token (recommended)**: Clean behavior. Cookie disappears at the same time the token becomes invalid.

#### ASP.NET Core Implementation

```csharp
// Cookie configuration for refresh token
var refreshTokenExpiry = request.RememberMe
    ? TimeSpan.FromDays(14)
    : (TimeSpan?)null; // session cookie

var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict,
    Path = "/auth/refresh",
    IsEssential = true,
};

if (refreshTokenExpiry.HasValue)
{
    cookieOptions.Expires = DateTimeOffset.UtcNow.Add(refreshTokenExpiry.Value);
}

// Store in DB with SAME expiry
await refreshTokenService.CreateAsync(new RefreshToken
{
    Token = hashedToken,
    UserId = user.Id,
    ExpiresAt = DateTime.UtcNow.Add(refreshTokenExpiry ?? TimeSpan.FromHours(24)),
    CreatedAt = DateTime.UtcNow,
});
```

---

## 5. Refresh Token Patterns with BFF

### Recommendation: Database-Stored Refresh Tokens with Rotation

Of the three options you listed, here is the analysis:

#### Option A: Separate Refresh Token in Database -- RECOMMENDED

**How it works**:
1. On login, generate a cryptographically random refresh token
2. Hash it (SHA-256) and store in database with user ID, expiry, and device info
3. Send the unhashed token to client in httpOnly cookie
4. On refresh, validate against database, issue new access + refresh tokens, invalidate old

**Pros**:
- Full revocation capability (delete from DB = instant logout)
- Token rotation (new refresh token on each use) detects theft
- Audit trail of all sessions
- Can implement "log out all devices" by clearing all tokens for a user
- Works perfectly with BFF pattern

**Cons**:
- Database hit on every refresh (once per 15 min max, negligible)
- Slightly more code than re-signing JWTs

#### Option B: Re-issue JWTs from Expired JWTs -- NOT RECOMMENDED

**Why not**:
- If you accept expired JWTs, an attacker with a stolen expired JWT has indefinite access
- No revocation mechanism
- Violates the principle that expired tokens should be useless
- OWASP explicitly warns against this pattern

#### Option C: ASP.NET Identity Built-in Refresh -- PARTIALLY RECOMMENDED

ASP.NET Core Identity's cookie authentication has its own sliding expiration and `IsPersistent` mechanism. However, in a JWT + BFF architecture, you are NOT using ASP.NET's cookie authentication on the frontend-facing side. You are using custom JWT cookies. Therefore:

- **Use ASP.NET Identity** for: User management, password hashing, role management, lockout
- **Do NOT use ASP.NET Identity's cookie auth** for: Frontend session management (you have JWTs)
- **Build custom refresh token service** that integrates with ASP.NET Identity's UserManager

### Recommended Database Schema

```sql
CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    token_hash VARCHAR(128) NOT NULL,        -- SHA-256 hash of token
    user_id UUID NOT NULL REFERENCES asp_net_users(id),
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    revoked_at TIMESTAMPTZ NULL,             -- NULL = active
    replaced_by_token_hash VARCHAR(128) NULL, -- For rotation tracking
    device_info VARCHAR(256) NULL,            -- User-Agent or device fingerprint
    ip_address INET NULL,

    CONSTRAINT fk_user FOREIGN KEY (user_id)
        REFERENCES asp_net_users(id) ON DELETE CASCADE
);

CREATE INDEX idx_refresh_tokens_token_hash ON refresh_tokens(token_hash);
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
```

### Refresh Token Rotation Flow

```
1. Client sends refresh token cookie to /auth/refresh
2. Server hashes received token, looks up in DB
3. If found AND not expired AND not revoked:
   a. Mark old token as revoked (set revoked_at)
   b. Generate new refresh token
   c. Store new token hash in DB (link to old via replaced_by_token_hash)
   d. Generate new JWT access token
   e. Set both in httpOnly cookies
   f. Return user data
4. If token is already revoked (REUSE DETECTED):
   a. Revoke ALL tokens for this user (security breach)
   b. Return 401
   c. Log security event
5. If expired or not found:
   a. Return 401
```

### Reuse Detection (Critical Security Feature)

If a revoked refresh token is presented, it means either:
- An attacker stole the token and used it after the legitimate user already refreshed
- OR the legitimate user is trying to use an old token after the attacker refreshed

Either way, ALL sessions for that user should be invalidated. This is a standard security practice recommended by Auth0 and OWASP.

---

## 6. OWASP Security Considerations

### OWASP Session Management Recommendations

| Application Type | Idle Timeout | Absolute Timeout | Remember Me Max |
|---|---|---|---|
| High-value (banking) | 2-5 minutes | 4-8 hours | Not recommended |
| Standard (most apps) | 15-30 minutes | 4-8 hours | 7-14 days |
| Low-risk (content) | 30-60 minutes | 24 hours | 30 days |
| **WitchCityRope** | **15 minutes** | **24 hours** | **14 days** |

### WitchCityRope Classification

WitchCityRope is a **standard-risk application** with **elevated privacy sensitivity**:
- Handles personal safety information (consent, vetting status)
- Contains private community membership data
- Has role-based access to sensitive features (safety coordination)
- But is NOT financial, medical, or government

### Recommended Timeout Strategy

```
Without "Remember Me":
- Access token: 15 minutes
- Refresh token: 24 hours (session cookie)
- Idle timeout: Handled by access token expiry (15 min)
- Absolute timeout: 24 hours (refresh token expiry)

With "Remember Me":
- Access token: 15 minutes (unchanged)
- Refresh token: 14 days (persistent cookie)
- Idle timeout: Still 15 minutes between active requests
- Absolute timeout: 14 days (must re-authenticate)
- Re-authentication: Required for sensitive ops (role changes,
  safety data, consent modifications) regardless of session age
```

### OWASP Cookie Attributes Checklist

| Attribute | Setting | Why |
|---|---|---|
| `HttpOnly` | `true` | Prevents JavaScript access (XSS protection) |
| `Secure` | `true` | HTTPS only transmission |
| `SameSite` | `Strict` | Prevents CSRF (both cookies same-site) |
| `Path` (refresh) | `/auth/refresh` | Limits refresh token to refresh endpoint only |
| `Path` (access) | `/` | Access token sent with all API requests |
| `Domain` | Not set | Restricts to exact host |

### Re-authentication for Sensitive Operations

Even with a valid session, require password re-entry for:
- Changing email or password
- Modifying safety/consent settings
- Changing user roles (admin)
- Viewing other users' emergency contacts
- Deleting account

---

## Comparative Summary: All Decisions

| Decision | Recommendation | Confidence |
|---|---|---|
| Access token duration | 15 minutes | High (95%) |
| Refresh token (no remember me) | 24 hours, session cookie | High (90%) |
| Refresh token (remember me) | 14 days, persistent cookie | High (85%) |
| Refresh token storage | Database with rotation | High (90%) |
| Frontend auth cache | sessionStorage via Zustand persist | High (85%) |
| Wake/resume detection | TanStack Query v5 `refetchOnWindowFocus` | High (90%) |
| Cookie = JWT expiry alignment | Yes, match them | High (90%) |
| CSRF protection | `SameSite=Strict` (sufficient for same-origin BFF) | High (85%) |

---

## Implementation Priority

### Phase 1: Core Auth (Implement First)
1. JWT access token generation (15 min expiry)
2. Access token in httpOnly cookie
3. `/auth/me` endpoint for session validation
4. Zustand auth store with sessionStorage

### Phase 2: Refresh Tokens
1. RefreshToken database table and service
2. `/auth/refresh` endpoint with rotation
3. Refresh token in httpOnly cookie (session-only initially)
4. TanStack Query `useAuthSession` hook with auto-refresh

### Phase 3: Remember Me
1. Add `rememberMe` flag to login request
2. Conditional persistent cookie for refresh token
3. 14-day refresh token expiry when remembered
4. Reuse detection and security logging

### Phase 4: Security Hardening
1. Re-authentication for sensitive operations
2. "Log out all devices" feature
3. Session audit logging
4. Rate limiting on refresh endpoint

---

## Risk Assessment

### High Risk
- **Refresh token theft via XSS**: If XSS vulnerability exists, attacker could trigger refresh endpoint
  - **Mitigation**: `SameSite=Strict`, CSP headers, XSS prevention in React, `Path=/auth/refresh`

### Medium Risk
- **Stale auth state in Zustand after token refresh fails**: User sees authenticated UI but API calls fail
  - **Mitigation**: TanStack Query error handlers clear Zustand state on 401, redirect to login

### Low Risk
- **Database refresh token table growth**: Tokens accumulate over time
  - **Mitigation**: Background job to clean expired/revoked tokens older than 30 days

---

## Research Sources

- [OWASP Session Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html)
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [Auth0: Token Best Practices](https://auth0.com/docs/secure/tokens/token-best-practices)
- [Auth0: What Are Refresh Tokens](https://auth0.com/blog/refresh-tokens-what-are-they-and-when-to-use-them/)
- [Code Maze: HttpOnly Cookie in .NET Core for Auth and Refresh Tokens](https://code-maze.com/how-to-use-httponly-cookie-in-net-core-for-authentication-and-refresh-token-actions/)
- [BFF Pattern Security](https://dev.to/damikun/web-app-security-understanding-the-meaning-of-the-bff-pattern-i85)
- [Curity: Token Handler Pattern](https://curity.io/resources/learn/the-token-handler-pattern/)
- [TanStack Query: Window Focus Refetching](https://tanstack.com/query/latest/docs/framework/react/guides/window-focus-refetching)
- [JWT in Practice: Refresh Tokens and Best Practices](https://dev.to/gabrielle_eduarda_776996b/jwt-in-practice-part-2-refresh-tokens-expiration-and-best-practices-20p2)
- [WorkOS: Session Management Best Practices](https://workos.com/blog/session-management-best-practices)
- [LogRocket: Persistent Login with Refresh Token Rotation](https://blog.logrocket.com/persistent-login-in-react-using-refresh-token-rotation/)
- [ASP.NET Core JWT and Refresh Token with HttpOnly Cookies](https://alimozdemir.com/posts/aspnet-core-jwt-and-refresh-token-with-httponly-cookies)
- [React Query Auth Token Refresh Pattern](https://elazizi.com/posts/react-query-auth-token-refresh/)
- [Zustand Persist Middleware Guide](https://sanjewa.com/blogs/zustand-persistence-middleware-guide/)
- [JWT Token Lifecycle Management](https://skycloak.io/blog/jwt-token-lifecycle-management-expiration-refresh-revocation-strategies/)
