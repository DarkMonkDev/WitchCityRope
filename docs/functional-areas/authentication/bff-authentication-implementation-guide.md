# BFF Authentication Pattern Implementation
<!-- Last Updated: 2026-03-17 -->
<!-- Version: 2.0 -->
<!-- Owner: Authentication Team -->
<!-- Status: Active -->

## Executive Summary

The Backend-for-Frontend (BFF) authentication pattern uses dual httpOnly cookies with refresh token rotation to provide secure, seamless authentication. This implementation eliminates XSS vulnerability from localStorage tokens and prevents authentication timeouts through automatic token refresh with rotation.

## Problem Statement

**Original Issue**: Users experiencing frequent authentication timeouts ("I keep waiting a bit and then being asked to login again") because the system used JWT tokens in localStorage instead of the designed httpOnly cookie approach.

**Root Cause**: Architecture deviation from original security design leading to:
- No automatic token refresh mechanism
- XSS vulnerability through localStorage token exposure
- No multi-tab session synchronization

## Solution Implemented

### 1. Dual-Cookie Authentication

The system uses two httpOnly cookies:

| Cookie | Purpose | Lifetime | Path | Details |
|--------|---------|----------|------|---------|
| `auth-token` | JWT access token | 15-minute JWT, session cookie | `/` | Short-lived for security; session cookie (deleted when browser closes) |
| `refresh-token` | DB-backed refresh token | Depends on RememberMe | `/api/auth` | Used only for token refresh endpoint |

### 2. Authentication Endpoints

**Modified `/apps/api/Features/Authentication/Endpoints/AuthenticationEndpoints.cs`:**

#### Login Endpoint (`POST /api/auth/login`)
- Validates credentials, generates 15-minute JWT and DB-backed refresh token
- Sets `auth-token` httpOnly cookie (session cookie, SameSite=Strict)
- Sets `refresh-token` httpOnly cookie (Path=/api/auth, SameSite=Strict)
- **RememberMe=ON**: refresh cookie persists 14 days
- **RememberMe=OFF**: refresh cookie is a 24-hour session cookie
- Returns user info only (no tokens in response body)

#### Logout Endpoint (`POST /api/auth/logout`)
- Revokes the refresh token in the database
- Deletes both httpOnly cookies server-side
- Guaranteed logout even with invalid cookies

#### User Info Endpoint (`GET /api/auth/user`)
- Reads JWT from `auth-token` cookie, validates, returns user info
- Clears invalid cookies automatically
- No token exposure to client-side JavaScript

#### Token Refresh Endpoint (`POST /api/auth/refresh`)
- Reads `refresh-token` cookie, validates against database
- **Rotation**: Revokes the old refresh token, issues a new one
- **Reuse detection**: If a revoked token is presented, all tokens for that user family are invalidated (potential theft indicator)
- Issues new 15-minute JWT in `auth-token` cookie
- Issues new refresh token in `refresh-token` cookie
- Silent operation -- no user interaction required

### 3. RememberMe Behavior

| Setting | Refresh Token Cookie | Auth Token Cookie | Session Duration |
|---------|---------------------|-------------------|------------------|
| **ON** | 14-day persistent cookie | Session cookie (15-min JWT) | Up to 14 days with automatic refresh |
| **OFF** | 24-hour session cookie | Session cookie (15-min JWT) | Until browser closes or 24 hours of inactivity |

In both cases, the `auth-token` JWT expires every 15 minutes. The frontend refresh strategy (below) ensures it is renewed transparently before expiration.

### 4. Enhanced JWT Service

**Modified `/apps/api/Services/JwtService.cs` and `/apps/api/Services/IJwtService.cs`:**

- `IsTokenNearExpiry(string token)`: Checks if token expires within 2 minutes
- `ValidateTokenStructure(string token)`: Validates without checking expiry (for refresh scenarios)
- JWT lifetime: **15 minutes**

### 5. Refresh Token Storage

Refresh tokens are stored in the database (not derived from JWTs):
- Token hash, user ID, expiry, revocation status, device info
- Models located in `Features/Authentication/Models/` (vertical slice architecture)
- Rotation creates a new token record and revokes the old one atomically

## Technical Architecture

### Cookie Configuration
```
auth-token cookie:
  HttpOnly = true
  Secure = true (all environments)
  SameSite = Strict
  Path = /

refresh-token cookie:
  HttpOnly = true
  Secure = true (all environments)
  SameSite = Strict
  Path = /api/auth
```

**SameSite=Strict** works in all environments because the frontend and API share the same origin:
- **Development**: Vite proxy forwards `/api/*` to the API container
- **Staging/Production**: nginx reverse proxy serves both frontend and API

### Authentication Flow
```
1. User logs in -> POST /api/auth/login
2. API validates credentials
3. API generates 15-minute JWT + DB-backed refresh token
4. API sets auth-token cookie (session) + refresh-token cookie
5. API returns user info (no tokens in response)
6. Subsequent requests include auth-token cookie automatically
7. Frontend proactively refreshes before JWT expiry (see below)
8. Refresh endpoint rotates refresh token + issues new JWT
9. Logout revokes refresh token + clears both cookies
```

### Frontend Token Refresh Strategy

The React frontend uses three complementary refresh mechanisms:

1. **401 Interceptor**: API client intercepts 401 responses, calls `/api/auth/refresh` silently, then retries the original request. Prevents queuing multiple simultaneous refresh calls.

2. **Visibility Change Listener**: When the browser tab regains focus (`visibilitychange` event), checks if a refresh is needed. Handles the case where a user returns to a tab after the JWT has expired.

3. **Proactive Interval**: A 13-minute interval timer triggers refresh before the 15-minute JWT expires. Prevents the JWT from ever actually expiring during active use.

All three mechanisms use `credentials: 'include'` so cookies are sent automatically.

### Rate Limiting

| Endpoint Group | Rate Limit |
|---------------|------------|
| Login, Register | 5 requests/minute |
| Refresh, User, Logout | 30 requests/minute |

## Security Improvements

### XSS Protection
- **Before**: JWT tokens exposed in localStorage vulnerable to XSS attacks
- **After**: Tokens in httpOnly cookies inaccessible to JavaScript

### CSRF Protection
- **SameSite=Strict**: Prevents cross-site cookie transmission
- **Secure Flag**: HTTPS-only transmission in all environments
- **Refresh endpoint**: CSRF validation intentionally disabled — SameSite=Strict + httpOnly + Path scoping + token rotation provide equivalent protection. CSRF on refresh caused a catch-22: when the JWT expired, the user's identity changed to anonymous, causing CSRF validation to fail with "meant for a different claims-based user" and blocking the refresh that was needed to restore the identity.

### Token Theft Mitigation
- **Refresh rotation**: Each refresh revokes the old token, so a stolen token can only be used once
- **Reuse detection**: Presenting a revoked token invalidates the entire token family

### Session Management
- **Automatic Expiry**: JWT expires in 15 minutes; refresh token expires per RememberMe setting
- **Clean Logout**: Server-side cookie deletion and token revocation
- **Multi-tab Sync**: Cookies automatically synchronized across tabs

## Backwards Compatibility

The implementation maintains backwards compatibility:
- **JWT Bearer tokens** still work via Authorization header
- **Existing API clients** continue to function unchanged
- **Cookie source**: OnMessageReceived checks Authorization header first, then falls back to `auth-token` cookie

## Performance Impact

- **Minimal overhead**: Cookie parsing adds <1ms per request
- **Reduced client complexity**: No token management in JavaScript
- **Silent refresh**: No user interruption for token renewal

## Success Metrics

### Resolved Issues
- No more authentication timeouts: Silent refresh prevents interruptions
- XSS vulnerability eliminated: Tokens never exposed to JavaScript
- Multi-tab synchronization: Cookies shared automatically
- Seamless user experience: No re-login required during sessions

### Technical Achievements
- BFF pattern implemented: Industry-standard security architecture
- Backwards compatibility: Zero breaking changes for existing clients
- Refresh token rotation: Limits blast radius of token theft
- RememberMe support: User-controlled session persistence

## Files Modified

1. **`/apps/api/Features/Authentication/Endpoints/AuthenticationEndpoints.cs`**
   - Login sets dual cookies with RememberMe support
   - Logout revokes refresh token and clears both cookies
   - `/api/auth/user` endpoint reads from auth-token cookie
   - `/api/auth/refresh` endpoint with token rotation

2. **`/apps/api/Features/Authentication/Models/`**
   - Refresh token entity with hash, expiry, revocation tracking
   - All auth models in vertical slice structure

3. **`/apps/api/Services/JwtService.cs`** and **`/apps/api/Services/IJwtService.cs`**
   - `IsTokenNearExpiry` method
   - `ValidateTokenStructure` method
   - 15-minute JWT lifetime

4. **`/apps/api/Program.cs`**
   - JWT Bearer configuration with cookie fallback
   - OnMessageReceived event for dual authentication support
   - Rate limiting configuration

## Conclusion

The BFF authentication pattern with dual-cookie refresh token rotation provides secure, seamless authentication. The 15-minute JWT lifetime with automatic rotation limits exposure from token theft, while RememberMe support gives users control over session persistence. The frontend's triple refresh strategy (401 interceptor, visibility listener, proactive interval) ensures tokens are always fresh without user interruption.
