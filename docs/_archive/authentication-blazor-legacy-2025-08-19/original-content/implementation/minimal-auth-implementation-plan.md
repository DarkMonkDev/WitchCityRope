# Minimal Authentication Flow - Implementation Plan

## Overview
This document outlines the step-by-step implementation of a minimal authentication flow that integrates all three validated technologies: TanStack Query v5, Zustand, and React Router v7.

## Goal
Create the smallest possible working authentication system that validates the integration of our researched patterns.

## Scope
- **IN SCOPE**: Login, logout, protected route, auth state management
- **OUT OF SCOPE**: Registration, password reset, 2FA, role-based access (for now)

## Success Criteria
1. User can login with test credentials
2. Auth state persists on page refresh
3. Protected routes redirect to login when unauthenticated
4. Logout clears auth state and redirects to login
5. All three technologies work together without conflicts

## Implementation Steps

### Step 1: Zustand Auth Store (30 minutes)
**File**: `/apps/web/src/stores/authStore.ts`

**Implementation**:
- Create auth store following validated Zustand patterns
- Include: user state, isAuthenticated, login/logout actions
- Use sessionStorage for persistence (not localStorage)
- Reference: `/docs/functional-areas/state-management-validation/requirements/functional-specification.md`

**Testing**:
- Store updates correctly on login
- State persists on refresh
- Logout clears state

### Step 2: React Router v7 Setup (30 minutes)
**Files**:
- `/apps/web/src/routes/router.tsx` - Main router configuration
- `/apps/web/src/routes/loaders/authLoader.ts` - Auth checking loader
- `/apps/web/src/routes/guards/ProtectedRoute.tsx` - Route guard component

**Implementation**:
- Setup data router with createBrowserRouter
- Create auth loader for protected routes
- Implement route guard pattern
- Reference: `/docs/functional-areas/routing-validation/requirements/functional-specification.md`

**Testing**:
- Public routes accessible without auth
- Protected routes redirect to login
- Navigation works correctly

### Step 3: Login Page (45 minutes)
**File**: `/apps/web/src/pages/Login.tsx`

**Implementation**:
- Simple form with email/password using Mantine components
- Use TanStack Query mutation for login API call
- Update Zustand store on success
- Navigate to dashboard on success
- Show error messages on failure

**API Integration**:
- POST `/api/auth/login`
- Use existing TanStack Query setup from `/apps/web/src/api/`
- Reference: `/docs/functional-areas/api-integration-validation/requirements/functional-specification-v2.md`

**Testing**:
- Form validation works
- Login mutation executes
- Success updates auth store
- Failure shows error message

### Step 4: Protected Dashboard (30 minutes)
**File**: `/apps/web/src/pages/Dashboard.tsx`

**Implementation**:
- Simple page showing "Welcome [username]"
- Logout button that calls logout mutation
- Use React Router v7 loader for auth check
- Redirect to login if not authenticated

**Testing**:
- Page shows user info
- Logout works correctly
- Direct navigation redirects when not authenticated

### Step 5: Integration Testing (30 minutes)
**File**: `/apps/web/src/test/integration/auth-flow.test.tsx`

**Tests**:
1. Complete login flow
2. Protected route access
3. Logout flow
4. Session persistence
5. Error handling

## File Structure
```
/apps/web/src/
├── stores/
│   └── authStore.ts              # Zustand auth state
├── routes/
│   ├── router.tsx                # React Router v7 setup
│   ├── loaders/
│   │   └── authLoader.ts         # Auth checking
│   └── guards/
│       └── ProtectedRoute.tsx    # Route protection
├── features/
│   └── auth/
│       └── api/
│           ├── queries.ts        # useUser query
│           └── mutations.ts      # useLogin, useLogout
├── pages/
│   ├── Login.tsx                 # Login page
│   └── Dashboard.tsx             # Protected dashboard
└── test/
    └── integration/
        └── auth-flow.test.tsx    # Integration tests
```

## Dependencies Required
- ✅ @tanstack/react-query (already installed)
- ✅ zustand (already installed)
- ✅ react-router-dom (already installed)
- ✅ @mantine/core (already installed)
- ✅ @mantine/form (already installed)
- ✅ axios (already installed)
- ✅ msw (already installed for testing)

## Risk Mitigation
- **Risk**: Integration conflicts between technologies
  - **Mitigation**: Test after each step, rollback if issues
  
- **Risk**: Auth state not syncing properly
  - **Mitigation**: Use React Query DevTools + Zustand DevTools to debug

- **Risk**: Route protection not working
  - **Mitigation**: Test both client and server-side protection

## Timeline
- Step 1: 30 minutes + testing + commit
- Step 2: 30 minutes + testing + commit
- Step 3: 45 minutes + testing + commit
- Step 4: 30 minutes + testing + commit
- Step 5: 30 minutes + testing + commit
- **Total**: ~3 hours with testing and commits

## Next Steps After Success
Once minimal auth is working:
1. Add registration flow
2. Add password reset
3. Add role-based access control
4. Add 2FA support
5. Add remember me functionality

## References
- [TanStack Query Functional Spec](/docs/functional-areas/api-integration-validation/requirements/functional-specification-v2.md)
- [Zustand Functional Spec](/docs/functional-areas/state-management-validation/requirements/functional-specification.md)
- [React Router v7 Functional Spec](/docs/functional-areas/routing-validation/requirements/functional-specification.md)
- [Authentication Implementation Guide](/docs/guides-setup/authentication-implementation-guide.md)