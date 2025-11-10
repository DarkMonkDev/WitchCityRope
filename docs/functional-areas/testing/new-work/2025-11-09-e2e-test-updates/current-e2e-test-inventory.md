# Current E2E Test Inventory

**Date**: 2025-11-09
**Total Test Files**: 27 (reduced from 50)

---

## Playwright Tests (3 files)

### Location: `/tests/playwright/`

1. **auth/login-with-scene-name.spec.ts** (14 tests)
   - Comprehensive login tests with email or scene name
   - P1 critical tests for both login paths
   - Error handling, validation, edge cases
   - **Status**: 12 passing, 2 skipped

2. **specs/dashboard-navigation.spec.ts** (5 tests)
   - Dashboard navigation after login
   - Direct URL navigation
   - Page refresh persistence
   - Content verification
   - Unauthenticated access handling
   - **Status**: Passing (when backend healthy)

3. **specs/admin-events-navigation.spec.ts** (4 tests)
   - Admin events page navigation
   - Event details access
   - No events scenario
   - Authentication persistence
   - Non-admin access restriction
   - **Status**: Passing (when backend healthy)

**Total Playwright Tests**: 23 tests across 3 files

---

## E2E Tests (24 files)

### Location: `/tests/e2e/`

#### Admin Events Tests (7 files)
1. **admin-events-comprehensive.spec.ts**
   - Comprehensive admin events functionality

2. **admin-events-dashboard-final.spec.ts**
   - Admin events dashboard (final consolidated version)

3. **admin-events-dependencies.spec.ts**
   - Admin events dependencies and relationships

4. **admin-events-sessions.spec.ts**
   - Event session management

5. **admin-events-simplified.spec.ts**
   - Simplified admin events workflow

6. **admin-events-ui-consistency.spec.ts**
   - UI consistency across admin events pages

7. **admin-events-volunteers.spec.ts**
   - Volunteer management for events

#### Checkout & Payment Tests (3 files)
8. **checkout-pricing.spec.ts**
   - Checkout pricing calculations

9. **payment.spec.ts**
   - Payment processing workflows

10. **paypal-integration.spec.ts**
    - PayPal integration testing

#### Event Management Tests (2 files)
11. **event-update-complete-flow.spec.ts**
    - Complete event update workflow (23K, comprehensive)

12. **verify-event-type-column.spec.ts**
    - Event type column verification

#### Form Design Tests (2 files)
13. **form-components.spec.ts**
    - Form component testing

14. **form-designs-check.spec.ts**
    - Form design validation

#### RSVP & Participation Tests (2 files)
15. **comprehensive-rsvp-verification.spec.ts**
    - Comprehensive RSVP verification workflows

16. **test-auth-rsvp.spec.ts**
    - Authenticated RSVP testing

#### Vetting System Tests (4 files)
17. **vetting-application.spec.ts**
    - Vetting application process

18. **vetting-email-templates.spec.ts**
    - Vetting email template testing

19. **vetting-hold-reinstatement.spec.ts**
    - Vetting hold and reinstatement workflows

20. **vetting-system.spec.ts**
    - Comprehensive vetting system tests (25K, most complete)

#### Authentication Tests (2 files)
21. **test-bff-authentication.spec.ts**
    - Backend-for-Frontend authentication

22. **test-vetting-authorization.spec.ts**
    - Vetting authorization testing

#### Infrastructure Tests (1 file)
23. **docker-services-test.spec.ts**
    - Docker services health checks

#### Other Tests (1 file)
24. **home-page.spec.ts**
    - Home page functionality

---

## Test Coverage Map

### Authentication & Authorization
- ✅ Login with email/scene name (Playwright)
- ✅ BFF authentication
- ✅ Vetting authorization
- ✅ Dashboard access control

### Events Management
- ✅ Admin events CRUD
- ✅ Event sessions
- ✅ Event volunteers
- ✅ Event dependencies
- ✅ Event updates
- ✅ Event type display

### Vetting System
- ✅ Vetting application
- ✅ Vetting emails
- ✅ Vetting holds/reinstatement
- ✅ Complete vetting workflows

### RSVP & Participation
- ✅ RSVP workflows
- ✅ Authenticated RSVP
- ✅ RSVP verification

### Payment & Checkout
- ✅ Checkout pricing
- ✅ Payment processing
- ✅ PayPal integration

### UI/UX
- ✅ Form components
- ✅ Form designs
- ✅ UI consistency
- ✅ Home page

### Infrastructure
- ✅ Docker services health
- ✅ Navigation flows
- ✅ Dashboard loading

---

## Test Quality Indicators

### Well-Organized Tests
- ✅ Playwright tests follow modern patterns
- ✅ Clear test names and descriptions
- ✅ Proper error filtering (401 errors)
- ✅ Appropriate wait strategies
- ✅ Good timeout configuration

### Areas for Future Improvement
- ⚠️ Some E2E tests may use older patterns
- ⚠️ Potential overlap in admin-events-* tests (7 files)
- ⚠️ Potential overlap in vetting-* tests (4 files)
- ⚠️ Potential overlap in payment tests (3 files)

---

## Maintenance Notes

### Files to Keep as Single Source of Truth
1. **login-with-scene-name.spec.ts** - Only login test (11 duplicates removed)
2. **admin-events-dashboard-final.spec.ts** - Final admin dashboard version (3 iterations removed)
3. **vetting-system.spec.ts** - Most comprehensive vetting test (25K)
4. **event-update-complete-flow.spec.ts** - Most comprehensive event update test (23K)

### Tests to Review for Potential Consolidation
1. Admin events tests (7 files) - May have overlap
2. Vetting tests (4 files) - May have overlap
3. Payment tests (3 files) - May have overlap
4. Form tests (2 files) - May have overlap

---

## Test Execution

### Run All Playwright Tests
```bash
cd /tests/playwright
npx playwright test
```

### Run Specific Test File
```bash
cd /tests/playwright
npx playwright test auth/login-with-scene-name.spec.ts
```

### Run All E2E Tests (if configured)
```bash
cd /tests/e2e
# Configuration may vary
```

---

## Historical Context

### Before Consolidation (Phase 1 complete)
- 50 total E2E test files
- Many duplicate login tests (11 files)
- Multiple iterations of same tests
- Debug/diagnostic files cluttering suite
- ~50% pass rate

### After Consolidation (Phase 3 complete)
- 27 total E2E test files (-46% reduction)
- Single authoritative login test
- Final versions only (no iterations)
- No debug files
- 80.8% pass rate (when backend healthy)

---

## Next Steps

1. **Fix backend DI issue** - IVettingEmailService registration
2. **Re-run all tests** - Verify 80%+ pass rate
3. **Update TEST_CATALOG** - Document deleted files
4. **Consider further consolidation** - Admin/vetting/payment tests
5. **Migrate to Playwright** - Modernize remaining E2E tests
