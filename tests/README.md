# WitchCityRope Test Directory Structure

## 🚨 IMPORTANT: We Use Playwright for E2E Testing 🚨

All E2E tests are written in **Playwright** (TypeScript) and located in `/tests/playwright/`.

## Directory Structure

```
tests/
├── README.md (this file)
├── playwright/          ← ALL E2E TESTS ARE HERE!
│   ├── admin/          # Admin functionality tests
│   ├── api/            # API endpoint tests  
│   ├── auth/           # Authentication tests (login, register)
│   ├── blazor/         # Blazor-specific tests
│   ├── diagnostics/    # Diagnostic tests
│   ├── events/         # Event management tests
│   ├── helpers/        # Test utilities and helpers
│   ├── pages/          # Page Object Models
│   ├── rsvp/           # RSVP functionality tests
│   ├── ui/             # UI interaction tests
│   ├── validation/     # Form validation tests
│   └── visual-regression/  # Visual snapshot tests
├── e2e/                ← DEPRECATED Puppeteer tests (DO NOT USE!)
└── WitchCityRope.*.Tests/  # .NET unit/integration tests

```

## Quick Start - Running E2E Tests

```bash
# Run all tests
npm test

# Run tests in UI mode (RECOMMENDED for debugging)
npm run test:ui

# Run specific test file
npx playwright test tests/playwright/auth/login-basic.spec.ts

# Run tests by category
npm run test:auth      # Authentication tests
npm run test:admin     # Admin tests
npm run test:events    # Event tests
```

## Test Categories

- **Unit Tests**: Located in `WitchCityRope.*.Tests/` directories
- **E2E Tests**: Located in `/tests/playwright/` (Playwright)
- **Deprecated Tests**: Located in `/tests/e2e/` (DO NOT USE)

## Writing New Tests

### E2E Tests (Playwright)
1. Create test in appropriate subdirectory under `/tests/playwright/`
2. Use TypeScript (`.spec.ts` extension)
3. Import helpers from `/tests/playwright/helpers/`
4. Follow existing patterns

Example:
```typescript
import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth.helpers';

test('admin dashboard access', async ({ page }) => {
  await login(page, 'admin@witchcityrope.com', 'Test123!');
  await page.goto('/admin/dashboard');
  await expect(page).toHaveURL(/.*admin\/dashboard/);
});
```

### Unit Tests (.NET)
Follow standard .NET testing practices using xUnit.

## Common Mistakes to Avoid

1. ❌ **DO NOT** create tests in `/tests/e2e/` - these are deprecated Puppeteer tests
2. ❌ **DO NOT** use JavaScript for E2E tests - use TypeScript
3. ❌ **DO NOT** write tests outside the organized structure
4. ✅ **DO** use the Page Object Model in `/tests/playwright/pages/`
5. ✅ **DO** use test helpers from `/tests/playwright/helpers/`

## Documentation

- **Complete E2E Testing Guide**: See `/E2E_TESTING_GUIDE.md` in project root
- **Playwright Migration**: See `/docs/enhancements/playwright-migration/`
- **Test Categories**: See `/tests/playwright/test-categories.json`

## CI/CD

Tests run automatically on GitHub Actions using `playwright.config.ci.ts`.

---

**Remember: All new E2E tests must use Playwright in `/tests/playwright/`!**