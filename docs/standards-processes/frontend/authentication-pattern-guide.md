# Authentication Pattern Guide - WitchCityRope Project

**Last Updated**: 2025-11-23
**Status**: ACTIVE - This is the official authentication pattern
**Audience**: Backend developers and React developers working on authentication

## Table of Contents
- [Quick Reference](#quick-reference)
- [Overview](#overview)
- [Architecture](#architecture)
- [Standard Pattern](#standard-pattern)
- [Implementation Guide](#implementation-guide)
- [CSRF Protection](#csrf-protection)
- [Error Handling](#error-handling)
- [Testing](#testing)
- [Migration Notes](#migration-notes)

---

## Quick Reference

### ✅ DO - Use These Patterns

**Reading Auth State** (React components):
```typescript
import { useUser, useIsAuthenticated } from '@/stores/authStore'

const user = useUser()                    // Get current user data
const isAuthenticated = useIsAuthenticated() // Check if logged in
```

**Auth Operations** (login, logout, register):
```typescript
import { useLogin, useLogout, useRegister } from '@/features/auth/api/mutations'

const loginMutation = useLogin()
const logoutMutation = useLogout()
const registerMutation = useRegister()

// Usage
loginMutation.mutate({ email, password })
logoutMutation.mutate()
registerMutation.mutate({ email, password, sceneName })
```

**Getting Current User Data** (when you need server-verified user):
```typescript
import { useCurrentUser } from '@/lib/api/hooks/useAuth'

const { data: currentUser } = useCurrentUser()
```

### ❌ DON'T - Obsolete Patterns (Deleted)

```typescript
// ❌ WRONG - AuthContext deleted
import { useAuth } from '@/hooks/useAuth'
const { user, login, logout } = useAuth()

// ❌ WRONG - authService deleted
import { authService } from '@/services/authService'
await authService.login(credentials)

// ❌ WRONG - AuthProvider removed from main.tsx
<AuthProvider>...</AuthProvider>
```

---

## Overview

WitchCityRope uses a **hybrid authentication pattern** combining:
- **TanStack Query v5 mutations** for auth operations (login, logout, register)
- **Zustand v4 store** for global authentication state
- **.NET 9 httpOnly cookies** for session management
- **CSRF token validation** for state-changing operations

This pattern was selected after comprehensive research (November 2025) as the industry standard for .NET 9 + React applications.

### Why This Pattern?

1. **Industry Standard**: Recommended by Microsoft and React ecosystem leaders
2. **Security**: HttpOnly cookies prevent XSS attacks, CSRF tokens prevent CSRF attacks
3. **Developer Experience**: Automatic loading/error states, no manual state management
4. **Maintainability**: Single source of truth, clear separation of concerns
5. **Performance**: Optimized caching, minimal re-renders

---

## Architecture

### High-Level Flow

```
┌─────────────────┐
│  React Component│
└────────┬────────┘
         │ Uses
         ↓
┌─────────────────────────────────┐
│   TanStack Query Mutation       │
│  (useLogin, useLogout, etc.)    │
└────────┬────────────────────────┘
         │ Calls
         ↓
┌─────────────────────────────────┐
│    API Client (axios)           │
│  - Includes CSRF token          │
│  - HttpOnly cookie auth         │
└────────┬────────────────────────┘
         │ HTTP Request
         ↓
┌─────────────────────────────────┐
│  .NET 9 Minimal API Backend     │
│  - Validates CSRF token         │
│  - Sets httpOnly cookie         │
│  - Returns user data            │
└────────┬────────────────────────┘
         │ Success Response
         ↓
┌─────────────────────────────────┐
│   Mutation onSuccess Handler    │
│  - Updates Zustand store        │
│  - Clears/invalidates queries   │
│  - Navigates to next page       │
└─────────────────────────────────┘
```

### Component Architecture

```
apps/web/src/
├── features/auth/api/
│   └── mutations.ts              ← ALL auth mutations (login, logout, register)
├── stores/
│   └── authStore.ts              ← Zustand auth state (user data, isAuthenticated)
├── hooks/
│   └── useCSRFToken.ts           ← CSRF token management
├── lib/api/hooks/
│   └── useAuth.ts                ← useCurrentUser() only (server-verified user data)
└── api/
    └── client.ts                 ← Axios client with CSRF interceptor
```

---

## Standard Pattern

### 1. Login Flow

**File**: `/apps/web/src/features/auth/api/mutations.ts`

```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '@/api/client'
import { useAuthActions } from '@/stores/authStore'

export function useLogin() {
  const queryClient = useQueryClient()
  const { login } = useAuthActions()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: async (credentials: LoginRequest) => {
      const response = await api.post('/api/auth/login', credentials)
      return response.data
    },
    onSuccess: async (data) => {
      // 1. Update Zustand store with user data
      login(data.user)

      // 2. Initialize CSRF protection
      await initializeCSRFProtection()

      // 3. Invalidate user-related queries
      queryClient.invalidateQueries({ queryKey: ['user'] })
      queryClient.invalidateQueries({ queryKey: ['auth'] })

      // 4. Navigate to dashboard or return URL
      const returnUrl = data.returnUrl || '/dashboard'
      navigate(returnUrl, { replace: true })
    },
    retry: false, // Don't retry failed login attempts
  })
}
```

**Usage in Component**:
```typescript
import { useLogin } from '@/features/auth/api/mutations'

export const LoginPage = () => {
  const loginMutation = useLogin()

  const handleSubmit = (data: LoginFormData) => {
    loginMutation.mutate({
      email: data.email,
      password: data.password,
    })
  }

  return (
    <form onSubmit={handleSubmit}>
      {/* Form fields */}
      <button
        type="submit"
        disabled={loginMutation.isPending}
      >
        {loginMutation.isPending ? 'Logging in...' : 'Login'}
      </button>
      {loginMutation.error && (
        <Alert color="red">{loginMutation.error.message}</Alert>
      )}
    </form>
  )
}
```

### 2. Logout Flow

**File**: `/apps/web/src/features/auth/api/mutations.ts`

```typescript
export function useLogout() {
  const queryClient = useQueryClient()
  const { logout } = useAuthActions()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: async () => {
      // Check if CSRF token exists, fetch if needed
      let csrfToken = getCSRFToken()
      if (!csrfToken) {
        await initializeCSRFProtection()
        csrfToken = getCSRFToken()
      }

      // Call logout endpoint (CSRF token sent via interceptor)
      await api.post('/api/auth/logout')
    },
    onSuccess: () => {
      // 1. Clear Zustand auth store
      logout()

      // 2. Clear sessionStorage (Zustand persistence)
      sessionStorage.removeItem('auth-store')

      // 3. Clear React Query cache
      // CRITICAL: Use clear() NOT invalidateQueries()
      queryClient.clear()

      // 4. Navigate to home page
      navigate('/', { replace: true })
    },
    onError: (error) => {
      // CRITICAL: Still clear local state even on error
      // Backend logout may have succeeded even with error response
      logout()
      sessionStorage.removeItem('auth-store')
      queryClient.clear()
      navigate('/', { replace: true })
    },
    retry: false,
  })
}
```

**Usage in Component**:
```typescript
import { useLogout } from '@/features/auth/api/mutations'

export const UserMenu = () => {
  const logoutMutation = useLogout()

  return (
    <button
      onClick={() => logoutMutation.mutate()}
      disabled={logoutMutation.isPending}
    >
      {logoutMutation.isPending ? 'Logging out...' : 'Logout'}
    </button>
  )
}
```

### 3. Reading Auth State

**File**: `/apps/web/src/stores/authStore.ts` (Zustand store)

```typescript
import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface AuthState {
  user: UserDto | null
  isAuthenticated: boolean
  login: (user: UserDto) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      isAuthenticated: false,
      login: (user) => set({ user, isAuthenticated: true }),
      logout: () => set({ user: null, isAuthenticated: false }),
    }),
    {
      name: 'auth-store',
      storage: sessionStorage,
    }
  )
)

// Convenience selectors
export const useUser = () => useAuthStore((state) => state.user)
export const useIsAuthenticated = () => useAuthStore((state) => state.isAuthenticated)
export const useAuthActions = () => useAuthStore((state) => ({
  login: state.login,
  logout: state.logout,
}))
```

**Usage in Components**:
```typescript
import { useUser, useIsAuthenticated } from '@/stores/authStore'

export const Navigation = () => {
  const user = useUser()
  const isAuthenticated = useIsAuthenticated()

  return (
    <nav>
      {isAuthenticated && user ? (
        <span>Welcome, {user.sceneName}</span>
      ) : (
        <Link to="/login">Login</Link>
      )}
    </nav>
  )
}
```

---

## Implementation Guide

### For Backend Developers

#### 1. Authentication Endpoints

**Login Endpoint** (`/api/auth/login`):
```csharp
app.MapPost("/api/auth/login", async (
    LoginRequest request,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) =>
{
    // 1. Validate credentials
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user == null)
        return Results.Problem("Invalid credentials", statusCode: 401);

    var result = await signInManager.CheckPasswordSignInAsync(
        user, request.Password, lockoutOnFailure: true);

    if (!result.Succeeded)
        return Results.Problem("Invalid credentials", statusCode: 401);

    // 2. Sign in user (creates httpOnly cookie)
    await signInManager.SignInAsync(user, isPersistent: false);

    // 3. Return user data
    var userDto = new UserDto
    {
        Id = user.Id,
        Email = user.Email,
        SceneName = user.SceneName,
        Role = user.Role,
        // ... other fields
    };

    return Results.Ok(new { user = userDto });
})
.AllowAnonymous();
```

**Logout Endpoint** (`/api/auth/logout`):
```csharp
app.MapPost("/api/auth/logout", async (
    SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
})
.RequireAuthorization()
.RequireAntiforgeryToken(); // CRITICAL: CSRF protection
```

#### 2. CSRF Protection Setup

**Program.cs** - Configure antiforgery:
```csharp
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = ".AspNetCore.Antiforgery";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
```

**CSRF Token Endpoint** (`/api/auth/csrf-token`):
```csharp
app.MapGet("/api/auth/csrf-token", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
        new CookieOptions
        {
            HttpOnly = false, // Must be readable by JavaScript
            SameSite = SameSiteMode.Strict,
            Secure = true
        });
    return Results.Ok(new { token = tokens.RequestToken });
})
.AllowAnonymous();
```

#### 3. Protected Endpoint Example

```csharp
app.MapPost("/api/events/{eventId}/register", async (
    Guid eventId,
    RegistrationRequest request,
    IEventService eventService,
    ClaimsPrincipal user) =>
{
    var userId = user.GetUserId(); // Extension method to get user ID from claims
    await eventService.RegisterUserAsync(eventId, userId, request);
    return Results.Ok();
})
.RequireAuthorization()
.RequireAntiforgeryToken(); // CSRF protection for state changes
```

### For React Developers

#### 1. Creating New Auth Mutations

All auth mutations go in `/apps/web/src/features/auth/api/mutations.ts`:

```typescript
export function usePasswordReset() {
  const navigate = useNavigate()

  return useMutation({
    mutationFn: async ({ email }: { email: string }) => {
      const response = await api.post('/api/auth/forgot-password', { email })
      return response.data
    },
    onSuccess: () => {
      navigate('/login?message=Password reset email sent')
    },
    retry: false,
  })
}
```

#### 2. Protected Routes

```typescript
import { useIsAuthenticated } from '@/stores/authStore'
import { Navigate } from 'react-router-dom'

export const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const isAuthenticated = useIsAuthenticated()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}
```

#### 3. Role-Based Access

```typescript
import { useUser } from '@/stores/authStore'
import type { components } from '@witchcityrope/shared-types'

type UserRole = components['schemas']['UserRole']

export const AdminPanel = () => {
  const user = useUser()

  if (user?.role !== ('Administrator' as UserRole)) {
    return <Alert>Access denied</Alert>
  }

  return <div>Admin content</div>
}
```

---

## CSRF Protection

### How It Works

WitchCityRope uses the **two-cookie pattern** for CSRF protection:

1. **XSRF-TOKEN** cookie: Readable by JavaScript, sent to frontend
2. **.AspNetCore.Antiforgery** cookie: HttpOnly, validated by backend
3. **X-XSRF-TOKEN** header: Frontend sends token in this header

### Automatic CSRF Handling

**File**: `/apps/web/src/api/client.ts`

The axios client automatically:
1. Fetches CSRF token before first request
2. Includes `X-XSRF-TOKEN` header in all requests
3. Refreshes token if expired

```typescript
import axios from 'axios'
import { getCSRFToken, initializeCSRFProtection } from '@/hooks/useCSRFToken'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true, // Send httpOnly cookies
})

// Request interceptor - add CSRF token
api.interceptors.request.use(async (config) => {
  let token = getCSRFToken()

  if (!token) {
    await initializeCSRFProtection()
    token = getCSRFToken()
  }

  if (token) {
    config.headers['X-XSRF-TOKEN'] = token
  }

  return config
})
```

### Manual CSRF Initialization

Sometimes you need to initialize CSRF protection manually (e.g., after login):

```typescript
import { initializeCSRFProtection } from '@/hooks/useCSRFToken'

// In mutation onSuccess
onSuccess: async (data) => {
  login(data.user)
  await initializeCSRFProtection() // Fetch fresh CSRF token
  navigate('/dashboard')
}
```

---

## Error Handling

### Standard Error Pattern

**File**: `/apps/web/src/lib/api/utils/errors.ts`

```typescript
export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    // RFC 9457 Problem Details format
    if (error.response?.data?.title) {
      return error.response.data.title
    }
    // Fallback to generic message
    return error.response?.data?.message || 'An error occurred'
  }
  return 'An unexpected error occurred'
}
```

**Usage in Mutations**:
```typescript
mutationFn: async (credentials: LoginRequest) => {
  try {
    const response = await api.post('/api/auth/login', credentials)
    return response.data
  } catch (error: any) {
    const userFriendlyMessage = extractErrorMessage(error)
    throw new Error(userFriendlyMessage)
  }
}
```

**Display in Component**:
```typescript
{loginMutation.error && (
  <Alert color="red">
    {loginMutation.error.message}
  </Alert>
)}
```

---

## Testing

### Unit Testing Auth Mutations

**File**: `/apps/web/src/features/auth/api/__tests__/mutations.test.ts`

```typescript
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useLogin } from '../mutations'

describe('useLogin', () => {
  it('should login successfully', async () => {
    const queryClient = new QueryClient()
    const wrapper = ({ children }) => (
      <QueryClientProvider client={queryClient}>
        {children}
      </QueryClientProvider>
    )

    const { result } = renderHook(() => useLogin(), { wrapper })

    result.current.mutate({
      email: 'admin@witchcityrope.com',
      password: 'Test123!',
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
  })
})
```

### E2E Testing with Playwright

**File**: `/tests/playwright/auth.spec.ts`

```typescript
import { test, expect } from '@playwright/test'

test('login flow', async ({ page }) => {
  await page.goto('/login')

  await page.fill('input[name="email"]', 'admin@witchcityrope.com')
  await page.fill('input[name="password"]', 'Test123!')
  await page.click('button[type="submit"]')

  await expect(page).toHaveURL('/dashboard')
  await expect(page.locator('[data-testid="user-greeting"]'))
    .toContainText('Welcome, TestAdmin')
})

test('logout flow', async ({ page }) => {
  // Login first
  await page.goto('/login')
  await page.fill('input[name="email"]', 'admin@witchcityrope.com')
  await page.fill('input[name="password"]', 'Test123!')
  await page.click('button[type="submit"]')
  await expect(page).toHaveURL('/dashboard')

  // Logout
  await page.click('[data-testid="button-logout"]')
  await expect(page).toHaveURL('/')
  await expect(page.locator('[data-testid="link-login"]')).toBeVisible()
})
```

---

## Migration Notes

### What Was Changed (November 2025)

**Removed** (obsolete patterns):
- ❌ `/apps/web/src/contexts/AuthContext.tsx` - React Context pattern
- ❌ `/apps/web/src/services/authService.ts` - Direct fetch calls
- ❌ `/apps/web/src/hooks/useAuth.ts` - Context wrapper hook
- ❌ `/apps/web/src/examples/LoginFormExample.tsx` - Old example code

**Updated**:
- ✅ `/apps/web/src/features/auth/api/mutations.ts` - Added useLogout() with CSRF
- ✅ `/apps/web/src/components/layout/UtilityBar.tsx` - Uses useLogout() mutation
- ✅ `/apps/web/src/components/layout/Navigation.tsx` - Uses useLogout() mutation
- ✅ `/apps/web/src/main.tsx` - Removed AuthProvider
- ✅ `/apps/web/src/lib/api/index.ts` - Removed hooks/useAuth export

**Kept** (still valid):
- ✅ `/apps/web/src/lib/api/hooks/useAuth.ts` - useCurrentUser() only
- ✅ `/apps/web/src/stores/authStore.ts` - Zustand auth state
- ✅ `/apps/web/src/features/auth/api/mutations.ts` - useLogin(), useRegister()

### Common Migration Patterns

**Old Pattern** (AuthContext):
```typescript
// ❌ WRONG - Deleted
import { useAuth } from '@/hooks/useAuth'
const { user, isAuthenticated, logout } = useAuth()
```

**New Pattern** (Zustand + Mutations):
```typescript
// ✅ CORRECT
import { useUser, useIsAuthenticated } from '@/stores/authStore'
import { useLogout } from '@/features/auth/api/mutations'

const user = useUser()
const isAuthenticated = useIsAuthenticated()
const logoutMutation = useLogout()

// Usage
logoutMutation.mutate()
```

---

## Critical Reminders

### ⚠️ queryClient.clear() vs invalidateQueries()

**CRITICAL**: On logout, use `queryClient.clear()` NOT `queryClient.invalidateQueries()`.

```typescript
// ✅ CORRECT - Clears cache without refetch
onSuccess: () => {
  logout()
  queryClient.clear()
  navigate('/')
}

// ❌ WRONG - Triggers refetch while logged out (bug)
onSuccess: () => {
  logout()
  queryClient.invalidateQueries({ queryKey: ['user'] })
  navigate('/')
}
```

**Why**: `invalidateQueries()` marks queries as stale and triggers refetch. After logout, user is no longer authenticated, so refetch fails and causes errors.

### 🔒 CSRF Token Requirements

**Backend - Always Require CSRF for State Changes**:
```csharp
app.MapPost("/api/...", handler)
   .RequireAuthorization()
   .RequireAntiforgeryToken(); // ← CRITICAL for POST/PUT/DELETE
```

**Frontend - Automatic via Interceptor**:
```typescript
// No manual code needed - axios interceptor handles it
await api.post('/api/auth/logout') // ✅ CSRF token added automatically
```

### 📝 Error Message Standards

**Always use extractErrorMessage utility**:
```typescript
import { extractErrorMessage } from '@/lib/api/utils/errors'

try {
  await api.post('/api/auth/login', credentials)
} catch (error: any) {
  const userFriendlyMessage = extractErrorMessage(error)
  throw new Error(userFriendlyMessage)
}
```

---

## Related Documentation

- [Technology Research Document](/docs/functional-areas/authentication/research/2025-11-23-authentication-pattern-research.md) - Why we chose this pattern
- [React Patterns](/docs/standards-processes/frontend/react-patterns.md) - General React guidelines
- [API Design Patterns](/docs/standards-processes/backend/api-design-patterns.md) - Backend API standards
- [Error Handling Patterns](/docs/standards-processes/backend/error-handling-patterns.md) - Error response standards

---

## Questions or Issues?

If you encounter authentication-related issues:

1. **Check this guide first** - Most common patterns are documented here
2. **Review the research document** - Understand why we made these choices
3. **Check lessons learned** - See `/docs/lessons-learned/backend-developer-lessons-learned.md` and `/docs/lessons-learned/react-developer-lessons-learned.md`
4. **Ask in project channels** - Don't reinvent authentication patterns

---

**Last Updated**: 2025-11-23
**Author**: WitchCityRope Development Team
**Version**: 1.0
