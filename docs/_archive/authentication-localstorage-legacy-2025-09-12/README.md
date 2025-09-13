# Legacy Authentication with localStorage - ARCHIVED

<!-- Archive Date: 2025-09-12 -->
<!-- Reason: Security vulnerability and authentication timeout issues -->
<!-- Replacement: BFF authentication pattern with httpOnly cookies -->

## Why This Was Archived

This authentication implementation was archived on **September 12, 2025** due to critical security and user experience issues:

### Security Issues
- **XSS Vulnerability**: JWT tokens stored in `localStorage` were accessible to JavaScript, creating XSS attack vectors
- **No CSRF Protection**: No built-in protection against cross-site request forgery attacks
- **Token Exposure**: Tokens visible in browser developer tools and accessible to any script

### User Experience Problems
- **Authentication Timeouts**: Users frequently logged out due to lack of automatic token refresh
- **Multi-tab Issues**: Authentication state not synchronized across browser tabs
- **Manual Token Management**: Complex client-side token handling prone to errors

## What Replaced This

The **BFF (Backend-for-Frontend) authentication pattern** with httpOnly cookies implemented in `/apps/api/Features/Authentication/Endpoints/AuthenticationEndpoints.cs`:

### New Security Features
- **httpOnly Cookies**: Tokens never exposed to JavaScript (XSS protection)
- **Automatic CSRF Protection**: SameSite=Strict cookie settings
- **Silent Token Refresh**: Prevents authentication interruptions
- **Server-side Session Management**: Secure cookie handling

### Improved User Experience
- **No Authentication Timeouts**: Silent refresh keeps users logged in
- **Multi-tab Synchronization**: Cookies automatically shared across tabs
- **Zero Token Management**: No client-side token handling required

## Files Archived

- `useAuth.ts` - TanStack Query hooks with localStorage token management

## Migration Details

**From**: JWT tokens in localStorage
```typescript
// Old pattern - INSECURE
localStorage.setItem('auth_token', loginResponse.token)
fetch('/api/data', {
  headers: { 'Authorization': `Bearer ${localStorage.getItem('auth_token')}` }
})
```

**To**: httpOnly cookies with BFF pattern
```typescript
// New pattern - SECURE
fetch('/api/auth/login', {
  method: 'POST',
  credentials: 'include', // httpOnly cookies
  body: JSON.stringify(credentials)
})
// Token automatically included in subsequent requests
```

## References

- **Implementation Details**: `/session-work/2025-09-12/bff-authentication-implementation-summary.md`
- **Security Analysis**: `/docs/functional-areas/authentication/analysis/2025-09-12-authentication-issue-analysis-report.md`
- **Current Implementation**: `/apps/api/Features/Authentication/Endpoints/AuthenticationEndpoints.cs`

## DO NOT RESTORE

This implementation should **NEVER** be restored to active use due to:
1. **Security vulnerabilities** that expose user sessions to XSS attacks
2. **Poor user experience** with frequent authentication timeouts
3. **Architecture violations** of modern security best practices

For historical reference only. All new authentication work must use the BFF pattern with httpOnly cookies.