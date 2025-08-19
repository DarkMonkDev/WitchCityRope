# Authentication Implementation Guide

<!-- Last Updated: 2025-08-19 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian -->
<!-- Status: Active -->

## Overview

This guide provides a comprehensive reference for implementing authentication in WitchCityRope using the validated technology patterns. It integrates TanStack Query v5, Zustand state management, and React Router v7 with httpOnly cookie security.

**CRITICAL**: This guide references extensively validated technology patterns. Do NOT deviate from these patterns without updating the underlying functional specifications.

## Technology Stack Integration

### Core Technologies
- **API Integration**: TanStack Query v5 (validated)
- **State Management**: Zustand (validated) 
- **Routing**: React Router v7 (validated)
- **Authentication**: httpOnly cookies (validated)

### Reference Documents
- **API Integration**: [TanStack Query v5 Functional Specification](/docs/functional-areas/api-integration-validation/requirements/functional-specification-v2.md)
- **State Management**: [Zustand Functional Specification](/docs/functional-areas/state-management-validation/requirements/functional-specification.md)
- **Routing**: [React Router v7 Functional Specification](/docs/functional-areas/routing-validation/requirements/functional-specification.md)

## Authentication Flow Architecture

### High-Level Flow
```
User Login → API validates → httpOnly cookie set → 
Zustand store updated → React Router guards validated → 
TanStack Query authorized requests
```

### Security Model
- **Frontend**: No token storage, httpOnly cookies only
- **Backend**: Session validation, CORS with credentials
- **Route Protection**: Server-side validation via loaders
- **API Requests**: Automatic cookie inclusion

## Implementation Patterns

### 1. Zustand Authentication Store

**File Location**: `/apps/web/src/stores/authStore.ts`

```typescript
import { create } from 'zustand';
import { devtools } from 'zustand/middleware';

interface User {
  id: string;
  email: string;
  roles: ('admin' | 'teacher' | 'vetted' | 'member' | 'guest')[];
  firstName: string;
  lastName: string;
}

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  permissions: string[];
}

interface AuthActions {
  login: (user: User) => void;
  logout: () => void;
  updateUser: (updates: Partial<User>) => void;
  checkAuth: () => Promise<void>;
}

type AuthStore = AuthState & { actions: AuthActions };

const useAuthStore = create<AuthStore>()(
  devtools(
    (set, get) => ({
      // State
      user: null,
      isAuthenticated: false,
      isLoading: true,
      permissions: [],
      
      // Actions
      actions: {
        login: (user) => set({
          user,
          isAuthenticated: true,
          isLoading: false,
          permissions: calculatePermissions(user.roles)
        }),
        
        logout: () => {
          // Call API logout endpoint
          fetch('/api/auth/logout', {
            method: 'POST',
            credentials: 'include'
          }).finally(() => {
            set({
              user: null,
              isAuthenticated: false,
              permissions: [],
              isLoading: false
            });
          });
        },
        
        updateUser: (updates) => set((state) => ({
          user: state.user ? { ...state.user, ...updates } : null
        })),
        
        checkAuth: async () => {
          set({ isLoading: true });
          
          try {
            const response = await fetch('/api/auth/session', {
              credentials: 'include'
            });
            
            if (response.ok) {
              const user = await response.json();
              get().actions.login(user);
            } else {
              get().actions.logout();
            }
          } catch (error) {
            console.error('Auth check failed:', error);
            get().actions.logout();
          }
        }
      }
    }),
    { name: 'auth-store' }
  )
);

// Selector hooks for performance
export const useAuth = () => useAuthStore((state) => ({
  user: state.user,
  isAuthenticated: state.isAuthenticated,
  isLoading: state.isLoading
}));

export const useAuthActions = () => useAuthStore((state) => state.actions);
export const useUserRoles = () => useAuthStore((state) => state.user?.roles || []);
export const usePermissions = () => useAuthStore((state) => state.permissions);

export const useHasRole = (requiredRole: string) => 
  useAuthStore((state) => state.user?.roles.includes(requiredRole) || false);

function calculatePermissions(roles: string[]): string[] {
  // Role hierarchy: guest < member < vetted < teacher < admin
  const permissions: string[] = [];
  
  if (roles.includes('admin')) {
    permissions.push('read', 'write', 'delete', 'admin', 'manage_users');
  } else if (roles.includes('teacher')) {
    permissions.push('read', 'write', 'manage_events', 'view_members');
  } else if (roles.includes('vetted')) {
    permissions.push('read', 'write', 'view_members');
  } else if (roles.includes('member')) {
    permissions.push('read', 'write');
  } else {
    permissions.push('read');
  }
  
  return permissions;
}
```

### 2. TanStack Query Authentication Hooks

**File Location**: `/apps/web/src/api/auth.queries.ts`

```typescript
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuthActions } from '../stores/authStore';

// Query keys
export const authKeys = {
  all: ['auth'] as const,
  session: () => [...authKeys.all, 'session'] as const,
} as const;

// Login mutation
export function useLogin() {
  const { login } = useAuthActions();
  
  return useMutation({
    mutationFn: async (credentials: { email: string; password: string }) => {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(credentials)
      });
      
      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Login failed');
      }
      
      return response.json();
    },
    onSuccess: (data) => {
      if (data.success && data.user) {
        login(data.user);
      }
    },
    onError: (error) => {
      console.error('Login failed:', error);
    }
  });
}

// Logout mutation
export function useLogout() {
  const { logout } = useAuthActions();
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async () => {
      await fetch('/api/auth/logout', {
        method: 'POST',
        credentials: 'include'
      });
    },
    onSuccess: () => {
      logout();
      // Clear all queries on logout
      queryClient.clear();
    }
  });
}

// Session validation query
export function useSession() {
  return useQuery({
    queryKey: authKeys.session(),
    queryFn: async () => {
      const response = await fetch('/api/auth/session', {
        credentials: 'include'
      });
      
      if (!response.ok) {
        throw new Error('Not authenticated');
      }
      
      return response.json();
    },
    retry: false, // Don't retry auth checks
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}
```

### 3. React Router v7 Route Guards

**File Location**: `/apps/web/src/loaders/authLoader.ts`

```typescript
import { LoaderFunctionArgs, redirect, data } from 'react-router';

export enum UserRole {
  Guest = 0,
  GeneralMember = 1,
  VettedMember = 2,
  Teacher = 3,
  Admin = 4
}

// Authentication loader
export async function authLoader({ request }: LoaderFunctionArgs) {
  const response = await fetch('/api/auth/session', {
    credentials: 'include'
  });
  
  if (!response.ok) {
    const returnTo = encodeURIComponent(new URL(request.url).pathname);
    throw redirect(`/login?returnTo=${returnTo}`);
  }
  
  const session = await response.json();
  return { user: session };
}

// Role-based loader factory
export function requireMinimumRole(minimumRole: UserRole) {
  return async ({ request }: LoaderFunctionArgs) => {
    const { user } = await authLoader({ request } as any);
    
    if (!user || user.role < minimumRole) {
      throw data("Access Denied - Insufficient Permissions", { status: 403 });
    }
    
    return { user };
  };
}

// Specific role loaders
export const requireVettedMember = requireMinimumRole(UserRole.VettedMember);
export const requireTeacher = requireMinimumRole(UserRole.Teacher);
export const requireAdmin = requireMinimumRole(UserRole.Admin);
```

**File Location**: `/apps/web/src/routes.tsx`

```typescript
import { createBrowserRouter } from 'react-router';
import { authLoader, requireVettedMember, requireAdmin } from './loaders/authLoader';

export const router = createBrowserRouter([
  {
    path: "/",
    element: <RootLayout />,
    children: [
      // Public routes
      { index: true, element: <HomePage /> },
      { path: "login", element: <LoginPage /> },
      { path: "register", element: <RegisterPage /> },
      
      // Protected routes
      {
        path: "dashboard",
        element: <DashboardLayout />,
        loader: authLoader,
        children: [
          { index: true, element: <DashboardHome /> },
          { path: "profile", element: <ProfilePage /> }
        ]
      },
      
      // Role-based routes
      {
        path: "members",
        element: <MemberDirectoryPage />,
        loader: requireVettedMember
      },
      {
        path: "admin",
        element: <AdminLayout />,
        loader: requireAdmin,
        children: [
          { index: true, element: <AdminDashboard /> },
          { path: "users", element: <UserManagement /> }
        ]
      }
    ]
  }
]);
```

### 4. Authenticated API Client

**File Location**: `/apps/web/src/api/client.ts`

```typescript
import axios from 'axios';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5653',
  withCredentials: true, // Include httpOnly cookies
  headers: {
    'Content-Type': 'application/json',
  },
});

// Response interceptor for auth errors
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Try to refresh session
      try {
        await apiClient.post('/api/auth/refresh');
        // Retry original request
        return apiClient.request(error.config);
      } catch (refreshError) {
        // Redirect to login
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);
```

## Backend API Implementation

### Authentication Endpoints

**File Location**: `/apps/api/Controllers/AuthController.cs`

```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginDto request)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);
        
        if (result.Success)
        {
            // Set httpOnly cookie
            Response.Cookies.Append("__session", result.SessionToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = environment.IsProduction(),
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromDays(30)
            });
            
            return Ok(new AuthResponse 
            { 
                Success = true, 
                User = result.User 
            });
        }
        
        return Unauthorized(new AuthResponse 
        { 
            Success = false, 
            Message = result.ErrorMessage 
        });
    }
    
    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        Response.Cookies.Delete("__session");
        return Ok(new { Success = true });
    }
    
    [HttpGet("session")]
    public async Task<ActionResult<SessionData>> GetSession()
    {
        if (!Request.Cookies.TryGetValue("__session", out var sessionToken))
            return Unauthorized();
            
        var session = await _authService.ValidateSessionAsync(sessionToken);
        return session != null ? Ok(session) : Unauthorized();
    }
    
    [HttpPost("refresh")]
    public async Task<ActionResult> RefreshSession()
    {
        if (!Request.Cookies.TryGetValue("__session", out var sessionToken))
            return Unauthorized();
            
        var newSession = await _authService.RefreshSessionAsync(sessionToken);
        
        if (newSession != null)
        {
            Response.Cookies.Append("__session", newSession.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = environment.IsProduction(),
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromDays(30)
            });
            
            return Ok();
        }
        
        return Unauthorized();
    }
}
```

### CORS Configuration

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDevelopment", builder => builder
        .WithOrigins(
            "http://localhost:5173",    // Vite dev server
            "http://localhost:3000",    // Alternative React port
            "http://127.0.0.1:5173"     // Local alternative
        )
        .AllowCredentials()             // CRITICAL for httpOnly cookies
        .AllowAnyMethod()
        .AllowAnyHeader());
});

app.UseCors("ReactDevelopment");
```

## Component Integration

### Login Component

**File Location**: `/apps/web/src/components/auth/LoginForm.tsx`

```typescript
import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useLogin } from '../../api/auth.queries';
import { Button, TextInput, PasswordInput, Box, Alert } from '@mantine/core';

const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(1, 'Password is required')
});

type LoginFormData = z.infer<typeof loginSchema>;

export function LoginForm() {
  const login = useLogin();
  
  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: ''
    }
  });
  
  const onSubmit = (data: LoginFormData) => {
    login.mutate(data);
  };
  
  return (
    <Box component="form" onSubmit={form.handleSubmit(onSubmit)}>
      <TextInput
        label="Email"
        {...form.register('email')}
        error={form.formState.errors.email?.message}
        required
      />
      
      <PasswordInput
        label="Password"
        {...form.register('password')}
        error={form.formState.errors.password?.message}
        required
        mt="md"
      />
      
      {login.error && (
        <Alert color="red" mt="md">
          {login.error.message}
        </Alert>
      )}
      
      <Button
        type="submit"
        loading={login.isPending}
        fullWidth
        mt="xl"
      >
        Sign In
      </Button>
    </Box>
  );
}
```

### Protected Route Component

```typescript
import React from 'react';
import { useAuth } from '../../stores/authStore';
import { LoadingSpinner } from '../ui/LoadingSpinner';

interface ProtectedRouteProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

export function ProtectedRoute({ children, fallback }: ProtectedRouteProps) {
  const { isAuthenticated, isLoading } = useAuth();
  
  if (isLoading) {
    return <LoadingSpinner />;
  }
  
  if (!isAuthenticated) {
    return fallback || <div>Access denied</div>;
  }
  
  return <>{children}</>;
}
```

## Testing Strategy

### Mock Service Worker (MSW) Setup

**File Location**: `/apps/web/src/test/mocks/handlers.ts`

```typescript
import { http, HttpResponse } from 'msw';

export const handlers = [
  // Authentication endpoints
  http.post('/api/auth/login', async ({ request }) => {
    const { email, password } = await request.json() as any;
    
    if (email === 'admin@witchcityrope.com' && password === 'Test123!') {
      return HttpResponse.json({
        success: true,
        user: {
          id: '1',
          email: 'admin@witchcityrope.com',
          firstName: 'Test',
          lastName: 'Admin',
          roles: ['admin']
        }
      });
    }
    
    return new HttpResponse(null, { status: 401 });
  }),
  
  http.get('/api/auth/session', () => {
    return HttpResponse.json({
      id: '1',
      email: 'admin@witchcityrope.com',
      firstName: 'Test',
      lastName: 'Admin',
      roles: ['admin']
    });
  }),
  
  http.post('/api/auth/logout', () => {
    return new HttpResponse(null, { status: 200 });
  })
];
```

### Integration Test Example

```typescript
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { LoginForm } from '../LoginForm';

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  });
  
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
}

test('successful login updates auth store', async () => {
  render(<LoginForm />, { wrapper: createWrapper() });
  
  fireEvent.change(screen.getByLabelText('Email'), {
    target: { value: 'admin@witchcityrope.com' }
  });
  fireEvent.change(screen.getByLabelText('Password'), {
    target: { value: 'Test123!' }
  });
  fireEvent.click(screen.getByRole('button', { name: 'Sign In' }));
  
  await waitFor(() => {
    // Verify auth store updated
    expect(useAuthStore.getState().isAuthenticated).toBe(true);
  });
});
```

## Troubleshooting

### Common Issues

#### 1. Cookies Not Being Sent
- **Problem**: API requests not including cookies
- **Solution**: Ensure `withCredentials: true` in axios config
- **Check**: CORS must include `AllowCredentials()`

#### 2. Route Guards Not Working
- **Problem**: Protected routes allowing unauthorized access
- **Solution**: Verify loader returns redirect on auth failure
- **Check**: Ensure loaders are assigned to route definitions

#### 3. State Not Updating After Login
- **Problem**: Zustand store not reflecting login state
- **Solution**: Call login action in TanStack Query onSuccess
- **Check**: Verify mutation success handler calls store action

#### 4. Session Not Persisting
- **Problem**: User logged out on page refresh
- **Solution**: Call checkAuth on app initialization
- **Check**: Verify httpOnly cookies are being set and sent

### Debug Steps

1. **Check Network Tab**: Verify cookies in request headers
2. **Inspect Application Tab**: Confirm httpOnly cookies are set
3. **Console Errors**: Look for CORS or authentication errors
4. **React Query DevTools**: Check query states and cache
5. **Zustand DevTools**: Verify store state changes

## File Locations Summary

**Reusable Code Locations**:
- Auth Store: `/apps/web/src/stores/authStore.ts`
- Auth Queries: `/apps/web/src/api/auth.queries.ts`
- Route Loaders: `/apps/web/src/loaders/authLoader.ts`
- API Client: `/apps/web/src/api/client.ts`
- Auth Components: `/apps/web/src/components/auth/`
- Test Mocks: `/apps/web/src/test/mocks/handlers.ts`

**Backend Implementation**:
- Auth Controller: `/apps/api/Controllers/AuthController.cs`
- Auth Service: `/apps/api/Services/AuthService.cs`
- CORS Configuration: `/apps/api/Program.cs`

## Minimal Implementation Checklist

**Frontend Setup**:
- [ ] Configure Zustand auth store with httpOnly cookie support
- [ ] Set up TanStack Query with auth mutations and session queries
- [ ] Implement React Router v7 loaders for route protection
- [ ] Configure axios client with credentials and interceptors
- [ ] Create login/logout components using validated patterns

**Backend Setup**:
- [ ] Implement /auth/login, /auth/logout, /auth/session endpoints
- [ ] Configure httpOnly cookies with secure settings
- [ ] Set up CORS with AllowCredentials for cookie support
- [ ] Implement session validation middleware
- [ ] Add role-based authorization where needed

**Testing Setup**:
- [ ] Configure MSW with authentication endpoints
- [ ] Create integration tests for auth flow
- [ ] Test protected routes and role-based access
- [ ] Verify cookie handling in tests

## Security Considerations

- **httpOnly Cookies**: Prevent XSS attacks by keeping tokens out of JavaScript
- **CORS with Credentials**: Only allow trusted origins with credential support
- **Server-Side Validation**: Always validate sessions and permissions on the server
- **Secure Cookie Settings**: Use Secure, SameSite=Strict in production
- **Session Timeout**: Implement reasonable session expiration
- **CSRF Protection**: Consider anti-forgery tokens for state-changing operations

---

*This guide implements the validated authentication patterns from our comprehensive technology research. All patterns have been tested and proven to work together securely and efficiently.*