# Playwright E2E Testing Guide - Comprehensive Patterns
<!-- Last Updated: 2025-12-01 -->
<!-- Version: 4.0 - Enhanced with container-compatible patterns -->
<!-- Owner: Test Development Team -->
<!-- Status: Active - SINGLE SOURCE OF TRUTH for E2E Testing -->

> 🚨 **CRITICAL: This is the ONLY E2E testing tool for WitchCityRope** 🚨
>
> **Playwright is the exclusive E2E testing framework. Puppeteer and Stagehand are deprecated.**

## Quick Reference - Most Critical Patterns

**Before writing ANY E2E test, review these critical patterns:**

1. **✅ ALWAYS use `AuthHelpers.loginAs(page, role)`** - NEVER implement login manually
2. **✅ ALWAYS use relative URLs** - `page.goto('/')` NOT `page.goto('http://localhost:5173/')`
3. **✅ ALWAYS use `waitUntil: 'domcontentloaded'`** - NOT `networkidle` (causes timeouts)
4. **✅ ALWAYS create test data via TestHelperService** - NEVER rely on seed data
5. **✅ ALWAYS use `.last()` for interactive elements** - React strict mode creates duplicates
6. **⛔ NEVER hardcode API URLs** - Use `page.evaluate()` for API calls from tests

---

## Table of Contents

1. [Test Data Management (CRITICAL)](#1-test-data-management-critical)
2. [Container-Compatible URL Patterns (CRITICAL)](#2-container-compatible-url-patterns-critical)
3. [Wait Strategies (CRITICAL)](#3-wait-strategies-critical)
4. [CSRF Token Handling](#4-csrf-token-handling)
5. [Mantine v7 Component Patterns](#5-mantine-v7-component-patterns)
6. [React Strict Mode Testing Pattern](#6-react-strict-mode-testing-pattern)
7. [TDD Test Patterns](#7-tdd-test-patterns)
8. [Authentication Patterns](#8-authentication-patterns)
9. [Database Persistence Verification](#9-database-persistence-verification)
10. [Debugging Test Failures](#10-debugging-test-failures)
11. [Common Anti-Patterns to Avoid](#11-common-anti-patterns-to-avoid)

---

## 1. Test Data Management (CRITICAL)

### ⛔ NEVER Rely on Seed Data

**Problem**: Tests that rely on seed data are fragile - they break when seed data changes, and multiple tests using the same data create race conditions.

**Solution**: Create isolated test data per test run using TestHelperService endpoints.

### Available Test Helper Endpoints

#### Create Test User
```typescript
const response = await page.request.post('/api/test-helpers/users', {
  data: {
    email: `e2e-test-${Date.now()}@test.local`,
    password: 'Test123!',
    sceneName: `E2E Test User ${Date.now()}`,
    firstName: 'Test',
    lastName: 'User',
    role: 'Member',            // Optional: 'Member', 'Administrator', 'Teacher'
    vettingStatus: 3,          // Optional: 0-6 enum, 3 = Approved (vetted)
    bio: 'Test bio',           // Optional
    pronouns: 'they/them',     // Optional
    dateOfBirth: '1990-01-01', // Optional
  }
});
const { id, email, sceneName } = await response.json();
```

#### Delete Test User
```typescript
await page.request.delete(`/api/test-helpers/users/${userId}`);
```

#### Create Test Ticket Purchase
```typescript
const response = await page.request.post('/api/test-helpers/ticket-purchases', {
  data: {
    ticketTypeId: 'guid-string',  // Optional: uses first available if not provided
    totalPrice: 25.00,            // REQUIRED
    quantity: 1,
    paymentMethod: 'PayPal',      // 'PayPal', 'Venmo', 'Cash', 'Free'
    paymentStatus: 'Completed',   // 'Pending', 'Completed', 'Failed'
    userId: 'guid-string',        // Optional: creates unique user if not provided
    notes: 'E2E Test Purchase',   // Optional
    includePayPalCaptureId: true, // Optional: for refund testing
  }
});
const { id, paymentReference, userId, ticketTypeId, eventName } = await response.json();
```

**What TestHelperService Creates Automatically**:
1. ✅ **TicketPurchase record** - The payment record
2. ✅ **EventAttendance record** - Links user to event (AttendanceType.Ticket, Status.Active)
3. ✅ **Unique test user** (if no userId provided) - Avoids unique constraint violations

#### Delete Test Ticket Purchase
```typescript
await page.request.delete(`/api/test-helpers/ticket-purchases/${purchaseId}`);
```

### Correct Test Pattern - Create and Cleanup

```typescript
test.describe('Feature Tests', () => {
  let testPurchaseId: string;

  test('ticket type with sales cannot be deleted', async ({ page }) => {
    // Create isolated test data
    const response = await page.request.post('/api/test-helpers/ticket-purchases', {
      data: {
        ticketTypeId: ticketType.id,
        totalPrice: 25.00,
        quantity: 1,
        paymentMethod: 'PayPal',
        paymentStatus: 'Completed'
      }
    });
    const purchase = await response.json();
    testPurchaseId = purchase.id;

    // Test logic...
  });

  test.afterEach(async ({ page }) => {
    // Cleanup test data
    if (testPurchaseId) {
      await page.request.delete(`/api/test-helpers/ticket-purchases/${testPurchaseId}`);
    }
  });
});
```

### Prevention Rules

1. ✅ **NEVER rely on specific seed data** - Create test data per test
2. ✅ **ALWAYS clean up in afterEach/afterAll** - Prevent test pollution
3. ✅ **USE unique identifiers** - Prevent race conditions between tests
4. ✅ **CHECK CSRF tokens** - Some endpoints require X-CSRF-TOKEN header
5. ✅ **USE PaymentHelper** - Encapsulates common purchase scenarios

**PaymentHelper Location**: `/tests/e2e/test-utils/helpers/payment.helper.ts`

---

## 2. Container-Compatible URL Patterns (CRITICAL)

### 🔥 THE PROBLEM: Hardcoded URLs Break Container Testing

**Problem**: Tests written with hardcoded `http://localhost:5173` URLs fail when run inside test containers via test-environment skill.

**Root Cause**: Inside a container, `localhost` refers to the container itself, not the host machine. Container networking requires using service names (e.g., `test-web`) or relative URLs.

**Impact**: Tests that work locally fail 100% in containerized CI/CD pipelines.

### ✅ CORRECT PATTERN: Relative URLs for Portability

**In E2E Tests - ALWAYS Use Relative URLs**:
```typescript
// ✅ CORRECT - Works in local AND container environments
await page.goto('/');
await page.goto('/login');
await page.goto('/admin/events');
await page.goto('/events/123/edit');

// ❌ WRONG - Only works locally, breaks in containers
await page.goto('http://localhost:5173/');
await page.goto('http://localhost:5173/login');
await page.goto('http://localhost:5173/admin/events');
```

**Why Relative URLs Work**:
- Playwright uses `baseURL` from config (e.g., `http://localhost:5173` locally)
- test-environment skill sets `PLAYWRIGHT_BASE_URL=http://test-web:5173` for containers
- Same test code works in both environments without modification

### ✅ CORRECT PATTERN: API Request Patterns

**Use Pattern Matching, NOT Absolute URLs**:
```typescript
// ✅ CORRECT - Pattern matching works everywhere
await page.waitForResponse('**/api/events');
await page.waitForRequest('**/api/auth/login');
await page.waitForResponse(response =>
  response.url().includes('/api/events') && response.status() === 200
);

// ❌ WRONG - Hardcoded localhost breaks in containers
await page.waitForResponse('http://localhost:5655/api/events');
await page.waitForRequest('http://localhost:5655/api/auth/login');
```

**Why Pattern Matching Works**:
- Matches regardless of hostname (localhost vs test-api)
- Matches regardless of port (5655 vs 80)
- Tests remain portable across environments

### 🔧 REQUIRED CONFIGURATION: Vite allowedHosts

**Problem**: Vite dev server rejects connections from container names by default.

**Solution Required in vite.config.ts**:
```typescript
export default defineConfig({
  server: {
    host: '0.0.0.0',
    port: 5173,
    allowedHosts: ['test-web', 'localhost'], // REQUIRED for container networking
  },
});
```

**Why This Is Critical**:
- `test-web` allows Playwright container to access Vite dev server
- Without this, Vite returns 403 Forbidden to container requests
- Tests fail with network errors even though all containers are running

### 🔧 REQUIRED CONFIGURATION: Playwright baseURL

**Dynamic baseURL in playwright.config.ts**:
```typescript
export default defineConfig({
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173',
  },
});
```

**Why This Works**:
- Locally: Uses `http://localhost:5173` (default)
- In containers: test-environment skill sets `PLAYWRIGHT_BASE_URL=http://test-web:5173`
- Tests use relative URLs that combine with baseURL automatically

### 📋 PRE-FLIGHT CHECKLIST FOR E2E TEST CREATION

**Before Writing ANY E2E Test**:
- [ ] Use relative URLs: `page.goto('/')` NOT `page.goto('http://localhost:5173/')`
- [ ] Use API patterns: `'**/api/endpoint'` NOT `'http://localhost:5655/api/endpoint'`
- [ ] Verify vite.config.ts has `allowedHosts: ['test-web', 'localhost']`
- [ ] Verify playwright.config.ts uses `process.env.PLAYWRIGHT_BASE_URL`
- [ ] Test locally: `npm run test:e2e`
- [ ] Test in containers: Use test-environment skill

**During Code Review**:
- [ ] Search for `http://localhost` in test files → Flag for fix
- [ ] Search for hardcoded port numbers → Flag for fix
- [ ] Verify all page.goto() calls use relative URLs
- [ ] Verify all API waiting patterns use wildcards

### 🚨 CRITICAL: NEVER Hardcode API URLs in E2E Tests

**Problem**: Using `const API_BASE_URL = 'http://localhost:5655'` with Node.js `fetch()` breaks in Docker test containers because localhost doesn't resolve to the API container.

**Root Cause**: Node.js `fetch()` runs in the test runner container context, where `localhost:5655` doesn't reach the API service (which is in a different Docker container).

**Wrong Pattern (NEVER DO THIS):**
```typescript
// ❌ BROKEN - Node.js fetch in test runner context
const API_BASE_URL = 'http://localhost:5655';

async function fetchEventByTitle(title: string) {
  const response = await fetch(`${API_BASE_URL}/api/events`);  // ECONNREFUSED
  return response.json();
}

test('my test', async ({ page }) => {
  const event = await fetchEventByTitle('Test Event');  // FAILS IN DOCKER
});
```

**Why This Fails**:
- Test runner executes in `witchcity-test-runner` container
- API service runs in `witchcity-api` container
- `localhost:5655` in test runner context = test runner's localhost, NOT API container
- Result: `ECONNREFUSED 127.0.0.1:5655`

**Correct Pattern (ALWAYS DO THIS):**
```typescript
// ✅ CORRECT - Browser context fetch via page.evaluate()
async function apiRequest(page: Page, url: string): Promise<any> {
  return await page.evaluate(async (url) => {
    const res = await fetch(url, { credentials: 'include' });
    const text = await res.text();
    try {
      return { status: res.status, data: JSON.parse(text) };
    } catch {
      return { status: res.status, data: text };
    }
  }, url);
}

async function fetchEventByTitle(page: Page, title: string) {
  const response = await apiRequest(page, '/api/events');  // Relative URL
  return response.data.find((e: any) => e.title === title);
}

test('my test', async ({ page }) => {
  const event = await fetchEventByTitle(page, 'Test Event');  // WORKS IN DOCKER
});
```

**Why This Works**:
- `page.evaluate()` runs fetch **inside the browser context**
- Browser knows how to resolve `/api/events` via the web app's origin
- Web app correctly proxies to API service via Docker networking
- No hardcoded URLs needed - uses relative paths

**Key Differences**:
| Pattern | Context | URL Resolution | Docker Compatible |
|---------|---------|----------------|-------------------|
| `fetch()` directly | Node.js test runner | Test runner's localhost | ❌ NO |
| `page.evaluate(fetch)` | Browser | Web app's origin + proxy | ✅ YES |

**Working Example**: `/tests/e2e/session-ticket-availability.spec.ts` (lines 31-47)

---

## 3. Wait Strategies (CRITICAL)

### ⛔ NEVER Use `networkidle` Wait Strategy

**Problem**: Using `waitForLoadState('networkidle')` causes test timeouts in applications with continuous background requests (polling, analytics, metrics).

**Root Cause**: App has continuous background requests that prevent network from ever becoming truly "idle".

#### Why networkidle Fails

**networkidle Definition**: Waits until there are no network connections for at least 500ms.

**Apps with Continuous Requests**:
- API polling (e.g., notification checks every 30 seconds)
- Analytics beacons
- Health check endpoints
- Real-time updates
- Background data synchronization

**Result**: Network NEVER becomes idle → Test times out after 30 seconds

#### ❌ WRONG Pattern - Using networkidle

```typescript
// ❌ WRONG - Causes timeouts with background requests
await page.goto('http://localhost:5173/events')
await page.waitForLoadState('networkidle')  // HANGS - network never idle

await page.reload()
await page.waitForLoadState('networkidle')  // HANGS - network never idle
```

#### ✅ CORRECT Pattern - Use domcontentloaded

```typescript
// ✅ CORRECT - Waits for DOM ready, not network idle
await page.goto('/events')
await page.waitForLoadState('domcontentloaded')  // DOM ready, doesn't wait for network

await page.reload()
await page.waitForLoadState('domcontentloaded')  // DOM ready after refresh
```

#### When to Use Each Wait Strategy

**Use `domcontentloaded` (DEFAULT for most tests)**:
- ✅ Navigation between pages
- ✅ Page refreshes
- ✅ Apps with polling/background requests
- ✅ When you just need DOM elements to be ready
- ✅ 95% of E2E test scenarios

**Use `networkidle` (RARE - only when specifically needed)**:
- ⚠️ Waiting for dynamic content that loads via AJAX
- ⚠️ Testing lazy-loaded images/resources
- ⚠️ Apps with NO background requests/polling
- ⚠️ Specific scenarios where you need all network activity to finish

**Use `load` (MIDDLE GROUND)**:
- ⚠️ Need all resources loaded (images, stylesheets, scripts)
- ⚠️ Testing initial page load performance
- ⚠️ Can still timeout if resources fail to load

#### Detection and Prevention

**How to Detect This Issue**:
1. Test hangs for ~30 seconds before failing
2. Error: `Timeout 30000ms exceeded`
3. Error context: `waiting for load state "networkidle"`
4. Check browser DevTools: Network tab shows ongoing requests

**Prevention Rules**:
1. ✅ **DEFAULT to `domcontentloaded`** for all navigation waits
2. ✅ **AVOID `networkidle`** unless you have a specific reason
3. ✅ **DOCUMENT why** if you use `networkidle` (comment explaining need)
4. ❌ **NEVER use `networkidle`** in apps with polling/background requests

#### Alternative Patterns

**If you need to wait for specific API calls**:
```typescript
// ✅ CORRECT - Wait for specific request, not all network
const responsePromise = page.waitForResponse(resp =>
  resp.url().includes('/api/events') && resp.status() === 200
)
await page.goto('/events')
await responsePromise  // Wait for specific API call
```

**If you need to wait for specific element after load**:
```typescript
// ✅ CORRECT - Wait for DOM, then wait for specific element
await page.goto('/events')
await page.waitForLoadState('domcontentloaded')
await page.locator('[data-testid="event-list"]').waitFor({ state: 'visible' })
```

#### Key Lesson

**ABSOLUTE RULE**: Use `domcontentloaded` as the default wait strategy for page loads and navigation. Only use `networkidle` if you have a specific, documented reason and the app has NO background requests.

**If your tests timeout on `networkidle`**:
- ❌ Don't increase timeout duration
- ✅ Replace with `domcontentloaded`
- ✅ Wait for specific elements/requests if needed

**Why This Matters**: Modern web apps often have analytics, polling, or real-time features that prevent network from becoming truly idle. Tests using `networkidle` will fail or become extremely slow in these apps.

---

## 4. CSRF Token Handling

### Extracting CSRF Token from Page Context

```typescript
// Helper to get CSRF token from cookies
async function getCsrfToken(page: Page): Promise<string> {
  const cookies = await page.context().cookies();
  const csrfCookie = cookies.find((c) => c.name === 'XSRF-TOKEN');
  if (!csrfCookie) {
    throw new Error('CSRF token cookie not found - ensure user is logged in');
  }
  return csrfCookie.value;
}

// Using the token
const csrfToken = await getCsrfToken(page);
await page.request.post('/api/events', {
  headers: { 'X-CSRF-TOKEN': csrfToken },
  data: { /* ... */ }
});
```

**When CSRF tokens are required**:
- All state-changing operations (POST, PUT, DELETE, PATCH)
- After user is logged in (CSRF system initializes on login)
- Some test helper endpoints require CSRF tokens

---

## 5. Mantine v7 Component Patterns

### 🚨 CRITICAL: Checkbox Interaction Pattern

**Problem**: Mantine v7 completely hides the actual checkbox input with CSS, making it invisible to Playwright's actionability checks. Standard `.check()` and label-based interactions fail.

#### ✅ CORRECT Pattern: Click the Parent Mantine Wrapper

```typescript
// ✅ CORRECT - Click parent wrapper element
const mantineCheckbox = page.locator('input[data-testid="waiver-checkbox"]')
  .locator('..')  // Get parent element (the Mantine checkbox wrapper)
  .last();  // Use .last() to avoid React strict mode duplicate
await mantineCheckbox.click();

// Verify the checkbox is checked
await expect(page.locator('input[data-testid="waiver-checkbox"]')).toBeChecked();
```

#### Prevention Rules for Mantine v7 Checkboxes

1. ✅ **ALWAYS click parent wrapper**: `page.locator('input[data-testid]').locator('..').last().click()`
2. ✅ **ALWAYS use `.last()`**: Avoids React strict mode duplicates
3. ✅ **ALWAYS verify with `.toBeChecked()`**: Check the actual input state after clicking
4. ❌ **NEVER use `.check()` on Mantine checkboxes**: Will fail with visibility errors
5. ❌ **NEVER click labels directly**: Doesn't trigger Mantine checkbox state

### Selector Patterns for Mantine Components

**Use data-testid attributes for stability**:
```typescript
// Good - Mantine component with data-testid
await page.locator('[data-testid="submit-button"]').click();

// Avoid - CSS class selectors (can change)
await page.locator('.mantine-Button-root').click();
```

---

## 6. React Strict Mode Testing Pattern

### 🚨 CRITICAL: Always Use .last() for Interactive Elements

**Problem**: React strict mode creates duplicate DOM elements (both hidden and visible) for debugging purposes. Using `.first()` selects hidden duplicates, causing "Element is not visible" errors.

#### ✅ CORRECT Pattern: Using .last()

```typescript
// ✅ CORRECT - Selects visible element
const button = page.locator('button:has-text("Submit RSVP")').last();
await button.click(); // Works - clicks visible element

const checkbox = page.locator('input[data-testid="waiver-checkbox"]')
  .locator('..')  // For Mantine - get parent wrapper
  .last();  // Select visible duplicate
await checkbox.click(); // Works - clicks visible element
```

#### When to Apply This Pattern

**ALWAYS use `.last()` for**:
- ✅ Buttons
- ✅ Inputs
- ✅ Checkboxes (especially Mantine)
- ✅ Links
- ✅ Any interactive element that could be duplicated

**ONLY use `.first()` when**:
- ❌ You explicitly want the FIRST occurrence in a list (e.g., first item in search results)
- ⚠️ **CAUTION**: Even in these cases, `.last()` is safer if elements might be duplicated

---

## 7. TDD Test Patterns

### 🚨 CRITICAL: Defensive Skip Conditions for TDD Tests

**Problem**: TDD tests written before feature implementation fail with "element not found" errors when UI components don't exist yet, creating false negatives.

**Root Cause**: Tests were written as valid TDD specifications but lacked defensive checks for whether features are actually implemented.

#### ✅ CORRECT PATTERN: Defensive Skip Conditions

**MANDATORY for all TDD tests written before implementation:**

```typescript
test('should show vetting status on dashboard', async ({ page }) => {
  await AuthHelpers.loginAs(page, 'member');
  await page.goto('/dashboard');
  await page.waitForLoadState('domcontentloaded');

  // 1. Flexible selectors with OR patterns
  const vettingStatusSection = page.locator('[data-testid="vetting-status-section"]')
    .or(page.locator('section').filter({ hasText: /vetting/i }))
    .or(page.locator('[class*="vetting"]'))
    .or(page.locator('text=/vetting status/i')).first();

  // 2. Check if element exists before proceeding
  const count = await vettingStatusSection.count();
  if (count === 0) {
    console.log('⚠️ Vetting status section not found - feature may not be implemented yet. Skipping test.');
    test.skip();
    return;
  }

  // 3. ONLY run assertions if element exists
  await expect(vettingStatusSection).toBeVisible();
  // ... rest of test ...
});
```

#### Route Existence Check Pattern

**For tests that depend on specific routes existing:**

```typescript
test.beforeEach(async ({ browser }) => {
  page = await browser.newPage();
  await AuthHelpers.loginAs(page, 'admin');

  // Navigate to expected page
  await page.goto('/admin/email-templates');
  await page.waitForLoadState('domcontentloaded');

  // Check if route exists (may redirect or 404 if not implemented)
  const currentUrl = page.url();
  if (!currentUrl.includes('/admin/email-templates')) {
    console.log('⚠️ Email templates page not found - feature may not be implemented yet. Skipping test.');
    test.skip();
    return;
  }

  // Check for required UI elements
  const vettingTab = page.locator('[data-testid="tab-vetting"]')
    .or(page.getByRole('tab', { name: 'Vetting' }));

  if (await vettingTab.count() === 0) {
    console.log('⚠️ Vetting tab not found - feature may not be implemented yet. Skipping test.');
    test.skip();
    return;
  }

  await vettingTab.click();
});
```

#### Why This Pattern is MANDATORY

**Before (Tests Fail)**:
- ❌ Tests throw "element not found" errors
- ❌ False negatives - tests fail when they shouldn't
- ❌ No clear indication why tests are failing
- ❌ Developers waste time debugging "broken" tests

**After (Tests Skip Gracefully)**:
- ✅ Tests skip with clear explanations
- ✅ Console logs show exact reason for skip
- ✅ Tests automatically activate when features are implemented
- ✅ No modification needed when feature is built
- ✅ Clear signal that feature isn't ready yet

#### When to Apply This Pattern

**ALWAYS apply for**:
- ✅ TDD tests written before implementation
- ✅ Tests for features in development
- ✅ Tests that depend on specific routes existing
- ✅ Tests that interact with UI components that may not exist yet
- ✅ Tests for features behind feature flags

**DON'T apply for**:
- ❌ Tests for stable, production features
- ❌ Tests for features known to be fully implemented
- ❌ Regression tests for existing functionality

---

## 8. Authentication Patterns

### 🚨🚨🚨 ULTRA CRITICAL: E2E TESTS MUST USE LOGIN HELPER - ZERO TOLERANCE

**THIS IS NON-NEGOTIABLE. VIOLATIONS WILL BE REJECTED IMMEDIATELY.**

#### ❌ ABSOLUTELY FORBIDDEN - Manual Login Implementation

```typescript
// ❌ WRONG - NEVER DO THIS - AUTOMATIC REJECTION
test.beforeEach(async ({ page }) => {
  await page.goto('http://localhost:5173/login');
  await page.fill('input[name="email"]', 'admin@witchcityrope.com');
  await page.fill('input[name="password"]', 'Test123!');
  await page.click('button[type="submit"]');
  await page.waitForURL('http://localhost:5173/');
});
```

**Why this is FORBIDDEN**:
- Duplicates existing helper code
- Breaks when login UI changes
- Not battle-tested like helper
- Violates DRY principle
- User gets extremely frustrated

#### ✅ MANDATORY PATTERN - Use AuthHelpers.loginAs()

```typescript
// ✅ CORRECT - ALWAYS USE THIS
import { AuthHelpers } from '../../../../tests/e2e/test-utils/helpers/auth.helpers';

test.beforeEach(async ({ page }) => {
  await AuthHelpers.loginAs(page, 'admin');
  await page.goto('/admin/events');
});
```

**Login Helper Location**: `/tests/e2e/test-utils/helpers/auth.helpers.ts`

**Available Roles**: 'admin', 'teacher', 'member', 'vetted', 'guest'

**Why This is MANDATORY**:
- ✅ Single source of truth for login logic
- ✅ Handles Mantine form interactions correctly
- ✅ Includes error recovery strategies
- ✅ Monitors console errors
- ✅ Battle-tested across 100+ tests
- ✅ Waits for CSRF readiness before login (prevents 401 errors)

#### 🛑 PRE-FLIGHT CHECKLIST FOR E2E TESTS

**BEFORE writing ANY E2E test with authentication**:
- [ ] Import AuthHelpers from correct path
- [ ] Use `AuthHelpers.loginAs(page, role)` - NOTHING ELSE
- [ ] NEVER manually implement login flow
- [ ] NEVER use page.goto to login page in tests
- [ ] NEVER use page.fill for email/password in tests

**If you find yourself typing "page.goto('/login')" → STOP and use AuthHelpers instead.**

**This lesson learned exists because this mistake happened. Do not repeat it.**

### Session Management

```typescript
// Clear authentication state
await AuthHelpers.clearAuthState(page);

// Login as different user
await AuthHelpers.loginAs(page, 'member');

// Verify authenticated state
await expect(page.locator('[data-testid="user-menu"]')).toBeVisible();
```

### Cookie Handling

**Authentication cookies are managed automatically by AuthHelpers**:
- httpOnly cookies set by backend
- CSRF tokens extracted and sent with requests
- Session persistence verified
- No manual cookie management needed

---

## 9. Database Persistence Verification

### 🚨 CRITICAL: E2E Persistence Testing Value - Real Bug Discovery

**Problem**: E2E tests successfully caught a critical backend bug where POST /api/events/{id}/rsvp returns 201 success but doesn't persist the RSVP to the database.

**Bug Impact**: Users see "RSVP confirmed" but database has no record - data loss!

#### Pattern: What E2E Persistence Tests MUST Verify

**1. UI Shows Success**
**2. API Returns Success Status Code**
**3. Database Record Exists with Correct Status** (MOST IMPORTANT)
**4. Database Record Persists After Page Refresh** (CRITICAL):

```typescript
// Refresh page to clear React Query cache
await page.reload();

// Verify UI still shows correct state (from database, not cache)
await expect(page.locator('button:has-text("Cancel RSVP")')).toBeVisible();

// Verify database still has record
await DatabaseHelpers.verifyEventParticipation(userId, eventId, 1, 2);
```

#### Database Query Helper Pattern

```typescript
import { DatabaseHelpers } from './test-utils/utils/database-helpers';

// Verify event participation in database
await DatabaseHelpers.verifyEventParticipation(
  userId,        // User who registered
  eventId,       // Event they registered for
  1,             // Status: 1=Active (filters out cancelled)
  2              // AttendanceType: 2=RSVP (filters out Ticket/CheckIn)
);

// Close database connections after test
test.afterAll(async () => {
  await DatabaseHelpers.closeDatabaseConnections();
});
```

#### 🚨 CRITICAL: Database Query Filtering - Filter by Status AND Type

**Problem**: Users can have multiple attendance records for the same event (e.g., both RSVP and Ticket, or old cancelled records). Querying only by userId + eventId returns wrong record type or old cancelled records.

#### ✅ CORRECT Pattern: Filter by Status AND AttendanceType

```typescript
// ✅ CORRECT - Filters by BOTH Status AND Type
const sql = `
  SELECT * FROM "EventAttendances"
  WHERE "UserId" = $1
    AND "EventId" = $2
    AND "Status" = $3
    AND "AttendanceType" = $4
  ORDER BY "UpdatedAt" DESC
  LIMIT 1
`;

const result = await query(sql, [
  userId,
  eventId,
  1,  // Status: 1=Active (filters out cancelled records)
  2   // AttendanceType: 2=RSVP (ensures we get RSVP, not Ticket)
]);
```

**AttendanceType Enum Reference**:
- 1 = Ticket (Paid ticket purchase)
- 2 = RSVP (Free RSVP, requires waiver)
- 3 = CheckIn (Walk-in at event)
- 4 = Volunteer (Volunteer assignment)

**Status Enum Reference**:
- 0 = Cancelled/inactive
- 1 = Active

#### Prevention Rules for E2E Persistence Tests

1. ✅ **NEVER trust API status codes alone** - Verify database state
2. ✅ **ALWAYS test page refresh** - Ensures data persists, not just cached
3. ✅ **FILTER database queries properly** - Status AND Type
4. ✅ **DEBUG log what records exist** - When verification fails, show all records

---

## 10. Debugging Test Failures

### Visual Debugging
```bash
# Run in headed mode
npx playwright test --headed

# Use UI mode for step-by-step debugging
npx playwright test --ui

# Debug specific test
npx playwright test --debug test-name
```

### Taking Screenshots
```typescript
test('visual regression', async ({ page }) => {
  await page.goto('/');

  // Take screenshot on failure (automatic)
  await expect(page).toHaveScreenshot('homepage.png');

  // Take manual screenshot
  await page.screenshot({ path: 'debug.png', fullPage: true });
});
```

### Console Logs
```typescript
// Capture console logs
page.on('console', msg => console.log('Browser log:', msg.text()));

// Capture network failures
page.on('requestfailed', request => {
  console.log('Failed request:', request.url());
});
```

### Common Debugging Patterns

**Selector Issues**:
```typescript
// Check if element exists
const count = await page.locator('[data-testid="my-element"]').count();
console.log(`Element count: ${count}`);

// Get element properties
const element = page.locator('[data-testid="my-element"]');
console.log('Visible:', await element.isVisible());
console.log('Enabled:', await element.isEnabled());
console.log('Text:', await element.textContent());
```

**Network Issues**:
```typescript
// Monitor all network requests
page.on('request', request => {
  console.log('Request:', request.method(), request.url());
});

page.on('response', response => {
  console.log('Response:', response.status(), response.url());
});
```

---

## 11. Common Anti-Patterns to Avoid

### ❌ NEVER Use Soft Assertions in E2E Tests

**Problem**: Using `if (await element.isVisible())` pattern makes tests pass even when features are broken, creating FALSE CONFIDENCE in test suite.

```typescript
// ❌ WRONG - Test passes if modal doesn't exist
if (await modal.isVisible()) {
  await expect(modal).toContainText('Success');
}

// ✅ CORRECT - Test FAILS if modal doesn't exist
await expect(modal).toBeVisible();
await expect(modal).toContainText('Success');
```

**When Soft Assertions Are Acceptable**: Only for INTENTIONALLY optional elements like marketing banners, not core features.

### ❌ NEVER Suggest Long Timeouts (10+ Minutes)

**Problem**: Agents repeatedly suggest 10-minute or longer timeouts for tests, masking stalled/broken tests.

**User Feedback**: "NO TEST should ever take 10 minutes. Most will not take more than 30 seconds, giving them 1 minute maybe 1.5 at the absolute most is plenty."

```typescript
// ❌ WRONG - 10 minute timeout masks stalled test
test.setTimeout(600000); // ABSOLUTELY NO!

// ✅ CORRECT - 90 second ABSOLUTE MAXIMUM
test.setTimeout(90000); // ABSOLUTE MAX
await page.waitForSelector('.element', { timeout: 30000 }); // 30 seconds typical
```

**What to Do When Tests Timeout**: Fix the underlying issue (wrong selector, missing feature, service down), don't increase timeout above 90 seconds.

### ❌ NEVER Use Ambiguous Label Selectors

**Problem**: Playwright tests fail with "strict mode violation" when using `getByLabel()` for form fields with duplicate labels on the same page.

**Root Cause**: Multiple forms on same page (e.g., "Event Position" form and "Session" modal) with identical labels like "Start Time", causing Playwright to find 2+ matching elements.

**Solution**: Use `getByTestId()` instead of `getByLabel()` when multiple fields share the same label.

```typescript
// ❌ WRONG - Fails with strict mode violation if multiple "Start Time" labels exist
await page.getByLabel('Start Time').fill('09:00');

// ✅ CORRECT - Targets specific field using data-testid
await page.getByTestId('input-session-start-time').fill('09:00');
```

### ❌ NEVER Reuse Stale Element Locators After Navigation

**Problem**: Reusing element references after navigation causes "element is detached" errors.

```typescript
// ❌ WRONG - Reusing element after navigation
const eventCard = page.locator('[data-testid="event-card"]').first();
await eventCard.click();
await page.goBack();
await eventCard.click(); // FAILS - Element is detached!

// ✅ CORRECT - Create fresh locator after navigation
const eventCard1 = page.locator('[data-testid="event-card"]').first();
await eventCard1.click();
await page.goBack();
const eventCard2 = page.locator('[data-testid="event-card"]').first();
await eventCard2.click(); // WORKS
```

### ❌ NEVER Use Global Text Searches

**Problem**: Generic text selectors match unintended elements.

```typescript
// ❌ WRONG - Global text search
const eventsLink = page.locator('text=Events');

// ✅ CORRECT - Scoped to specific container
const breadcrumb = page.locator('[data-testid="event-details"]');
const eventsLink = breadcrumb.getByRole('link', { name: 'Events' });
```

---

## Quick Commands Reference

```bash
# All commands run from project root
cd /home/chad/repos/witchcityrope

# Run all E2E tests
npx playwright test

# Run specific file
npx playwright test tests/e2e/auth/login.spec.ts

# Debug mode with browser
npx playwright test --debug

# UI mode (recommended for debugging)
npx playwright test --ui

# Update screenshots
npx playwright test --update-snapshots

# List all tests (verify structure)
npx playwright test --list

# Run specific browser
npx playwright test --project=chromium
```

---

## Test Accounts

These accounts are seeded by DbInitializer and available for testing:

- **Admin**: admin@witchcityrope.com / Test123!
- **Teacher**: teacher@witchcityrope.com / Test123!
- **Vetted Member**: vetted@witchcityrope.com / Test123!
- **General Member**: member@witchcityrope.com / Test123!
- **Guest/Attendee**: guest@witchcityrope.com / Test123!
- **Safety Coordinator 1**: coordinator1@witchcityrope.com / Test123! (SafetyTeam role)
- **Safety Coordinator 2**: coordinator2@witchcityrope.com / Test123! (SafetyTeam role)

---

## Resources

- [Playwright Documentation](https://playwright.dev)
- [Playwright for .NET](https://playwright.dev/dotnet/)
- [Test Catalog](/docs/standards-processes/testing/TEST_CATALOG.md) - Complete inventory of all tests
- [Test Parity Investigation](/docs/test-baselines/test-parity-investigation-2025-12-01.md) - Container compatibility findings
- [Test Developer Lessons Learned](/docs/lessons-learned/test-developer-lessons-learned.md) - Critical patterns
- [Test Developer Lessons Learned Part 2](/docs/lessons-learned/test-developer-lessons-learned-2.md) - Additional patterns

---

## Deprecated Tools

The following tools are no longer used for E2E testing:
- ❌ **Puppeteer** - All tests migrated to Playwright
- ❌ **Stagehand** - Not needed with Playwright's capabilities

For historical reference, see `/docs/_archive/deprecated-testing-tools.md`
