# Authentication Store Usage Guide

This document shows how to use the Zustand authentication store in WitchCityRope React components.

## Quick Start

```typescript
import { useAuth, useAuthActions, useHasRole } from '@/stores/authStore';

function MyComponent() {
  const { user, isAuthenticated, isLoading } = useAuth();
  const { login, logout } = useAuthActions();
  const isAdmin = useHasRole('admin');

  if (isLoading) return <div>Loading...</div>;
  
  if (!isAuthenticated) {
    return <button onClick={() => login(userData)}>Login</button>;
  }

  return (
    <div>
      <h1>Welcome {user.firstName}!</h1>
      {isAdmin && <AdminPanel />}
      <button onClick={logout}>Logout</button>
    </div>
  );
}
```

## Available Hooks

### `useAuth()`
Returns current authentication state:
```typescript
const { user, isAuthenticated, isLoading } = useAuth();
```

### `useAuthActions()`
Returns authentication actions:
```typescript
const { login, logout, checkAuth, refreshAuth, updateUser, setLoading } = useAuthActions();
```

### `useHasRole(role: string)`
Check if user has specific role:
```typescript
const isAdmin = useHasRole('admin');
const isTeacher = useHasRole('teacher');
```

### `useHasPermission(permission: string)`
Check if user has specific permission:
```typescript
const canManageEvents = useHasPermission('manage_events');
const canWrite = useHasPermission('write');
```

### `usePermissions()`
Get all user permissions:
```typescript
const permissions = usePermissions();
// ['read', 'write', 'manage_events', ...]
```

### `useUserRoles()`
Get all user roles:
```typescript
const roles = useUserRoles();
// ['admin', 'teacher']
```

## Role-Based Permissions

The store automatically calculates permissions based on user roles:

- **admin**: `['read', 'write', 'delete', 'manage_users', 'manage_events']`
- **teacher**: `['read', 'write', 'manage_events', 'manage_registrations']`
- **vetted**: `['read', 'write', 'register_events']`
- **member**: `['read', 'register_events']`
- **guest**: `['read']`

## Authentication Flow

1. **Check Auth on App Start**:
```typescript
useEffect(() => {
  const { checkAuth } = useAuthStore.getState().actions;
  checkAuth();
}, []);
```

2. **Login**:
```typescript
const { login } = useAuthActions();
await apiLogin(credentials); // Your API call
login(userData); // Update store
```

3. **Logout**:
```typescript
const { logout } = useAuthActions();
logout(); // Calls API and clears state
```

## Security Features

- ✅ Uses sessionStorage (closes on browser close)
- ✅ httpOnly cookies for API authentication
- ✅ No JWT tokens stored in client
- ✅ Automatic logout on API auth failures
- ✅ Server-side permission validation required

## Performance Notes

- Selectors are optimized to prevent unnecessary re-renders
- Use specific selector hooks rather than the full store
- Permissions are calculated once on login and cached

## Testing

The store includes comprehensive tests covering:
- State management
- Permission calculations
- API integration
- Error handling
- Persistence

Run tests with:
```bash
npm test -- src/stores/__tests__/authStore.test.ts
```