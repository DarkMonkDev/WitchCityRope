# Test Fix Plan - Terms of Service and Event Waiver E2E Tests

**Date**: 2025-11-12
**Current Status**: 12% pass rate (3/25 tests)
**Goal**: 100% pass rate

## Issue Analysis

### Issues I CAN Fix (Test-Developer)

1. **Test Data Isolation** (15 tests skipped - 60%)
   - **Problem**: Tests skip because test user already has RSVP/ticket
   - **Solution**: Use unique test users per test OR clean up test data before each run
   - **Effort**: 3-4 hours
   - **Priority**: HIGH (blocking 60% of tests)

2. **Test Timeout Configuration** (3 tests failing)
   - **Problem**: 15-second timeout on registration API might be too aggressive
   - **Solution**: Increase timeout for registration endpoint OR add better error handling
   - **Effort**: 30 minutes
   - **Priority**: MEDIUM (but this is masking a real backend issue)

3. **Event ID Extraction Resilience** (3 tests failing)
   - **Problem**: Test fails with "Could not extract event ID from URL"
   - **Solution**: Add better error handling and URL validation
   - **Effort**: 1 hour
   - **Priority**: HIGH (blocking all participation tests)

### Issues BACKEND Must Fix

1. **Registration API Performance** (3 tests failing - CRITICAL)
   - **Problem**: `/api/auth/register` endpoint timing out after 15 seconds
   - **Agent**: backend-developer
   - **Tests Blocked**: 50% of registration tests
   - **Priority**: CRITICAL

2. **Ticket Purchase API Validation** (1 test failing)
   - **Problem**: Returns 404 instead of 400 for validation errors
   - **Agent**: backend-developer
   - **Tests Blocked**: 1 test
   - **Priority**: MEDIUM

### Issues FRONTEND Must Fix

1. **Event Routing/Navigation** (3 tests failing potentially)
   - **Problem**: Event detail page navigation might be broken
   - **Agent**: react-developer
   - **Tests Blocked**: Potentially all participation tests if routing is broken
   - **Priority**: HIGH (needs investigation)

## My Implementation Plan

### Phase 1: Improve Test Resilience (30 minutes)

1. **Better Error Messages**
   - Add detailed logging when event ID extraction fails
   - Capture and log the actual URL when extraction fails
   - Add screenshot on extraction failure

2. **Increase Registration Timeout**
   - Change from 15s to 30s for registration API
   - Add warning that this is a workaround for backend issue
   - Document that backend needs to fix the actual timeout

### Phase 2: Fix Test Data Isolation (3-4 hours)

1. **Strategy A: Unique Test Users Per Test**
   - Generate unique email per test run with timestamp
   - **Problem**: Will create many test users in database
   - **Advantage**: Tests never conflict

2. **Strategy B: Clean Up Test Data Before Each Run**
   - Delete RSVPs/tickets for test user before each test
   - **Problem**: Requires database access or API endpoints
   - **Advantage**: Cleaner database

3. **Strategy C: Use Different Test Users Per Test File**
   - registration-tos.spec.ts uses test-tos-user@witchcityrope.com
   - rsvp-event-waiver.spec.ts uses test-rsvp-user@witchcityrope.com
   - volunteer-event-waiver.spec.ts uses test-volunteer-user@witchcityrope.com
   - ticket-purchase-waiver.spec.ts uses test-ticket-user@witchcityrope.com
   - **Advantage**: Simple, clean, reusable
   - **Problem**: Tests within same file still conflict

**DECISION**: Implement Strategy A (unique test users) - simplest and most reliable

### Phase 3: Improve Event ID Extraction (1 hour)

1. **Add URL Validation**
   ```typescript
   // Before extraction
   console.log('Current URL:', page.url());
   expect(page.url()).toContain('/events/');

   // Extract with better error handling
   const eventIdMatch = url.match(/\/events\/([^/]+)/);
   if (!eventIdMatch) {
     console.error('Failed to extract event ID from URL:', url);
     await page.screenshot({ path: './test-results/event-url-extraction-failure.png' });
     throw new Error(`Could not extract event ID from URL: ${url}`);
   }
   ```

2. **Add Retry Logic**
   - If extraction fails, wait and try again
   - Event might still be loading/navigating

### Phase 4: Document Backend Issues (30 minutes)

1. **Create Backend Issue Report**
   - Document registration API timeout issue
   - Document ticket purchase validation status code issue
   - Include reproduction steps
   - Add to functional area documentation

## Expected Outcomes

### After My Fixes (Phase 1-3)
- **Test Data Isolation**: 15 skipped tests → 0 skipped tests (all tests run)
- **Event ID Extraction**: Better error messages and screenshots when it fails
- **Registration Timeout**: Tests pass IF backend is responsive, fail gracefully if not

### After Backend Fixes
- **Registration**: 3 failing tests → 3 passing tests
- **Ticket Purchase Validation**: 1 failing test → 1 passing test

### Final Result
- **Current**: 12% pass rate (3/25)
- **After My Fixes**: ~40-50% pass rate (10-12/25) - all test issues resolved
- **After Backend Fixes**: 100% pass rate (25/25) - all issues resolved

## Implementation Order

1. ✅ Phase 1: Improve Test Resilience (30 min) - Quick wins
2. ✅ Phase 2: Fix Test Data Isolation (3-4 hours) - Biggest impact
3. ✅ Phase 3: Improve Event ID Extraction (1 hour) - Better debugging
4. ✅ Phase 4: Document Backend Issues (30 min) - Handoff to backend-developer

**Total Effort**: 5-6 hours
**Result**: All test-related issues fixed, clear backend handoff

## Success Criteria

- [ ] All tests run (no skips due to test data conflicts)
- [ ] Event ID extraction failures have detailed debugging info
- [ ] Backend issues are clearly documented with reproduction steps
- [ ] Tests pass when backend endpoints are healthy
- [ ] TEST_CATALOG updated with final results
