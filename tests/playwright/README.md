# Playwright Tests for WitchCityRope

This directory contains the Playwright test suite for the WitchCityRope application.

## Directory Structure

### Core Feature Tests
- `admin/` - Admin-specific functionality tests
- `api/` - API endpoint tests
- `auth/` - Authentication and authorization tests
- `events/` - Event management tests
- `rsvp/` - RSVP functionality tests
- `validation/` - Form validation tests

### UI & Infrastructure Tests
- `ui/` - User interface component tests (dropdowns, navigation, buttons)
- `blazor/` - Blazor-specific functionality and state management tests
- `infrastructure/` - CSS loading, error handling, page status, layout tests

### Support Files
- `diagnostics/` - Diagnostic and debugging tests
- `helpers/` - Shared test utilities and helpers
- `pages/` - Page Object Model files
- `visual-regression/` - Visual regression tests

## Running Tests

```bash
# Run all tests
npx playwright test

# Run specific directory
npx playwright test ui/
npx playwright test blazor/
npx playwright test infrastructure/

# Run specific test file
npx playwright test ui/user-dropdown.spec.ts

# Run tests in headed mode
npx playwright test --headed

# Run tests with UI mode for debugging
npx playwright test --ui

# Run with trace for debugging failures
npx playwright test --trace on
```

## Test Categories

### UI Tests (`ui/`)
- User dropdown menu functionality
- Navigation between pages
- Button interactivity and click handling

### Blazor Tests (`blazor/`)
- Blazor framework initialization
- Component interactivity and event bindings
- Render mode detection (SSR vs Interactive)
- Circuit connection monitoring

### Infrastructure Tests (`infrastructure/`)
- CSS file loading and styling
- Error timing and monitoring
- Page load status and performance
- Layout system consistency
- Theme and styling validation

## Writing Tests

### Page Object Model
Use the page objects in the `pages/` directory for consistent element selection:

```typescript
import { LoginPage } from '../pages/login.page';

test('login test', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.login('user@example.com', 'password');
});
```

### Helpers
Use the helper functions in `helpers/` for common operations:

```typescript
import { login } from '../helpers/auth.helpers';

test.beforeEach(async ({ page }) => {
    await login(page, 'admin@witchcityrope.com', 'Test123!');
});
```

### Best Practices

1. **Use descriptive test names**: Clearly describe what the test does
2. **Group related tests**: Use `test.describe()` blocks
3. **Clean up after tests**: Reset state when necessary
4. **Use Page Objects**: For maintainable selectors
5. **Add comments**: Explain complex test logic
6. **Take screenshots**: For debugging failures

## Debugging

### Visual Debugging
```bash
# Use UI mode for step-by-step debugging
npx playwright test --ui

# Generate trace on failure
npx playwright test --trace on
```

### Console Output
Tests include console logging for debugging:
- API calls are tracked in button interactivity tests
- Blazor state is logged in Blazor tests
- CSS loading is logged in infrastructure tests

### Screenshots
Tests save screenshots to `test-results/` for debugging:
- User dropdown states
- Error conditions
- Styling verification

## CI/CD Integration

Tests can be run in CI pipelines:

```yaml
# Example GitHub Actions
- name: Run Playwright tests
  run: npx playwright test
- uses: actions/upload-artifact@v3
  if: always()
  with:
    name: playwright-report
    path: playwright-report/
```

## Configuration

Tests are configured in `playwright.config.ts` in the root directory. Key settings:
- Base URL
- Test timeout
- Browser configurations
- Screenshot and video settings