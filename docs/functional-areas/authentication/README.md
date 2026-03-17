# Authentication System Documentation
<!-- Last Updated: 2026-03-17 -->
<!-- Version: 4.0 -->
<!-- Owner: Authentication Team -->
<!-- Status: Active - Dual-Cookie BFF with Refresh Token Rotation -->

## Overview
The WitchCityRope authentication system manages user identity, access control, and session management for the rope bondage community platform. The system uses a **secure BFF (Backend-for-Frontend) pattern** with dual httpOnly cookies (`auth-token` + `refresh-token`), refresh token rotation, and a React + TypeScript frontend.

## Quick Links
- **Current Requirements**: [current-state/business-requirements.md](current-state/business-requirements.md)
- **Technical Design**: [current-state/functional-design.md](current-state/functional-design.md)
- **BFF Implementation Guide**: [bff-authentication-implementation-guide.md](bff-authentication-implementation-guide.md)
- **Frontend Auth Patterns**: [/docs/standards-processes/frontend/authentication-pattern-guide.md](/docs/standards-processes/frontend/authentication-pattern-guide.md)
- **JWT Service-to-Service Auth**: [jwt-service-to-service-auth.md](jwt-service-to-service-auth.md)
- **User Flows**: [current-state/user-flows.md](current-state/user-flows.md)
- **Test Coverage**: [current-state/test-coverage.md](current-state/test-coverage.md)
- **Active Work**: [new-work/status.md](new-work/status.md)

## Key Concepts
- **Dual-Cookie BFF Authentication**: `auth-token` (15-min JWT, session cookie) + `refresh-token` (DB-backed, Path=/api/auth)
- **Refresh Token Rotation**: Each refresh revokes the old token and issues a new one; reuse detection invalidates the token family
- **RememberMe**: ON = 14-day persistent refresh cookie, OFF = 24-hour session cookie
- **Zero localStorage Exposure**: No JWT tokens accessible to JavaScript (XSS protection)
- **SameSite=Strict**: All environments same-origin (dev via Vite proxy, staging/prod via nginx)
- **Frontend Refresh Strategy**: 401 interceptor with silent retry, visibilitychange listener, 13-minute proactive interval
- **Rate Limiting**: login/register at 5r/m, refresh/user/logout at 30r/m
- **Role-Based Access**: Administrator, Member, EventOrganizer, etc.
- **Vetting System**: Members must be vetted for social event access
- **Age Verification**: 21+ requirement enforced
- **Scene Names**: Public display names for privacy

## Critical Implementation Notes

### Dual-Cookie BFF Authentication Pattern
**Current Architecture**: React 18 + TypeScript + Vite frontend, ASP.NET Core 10 Minimal API backend.

- **`auth-token`**: 15-minute JWT in httpOnly session cookie (Path=/, SameSite=Strict)
- **`refresh-token`**: DB-backed token in httpOnly cookie (Path=/api/auth, SameSite=Strict)
- **Token refresh**: Rotation with reuse detection; frontend refreshes proactively at 13-minute intervals
- **Backwards Compatibility**: Dual authentication support (Bearer header + Cookie) maintained
- **Complete implementation guide**: [bff-authentication-implementation-guide.md](bff-authentication-implementation-guide.md)
- **All auth models**: `Features/Authentication/Models/` (vertical slice architecture)
- **Zustand**: Uses sessionStorage for UI cache only; no auth tokens in frontend stores

### ARCHIVED: Legacy Patterns
- **localStorage JWT pattern**: Archived at `/docs/_archive/authentication-localstorage-legacy-2025-09-12/`
- **Blazor Server patterns**: Archived at `/docs/_archive/authentication-blazor-legacy-2025-08-19/`

### Current Features
- Dual-cookie BFF authentication with refresh token rotation
- RememberMe with configurable session persistence (14-day / 24-hour)
- 15-minute JWT lifetime with proactive frontend refresh
- XSS and CSRF protection via httpOnly cookies + SameSite=Strict
- Rate limiting on all auth endpoints
- Email/password login (React and API)
- User registration with scene names
- Role-based authorization
- Account lockout protection
- Multi-tab session synchronization
- Silent token refresh (zero authentication timeouts)

### Not Yet Implemented
- Two-factor authentication (infrastructure exists)
- OAuth/Social login
- Password reset flow
- Email verification enforcement

## Related Areas
- **Events Management**: Uses authentication for RSVP/ticket access
- **Membership Vetting**: Determines social event access
- **User Dashboard**: Displays user-specific content

## Contact
- Technical Owner: Authentication Team
- Business Owner: Community Safety Team
- Last Major Update: 2026-03-17 (Dual-cookie refresh rotation documentation update)
