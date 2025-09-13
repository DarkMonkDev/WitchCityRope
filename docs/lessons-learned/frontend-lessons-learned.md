# Frontend Development Lessons Learned

This document captures important lessons learned during React frontend development and authentication migration for WitchCityRope.

## 🚨 CRITICAL: CheckIn System Implementation Patterns (2025-09-13) 🚨
**Date**: 2025-09-13
**Category**: Feature Implementation
**Severity**: CRITICAL

### What We Learned
**MOBILE-FIRST FEATURE IMPLEMENTATION**: Successfully implemented complete CheckIn System following established patterns with mobile-optimized offline capability.

**KEY SUCCESS PATTERNS**:
- **Feature-Based Organization**: Clean `/features/checkin/` structure following Safety system model
- **Mobile-First Design**: Touch targets 44px+, responsive grids, battery optimization
- **Offline-First Architecture**: Local storage, sync queues, connection status indicators
- **TypeScript-First Development**: Complete type definitions before implementation
- **React Query Integration**: Optimistic updates, cache invalidation, error handling

**MOBILE OPTIMIZATION PATTERNS**:
```typescript
// ✅ CORRECT: Touch target optimization
export const TOUCH_TARGETS = {
  MINIMUM: 44, // px - absolute minimum for accessibility
  PREFERRED: 48, // px - preferred size for comfortable interaction
  BUTTON_HEIGHT: 48, // px - standard button height
  SEARCH_INPUT_HEIGHT: 56, // px - search input height for typing
  CARD_MIN_HEIGHT: 72 // px - minimum card height for attendee info
} as const;

// ✅ CORRECT: Offline-first data management
const handleCheckIn = useCallback(async (attendee: CheckInAttendee) => {
  // If offline, queue the action
  if (!isOnline) {
    await queueOfflineAction({
      type: 'checkin',
      eventId,
      data: request,
      timestamp: new Date().toISOString()
    });
    
    // Return optimistic response for immediate feedback
    return { success: true, message: 'Check-in queued (offline)' };
  }
  return checkinApi.checkInAttendee(eventId, request);
}, [isOnline, queueOfflineAction]);
```

### Action Items
- [x] **IMPLEMENT feature-based organization** following `/features/[domain]/` pattern
- [x] **CREATE comprehensive TypeScript definitions** before components
- [x] **USE mobile-first responsive design** with Mantine grid system
- [x] **APPLY offline-first architecture** with local storage and sync queues
- [x] **INTEGRATE React Query** with optimistic updates and cache management
- [x] **IMPLEMENT role-based access control** with graceful degradation
- [x] **CREATE touch-optimized interfaces** with 44px+ touch targets
- [x] **DOCUMENT all patterns** for future feature implementations

### Tags
#critical #mobile-first #offline-capability #checkin-system #feature-implementation #react-patterns #mantine-v7 #accessibility #performance

---

## Authentication Migration: JWT to httpOnly Cookies

### Problem
- Frontend was using JWT tokens stored in localStorage (security risk)
- authStore.login() method had signature: `login(user, token, expiresAt)`
- Tests and components were using the old 3-parameter signature
- TypeScript compilation errors throughout codebase
- Frontend mutations still using old endpoints and expecting token responses

### Solution
- Migrated to Backend-for-Frontend (BFF) pattern with httpOnly cookies
- Updated authStore.login() to only accept user: `login(user)`
- Removed all token-related logic from frontend
- Updated all test files and components to use new signature
- Fixed API endpoints and response handling in mutations

### Implementation Details
1. **AuthStore Changes**:
   - Removed token and expiresAt parameters from login action
   - Authentication now handled via httpOnly cookies
   - checkAuth() method uses `fetch('/api/auth/user', { credentials: 'include' })` with relative URL to leverage Vite proxy

2. **AuthService Changes**:
   - All API calls use `credentials: 'include'` for cookie-based auth
   - Removed setToken() and token management methods
   - Login/logout communicate with API to set/clear httpOnly cookies

3. **API Client Changes**:
   - Axios client configured with `withCredentials: true`
   - Base URL properly set to environment variable
   - No Authorization headers needed

4. **Mutation Updates** (CRITICAL FIX):
   - Updated login mutation to use `/api/auth/login` (not `/api/Auth/login`)
   - Changed response type from `LoginResponseWithToken` to `LoginResponseData`
   - Removed token extraction from login response
   - Updated logout mutation to use `/api/auth/logout`
   - Updated register mutation to use `/api/auth/register`

5. **Test File Updates**:
   - `/src/stores/__tests__/authStore.test.ts`: Updated all login calls
   - `/src/test-router.tsx`: Updated mock login call
   - `/src/test/integration/auth-flow-simplified.test.tsx`: Updated all login calls
   - `/src/test/integration/msw-verification.test.ts`: Removed setToken usage

### Files Modified
- `/src/stores/authStore.ts` (login signature changed, relative URL for checkAuth)
- `/src/services/authService.ts` (removed token logic)
- `/src/lib/api/client.ts` (proper base URL configuration)
- `/src/features/auth/api/mutations.ts` (fixed endpoints and response handling)
- All test files using `actions.login()` calls
- Integration tests using auth store

### Critical Learning
1. **ALWAYS update test files when changing API signatures**. TypeScript compilation will catch these errors, but they need to be systematically fixed.
2. **Endpoint consistency is crucial**: Use lowercase endpoints (`/api/auth/login`) to match backend
3. **Use relative URLs in frontend**: Leverage Vite proxy for development by using `/api/*` instead of full URLs
4. **Update mutation response types**: When migrating from tokens to cookies, update TypeScript interfaces to match new response structure

## React Development Patterns

### Component Architecture
- Use functional components with hooks exclusively
- Implement proper TypeScript interfaces for all props
- Use Mantine v7 components consistently
- Implement error boundaries for robust error handling

### State Management
- Zustand for global state (auth, app-level state)
- TanStack Query for server state management
- Local state with useState for component-specific state
- No class components or legacy patterns

### Testing Strategy
- Vitest for unit testing
- React Testing Library for component testing
- MSW for API mocking in tests
- Integration tests for auth flows

## Common Issues and Solutions

### Issue: TypeScript compilation errors after auth changes
**Cause**: Test files still using old authStore.login(user, token, expiresAt) signature
**Solution**: Update all test files to use new login(user) signature
**Prevention**: Run `npx tsc --noEmit` frequently during development

### Issue: Authentication not working in tests
**Cause**: Missing `credentials: 'include'` in fetch calls
**Solution**: Ensure all API calls include credentials for cookie-based auth
**Prevention**: Create api client wrapper with default credentials

### Issue: MSW mocking not working with cookies
**Cause**: MSW handlers need to simulate cookie behavior
**Solution**: Update MSW handlers to work with cookie-based authentication
**Prevention**: Test both success and failure auth scenarios

## Development Standards

### Code Quality
- Strict TypeScript configuration
- ESLint and Prettier for consistent formatting
- No any types unless absolutely necessary
- Comprehensive error handling

### API Integration
- Use generated types from @witchcityrope/shared-types package
- Never create manual TypeScript interfaces for API data
- All API calls must include `credentials: 'include'` for cookie auth
- Handle loading and error states consistently

### Security
- No tokens in localStorage/sessionStorage
- All authentication via httpOnly cookies
- Validate user permissions on both frontend and backend
- Sanitize user inputs and display data

## Next Steps
- Implement comprehensive error boundaries
- Add performance monitoring and optimization
- Improve loading state management
- Add comprehensive E2E testing with Playwright

## Action Items for Future Development
1. Always run TypeScript compilation before committing changes
2. Update test files immediately when changing API signatures  
3. Use cookie-based authentication for all new API integrations
4. Follow established patterns for state management and component architecture
5. Document any deviations or new patterns in this file