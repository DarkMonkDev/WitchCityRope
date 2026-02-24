# Security Audit Report - WitchCityRope

**Date:** 2026-02-23
**Scope:** Full application security review (API, Frontend, Nginx, Configuration)
**Auditor:** Internal review with AI-assisted analysis
**Parity Reference:** DarkMonk-DO-Migration security audit (similar findings, nginx hardening implemented there first)

## Executive Summary

A comprehensive security audit identified 16+ vulnerabilities across the WitchCityRope application stack. All critical and high-severity code issues have been remediated in a single PR. Secrets rotation is deferred (private repo, user's decision on timing).

## Findings

### Critical Severity

| # | Finding | Location | Status | Fix |
|---|---------|----------|--------|-----|
| 1 | **Unauthenticated debug endpoint** exposing JWT token previews, JTI values, and all cookie data | `apps/api/.../AuthenticationEndpoints.cs` `/api/auth/debug-status` | **FIXED** | Endpoint removed entirely |
| 2 | **Hardcoded database password** in fallback connection strings (4 locations) | `apps/api/Program.cs` lines 67, 118, 183, 289 | **FIXED** | Replaced with `throw InvalidOperationException` for fail-fast behavior |
| 3 | **Hardcoded JWT secret** in fallback configuration | `apps/api/Program.cs` line 183 | **FIXED** | Replaced with fail-fast exception |
| 4 | **Hangfire dashboard open to all users** (authorization filter always returns true) | `apps/api/Program.cs` `HangfireAuthorizationFilter` | **FIXED** | Now requires authenticated user with Administrator role |

### High Severity

| # | Finding | Location | Status | Fix |
|---|---------|----------|--------|-----|
| 5 | **CORS AllowAnyOrigin policy** defined (not actively used but available) | `apps/api/Program.cs` `ReactDevelopment` policy | **FIXED** | Policy removed; environment-aware CORS (Development vs Production) |
| 6 | **XSS via dangerouslySetInnerHTML** - 4 locations rendering unsanitized HTML | `CmsPage.tsx`, `CmsRevisionCard.tsx`, `EventDetailPage.tsx` (x2) | **FIXED** | DOMPurify sanitization applied to all 4 locations |
| 7 | **CORS origin reflection** in nginx (`$http_origin` reflected back) | `apps/web/nginx.conf` lines 52-67 | **FIXED** | CORS headers removed from nginx (handled by API middleware; nginx proxies same-origin) |
| 8 | **Missing CSP headers** or overly permissive CSP | `apps/web/nginx.conf`, `nginx/staging.conf` | **FIXED** | Tightened CSP: removed `unsafe-inline`/`unsafe-eval` from script-src, added Authorize.net domains |
| 9 | **No rate limiting** on authentication endpoints | All nginx configs | **FIXED** | Added `auth_limit` (5r/m) and `api_limit` (30r/s) zones |

### Medium Severity

| # | Finding | Location | Status | Fix |
|---|---------|----------|--------|-----|
| 10 | **Weak password policy** (8 chars, no special char required, 1 unique char) | `apps/api/Program.cs` Identity config | **FIXED** | 10 chars, special char required, 4 unique chars |
| 11 | **Missing HSTS header** in dev nginx | `apps/web/nginx.conf` | **FIXED** | Added `Strict-Transport-Security` |
| 12 | **Missing Permissions-Policy header** | All nginx configs | **FIXED** | Added `Permissions-Policy` restricting geolocation, microphone, camera, payment, USB |
| 13 | **Console.debug logging in production** exposing request/response details | `apps/web/src/lib/api/client.ts` | **FIXED** | Guarded with `import.meta.env.DEV` |
| 14 | **No request size limit** in dev nginx | `apps/web/nginx.conf` | **FIXED** | Added `client_max_body_size 10M` |
| 15 | **Orphaned rate limiting config** in staging nginx (outside server block) | `nginx/staging.conf` lines 199-212 | **FIXED** | Moved rate limiting into API server block |
| 16 | **No version-controlled production nginx config** | Generated inline by deploy script | **FIXED** | Created `nginx/production.conf` with full security headers |

### Low Severity / Informational

| # | Finding | Location | Status | Fix |
|---|---------|----------|--------|-----|
| 17 | **Secrets committed in git history** (`.env.development`, `.env.staging`, `.env`) | Git history | **DEFERRED** | Private repo; documented in secrets inventory for rotation |
| 18 | **In-memory token blacklist** (not Redis-backed) | Token blacklist service | **ACCEPTED** | Acceptable for single-instance deployment |
| 19 | **No token refresh rotation** | JWT refresh flow | **DEFERRED** | Functional improvement, not critical security fix |

## Files Modified

| File | Changes |
|------|---------|
| `apps/api/Features/Authentication/Endpoints/AuthenticationEndpoints.cs` | Removed debug endpoint (55 lines deleted) |
| `apps/api/Program.cs` | Secured Hangfire, removed hardcoded secrets, fixed CORS, strengthened password policy |
| `apps/web/package.json` | Added `dompurify` dependency |
| `apps/web/src/lib/utils/sanitizeHtml.ts` | **NEW** - HTML sanitization utility wrapping DOMPurify |
| `apps/web/src/features/cms/components/CmsPage.tsx` | Added DOMPurify sanitization |
| `apps/web/src/features/cms/components/CmsRevisionCard.tsx` | Added DOMPurify sanitization |
| `apps/web/src/pages/events/EventDetailPage.tsx` | Added DOMPurify sanitization (2 locations) |
| `apps/web/src/lib/api/client.ts` | Guarded console.debug for dev-only |
| `apps/web/nginx.conf` | Full security hardening (headers, CSP, rate limiting, removed CORS reflection) |
| `nginx/staging.conf` | Added CSP, Permissions-Policy, fixed rate limiting placement |
| `nginx/production.conf` | **NEW** - Production-grade nginx config (version-controlled) |
| `deployment/deploy-linux.sh` | Updated to use static nginx configs instead of generating inline |

## Deferred Items

1. **Secrets rotation** - All exposed secrets documented in `exposed-secrets-inventory-2026-02-23.md`. User decides when to rotate.
2. **Git history rewrite** - Secrets exist in git history. Repo is private. Rotation is preferred over history rewrite.
3. **Redis-backed token blacklist** - Current in-memory approach is acceptable for single-instance deployment.
4. **Token refresh rotation** - Improvement but not a security-critical fix.

## Parity with DarkMonk-DO-Migration Audit

The DarkMonk audit found similar issues and implemented nginx hardening there first. This audit achieves parity:
- HSTS headers: Matching
- CSP headers: Matching (with app-specific Authorize.net allowances)
- Rate limiting: Matching
- Permissions-Policy: Matching
- Security headers suite: Matching
