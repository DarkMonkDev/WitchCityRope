# Blazor Server Architecture E2E Tests

This directory contains E2E tests specifically designed for the Blazor Server architecture migration.

## Context

The WitchCityRope website was converted from Razor Pages to Blazor Server architecture. This change required new E2E testing approaches because:

- Old tests were expecting Razor Pages behavior
- The Blazor E2E helper was timing out (likely outdated for the new architecture)
- Blazor Server components require different wait strategies

## New Tests

### admin-users-blazor.spec.ts

**Purpose**: Test admin user management functionality with Blazor Server components

**Features**:
- Works with current Blazor Server architecture (not old Razor Pages)
- Avoids broken Blazor E2E helper 
- Uses simple Playwright waits like `page.waitForLoadState('networkidle')`
- Direct navigation and login (bypasses global setup)
- Comprehensive debugging with screenshots
- Graceful handling of component loading states

**Test Cases**:
1. `should login as admin and access user management with Blazor Server` - Main functionality
2. `should handle page loading states gracefully` - Loading state verification  
3. `should capture page structure for debugging` - Debug helper

## Running the Tests

### From Project Root
```bash
# Run all admin tests
npm run test:admin

# Run specific Blazor Server test
npx playwright test tests/playwright/admin/admin-users-blazor.spec.ts

# Run with UI for debugging
npx playwright test tests/playwright/admin/admin-users-blazor.spec.ts --ui

# Run with headed browser for visual debugging
npx playwright test tests/playwright/admin/admin-users-blazor.spec.ts --headed
```

### From tests/playwright Directory
```bash
cd tests/playwright

# Run the specific test
npx playwright test admin/admin-users-blazor.spec.ts

# Debug mode
npx playwright test admin/admin-users-blazor.spec.ts --debug

# Generate HTML report
npx playwright test admin/admin-users-blazor.spec.ts --reporter=html
```

## Test Pattern for Blazor Server

### Simple Wait Strategy
```typescript
// Navigate to page
await page.goto(testConfig.urls.adminUsers);

// Wait for page to settle
await page.waitForLoadState('networkidle');

// Small delay for Blazor components to render
await page.waitForTimeout(2000);

// Verify elements are present
await expect(pageHeading).toBeVisible({ timeout: 15000 });
```

### Flexible Selectors
```typescript
// Use flexible selectors that work with Blazor component output
const pageHeading = page.locator('h1, h2, h3, [role="heading"]').filter({ 
  hasText: /User Management|Users|Admin|Management/i 
}).first();
```

### Debug-Friendly Approach
```typescript
// Always take screenshots for debugging
await page.screenshot({
  path: 'test-results/screenshots/admin-users-blazor-server.png',
  fullPage: true
});

// Log important information
console.log('✅ Page heading found:', headingText);
console.log('Current URL:', page.url());
```

## Migration Notes

### What Changed
- **Before**: Tests expected Razor Pages with immediate DOM elements
- **After**: Tests work with Blazor Server components that may load progressively

### Key Differences
1. **Timing**: Blazor components may take longer to render
2. **Selectors**: Component output may be different from static HTML
3. **Interactions**: Component state changes may require different approaches
4. **Loading**: Progressive enhancement means elements appear over time

### Best Practices
1. Use `page.waitForLoadState('networkidle')` for initial page loading
2. Add reasonable delays for component rendering (1-3 seconds)
3. Use flexible, content-based selectors rather than strict CSS classes
4. Always take screenshots for debugging Blazor Server rendering
5. Test both successful cases and loading state handling

## Troubleshooting

### Common Issues
- **Element not found**: Blazor components may not have rendered yet, add delays
- **Timeout errors**: Increase timeouts or use more specific wait conditions
- **Flaky tests**: Add explicit waits for component state changes

### Debug Steps
1. Run test with `--headed` to see browser visually
2. Check screenshots in `test-results/screenshots/`
3. Look at console logs for Blazor component errors
4. Verify page URL and basic structure before testing interactions

## Related Documentation
- [Testing Guide](/docs/standards-processes/testing/TESTING_GUIDE.md)
- [Playwright Guide](/docs/standards-processes/testing/browser-automation/playwright-guide.md)  
- [Test Writers Lessons](/docs/lessons-learned/test-writers.md)
- [Test Catalog](/docs/standards-processes/testing/TEST_CATALOG.md)