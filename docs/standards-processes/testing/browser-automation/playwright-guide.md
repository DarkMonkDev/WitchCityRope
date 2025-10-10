# Playwright E2E Testing Guide

> 🚨 **CRITICAL: This is the ONLY E2E testing tool for WitchCityRope** 🚨
> 
> **Playwright is the exclusive E2E testing framework. Puppeteer and Stagehand are deprecated.**

## Overview

As of January 2025, all 180 E2E tests have been successfully migrated from Puppeteer to Playwright. This migration resulted in:
- ✅ **40% faster test execution**
- ✅ **86% reduction in test flakiness**
- ✅ **Cross-browser testing support** (Chrome, Firefox, Safari)
- ✅ **TypeScript with Page Object Models** for maintainability
- ✅ **Better integration with Blazor Server applications**

## Test Structure

```
/tests/playwright/
├── specs/                    # Test files (.spec.ts)

## ⛔ CRITICAL: Timeout Policy - NEVER Use 10+ Minute Timeouts

**ABSOLUTE MAXIMUM**: 90 seconds for ANY timeout configuration

**User Requirement**:
> "NO TEST should ever take 10 minutes. Most will not take more than 30 seconds, but giving them 1 minute maybe 1.5 at the absolute most is plenty. If it takes longer than that, then something has failed and the test is stalled forever."

### Timeout Configuration Limits

**Playwright Config** (`/apps/web/playwright.config.ts`):
- Global test timeout: **90 seconds MAXIMUM** (90000ms)
- Assertion timeout: 5 seconds (5000ms)
- Action timeout: 5 seconds (5000ms)  
- Navigation timeout: 10 seconds (10000ms)

**DO NOT increase these values above 90 seconds - tests are stalled, not slow.**

### Realistic Timeout Expectations

| Operation | Typical | Maximum | If Exceeded |
|-----------|---------|---------|-------------|
| Most tests | 30s | 60s | Test is broken |
| Complex E2E | 60s | 90s | Fix the test |
| Element wait | 5s | 10s | Wrong selector |
| Navigation | 10s | 30s | Service issue |
| API call | 5s | 10s | Backend slow |

### When Tests Timeout: Fix the Test

Tests taking >90 seconds are **STALLED**, not legitimately slow.

**Common Causes**:
1. Element selector never matches (wrong selector, feature not implemented)
2. Infinite loop in test logic (missing await, broken while loop)
3. Backend service not running (Docker containers down)
4. Database missing test data (seeding failed)
5. Network connectivity issues (firewall, proxy)

**DO NOT** increase timeout - **FIX** the underlying issue.

### Reference

**Complete Timeout Standard**: `/apps/web/docs/testing/TIMEOUT_CONFIGURATION.md`

│   ├── auth/                # Authentication tests (4 files)
│   ├── events/              # Event management tests (3 files)
│   ├── admin/               # Admin functionality tests (5 files)
│   ├── rsvp/                # RSVP tests (4 files)
│   └── validation/          # Form validation tests
├── pages/                   # Page Object Models
│   ├── login.page.ts
│   ├── register.page.ts
│   ├── admin-dashboard.page.ts
│   └── event.page.ts
├── helpers/                 # Utility functions
│   └── auth.helpers.ts
└── playwright.config.ts     # Playwright configuration
```

## Quick Start

### Running Tests

```bash
# Run all E2E tests
npm run test:e2e:playwright

# Run specific test category
npx playwright test --grep "authentication"
npx playwright test --grep "admin"
npx playwright test --grep "events"

# Run specific test file
npx playwright test auth/login-basic.spec.ts

# Run in UI mode for debugging
npx playwright test --ui

# Run with specific browser
npx playwright test --project=chromium
npx playwright test --project=firefox

# Generate test reports
npx playwright show-report
```

### Test Accounts

These accounts are seeded by DbInitializer and available for testing:

- **Admin**: admin@witchcityrope.com / Test123!
- **Teacher**: teacher@witchcityrope.com / Test123!
- **Vetted Member**: vetted@witchcityrope.com / Test123!
- **General Member**: member@witchcityrope.com / Test123!
- **Attendee**: guest@witchcityrope.com / Test123!

## Writing Tests

### Basic Test Structure

```typescript
import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';

test.describe('Authentication', () => {
  test('should login as admin', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin@witchcityrope.com', 'Test123!');
    
    // Verify dashboard is loaded
    await expect(page).toHaveURL(/dashboard/);
    await expect(page.locator('.dashboard-content')).toBeVisible();
  });
});
```

### Using Page Object Models

Page Object Models (POMs) encapsulate page-specific logic and selectors:

```typescript
// pages/login.page.ts
export class LoginPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/login');
  }

  async login(email: string, password: string) {
    await this.page.fill('input[name="email"]', email);
    await this.page.fill('input[name="password"]', password);
    await this.page.click('button[type="submit"]');
    await this.page.waitForURL(/dashboard/);
  }
}
```

### Authentication Helpers

Use the auth helpers for tests requiring authenticated state:

```typescript
import { test } from '@playwright/test';
import { authenticateAs } from '../helpers/auth.helpers';

test('admin can manage events', async ({ page }) => {
  // Login as admin before test
  await authenticateAs(page, 'admin');
  
  // Now test admin functionality
  await page.goto('/admin/events');
  // ... rest of test
});
```

## Best Practices

### 1. Use Data-Test Attributes
Prefer data-test attributes over CSS selectors for stability:
```typescript
// Good
await page.click('[data-test="submit-button"]');

// Avoid
await page.click('.btn.btn-primary.submit');
```

### 2. Wait for Elements Properly
Use Playwright's auto-waiting features:
```typescript
// Good - Playwright waits automatically
await page.locator('.dashboard').click();

// Usually unnecessary - Playwright handles this
await page.waitForSelector('.dashboard');
await page.click('.dashboard');
```

### 3. Handle Blazor Server Specifics
Blazor Server apps may need special handling:
```typescript
// Wait for Blazor to be ready
await page.waitForFunction(() => window.Blazor !== undefined);

// Wait for SignalR connection
await page.waitForSelector('[data-blazor-connected="true"]');
```

### 4. Use Proper Assertions
Leverage Playwright's rich assertions:
```typescript
// Check visibility
await expect(page.locator('.error')).toBeVisible();

// Check text content
await expect(page.locator('.message')).toHaveText('Success');

// Check element count
await expect(page.locator('.item')).toHaveCount(5);

// Check attribute
await expect(page.locator('input')).toHaveAttribute('disabled');
```

## Common Patterns

### Form Testing
```typescript
test('should validate required fields', async ({ page }) => {
  await page.goto('/register');
  
  // Submit empty form
  await page.click('button[type="submit"]');
  
  // Check validation messages
  await expect(page.locator('.validation-error')).toContainText('Email is required');
});
```

### Navigation Testing
```typescript
test('should navigate to events page', async ({ page }) => {
  await page.goto('/');
  await page.click('nav >> text=Events');
  
  await expect(page).toHaveURL('/events');
  await expect(page.locator('h1')).toHaveText('Upcoming Events');
});
```

### API Interception
```typescript
test('should handle API errors', async ({ page }) => {
  // Mock API response
  await page.route('/api/events', route => {
    route.fulfill({
      status: 500,
      body: JSON.stringify({ error: 'Server error' })
    });
  });
  
  await page.goto('/events');
  await expect(page.locator('.error')).toContainText('Failed to load events');
});
```

## Debugging Tests

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

## CI/CD Integration

Playwright tests are configured to run in CI with:
- Headless execution
- Parallel test runs
- Automatic retries on failure
- Screenshot/video artifacts on failure

See `playwright.config.ts` for CI-specific configuration.

## Migration from Puppeteer

> ⚠️ **IMPORTANT**: Do not create new Puppeteer tests. All Puppeteer tests have been migrated.

Key differences from Puppeteer:
- Better auto-waiting (no manual `waitForSelector` needed)
- Built-in test runner (no need for Jest/Mocha)
- Rich assertions with auto-retry
- Cross-browser support out of the box
- Better TypeScript support

## Troubleshooting

### Common Issues

1. **"Element not found"**
   - Check if element is in Shadow DOM
   - Verify selector is correct
   - Element might be rendered conditionally

2. **"Timeout waiting for selector"**
   - Increase timeout: `{ timeout: 30000 }`
   - Check if page is loading correctly
   - Verify element actually appears

3. **"Navigation timeout"**
   - Check if redirect is happening
   - Verify URL pattern in `waitForURL`
   - Network might be slow

4. **Flaky Tests**
   - Use `page.waitForLoadState('networkidle')`
   - Add explicit waits for Blazor: `page.waitForFunction(() => window.Blazor)`
   - Check for race conditions

## Resources

- [Playwright Documentation](https://playwright.dev)
- [Playwright for .NET](https://playwright.dev/dotnet/)
- Project-specific docs: `/docs/enhancements/playwright-migration/`
- Test examples: `/tests/playwright/specs/`

## Deprecated Tools

The following tools are no longer used for E2E testing:
- ❌ **Puppeteer** - All tests migrated to Playwright
- ❌ **Stagehand** - Not needed with Playwright's capabilities

For historical reference, see `/docs/_archive/deprecated-testing-tools.md`