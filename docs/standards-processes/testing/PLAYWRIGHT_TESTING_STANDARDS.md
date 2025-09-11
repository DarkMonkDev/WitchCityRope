# Playwright Testing Standards for WitchCityRope React

## Overview
This document establishes testing standards and conventions for Playwright E2E tests in the WitchCityRope React application.

## Test File Organization

### Directory Structure
```
/apps/web/tests/playwright/
├── helpers/                    # Test helper utilities
│   ├── auth.helpers.ts        # Authentication utilities
│   ├── form.helpers.ts        # Form interaction utilities
│   └── wait.helpers.ts        # Wait strategy utilities
├── fixtures/                  # Test data and fixtures
├── pages/                     # Page Object Models (if needed)
└── *.spec.ts                  # Test specification files
```

### Test File Naming
- Use descriptive names: `auth-comprehensive.spec.ts`
- Include functionality area: `events-comprehensive.spec.ts`
- Use `-fixed` suffix for corrected tests: `auth-fixed.spec.ts`
- Use `-comprehensive` for complete test suites

## Data-TestId Conventions

### Mandatory Attributes
All interactive elements MUST have data-testid attributes:

```tsx
// ✅ CORRECT - Required patterns
<input data-testid="email-input" />
<button data-testid="login-button" />
<div data-testid="error-message" />
<form data-testid="login-form" />
```

### Naming Convention
- Format: `{element-purpose}-{element-type}`
- Use kebab-case: `submit-button`, `error-message`
- Be descriptive: `event-card`, `user-profile`

```typescript
// ✅ CORRECT examples
'[data-testid="email-input"]'
'[data-testid="password-input"]'
'[data-testid="login-button"]'
'[data-testid="error-message"]'
'[data-testid="event-card"]'
'[data-testid="user-profile"]'

// ❌ WRONG examples
'[data-testid="input1"]'         // Not descriptive
'[data-testid="emailInput"]'     // camelCase instead of kebab-case
'[data-testid="btn"]'           // Too abbreviated
```

## Selector Strategy (Priority Order)

### 1. Data-TestId First (Primary)
```typescript
// ✅ PREFERRED - Most reliable
await page.locator('[data-testid="login-button"]').click();
```

### 2. Semantic Selectors (Secondary)
```typescript
// ✅ ACCEPTABLE - When data-testid unavailable
await page.locator('button[type="submit"]').click();
await page.locator('input[type="email"]').fill('test@example.com');
```

### 3. Text-Based Selectors (Fallback)
```typescript
// ✅ FALLBACK - For specific text content
await page.locator('button:has-text("Sign In")').click();
await page.locator('text=Welcome Back').click();
```

### 4. CSS Classes (Avoid)
```typescript
// ❌ AVOID - Fragile and implementation-dependent
await page.locator('.btn-primary').click(); // DON'T DO THIS
```

## Test Structure Standards

### Test Organization
```typescript
test.describe('Feature Area - Context', () => {
  test.beforeEach(async ({ page }) => {
    // Setup code
    await AuthHelpers.clearAuth(page);
  });

  test('should do specific action with expected result', async ({ page }) => {
    // Arrange
    await page.goto('/target-page');
    await WaitHelpers.waitForPageLoad(page);

    // Act  
    await page.locator('[data-testid="action-button"]').click();

    // Assert
    await expect(page.locator('[data-testid="result"]')).toBeVisible();
    
    console.log('✅ Test completed successfully');
  });
});
```

### Test Naming Convention
Format: `should [action] [expected result]`

```typescript
// ✅ CORRECT examples
'should display login page with correct elements'
'should successfully login with admin credentials'
'should show error for invalid credentials'
'should redirect unauthenticated users to login'

// ❌ WRONG examples
'login test'                    // Too vague
'test authentication'          // Not action-oriented
'check if login works'         // Not assertive
```

## Required Test Helpers Usage

### Authentication Helper
```typescript
// ✅ REQUIRED - Use AuthHelpers for all auth operations
await AuthHelpers.loginAs(page, 'admin');
await AuthHelpers.logout(page);
await AuthHelpers.clearAuth(page);

// ❌ WRONG - Manual auth steps in tests
await page.fill('[data-testid="email-input"]', 'admin@...');
await page.fill('[data-testid="password-input"]', 'Test123!');
```

### Form Helper
```typescript
// ✅ REQUIRED - Use FormHelpers for form operations
await FormHelpers.fillFormData(page, {
  email: 'test@example.com',
  password: 'testpass'
});

await FormHelpers.waitForFormError(page, 'login-error');
```

### Wait Helper
```typescript
// ✅ REQUIRED - Use WaitHelpers for timing
await WaitHelpers.waitForPageLoad(page);
await WaitHelpers.waitForApiResponse(page, '/api/auth/login');
await WaitHelpers.waitForNavigation(page, '/dashboard');
```

## Error Handling Standards

### Expected Text Matching
```typescript
// ✅ CORRECT - Updated for React implementation
await expect(page.locator('h1')).toContainText('Welcome Back'); // NOT "Login"
await page.locator('button:has-text("Sign In")').click();       // NOT "Login"
```

### Error Message Testing
```typescript
// ✅ CORRECT - Test error display
await expect(page.locator('[data-testid="login-error"]')).toBeVisible();
await expect(page.locator('[data-testid="login-error"]')).toContainText(/invalid|failed|error/i);

// Use regex for flexible matching
await expect(errorElement).toContainText(/login failed|authentication error|invalid credentials/i);
```

### Network Error Simulation
```typescript
// ✅ CORRECT - Test API error handling
await page.route('**/api/auth/login', route => {
  route.fulfill({
    status: 401,
    contentType: 'application/json', 
    body: JSON.stringify({ error: 'Invalid credentials' })
  });
});
```

## Performance Standards

### Timing Expectations
```typescript
// ✅ REQUIRED - Set realistic timeouts
await page.waitForURL('/dashboard', { timeout: 10000 });      // 10s for navigation
await expect(element).toBeVisible({ timeout: 5000 });         // 5s for element visibility
await WaitHelpers.waitForApiResponse(page, '/api/events');    // Helper manages timeout
```

### Performance Testing
```typescript
test('should complete action within performance budget', async ({ page }) => {
  const startTime = Date.now();
  
  // Perform action
  await AuthHelpers.loginAs(page, 'admin');
  
  const duration = Date.now() - startTime;
  
  // Assert performance target
  expect(duration).toBeLessThan(3000); // 3 second target
  
  console.log(`✅ Action completed in ${duration}ms`);
});
```

## Responsive Testing Standards

### Required Viewports
```typescript
const viewports = [
  { name: 'Mobile', width: 375, height: 667 },
  { name: 'Tablet', width: 768, height: 1024 },
  { name: 'Desktop', width: 1920, height: 1080 }
];

viewports.forEach(viewport => {
  test(`should work on ${viewport.name}`, async ({ page }) => {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    
    // Test functionality
    await AuthHelpers.loginAs(page, 'admin');
    
    // Verify no horizontal scroll
    const body = await page.locator('body').boundingBox();
    expect(body?.width).toBeLessThanOrEqual(viewport.width);
    
    // Take screenshot for manual review
    await page.screenshot({
      path: `test-results/feature-${viewport.name.toLowerCase()}.png`,
      fullPage: true
    });
  });
});
```

## Console and Network Monitoring

### Required Monitoring Setup
```typescript
test.beforeEach(async ({ page }) => {
  // ✅ REQUIRED - Monitor console errors
  page.on('console', msg => {
    if (msg.type() === 'error') {
      console.log(`❌ Console Error: ${msg.text()}`);
    }
  });
  
  // ✅ REQUIRED - Monitor API responses
  page.on('response', response => {
    if (response.url().includes('/api/')) {
      console.log(`📡 API ${response.status()}: ${response.url()}`);
    }
  });
  
  // ✅ REQUIRED - Monitor failed requests
  page.on('requestfailed', request => {
    console.log(`❌ Request Failed: ${request.url()}`);
  });
});
```

## Test Data Management

### Use Seeded Test Accounts
```typescript
// ✅ CORRECT - Use AuthHelpers.accounts
const testAccounts = AuthHelpers.accounts;
await AuthHelpers.loginAs(page, 'admin');    // admin@witchcityrope.com
await AuthHelpers.loginAs(page, 'member');   // member@witchcityrope.com
```

### Generate Unique Data When Needed
```typescript
// ✅ CORRECT - For registration tests
const uniqueCredentials = {
  email: `test-${Date.now()}@example.com`,
  sceneName: `TestUser${Date.now()}`,
  password: 'StrongPass123!'
};
```

## Accessibility Testing

### Required Accessibility Checks
```typescript
// ✅ REQUIRED - Verify form accessibility
await FormHelpers.verifyFieldAccessibility(page, 'email', 'Email Address');

// ✅ REQUIRED - Check ARIA attributes
await expect(page.locator('[data-testid="login-error"]')).toHaveAttribute('role', 'alert');

// ✅ REQUIRED - Keyboard navigation
await page.keyboard.press('Tab');
await expect(page.locator(':focus')).toHaveAttribute('data-testid', 'email-input');
```

## Test Isolation Requirements

### State Cleanup
```typescript
test.beforeEach(async ({ page }) => {
  // ✅ MANDATORY - Clear auth state before each test
  await AuthHelpers.clearAuth(page);
  
  // Clear any cached data
  await page.evaluate(() => {
    localStorage.clear();
    sessionStorage.clear();
  });
});
```

### Database State
- Integration tests should use unique data to avoid conflicts
- Use GUIDs or timestamps for test data uniqueness
- Don't rely on specific database state between tests

## Screenshot and Debug Standards

### Debugging Screenshots
```typescript
// ✅ REQUIRED - Screenshots for failed tests
test('complex user flow', async ({ page }) => {
  try {
    // Test logic here
    await AuthHelpers.loginAs(page, 'admin');
    
  } catch (error) {
    // Take debug screenshot on failure
    await page.screenshot({
      path: `test-results/debug-${Date.now()}.png`,
      fullPage: true
    });
    throw error; // Re-throw to fail test
  }
});

// ✅ REQUIRED - Screenshots for visual verification
await page.screenshot({
  path: `test-results/login-page-${Date.now()}.png`
});
```

## API Integration Testing

### Mock vs Real API
```typescript
// ✅ PREFERRED - Test against real API when possible
await WaitHelpers.waitForApiResponse(page, '/api/auth/login');

// ✅ ACCEPTABLE - Mock for error scenarios
await page.route('**/api/events', route => {
  route.fulfill({
    status: 500,
    body: JSON.stringify({ error: 'Server Error' })
  });
});
```

## Test Maintenance

### Regular Review Checklist
- [ ] All tests use data-testid selectors as primary
- [ ] No hardcoded waits (use WaitHelpers)
- [ ] All authentication uses AuthHelpers
- [ ] Error scenarios are tested
- [ ] Console errors are monitored
- [ ] Screenshots captured for debugging
- [ ] Performance targets are validated
- [ ] Accessibility is verified

### Updating Tests for UI Changes
1. **Text Changes**: Update expect statements to match new UI text
2. **Selector Changes**: Update data-testid attributes in components first, then tests
3. **Flow Changes**: Update helper methods, then tests using them
4. **New Features**: Add data-testid attributes to new components immediately

### Test Health Monitoring
- Run tests locally before committing
- Monitor test results in CI/CD pipeline  
- Address flaky tests immediately
- Update TEST_CATALOG.md when adding new tests

## Common Patterns Library

### Login Flow
```typescript
// Standard login pattern
await AuthHelpers.loginAs(page, 'admin');
await expect(page).toHaveURL('/dashboard');
```

### Form Validation Testing
```typescript
// Standard validation pattern
await FormHelpers.testFormValidation(
  page,
  { email: 'invalid-email', password: 'valid' },
  { email: 'Invalid email format' },
  'submit-button'
);
```

### Navigation Testing
```typescript
// Standard navigation pattern
await page.locator('[data-testid="nav-link"]').click();
await WaitHelpers.waitForNavigation(page, '/expected-url');
await expect(page.locator('h1')).toContainText('Expected Title');
```

### API Error Testing
```typescript
// Standard API error pattern
await page.route('**/api/endpoint', route => route.fulfill({ status: 500 }));
await page.locator('[data-testid="action-button"]').click();
await WaitHelpers.waitForComponent(page, 'error-message');
```

## Conclusion

These standards ensure:
- **Reliability**: Tests that don't break with minor UI changes
- **Maintainability**: Clear, consistent patterns across all tests
- **Readability**: Easy to understand test intentions and failures
- **Performance**: Efficient test execution with appropriate waits
- **Coverage**: Comprehensive testing of user journeys and edge cases

All developers must follow these standards when writing or updating Playwright tests.