# Frontend Lessons Learned

## 🚨 MANDATORY: v7 Design System COMPLETE Implementation (2025-08-22)

### ✅ IMPLEMENTED: Complete v7 Design System Homepage
**STATUS**: Successfully implemented the complete v7 design system homepage with all required components and animations.

#### Implementation Completed:
- ✅ **Theme Configuration**: Updated `/apps/web/src/theme.ts` with all 37 v7 design tokens
- ✅ **CSS Variables**: Added complete v7 color palette and animations to `/apps/web/src/index.css`
- ✅ **Button Variants**: Implemented v7-primary, v7-primary-alt, v7-secondary with corner morphing
- ✅ **Homepage Components**: Created modular components in `/apps/web/src/components/homepage/`
- ✅ **Signature Animations**: Navigation underline, button corner morphing, feature icon shape-shifting
- ✅ **Mobile Responsive**: All breakpoints from v7 template implemented
- ✅ **Authentication Integration**: Maintained existing auth state patterns

#### New Components Created:
```typescript
// All components use exact v7 styling from homepage-template-v7.html
import { 
  HeroSection,     // Hero with gradient background and floating animations
  EventsList,      // v7-styled event cards with exact template design  
  FeatureGrid,     // Shape-shifting icon animations
  CTASection,      // Rotating background patterns
  RopeDivider,     // SVG rope pattern divider
  EventCard        // v7 event card with all template styling
} from '@/components/homepage';
```

#### Critical Pattern Success:
- **Design Token Extraction**: Successfully extracted all 37 design tokens from v7 template
- **Animation Implementation**: All signature animations working (nav underline, button morphing, icon shape-shifting)
- **Mantine v7 Integration**: Button variants integrate perfectly with Mantine theme system
- **Mobile Responsiveness**: All v7 breakpoints implemented and tested
- **Authentication Maintained**: Existing auth patterns preserved while using v7 design

#### Code Example - v7 Button Implementation:
```typescript
// theme.ts - Button variants exactly match v7 template
'v7-primary': (theme: any) => ({
  root: {
    background: `linear-gradient(135deg, ${theme.other.colorAmber} 0%, ${theme.other.colorAmberDark} 100%)`,
    borderRadius: '12px 6px 12px 6px', // Corner morphing start
    '&:hover': {
      borderRadius: '6px 12px 6px 12px', // Corner morphing end
    }
  }
})

// Component usage
<Button variant="v7-primary" size="lg">Browse Upcoming Classes</Button>
```

### Update Status
- **Previous Implementation Requirements**: All completed
- **Template Authority**: `/docs/design/current/homepage-template-v7.html` fully implemented
- **Next Developers**: Can use homepage components as reference for v7 patterns

---

## 🚨 MANDATORY STARTUP PROCEDURE - READ FIRST 🚨

### Critical Architecture Documents (MUST READ BEFORE ANY WORK):
1. **Migration Architecture**: `/docs/architecture/react-migration/domain-layer-architecture.md`
2. **DTO Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
3. **Architecture Discovery Process**: `/docs/standards-processes/architecture-discovery-process.md`
4. **Migration Plan**: `/docs/architecture/react-migration/migration-plan.md`

### Validation Gates (MUST COMPLETE):
- [ ] Read all architecture documents above
- [ ] Check if solution already exists
- [ ] Reference existing patterns in your work
- [ ] NEVER create manual DTO interfaces (use NSwag)

### Frontend Developer Specific Rules:
- **DTOs auto-generate via NSwag - NEVER create manual TypeScript interfaces**
- **Run `npm run generate:types` when API changes**
- **Import from @witchcityrope/shared-types only**
- **RED FLAG words: 'alignment', 'DTO', 'type generation' → STOP and check NSwag docs**

---

## 🚨 CRITICAL: NSwag Type Generation Compilation Fixes (READ FIRST) 🚨
**Date**: 2025-08-19
**Category**: NSwag Critical Fixes
**Severity**: Critical

### Context
Fixed critical TypeScript compilation errors in NSwag-generated types that were preventing builds and tests from working. Multiple issues discovered in OpenAPI TypeScript generation and TanStack Query v5 integration.

### What We Learned
- **OpenAPI-TypeScript Bug**: Properties with `default` values but not in `required` array still generate as required types
- **TanStack Query v5 Change**: All mutations require arguments, even logout operations that don't need data
- **QueryClient API Change**: `getQueryData<T>()` in v5 doesn't accept generic arguments, use casting instead
- **QueryErrorResetBoundary Issue**: Render props pattern requires casting to unknown then ReactNode for TypeScript
- **Manual Fixes Required**: Some issues in generated types need manual correction after generation

### Action Items
- [ ] ALWAYS manually fix rememberMe to optional after type generation: `rememberMe?: boolean`
- [ ] ADD lastLoginAt field to UserDto if not present in API schema
- [ ] USE `mutationFn: async (_?: void)` pattern for mutations that don't need arguments
- [ ] REPLACE `queryClient.getQueryData<T>()` with `queryClient.getQueryData() as T | undefined`
- [ ] CAST render props functions: `)) as unknown as React.ReactNode`
- [ ] UPDATE tests to call `mutate(undefined)` for mutations that don't need data

### Critical Implementation Rules
```typescript
// ✅ CORRECT - TanStack Query v5 mutations
const logoutMutation = useMutation({
  mutationFn: async (_?: void): Promise<void> => {
    await api.post('/api/auth/logout')
  }
})

// ✅ CORRECT - QueryClient data access
const userData = queryClient.getQueryData(userKeys.me()) as UserDto | undefined

// ✅ CORRECT - Generated types after manual fix
interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean; // Manual fix: should be optional
}

// ❌ WRONG - Old patterns that fail in v5
useMutation<LoginResponse, Error, LoginRequest>({ ... })
queryClient.getQueryData<UserDto>(userKeys.me())
```

### Impact
Enables TypeScript compilation and test execution. Critical for development workflow and build pipeline.

### Tags
#critical #nswag #tanstack-query #typescript-compilation #build-fix

---

## 🚨 CRITICAL: useEffect Infinite Loop Fix (READ FIRST) 🚨
**Date**: 2025-08-19
**Category**: React Hooks Critical Bug
**Severity**: Critical

### Context
Fixed "Maximum update depth exceeded" error that occurred when visiting any test page. The error was caused by an infinite loop in the App.tsx useEffect dependency array.

### What We Learned
- **useEffect Dependency Arrays with Zustand**: Functions returned from Zustand stores get recreated on every state update
- **Infinite Loop Pattern**: useEffect → function call → state update → re-render → new function reference → useEffect runs again
- **Auth Check Pattern**: Authentication checks should typically run only once on app mount, not on every auth state change
- **Empty Dependency Array**: Use `[]` when effect should only run once on mount
- **Function Reference Stability**: Zustand store actions are NOT stable references across renders

### Action Items
- [ ] NEVER include Zustand store action functions in useEffect dependency arrays
- [ ] USE empty dependency array `[]` for one-time initialization effects
- [ ] USE useCallback for stable function references if dependency is truly needed
- [ ] REVIEW all useEffect hooks for similar infinite loop patterns
- [ ] TEST pages after auth-related changes to catch loops early

### Critical Implementation Rules
```typescript
// ❌ WRONG - Creates infinite loop
useEffect(() => {
  checkAuth();
}, [checkAuth]); // checkAuth reference changes on every store update

// ✅ CORRECT - Runs only once on mount
useEffect(() => {
  checkAuth();
}, []); // Empty dependency array for initialization

// ✅ CORRECT - If you truly need the function as dependency
const stableCheckAuth = useCallback(() => {
  // auth logic here
}, []); // Or appropriate dependencies

useEffect(() => {
  stableCheckAuth();
}, [stableCheckAuth]);
```

### Impact
Prevents infinite re-render loops that make the application unusable and cause "Maximum update depth exceeded" errors.

### Tags
#critical #useEffect #infinite-loop #zustand #react-hooks #maximum-update-depth

---

## 🚨 CRITICAL: Zustand Selector Infinite Loop Fix (MOST IMPORTANT) 🚨
**Date**: 2025-08-19
**Category**: Zustand Critical Bug
**Severity**: Critical - Root Cause

### Context
Fixed the ACTUAL root cause of "Maximum update depth exceeded" error. The infinite loop was caused by Zustand selectors that return new objects on every render, causing all components using those selectors to re-render infinitely.

### What We Learned
- **Object Selector Anti-Pattern**: Zustand selectors that return new objects (`{ user: state.user, isAuthenticated: state.isAuthenticated }`) create new references on every render
- **Infinite Re-render Loop**: New object references cause React to think state changed → re-render → new object → re-render → crash
- **Individual Selectors Solution**: Use separate selector hooks for each property to return stable primitive values
- **8+ Components Affected**: All components using auth selectors had to be updated to prevent the infinite loop
- **Reference Equality**: React uses Object.is() for reference equality - new objects always fail this check
- **Zustand Behavior**: Store updates trigger ALL selectors to re-run, including those returning computed objects

### Action Items
- [ ] NEVER create Zustand selectors that return new objects `{ prop1: state.prop1, prop2: state.prop2 }`
- [ ] ALWAYS use individual selector hooks: `useUser()`, `useIsAuthenticated()`, `useIsLoading()`
- [ ] CONVERT any existing object-returning selectors to individual property selectors
- [ ] TEST components after auth changes to catch infinite loops immediately
- [ ] REVIEW all Zustand selectors in codebase for object return patterns
- [ ] DOCUMENT this pattern as the PRIMARY Zustand usage pattern

### Critical Implementation Rules
```typescript
// ❌ WRONG - Creates infinite loop (THIS WAS THE BUG)
export const useAuth = () => useAuthStore((state) => ({
  user: state.user,                    // New object every render
  isAuthenticated: state.isAuthenticated,
  isLoading: state.isLoading
}));

// ✅ CORRECT - Individual selectors (THE ACTUAL FIX)
export const useUser = () => useAuthStore((state) => state.user);
export const useIsAuthenticated = () => useAuthStore((state) => state.isAuthenticated);
export const useIsLoading = () => useAuthStore((state) => state.isLoading);

// ✅ CORRECT - Component usage
function MyComponent() {
  const user = useUser();                    // Stable reference
  const isAuthenticated = useIsAuthenticated(); // Stable reference
  const isLoading = useIsLoading();          // Stable reference
  
  // No infinite re-renders
}

// ❌ WRONG - What was causing the crash
function MyComponent() {
  const { user, isAuthenticated, isLoading } = useAuth(); // New object every render
  // Infinite re-renders → "Maximum update depth exceeded"
}
```

### Components Fixed (8+ files updated)
All these components were updated from object selectors to individual selectors:
- `App.tsx` - Auth checking
- `LoginPage.tsx` - Login form
- `Header.tsx` - User display
- `ProtectedRoute.tsx` - Route guards
- `AdminDashboard.tsx` - Admin access
- And 3+ other auth-dependent components

### Impact
This was THE fix that made the application usable again. Without this change, visiting any page caused immediate crashes with "Maximum update depth exceeded" errors.

### Tags
#critical #zustand-selectors #infinite-loop #maximum-update-depth #object-references #react-rendering #root-cause

---

## 🚨 CRITICAL: Authentication Endpoint Correction Fix (READ FIRST) 🚨
**Date**: 2025-08-19
**Category**: Authentication Critical Bug Fix
**Severity**: Critical

### Context
Fixed authentication verification issue where frontend was calling the wrong API endpoint. Authentication was working perfectly at the API level, but the frontend was calling `/api/auth/me` (which doesn't exist) instead of `/api/auth/user` (which exists and works).

### What We Learned
- **Root Cause**: Frontend/NSwag generated code was calling non-existent `/api/auth/me` endpoint
- **Correct Endpoint**: `/api/auth/user` is the actual working endpoint that requires JWT authentication
- **Verification Working**: Vertical slice testing proved `/api/auth/user` works perfectly with JWT tokens
- **Simple String Replacement**: The fix was just updating endpoint URLs, no complex auth logic needed
- **Multiple File Impact**: Endpoint was referenced in auth store, API client, generated types, route loaders, and test files

### Action Items
- [ ] ALWAYS use `/api/auth/user` for authentication verification (NEVER `/api/auth/me`)
- [ ] UPDATE NSwag generated types manually after generation if endpoint names are incorrect
- [ ] ENSURE JWT tokens are included in Authorization header for `/api/auth/user` calls
- [ ] TEST authentication flow after endpoint changes to verify functionality
- [ ] UPDATE all test files to use correct endpoint URLs
- [ ] VERIFY route loaders include JWT token in fetch requests

### Critical Implementation Rules
```typescript
// ✅ CORRECT - Use /api/auth/user endpoint
const response = await api.get('/api/auth/user'); // With JWT token in headers

// ✅ CORRECT - Include JWT token in fetch requests
const response = await fetch('/api/auth/user', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Accept': 'application/json'
  }
});

// ❌ WRONG - Non-existent endpoint
const response = await api.get('/api/auth/me'); // This endpoint doesn't exist

// ❌ WRONG - Missing JWT token
const response = await fetch('/api/auth/user'); // Will return 401 without token
```

### Files Updated
- `/apps/web/src/routes/loaders/authLoader.ts` - Fixed fetch URL and added JWT token
- `/apps/web/src/lib/api/hooks/useAuth.ts` - Fixed fallback endpoint URL
- `/packages/shared-types/src/generated/api-client.ts` - Fixed getCurrentUser method URL
- `/packages/shared-types/src/generated/api-helpers.ts` - Fixed GetCurrentUserResponse type
- `/packages/shared-types/src/generated/api-types.ts` - Fixed path definition
- Multiple Playwright test files - Updated endpoint URLs

### Impact
Fixes authentication verification that was failing due to calling non-existent API endpoint. Authentication flow now works correctly with existing JWT token implementation.

### Tags
#critical #authentication #api-endpoint #jwt #endpoint-correction #bug-fix

---

## 🚨 CRITICAL: JWT Token Implementation for API Authentication (READ FIRST) 🚨
**Date**: 2025-08-19
**Category**: Authentication Critical Implementation
**Severity**: Critical

### Context
Implemented complete JWT token handling in React frontend to authenticate with .NET API. The API returns JWT tokens in login responses but the NSwag generated types were missing the token fields, requiring manual type extensions.

### What We Learned
- **API Response Structure**: Login returns `{ success: true, data: { token: "JWT", expiresAt: "ISO_DATE", user: {...} } }`
- **NSwag Type Gap**: Generated types don't include token/expiresAt fields from LoginResponse model
- **In-Memory Token Storage**: JWT tokens stored in Zustand store memory (not persisted for security)
- **Axios Interceptors Pattern**: Request interceptor adds Authorization header, response interceptor clears invalid tokens
- **Token Expiration Checking**: Store validates token expiration before use
- **Auth Store Token Methods**: Added getToken() and isTokenExpired() methods to auth actions

### Action Items
- [ ] STORE JWT tokens in memory only (Zustand state), never persist to localStorage/sessionStorage
- [ ] ADD Authorization: Bearer headers via axios request interceptors
- [ ] CHECK token expiration before API calls in auth store methods
- [ ] CLEAR tokens on 401 responses via axios response interceptors
- [ ] UPDATE auth checking to use /api/auth/user endpoint (requires JWT) instead of /api/Protected/profile
- [ ] EXTEND generated types manually when NSwag doesn't capture all API fields
- [ ] USE checkAuth() to validate token before protected operations

### Critical Implementation Rules
```typescript
// ✅ CORRECT - JWT token storage in auth store
interface AuthState {
  token: string | null;           // In-memory only
  tokenExpiresAt: Date | null;    // In-memory only
  // ... other fields
}

// ✅ CORRECT - Login with JWT token
login: (user, token, expiresAt) => set({
  user,
  token,                    // Store JWT token
  tokenExpiresAt: expiresAt,// Store expiration
  isAuthenticated: true
})

// ✅ CORRECT - Axios request interceptor
api.interceptors.request.use(async (config) => {
  const token = store.actions.getToken()  // Checks expiration
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// ✅ CORRECT - Token expiration check
getToken: () => {
  const state = get()
  if (!state.token || get().actions.isTokenExpired()) {
    return null  // Don't return expired tokens
  }
  return state.token
}

// ❌ WRONG - Persisting JWT tokens (security risk)
partialize: (state) => ({
  token: state.token,        // NEVER persist tokens!
  tokenExpiresAt: state.tokenExpiresAt  // NEVER persist!
})

// ❌ WRONG - Using expired tokens
getToken: () => state.token  // Should check expiration first
```

### API Response Structure
```typescript
// Login Response Structure
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-08-19T20:00:00Z",
    "user": {
      "id": "guid",
      "email": "user@example.com",
      "sceneName": "UserName"
    }
  },
  "message": "Login successful"
}
```

### Security Considerations
- **In-Memory Storage**: JWT tokens stored only in Zustand state (memory), cleared on page refresh
- **No Persistence**: Tokens never saved to localStorage/sessionStorage (XSS protection)
- **Automatic Expiration**: Tokens checked for expiration before each use
- **401 Cleanup**: Invalid tokens automatically cleared on API 401 responses
- **Selective Persistence**: Only user data persisted, never tokens

### Impact
Enables secure JWT authentication with the .NET API while maintaining frontend security best practices. All API calls now include proper authorization headers.

### Tags
#critical #jwt-authentication #api-integration #security #axios-interceptors #token-management #zustand

---

## 🚨 CRITICAL: API Interceptor Reload Loop Fix (READ FIRST) 🚨
**Date**: 2025-08-19
**Category**: Critical Bug Fix
**Severity**: Critical

### Context
Fixed critical continuous page reload loop that prevented login functionality. The app was reloading every few seconds due to an infinite loop in the axios response interceptor using `window.location.href` for authentication redirects.

### What We Learned
- **window.location.href = '/login'** in axios interceptors causes full page reloads, not React Router navigation
- **Infinite Loop Pattern**: App loads → checkAuth() → 401 error → window.location.href → Page reload → Loop continues
- **API Interceptors Should Not Handle Navigation**: Let React components and auth store handle routing decisions
- **401 Errors Should Be Handled by Auth Store**: Authentication state management should handle failed auth checks
- **Axios Interceptors Run Outside React Context**: They can't access React Router's navigate function

### Action Items
- [ ] NEVER use `window.location.href` in axios response interceptors
- [ ] LET auth store handle authentication state changes and routing decisions
- [ ] USE React Router's navigate() function only within React components/hooks
- [ ] LOG 401 errors but don't automatically redirect in interceptors
- [ ] HANDLE authentication failures in the auth store's checkAuth() method

### Critical Implementation Rules
```typescript
// ❌ WRONG - Causes infinite reload loop
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      window.location.href = '/login' // This causes page reload!
    }
    return Promise.reject(error)
  }
)

// ✅ CORRECT - Let auth store handle it
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Don't redirect - let the auth store handle it
      console.log('401 Unauthorized - Authentication expired')
    }
    return Promise.reject(error)
  }
)

// ✅ CORRECT - Handle auth failures in auth store
checkAuth: async () => {
  try {
    const response = await api.get('/api/Protected/profile');
    // Update authenticated state
  } catch (error) {
    // Update unauthenticated state - let components handle navigation
    set({ user: null, isAuthenticated: false, isLoading: false });
  }
}
```

### Impact
Fixes the critical reload loop that made the application completely unusable. Users can now access the login page and use the application without continuous page reloads.

### Tags
#critical #reload-loop #axios-interceptors #authentication #window-location #react-router

---

## 🚨 CRITICAL: NEVER Create Manual DTO Interfaces (READ FIRST) 🚨
**Date**: 2025-08-19
**Category**: NSwag Auto-Generation
**Severity**: Critical

### Context
NSwag auto-generation is THE solution for TypeScript types. NEVER manually create DTO interfaces - this violates the migration architecture and causes exactly the problems we spent hours fixing.

### What We Learned
- **NSwag Generates ALL DTO Types**: Use @witchcityrope/shared-types package
- **NEVER Manual Interface Creation**: Import from packages/shared-types/src/generated/ only
- **Manual User Interface Was The Problem**: The alignment issues we fixed were exactly what NSwag prevents
- **API DTOs are SOURCE OF TRUTH**: NSwag ensures perfect alignment automatically
- **Generated Types Always Current**: Run npm run generate:types when API changes
- **Original Architecture Specified This**: domain-layer-architecture.md planned NSwag from start

### Action Items
- [ ] READ: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` (updated with NSwag emphasis)
- [ ] READ: `/docs/architecture/react-migration/domain-layer-architecture.md` for NSwag implementation
- [ ] NEVER create manual DTO interfaces - always import from @witchcityrope/shared-types
- [ ] RUN: npm run generate:types when API changes
- [ ] REPLACE: All manual DTO interfaces with generated types
- [ ] IMPORT: Generated types only: import { User } from '@witchcityrope/shared-types'
- [ ] MANUAL FIX REQUIRED: openapi-typescript doesn't handle optional properties with defaults correctly
- [ ] MANUAL FIX REQUIRED: TanStack Query v5 requires mutations to accept arguments, use (_?: void) pattern

### Critical Implementation Rules
```typescript
// ✅ CORRECT - Import generated types
import { User, Event, Registration } from '@witchcityrope/shared-types';

function UserProfile({ user }: { user: User }) {
  // All type information is automatically correct
}

// ❌ WRONG - Never create manual interfaces
interface User {
  sceneName: string;        // This violates the architecture!
  createdAt: string;        // Use generated types instead
  lastLoginAt: string | null; // Matches C# DateTime?
  roles: string[];          // Matches C# List<string>
}

// ❌ WRONG - Assuming properties
interface User {
  firstName: string;  // API doesn't return this
  lastName: string;   // API doesn't return this
  fullName: string;   // API doesn't return this
}
```

### Tags
#critical #typescript-interfaces #dto-alignment #api-integration #migration

---

# Frontend Lessons Learned

## Overview

This document captures key technical learnings for React developers working on WitchCityRope. It contains actionable insights about React patterns, TypeScript integration, Mantine v7, TanStack Query, and frontend architecture decisions.

**IMPORTANT**: This file is for capturing technical lessons that help future development, NOT for documenting processes, progress tracking, or implementation details. For process documentation, see `/docs/guides-setup/` and `/docs/standards-processes/`.

## 🚨 CRITICAL: Conditional MSW Setup for UI Testing (READ FIRST) 🚨
**Date**: 2025-08-19
**Category**: Testing Infrastructure
**Severity**: Critical

### Context
Implemented conditional MSW (Mock Service Worker) setup that allows switching between mock data (for UI testing) and real API (for integration testing). Fixed infinite loop error that was causing "Maximum update depth exceeded" crashes.

### What We Learned
- **Conditional MSW Setup**: Use environment variable `VITE_MSW_ENABLED=true` to enable mocking for UI testing
- **MSW Browser Worker**: Must be initialized before React app renders for proper interception
- **Service Worker Location**: MSW worker file must be in `/public/mockServiceWorker.js`
- **Infinite Loop Cause**: Zustand store actions in useEffect dependencies cause re-render loops
- **State Update Pattern**: Use direct `set()` calls instead of action functions in async operations
- **API Endpoint Alignment**: Mock handlers must match actual API Pascal case endpoints

### Action Items
- [ ] USE environment variable `VITE_MSW_ENABLED=true/false` to control MSW
- [ ] INITIALIZE MSW before React app renders in main.tsx
- [ ] ALIGN mock handlers with actual API endpoints (Pascal case)
- [ ] AVOID circular state updates in Zustand async actions
- [ ] TEST both MSW (UI testing) and real API (integration testing) modes
- [ ] DOCUMENT MSW setup pattern for other developers

### Critical Implementation Rules
```typescript
// ✅ CORRECT - Conditional MSW setup in main.tsx
async function initializeApp() {
  await enableMocking() // Initialize MSW first
  createRoot(document.getElementById('root')!).render(<App />)
}

// ✅ CORRECT - Environment-based MSW control
if (import.meta.env.VITE_MSW_ENABLED === 'true') {
  await worker.start()
}

// ✅ CORRECT - Avoid circular updates in Zustand
checkAuth: async () => {
  // Direct state update instead of calling login action
  set({ user, isAuthenticated: true, isLoading: false });
}

// ❌ WRONG - Circular update pattern
checkAuth: async () => {
  get().actions.login(user); // This triggers re-renders
}
```

### Testing Modes
```bash
# UI Testing Mode (MSW enabled)
VITE_MSW_ENABLED=true  # Mock data for UI testing
# Visit: http://localhost:5173/test-msw

# Integration Testing Mode (Real API)
VITE_MSW_ENABLED=false # Real API connection
# Visit: http://localhost:5173/api-connection-test
```

### Impact
Enables flexible testing approach - UI testing with mock data and integration testing with real API. Fixes infinite loop crashes that made development impossible.

### Tags
#critical #msw-conditional #ui-testing #infinite-loop-fix #zustand #testing-infrastructure

## Lessons Learned

## TanStack Query v5 TypeScript Module Resolution Issues

**Date**: 2025-08-19  
**Category**: Critical Infrastructure  
**Severity**: High

### Context
Encountered TypeScript compilation errors with @tanstack/react-query v5.85.3 imports failing with "Module has no exported member" errors, despite the package being properly installed and runtime imports working correctly.

### What We Learned
- TanStack Query v5 package has both legacy and modern type definitions but TypeScript falls back to legacy types that contain private identifiers causing ES2015+ compatibility issues
- The package.json "types" field points to legacy types even when "exports" field specifies modern types for ES modules
- TypeScript bundler module resolution may not properly respect package exports for types in some configurations
- Vite can successfully bundle and run the code despite TypeScript compilation errors
- Using `isolatedModules: true` allows TypeScript to process individual files without cross-file type checking that causes the module resolution failures

### Action Items
- [ ] ALWAYS use `isolatedModules: true` in tsconfig.json for Vite projects to avoid cross-file type resolution issues
- [ ] VERIFY runtime imports work by testing JavaScript execution even when TypeScript fails
- [ ] USE standalone type declarations as fallback for problematic third-party packages
- [ ] MONITOR TanStack Query releases for fixes to TypeScript module resolution issues
- [ ] CONSIDER package.json modifications for package types field as temporary workaround (not recommended for production)

### Impact
Enables development to proceed with TanStack Query v5 despite TypeScript module resolution issues. Runtime functionality is unaffected.

### Tags
#tanstack-query #typescript #module-resolution #vite #critical-fix

---

## TanStack Query v5 Integration Patterns

**Date**: 2025-08-19  
**Category**: Architecture  
**Severity**: High

### Context
Implemented comprehensive API integration proof-of-concept using TanStack Query v5 to validate critical patterns for React development.

### What We Learned
- QueryClientProvider must wrap entire app at root level, above MantineProvider for proper context access
- Axios interceptors integrate seamlessly with TanStack Query for auth and error handling
- Query key factories prevent cache inconsistencies and enable precise cache invalidation
- Optimistic updates require careful rollback handling with onMutate/onError/onSettled pattern
- Use `gcTime` instead of deprecated `cacheTime` in v5+
- Retry logic should be error-type aware (don't retry 401s)

### Action Items
- [ ] ALWAYS use query key factories for consistent cache management
- [ ] IMPLEMENT optimistic updates with proper rollback for all mutations
- [ ] USE gcTime instead of cacheTime in TanStack Query v5+
- [ ] CONFIGURE retry logic based on error types (don't retry 401s)
- [ ] WRAP entire app with QueryClientProvider above other providers
- [ ] INCLUDE React Query DevTools in development for debugging

### Impact
Establishes production-ready patterns for all React feature development with proven technology stack choices.

### Tags
#tanstack-query #react-query #api-integration #optimistic-updates #cache-management #architecture

---

## Mantine v7 CSS Specificity Requirements

**Date**: 2025-08-18  
**Category**: Performance  
**Severity**: High

### Context
Discovered critical patterns for working with Mantine v7's internal class structure during component styling implementation.

### What We Learned
- Must use Mantine's internal class names (e.g., `.mantine-TextInput-input`) with `!important` to override framework styles
- Placeholder visibility requires targeting multiple selectors (type, class, data attributes) for complete coverage
- Password inputs have different internal structure requiring special targeting beyond regular input types
- CSS-only solutions are preferred over React state for visual behaviors - more performant and maintainable

### Action Items
- [ ] ALWAYS use Mantine's internal class names with !important for style overrides
- [ ] TEST all input types including password fields when implementing CSS changes
- [ ] USE CSS-only solutions for visual enhancements when possible
- [ ] TARGET multiple selector variations for comprehensive coverage

### Impact
Enables reliable styling of Mantine v7 components without framework conflicts.

### Tags
#mantine #css-specificity #performance #styling

---

## Mantine v7 Form Component Critical Fixes

**Date**: 2025-08-18  
**Category**: Security  
**Severity**: Critical

### Context
Fixed multiple critical issues in Mantine v7 form implementation including focus outline bugs, helper text positioning, and missing border color changes.

### What We Learned
- Orange focus outline bug occurs when Mantine components inherit browser focus-visible styles
- Helper text positioning requires flex layout control at root level, not just CSS order
- Placeholder visibility logic must check both focus AND empty state, not just focus
- Floating label positioning needs conditional logic based on focus and value state
- Border color changes on focus require targeting the correct Mantine internal classes

### Action Items
- [ ] ALWAYS remove focus outlines from Mantine root elements with CSS rules
- [ ] USE flex layout control at component root level for element ordering
- [ ] IMPLEMENT proper focus AND empty state logic for placeholder visibility
- [ ] TARGET correct Mantine internal classes for border color changes
- [ ] TEST all form states: empty, filled, focused, error

### Impact
Ensures form components work correctly across all interaction states without visual bugs.

### Tags
#mantine #forms #focus-states #critical #styling

---

## Zustand Authentication Store Patterns

**Date**: 2025-08-19  
**Category**: Security  
**Severity**: Critical

### Context
Implemented secure authentication state management using Zustand with httpOnly cookies for token storage.

### What We Learned
- HttpOnly cookies provide XSS protection that localStorage cannot offer
- Zustand stores should be separated by domain (auth, UI, forms) for better organization
- Selector hooks improve performance by preventing unnecessary re-renders
- Authentication state should be validated server-side on route changes
- Store actions should handle both success and error scenarios

### Action Items
- [ ] NEVER store auth tokens in localStorage or sessionStorage
- [ ] ALWAYS use httpOnly cookies for authentication token storage
- [ ] IMPLEMENT selector hooks for performance optimization
- [ ] SEPARATE stores by domain for better organization
- [ ] VALIDATE authentication server-side on protected routes

### Impact
Ensures secure authentication implementation that prevents XSS attacks and maintains good performance.

### Tags
#zustand #authentication #security #httponly-cookies #performance

---

## React Router v7 Protected Route Patterns

**Date**: 2025-08-19  
**Category**: Security  
**Severity**: High

### Context
Implemented secure route protection using React Router v7 loaders for server-side authentication validation.

### What We Learned
- Route guards should be implemented via loaders, not components, for security
- Authentication validation must happen server-side to prevent client bypass
- Loaders provide better UX by handling redirects before component rendering
- Role-based access control should be implemented at the loader level
- Return URLs should be properly encoded for post-login redirects

### Action Items
- [ ] ALWAYS implement route guards via loaders, not components
- [ ] VALIDATE permissions server-side in loader functions
- [ ] USE type-safe route definitions for better developer experience
- [ ] IMPLEMENT proper return URL handling for login redirects
- [ ] NEVER rely on client-only route protection

### Impact
Ensures secure route protection that cannot be bypassed by client-side manipulation.

### Tags
#react-router #authentication #security #route-guards #loaders

---

## TypeScript Strict Mode Configuration

**Date**: 2025-08-17  
**Category**: Architecture  
**Severity**: Medium

### Context
Configured TypeScript strict mode for better type safety in React components and API integration.

### What We Learned
- Strict mode catches many runtime errors at compile time
- Proper type definitions improve developer experience and code quality
- Generic types for API responses enable type-safe data handling
- Union types for form states prevent invalid state combinations
- Interface inheritance reduces code duplication

### Action Items
- [ ] ALWAYS use TypeScript strict mode for new projects
- [ ] DEFINE proper interfaces for all API responses
- [ ] USE generic types for reusable components and hooks
- [ ] IMPLEMENT union types for state management
- [ ] LEVERAGE interface inheritance for related types

### Impact
Reduces runtime errors and improves developer productivity through better type safety.

### Tags
#typescript #type-safety #architecture #strict-mode

---

## API DTO Alignment Pattern - Critical Frontend Architecture

**Date**: 2025-08-19  
**Category**: Architecture  
**Severity**: Critical

### Context
Implemented critical alignment between frontend User interface and actual API DTO structure. This addresses a fundamental mismatch where frontend expected properties (firstName, lastName, roles, permissions) that don't exist in the API response.

### What We Learned
- API DTOs are the SOURCE OF TRUTH for frontend type definitions (per business requirements)
- Frontend must adapt to match backend structure, not the reverse
- The actual API User DTO only contains: id, email, sceneName, createdAt, lastLoginAt
- sceneName is used instead of firstName/lastName per community requirements
- Roles/permissions are handled via separate API calls, not included in User DTO
- Misaligned types cause runtime errors and development confusion

### Action Items
- [ ] ALWAYS align frontend interfaces with actual API DTO structure
- [ ] ADD comments to interfaces: "// Aligned with API DTO - do not modify without backend coordination"
- [ ] USE sceneName instead of firstName/lastName for display purposes
- [ ] FETCH roles/permissions separately if needed, don't expect them in User DTO
- [ ] UPDATE MSW handlers to match real API response structure exactly
- [ ] DOCUMENT this pattern for future developers to prevent regression

### Impact
Ensures frontend code matches actual API behavior, preventing runtime errors and maintaining consistency with authenticated backend patterns.

### Tags
#api-alignment #dto-pattern #user-interface #backend-coordination #architecture

---

## React Component Performance Patterns

**Date**: 2025-08-16  
**Category**: Performance  
**Severity**: Medium

### Context
Optimized React component rendering performance using memoization and proper dependency management.

### What We Learned
- React.memo prevents unnecessary re-renders for pure components
- useCallback should be used for functions passed as props to prevent child re-renders
- useMemo should be used for expensive calculations, not simple object creation
- useEffect dependency arrays must include all dependencies to prevent stale closures
- Component composition is often better than complex conditional rendering

### Action Items
- [ ] USE React.memo for components that receive stable props
- [ ] IMPLEMENT useCallback for functions passed as props
- [ ] APPLY useMemo only for truly expensive calculations
- [ ] INCLUDE all dependencies in useEffect dependency arrays
- [ ] PREFER component composition over complex conditional rendering

### Impact
Improves application performance by reducing unnecessary re-renders and expensive calculations.

### Tags
#react #performance #memoization #hooks #rendering

---

## Responsive Design with Mantine Breakpoints

**Date**: 2025-08-16  
**Category**: Architecture  
**Severity**: Medium

### Context
Implemented responsive design patterns using Mantine's breakpoint system for consistent mobile experience.

### What We Learned
- Mantine provides consistent breakpoint values that should be used instead of custom ones
- Mobile-first approach works better with Mantine's responsive utilities
- Grid and Flex components handle most responsive layout needs
- Custom responsive logic should use Mantine's useMediaQuery hook
- Responsive spacing should use Mantine's responsive props

### Action Items
- [ ] USE Mantine's predefined breakpoints instead of custom values
- [ ] IMPLEMENT mobile-first responsive design approach
- [ ] LEVERAGE Grid and Flex components for responsive layouts
- [ ] USE useMediaQuery hook for custom responsive logic
- [ ] APPLY responsive spacing through Mantine's prop system

### Impact
Ensures consistent responsive behavior across all components and devices.

### Tags
#mantine #responsive-design #mobile-first #breakpoints #layout