# E2E Testing Guide for WitchCityRope

## 🚨 IMPORTANT: We Use Playwright, NOT Puppeteer! 🚨

This project has migrated from Puppeteer to Playwright. All E2E tests are now written in Playwright.

## Quick Start

```bash
# Run all E2E tests
npx playwright test

# Run specific test file
npx playwright test tests/playwright/auth/login-basic.spec.ts

# Run tests in headed mode (see browser)
npx playwright test --headed

# Run only auth tests
npx playwright test tests/playwright/auth/

# Run with specific reporter
npx playwright test --reporter=html
```

## Test Location

All Playwright tests are located in:
```
/tests/playwright/
├── admin/          # Admin functionality tests
├── api/            # API endpoint tests
├── auth/           # Authentication tests (login, register, etc.)
├── blazor/         # Blazor-specific tests
├── diagnostics/    # Diagnostic and debugging tests
├── events/         # Event management tests
├── helpers/        # Test helpers and utilities
├── pages/          # Page Object Model files
├── rsvp/           # RSVP functionality tests
├── ui/             # UI interaction tests
├── validation/     # Form validation tests
└── visual-regression/  # Visual snapshot tests
```

## ⚠️ DO NOT USE These Directories ⚠️

- `/tests/e2e/tests/e2e/ToBeDeleted/` - Old Puppeteer tests (DEPRECATED)
- `/tests/e2e/` root level `.js` files - Legacy Puppeteer tests (DEPRECATED)

## Common Test Scenarios

### Testing Login
```typescript
// Use the existing login test
npx playwright test tests/playwright/auth/login-basic.spec.ts
```

### Testing Admin Dashboard Access
```typescript
// Run admin access tests
npx playwright test tests/playwright/admin/admin-access.spec.ts
npx playwright test tests/playwright/admin/admin-dashboard.spec.ts
```

### Using Test Helpers
Tests use helper functions from `/tests/playwright/helpers/`:
- `auth.helpers.ts` - Login/logout functions
- `blazor.helpers.ts` - Blazor-specific utilities
- `test.config.ts` - Shared test configuration

## Page Object Model
Page objects are in `/tests/playwright/pages/`:
- `login.page.ts` - Login page interactions
- `admin-dashboard.page.ts` - Admin dashboard
- etc.

## Running Tests in Docker

The application must be running before tests:
```bash
# Start the application
docker-compose up -d

# Wait for services to be ready
./simple-login-test.sh

# Run tests
npx playwright test
```

## Configuration

Main config file: `playwright.config.ts`
- Base URL: `http://localhost:5651`
- Test directory: `./tests/playwright`
- Browsers: Chromium, Firefox, WebKit

## Visual Regression Tests

Visual snapshots are stored in:
`/tests/playwright/visual-regression/__screenshots__/`

To update snapshots:
```bash
npx playwright test --update-snapshots
```

## Debugging Tests

```bash
# Debug mode
npx playwright test --debug

# UI mode (recommended)
npx playwright test --ui

# Trace on failure
npx playwright test --trace on
```

## Writing New Tests

1. Place tests in appropriate subdirectory under `/tests/playwright/`
2. Use TypeScript (`.spec.ts` extension)
3. Import helpers as needed
4. Follow existing patterns

Example:
```typescript
import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth.helpers';

test('should access admin dashboard', async ({ page }) => {
  await login(page, 'admin@witchcityrope.com', 'Test123!');
  await page.goto('/admin/dashboard');
  await expect(page).toHaveURL(/.*admin\/dashboard/);
});
```

## Test Categories

See `test-categories.json` for organizing tests by category.

## CI/CD

GitHub Actions runs tests using `playwright.config.ci.ts`

---

For more details, see the README files in each test subdirectory.