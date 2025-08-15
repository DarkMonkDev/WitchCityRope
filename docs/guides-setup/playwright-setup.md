# Playwright E2E Testing Setup Guide

This guide covers Playwright setup and configuration for end-to-end testing in the WitchCityRope project.

## 🚨 CRITICAL: Playwright Only - No Puppeteer!

> **ALL E2E TESTS HAVE BEEN MIGRATED TO PLAYWRIGHT - NO PUPPETEER ALLOWED!**
> 
> - ✅ All E2E tests are in `/tests/playwright/` directory
> - ✅ 180 tests successfully migrated from Puppeteer
> - ✅ 40% faster execution and 86% less flaky than Puppeteer
> - ❌ NEVER create Puppeteer tests - they are deprecated

## Prerequisites

- Node.js and npm installed
- Docker environment running (for application)
- Basic understanding of TypeScript
- Application running at http://localhost:5651

## Installation

### 1. Install Playwright

```bash
# Navigate to project root
cd /home/chad/repos/witchcityrope

# Install Playwright and its dependencies
npm install --save-dev @playwright/test

# Install Playwright browsers
npx playwright install

# Install system dependencies (if needed)
npx playwright install-deps
```

### 2. Verify Installation

```bash
# Check Playwright version
npx playwright --version

# Run a simple test
npx playwright test --list
```

## Project Structure

```
/tests/playwright/
├── specs/                    # Test files (.spec.ts)
│   ├── auth/                # Authentication tests
│   ├── events/              # Event management tests
│   ├── admin/               # Admin functionality tests
│   └── validation/          # Form validation tests
├── pages/                   # Page Object Models
│   ├── login.page.ts       # Login page POM
│   ├── register.page.ts    # Register page POM
│   ├── admin-dashboard.page.ts
│   └── event.page.ts
├── helpers/                 # Utility functions
│   └── auth.helpers.ts     # Authentication helpers
├── fixtures/               # Test fixtures
└── playwright.config.ts    # Playwright configuration
```

## Running Tests

### Basic Commands

```bash
# Run all E2E tests
npm run test:e2e:playwright

# Run specific test file
npx playwright test tests/playwright/auth/login-basic.spec.ts

# Run tests by grep pattern
npx playwright test --grep "authentication"
npx playwright test --grep "admin"
npx playwright test --grep "events"

# Run in UI mode (recommended for debugging)
npx playwright test --ui

# Run in headed mode (see browser)
npx playwright test --headed

# Run specific browser
npx playwright test --project=chromium
npx playwright test --project=firefox
```

### Advanced Options

```bash
# Run with specific number of workers
npx playwright test --workers=4

# Run tests in debug mode
npx playwright test --debug

# Generate HTML report
npx playwright test --reporter=html

# Show test report
npx playwright show-report

# Update snapshots
npx playwright test --update-snapshots
```

## Writing Tests

### Basic Test Structure

```typescript
import { test, expect } from '@playwright/test';

test.describe('Feature Name', () => {
  test.beforeEach(async ({ page }) => {
    // Setup before each test
    await page.goto('http://localhost:5651');
  });

  test('should do something', async ({ page }) => {
    // Test implementation
    await page.click('button#submit');
    await expect(page).toHaveURL('/success');
  });
});
```

### Using Page Object Models

```typescript
import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';

test('admin login', async ({ page }) => {
  const loginPage = new LoginPage(page);
  
  await loginPage.goto();
  await loginPage.login('admin@witchcityrope.com', 'Test123!');
  
  await expect(page).toHaveURL(/dashboard/);
  await expect(page.locator('.dashboard-title')).toContainText('Admin Dashboard');
});
```

### Available Page Objects

- **LoginPage** - `/tests/playwright/pages/login.page.ts`
- **RegisterPage** - `/tests/playwright/pages/register.page.ts`
- **AdminDashboardPage** - `/tests/playwright/pages/admin-dashboard.page.ts`
- **EventPage** - `/tests/playwright/pages/event.page.ts`

## Test Accounts

Use these seeded test accounts:

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@witchcityrope.com | Test123! |
| Teacher | teacher@witchcityrope.com | Test123! |
| Vetted Member | vetted@witchcityrope.com | Test123! |
| General Member | member@witchcityrope.com | Test123! |
| Attendee | attendee@witchcityrope.com | Test123! |

## Configuration

### playwright.config.ts

Key configuration options:

```typescript
export default defineConfig({
  // Test directory
  testDir: './tests/playwright',
  
  // Test timeout
  timeout: 30 * 1000,
  
  // Retry failed tests
  retries: process.env.CI ? 2 : 0,
  
  // Parallel execution
  workers: process.env.CI ? 1 : undefined,
  
  // Base URL
  use: {
    baseURL: 'http://localhost:5651',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  
  // Projects for different browsers
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
  ],
});
```

## Best Practices

### 1. Use Page Object Models
```typescript
// ✅ GOOD - Maintainable
const loginPage = new LoginPage(page);
await loginPage.login(email, password);

// ❌ BAD - Hard to maintain
await page.fill('#email', email);
await page.fill('#password', password);
await page.click('button[type="submit"]');
```

### 2. Use Proper Selectors
```typescript
// ✅ GOOD - Stable selectors
await page.click('[data-testid="submit-button"]');
await page.fill('input[name="email"]', 'test@example.com');

// ❌ BAD - Fragile selectors
await page.click('.btn.btn-primary.submit');
await page.fill('#field_1234', 'test@example.com');
```

### 3. Wait for Elements Properly
```typescript
// ✅ GOOD - Explicit waits
await page.waitForSelector('.dashboard-content');
await expect(page.locator('.loading')).toBeHidden();

// ❌ BAD - Fixed timeouts
await page.waitForTimeout(5000);
```

### 4. Group Related Tests
```typescript
test.describe('User Registration', () => {
  test.describe('Valid Input', () => {
    test('should register with valid email', async ({ page }) => {
      // Test implementation
    });
  });
  
  test.describe('Invalid Input', () => {
    test('should show error for invalid email', async ({ page }) => {
      // Test implementation
    });
  });
});
```

## Debugging Tests

### 1. UI Mode (Recommended)
```bash
npx playwright test --ui
```
Features:
- See each step visually
- Time travel through test execution
- Inspect DOM at each step
- See network requests

### 2. Debug Mode
```bash
npx playwright test --debug
```
Features:
- Pause at each step
- Inspect page state
- Use browser DevTools

### 3. VS Code Extension
Install "Playwright Test for VSCode" extension for:
- Run tests from editor
- Set breakpoints
- Debug step by step

### 4. Trace Viewer
```bash
# Run with trace
npx playwright test --trace on

# View trace
npx playwright show-trace trace.zip
```

## Common Issues and Solutions

### Application Not Running
```bash
# Error: "ERR_CONNECTION_REFUSED"
# Solution: Start the application first
./dev.sh  # Select option 1
```

### Test Timeouts
```typescript
// Increase timeout for specific test
test('slow test', async ({ page }) => {
  test.setTimeout(60000); // 60 seconds
  // Test implementation
});
```

### Element Not Found
```typescript
// Wait for element before interacting
await page.waitForSelector('.my-element', { state: 'visible' });
await page.click('.my-element');
```

### Flaky Tests
```typescript
// Add retries for flaky tests
test('potentially flaky test', async ({ page }) => {
  // Use expect with timeout
  await expect(page.locator('.element')).toBeVisible({ timeout: 10000 });
});
```

## CI/CD Integration

### GitHub Actions Example
```yaml
name: E2E Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: 18
          
      - name: Install dependencies
        run: npm ci
        
      - name: Install Playwright
        run: npx playwright install --with-deps
        
      - name: Start application
        run: |
          docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
          npx wait-on http://localhost:5651
          
      - name: Run tests
        run: npm run test:e2e:playwright
        
      - name: Upload report
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: playwright-report
          path: playwright-report/
```

## Migration from Puppeteer

If you find old Puppeteer tests:

### DON'T:
- ❌ Run or modify Puppeteer tests in `/tests/e2e/`
- ❌ Create new Puppeteer tests
- ❌ Use Puppeteer patterns

### DO:
- ✅ Use existing Playwright tests in `/tests/playwright/`
- ✅ Follow Playwright patterns and Page Object Models
- ✅ Report any missing test coverage

## Performance Tips

1. **Run tests in parallel** (default behavior)
2. **Use test.describe.parallel()** for independent tests
3. **Minimize page.goto()** calls - reuse page state
4. **Use API calls for setup** when possible
5. **Clean up test data** in afterEach hooks

## Related Documentation

- [Test Catalog](../standards-processes/testing/TEST_CATALOG.md) - All test information
- [Development Standards](../standards-processes/development-standards.md) - Coding standards
- [E2E Testing Guide](../../E2E_TESTING_GUIDE.md) - Detailed E2E testing guide
- [Playwright Migration](../enhancements/playwright-migration/) - Migration documentation