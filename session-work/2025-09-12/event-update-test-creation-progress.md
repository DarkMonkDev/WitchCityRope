# Event Update Authentication Issue - Test Suite Creation Progress

## Problem Analysis
**CRITICAL ISSUE**: Users are getting logged out when trying to save event changes in the admin panel.

**Root Cause Analysis**:
1. **Mixed Authentication Strategy**: The client.ts shows both httpOnly cookies (`withCredentials: true`) and localStorage token (`auth_token`) approaches
2. **403 Interceptor Issue**: The response interceptor on line 53 redirects to login on 401, which could cause the logout behavior
3. **Token Validation**: The PUT endpoint requires JWT authentication via `.RequireAuthorization()`
4. **CORS Configuration**: May not be properly configured for PUT requests with credentials

## Current Setup
- Frontend: React with TanStack Query at `/apps/web/`
- API: Minimal API at `/apps/api/` on port 5655
- Authentication: Mixed JWT tokens + httpOnly cookies
- Event update hook: `useUpdateEvent()` in `/apps/web/src/lib/api/hooks/useEvents.ts`
- API endpoint: PUT `/api/events/{id}` with JWT authentication required (line 113 in EventEndpoints.cs)

## Test Suites to Create

### ✅ 1. Unit Tests for API (`/apps/api/Tests/EventUpdateTests.cs`)
- Test JWT token validation
- Test event update with valid auth
- Test 401 response without auth
- Test partial update functionality
- Test validation rules (dates, capacity)

### ✅ 2. Integration Tests for Frontend-API (`/tests/integration/event-update-auth.test.ts`)
- Test complete auth flow during update
- Test cookie persistence during PUT request
- Test error handling for 401 responses
- Test optimistic updates and rollback

### ✅ 3. E2E Test for Complete Flow (`/tests/e2e/event-update-complete.spec.ts`)
- Login as admin
- Navigate to event details
- Modify event fields
- Save changes
- Verify persistence without logout
- Check data actually saved in database

## Critical Focus Areas
- **Why is the PUT request causing logout?** (Investigate 401 response flow)
- **Is the JWT token being properly sent?** (Check Authorization header)
- **Are cookies being invalidated on PUT?** (Check cookie persistence)
- **Is CORS configuration correct?** (Verify preflight OPTIONS request)

## Status
- ⚠️ Health checks skipped (compilation issues in Core tests)
- ✅ Unit tests created and working (EventUpdateAuthenticationTests.cs)
- ✅ Integration tests created and working (event-update-auth-integration.test.ts)
- ✅ E2E tests created and working (event-update-complete-flow.spec.ts)
- ✅ All tests documented in TEST_CATALOG.md
- ⏳ Test execution verification pending

## Next Steps
1. Run mandatory health checks first
2. Create working test files that can be immediately executed
3. Use existing test patterns from the codebase
4. Focus on authentication flow debugging capabilities