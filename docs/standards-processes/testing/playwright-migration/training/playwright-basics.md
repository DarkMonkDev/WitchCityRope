# Playwright Basics for Puppeteer Users

## Introduction

Welcome to Playwright! If you're coming from Puppeteer, you'll find many familiar concepts, but with more powerful features and better developer experience. This guide highlights the key differences and improvements.

## Core Differences from Puppeteer

### 1. Built-in Test Runner

Unlike Puppeteer, Playwright includes a full-featured test runner:

```typescript
// Puppeteer - requires external test runner (Jest, Mocha, etc.)
describe('Login Tests', () => {
  let browser;
  let page;
  
  beforeEach(async () => {
    browser = await puppeteer.launch();
    page = await browser.newPage();
  });
  
  afterEach(async () => {
    await browser.close();
  });
  
  it('should login', async () => {
    // test code
  });
});

// Playwright - built-in test runner
import { test, expect } from '@playwright/test';

test('should login', async ({ page }) => {
  // page is automatically created and cleaned up
  // test code
});
```

### 2. Auto-waiting and Better Selectors

Playwright automatically waits for elements to be actionable:

```typescript
// Puppeteer - manual waits required
await page.waitForSelector('.submit-button');
await page.click('.submit-button');

// Playwright - auto-waits before actions
await page.click('.submit-button'); // Automatically waits for element
```

### 3. Enhanced Locators

Playwright's locator API is more powerful and chainable:

```typescript
// Puppeteer
const element = await page.$('.card:nth-child(3) .title');
await element.click();

// Playwright - multiple approaches
await page.locator('.card').nth(2).locator('.title').click();
await page.locator('.card:has-text("Specific Card")').locator('.title').click();
await page.getByRole('heading', { name: 'Card Title' }).click();
```

## Key Playwright Features

### 1. Multiple Browser Contexts

Test isolation with browser contexts:

```typescript
test('parallel test execution', async ({ browser }) => {
  // Each test gets its own isolated context
  const context = await browser.newContext();
  const page = await context.newPage();
  
  // Cookies, localStorage, etc. are isolated
});
```

### 2. Network Interception

More powerful network handling:

```typescript
// Mock API responses
await page.route('**/api/users', route => {
  route.fulfill({
    status: 200,
    body: JSON.stringify({ users: [] })
  });
});

// Modify requests
await page.route('**/*', route => {
  const headers = {
    ...route.request().headers(),
    'X-Custom-Header': 'value'
  };
  route.continue({ headers });
});
```

### 3. Better Assertions

Built-in web-first assertions:

```typescript
// Puppeteer - manual assertions
const text = await page.$eval('.message', el => el.textContent);
expect(text).toBe('Success');

// Playwright - auto-retry assertions
await expect(page.locator('.message')).toHaveText('Success');
await expect(page.locator('.button')).toBeEnabled();
await expect(page.locator('.modal')).toBeVisible();
```

## Migration Quick Reference

| Puppeteer | Playwright |
|-----------|------------|
| `page.$(selector)` | `page.locator(selector)` |
| `page.$$(selector)` | `page.locator(selector).all()` |
| `page.waitForSelector()` | Built into actions or `locator.waitFor()` |
| `page.waitForNavigation()` | `page.waitForURL()` or `page.waitForLoadState()` |
| `page.evaluate()` | `page.evaluate()` (same) |
| `page.screenshot()` | `page.screenshot()` (same) |
| `page.type()` | `page.fill()` or `page.type()` |
| `element.click()` | `locator.click()` |

## Practical Examples from Our Codebase

### Example 1: Login Test

```typescript
// From our actual logout-functionality.spec.ts
test('should logout from member dashboard', async ({ page }) => {
  test.info().annotations.push({ type: 'test-id', description: 'logout-from-dashboard' });

  // Navigate and wait for Blazor
  await page.goto(testConfig.baseUrl + testConfig.urls.memberDashboard);
  await BlazorHelpers.waitForBlazorReady(page);

  // Find logout button with flexible selector
  const logoutButton = page.locator('button:has-text("Logout"), a:has-text("Logout")').first();
  await expect(logoutButton).toBeVisible();
  
  // Click with Blazor-aware helper
  await BlazorHelpers.clickAndWait(page, logoutButton);

  // Wait for navigation
  await page.waitForURL(url => url.includes('/login') || url.endsWith('/'));
});
```

### Example 2: Form Handling

```typescript
// From our event creation tests
test('should successfully create a new event', async ({ page }) => {
  // Use page object pattern
  const eventPage = new EventPage(page);
  
  // Fill form with better selectors
  await eventPage.titleInput.fill('Test Event');
  await eventPage.venueInput.fill('Test Venue');
  
  // Handle date inputs
  await eventPage.startDateInput.fill('2024-12-25');
  
  // Submit and verify
  await eventPage.submitButton.click();
  await expect(page).toHaveURL(/events\/\d+/);
});
```

## Best Practices

### 1. Use Test Fixtures

```typescript
// Define reusable test context
test.beforeEach(async ({ page }) => {
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.loginAsAdmin();
});
```

### 2. Leverage Auto-waiting

```typescript
// Don't do this
await page.waitForTimeout(2000);

// Do this
await page.waitForLoadState('networkidle');
await expect(page.locator('.content')).toBeVisible();
```

### 3. Use Semantic Locators

```typescript
// Prefer role-based selectors
await page.getByRole('button', { name: 'Submit' }).click();
await page.getByLabel('Email').fill('user@example.com');
await page.getByTestId('user-menu').click();

// Over CSS selectors when possible
await page.locator('.btn-submit').click(); // Less preferred
```

### 4. Handle Errors Gracefully

```typescript
test('should handle logout errors gracefully', async ({ page }) => {
  // Mock error response
  await page.route('**/logout', route => {
    route.fulfill({ status: 500 });
  });

  // Test still continues and verifies behavior
  await page.locator('button:has-text("Logout")').click();
  
  // Verify error handling
  await expect(page.locator('.error-message')).toBeVisible();
});
```

## Debugging Tips

### 1. Use Debug Mode

```bash
# Run with headed browser and dev tools
npx playwright test --debug

# Run specific test
npx playwright test logout -g "should logout from admin"
```

### 2. Take Screenshots on Failure

```typescript
test.afterEach(async ({ page }, testInfo) => {
  if (testInfo.status !== 'passed') {
    await page.screenshot({ 
      path: `screenshots/${testInfo.title}-failure.png` 
    });
  }
});
```

### 3. Use Trace Viewer

```bash
# Run with trace
npx playwright test --trace on

# View trace
npx playwright show-trace trace.zip
```

## Common Pitfalls and Solutions

### 1. Element Not Found

```typescript
// Problem: Strict mode throws if multiple elements match
await page.locator('.button').click(); // Error if multiple buttons

// Solution: Be more specific
await page.locator('.button').first().click();
await page.locator('.button:has-text("Submit")').click();
```

### 2. Timing Issues

```typescript
// Problem: Race conditions with dynamic content
await page.click('.dynamic-button'); // Might fail

// Solution: Wait for specific state
await page.waitForLoadState('networkidle');
await expect(page.locator('.dynamic-button')).toBeEnabled();
await page.locator('.dynamic-button').click();
```

### 3. Frame and Iframe Handling

```typescript
// Handle iframes properly
const frame = page.frameLocator('#my-iframe');
await frame.locator('.inside-iframe').click();
```

## Next Steps

- Review the [Blazor Testing Guide](./blazor-testing-guide.md) for app-specific patterns
- Check the [Quick Reference](./quick-reference.md) for common conversions
- Try the [Workshop Exercises](./workshop-exercises.md) for hands-on practice

Remember: Playwright's auto-waiting and better selectors will save you from many timing issues that were common in Puppeteer!