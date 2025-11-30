# E2E Test Modernization Handoff Document

**Date**: November 30, 2025
**Checkpoint Commit**: `75cf59d1`
**Status**: In Progress

## Executive Summary

This document provides comprehensive handoff information for the E2E test modernization effort. All agents MUST read this document before proceeding.

## Critical Rules (NON-NEGOTIABLE)

### 1. Authentication - ALWAYS Use AuthHelpers
```typescript
// CORRECT - Use this pattern
import { AuthHelpers } from './test-utils/helpers/auth.helpers';
await AuthHelpers.loginAs(page, 'admin');  // or 'teacher', 'vetted', 'member', 'guest'

// WRONG - Never do this (except in login-specific tests)
await page.locator('[data-testid="email-or-scenename-input"]').fill('...');
await page.locator('[data-testid="password-input"]').fill('...');
```

**Available Test Accounts**:
- `admin` - admin@witchcityrope.com / Test123!
- `teacher` - teacher@witchcityrope.com / Test123!
- `vetted` - vetted@witchcityrope.com / Test123!
- `member` - member@witchcityrope.com / Test123!
- `guest` - guest@witchcityrope.com / Test123!

### 2. Wait Strategy - Use domcontentloaded, NOT networkidle
```typescript
// CORRECT
await page.waitForLoadState('domcontentloaded');

// WRONG - causes timeouts
await page.waitForLoadState('networkidle');
```

### 3. Modal Popup Patterns (Known Issue Area)
```typescript
// Pattern 1: Wait for modal to appear
const modal = page.locator('[role="dialog"]');
await expect(modal).toBeVisible({ timeout: 5000 });

// Pattern 2: Interact with modal fields using data-testid
await page.getByTestId('input-field-name').fill('value');

// Pattern 3: Wait for modal to close
await page.waitForSelector('[role="dialog"]', { state: 'detached', timeout: 10000 });

// Alternative: Wait for modal using Mantine classes
const mantineModal = page.locator('.mantine-Modal-root');
await expect(mantineModal).toBeVisible();
```

### 4. Test Results Location
All test outputs go to `./test-results/` - NO exceptions:
- Screenshots: `./test-results/[descriptive-name].png`
- Reports: `./test-results/`

---

## Phase 1: Delete Debug/Diagnostic Tests

**DELETE these files** - they are one-off debugging tests:

```
tests/e2e/debug-form-fields.spec.ts
tests/e2e/debug-auth-cookies.spec.ts
tests/e2e/debug-dashboard-vetting.spec.ts
tests/e2e/debug-form-design.spec.ts
tests/e2e/debug-save-button-regression.spec.ts
tests/e2e/debug-login.spec.ts
tests/e2e/debug-login-issue.spec.ts
tests/e2e/debug-login-comprehensive.spec.ts
tests/e2e/vetting-navigation-debug.spec.ts
```

---

## Phase 2: Consolidate Login Tests

### Files to KEEP (3 files):
- `test-bff-authentication.spec.ts` - Main auth flow test
- `login-with-scene-name.spec.ts` - Scene name login variant
- `post-login-return.spec.ts` - Return URL after login

### Files to DELETE (redundant):
```
login-diagnostic.spec.ts
login-methods-test.spec.ts
login-verification-test.spec.ts
working-login-solution.spec.ts
test-login-functionality.spec.ts
login-selector-fix-test.spec.ts
manual-login-inspection.spec.ts
test-login-direct.spec.ts
verify-login-form.spec.ts
simple-login-attempt.spec.ts
real-api-login.spec.ts
verify-login-fix.spec.ts
simple-login-test.spec.ts
final-real-api-login-test.spec.ts
demo-working-login.spec.ts
focused-login-test.spec.ts
real-api-login-test.spec.ts
login-401-investigation.spec.ts
```

### NEW Tests to Create:
1. **Password Reset Test** (`password-reset-workflow.spec.ts`)
   - Test password reset request flow
   - Verify email sent notification

2. **Email Verification Test** (`email-verification-workflow.spec.ts`)
   - Test new account registration
   - Verify email verification flow
   - NOTE: Don't test login with unverified email accounts

---

## Phase 3.1: Footer Tests

**Current Test**: `footer-component-test.spec.ts` (DELETE - stale design validation)

**Create NEW**: `footer-regression.spec.ts`

Test requirements:
1. Footer appears on public pages
2. Key links work (Privacy Policy, Terms)
3. Desktop: 3 columns visible
4. Mobile: Accordion behavior works

**Note**: Footer is APPROVED - current design is final.

---

## Phase 3.2: Navigation Tests (COMPLEX - Needs Care)

**Current Tests to Review/Replace**:
- `navigation-comprehensive.spec.ts` (18 failures)
- `navigation-visual-verification.spec.ts`
- `dashboard-navigation.spec.ts`

**Create NEW**: `navigation-workflow.spec.ts`

Test requirements (complex business rules):
1. **Guest (unauthenticated)**: Can see public pages, Events (limited view), login/register
2. **Member (unvetted)**: Dashboard, Profile, Events, limited features
3. **Vetted Member**: Full access to member features, can see vetted-only events
4. **Admin**: Full admin menu visible, all features

**CRITICAL**: Read the actual navigation components to understand role-based visibility:
- `src/components/layout/HeaderNavigation.tsx`
- `src/components/layout/MobileNavigation.tsx`

---

## Phase 3.3: User Dashboard + Profile Tests

**Current Tests to Review/Replace**:
- `user-dashboard-wireframe-validation.spec.ts` (35 failures - stale)
- `dashboard-comprehensive.spec.ts`
- `profile-page.spec.ts`
- `profile-update-full-persistence.spec.ts` (14 failures)

**Create NEW**: `user-dashboard-workflow.spec.ts`, `profile-workflow.spec.ts`

Test requirements:
1. Dashboard loads for authenticated user
2. Shows correct user info
3. Vetting status displays correctly
4. Quick actions work:
   - View events
   - Update profile
   - Put account on hold

---

## Phase 3.4: Admin Events Tests

**Current Tests**: 14+ files with various failures

**Create NEW**: Focus on workflow tests, not UI validation

Test requirements:
1. Create event workflow (end-to-end)
2. Edit event workflow
3. Session management (modal interactions)
4. Ticket configuration
5. Event publishing

**Modal Pattern for Sessions** (from admin-events-sessions.spec.ts):
```typescript
// Navigate to Setup tab
const setupTab = page.getByRole('tab', { name: 'Sessions / Ticket Types' });
await setupTab.click();

// Open modal
const addButton = page.locator('[data-testid="button-add-session"]');
await addButton.click();

// Wait for modal
const modal = page.locator('[role="dialog"]');
await expect(modal).toBeVisible();

// Fill form
await page.getByTestId('input-session-name').fill('value');

// Save and wait for modal close
await page.locator('[data-testid="button-save-session"]').click();
await page.waitForSelector('[role="dialog"]', { state: 'detached' });
```

---

## Phase 3.5: Vetting System Tests

**Current Tests**: 12+ files

Test requirements:
1. Complete vetting application workflow:
   - Submit application
   - Admin receives notification (email)
   - Admin reviews
   - Status changes trigger emails:
     - Approved
     - On Hold (with reason)
     - Denied (with reason)
2. Verify status displays correctly on dashboard

**Email Verification**: Check for API calls to email endpoints or verify UI feedback.

---

## Phase 3.6: Checkout + Refund Tests (IMPORTANT)

**Refund process NOT manually tested** - needs thorough testing

Test requirements:
1. Ticket purchase workflow
2. **REFUND WORKFLOW** (critical):
   - Admin initiates refund
   - Refund processes correctly
   - Status updates properly
   - Confirmation sent

---

## Phase 3.8: CMS Tests

**Current Tests**:
- `cms.spec.ts`
- `cms-accessibility.spec.ts`
- `cms-mobile-quick-test.spec.ts`

Test requirements:
1. Public pages load correctly
2. CMS content displays
3. Mobile responsiveness

---

## Key File Locations

- **AuthHelpers**: `tests/e2e/test-utils/helpers/auth.helpers.ts`
- **Wait Helpers**: `tests/e2e/test-utils/helpers/wait.helpers.ts`
- **Test Results**: `./test-results/`
- **Playwright Config**: `playwright.config.ts`

---

## Agent Guidelines

1. **DO NOT HALLUCINATE** - Verify assumptions by reading actual code
2. **Read lessons learned** before implementing: `/docs/lessons-learned/`
3. **Use data-testid selectors** when available
4. **Test must PASS on current UI** - we're testing current approved state
5. **Focus on WORKFLOWS** not design elements
6. **Report modal issues** if patterns don't work - may need consolidation
