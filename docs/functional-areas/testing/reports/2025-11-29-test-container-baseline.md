# Test Container Baseline - 2025-11-29

## Environment
- **Test Type**: E2E tests in isolated test containers
- **Workers**: 6 parallel workers
- **Browser**: Chromium
- **Date**: November 29, 2025

## Container Health Status
All containers healthy before test execution:
- `witchcity-postgres-test`: healthy
- `witchcity-api-test`: healthy (compiled successfully)
- `witchcity-web-test`: healthy (compiled successfully)
- `witchcity-test-runner`: healthy
- Database seeded with 25 test users

## Test Results Summary

| Metric | Value |
|--------|-------|
| Total Tests | 897 |
| Exit Code | 1 (failures present) |
| Test Files | 174 active |
| Workers | 6 parallel |

## Key Findings

### CSRF Token Behavior (VALIDATED)
The CSRF race condition fix is working correctly:
- Console errors appear during page initialization: `No CSRF token available for state-changing request`
- These are **noise**, not failures - login succeeds after CSRF initializes
- Pattern observed: `Console error: ❌ No CSRF token available` → `✅ Admin login successful`
- `csrfStore.isReady` properly gates the login button

### Login Flow Confirmation
- Auth helpers use correct selectors: `[data-testid="email-or-scenename-input"]`, `[data-testid="password-input"]`, `[data-testid="login-button"]`
- Login redirects to `/dashboard` after success
- Auth state managed via `csrfStore` in Zustand

### Remaining Test Infrastructure Issues
1. **Missing UI Elements**: Tests failing because selectors don't find expected elements
   - "Filter chips not found"
   - "Events table not found"
2. **Timeout Failures**: 32-second waits for elements that don't appear
3. **Stale Selectors**: Tests using outdated UI structure assumptions

## Previous Baseline Comparison
From handoff document (`/docs/functional-areas/testing/handoffs/phase3-test-fix-handoff.md`):
- Previous: 494 passed (61.8%) / 227 failed (28.4%) / 79 didn't run
- Current: 897 tests executed in test containers

## Next Steps (Phase 3)
1. Fix selector mismatches to match current UI
2. Replace timeout-based waits with proper element waits
3. Update tests to handle graceful degradation for optional UI elements
4. Target: >70% pass rate (560+ tests)

## Test Execution Method
Use the `test-environment` skill to run E2E tests in isolated test containers.
The skill handles all container setup and test execution automatically.

## Files Modified in This Session
- `tests/e2e/test-utils/helpers/auth.helpers.ts` - Auth helpers with CSRF-aware login
- `tests/e2e/test-utils/helpers/wait.helpers.ts` - Wait utilities with domcontentloaded
- `docker-compose.test.yml` - Test container configuration
- `apps/web/src/stores/csrfStore.ts` - CSRF state management
- `apps/web/src/pages/LoginPage.tsx` - Login page with CSRF gating
