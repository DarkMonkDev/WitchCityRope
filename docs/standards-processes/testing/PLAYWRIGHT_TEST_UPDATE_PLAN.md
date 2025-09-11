# WitchCityRope React Playwright Test Suite Update Plan

## Executive Summary
The current Playwright test suite contains **critical mismatches** between expected UI elements and actual React implementation. Tests are failing because they expect outdated UI elements and selectors that don't match the modern Mantine-based React components.

## Critical Issues Identified

### 1. Login Page Text Mismatch
- **Expected by tests**: "Login" 
- **Actual in UI**: "Welcome Back" (line 109 in LoginPage.tsx)
- **Impact**: All authentication tests expecting "Login" text will fail

### 2. Correct Data-TestId Attributes Present
✅ **Good News**: The React components already have proper data-testid attributes:
- `data-testid="login-form"` (line 140)
- `data-testid="email-input"` (line 171) 
- `data-testid="password-input"` (line 214)
- `data-testid="login-button"` (line 270)
- `data-testid="login-error"` (line 146)

### 3. Button Text Mismatch
- **Expected by tests**: "Login" button
- **Actual in UI**: "Sign In" button (line 278)

### 4. Mantine Component Integration
- Tests need to work with Mantine form components and styling
- Alert components use Mantine structure
- PasswordInput has show/hide functionality

## Test Files Requiring Updates

### Authentication Tests (HIGH PRIORITY)
1. **`/apps/web/tests/playwright/auth.spec.ts`**
   - Update "Login" text expectations to "Welcome Back"
   - Update button selectors from "Login" to "Sign In"
   - Fix form selectors to use proper data-testids

2. **`/apps/web/tests/playwright/login-verification-test.spec.ts`**
   - Already has good fallback selector patterns
   - Update text expectations
   - Fix button text references

3. **`/apps/web/tests/playwright/auth-flow-improved.spec.ts`**
   - Update authentication flow expectations

4. **`/apps/web/tests/playwright/working-auth-test.spec.ts`**
   - Update to match current UI implementation

### Form and Component Tests (MEDIUM PRIORITY)
5. **`/apps/web/tests/playwright/form-components.spec.ts`**
   - Verify Mantine component compatibility
   - Test TinyMCE editor integration

6. **`/apps/web/tests/playwright/basic-functionality-check.spec.ts`**
   - Update basic UI element expectations

### Events and Dashboard Tests (MEDIUM PRIORITY)
7. **`/apps/web/tests/playwright/events-page-exploration.spec.ts`**
   - Update events page selectors for Mantine components
   - Fix navigation expectations

8. **Dashboard-related tests**
   - Update dashboard page expectations
   - Fix navigation patterns

## Updated Test Standards and Patterns

### 1. Reliable Selector Strategy
```typescript
// ✅ CORRECT - Use data-testid first, with fallbacks
const emailInput = page.locator('[data-testid="email-input"]');
const passwordInput = page.locator('[data-testid="password-input"]'); 
const submitButton = page.locator('[data-testid="login-button"]');

// ✅ CORRECT - Fallback patterns for form elements
const emailInputFallback = page.locator('input[type="email"], input[placeholder*="email"]');
const submitButtonFallback = page.locator('button[type="submit"], button:has-text("Sign In")');
```

### 2. Updated Text Expectations
```typescript
// ✅ CORRECT - Match actual UI text
await expect(page.locator('h1, .title')).toContainText('Welcome Back');

// ✅ CORRECT - Updated button text
await page.click('button:has-text("Sign In")');

// ✅ CORRECT - Error handling
await expect(page.locator('[data-testid="login-error"]')).toBeVisible();
```

### 3. Mantine Component Compatibility
```typescript
// ✅ CORRECT - Mantine Alert component
await expect(page.locator('[data-testid="login-error"]')).toContainText(/invalid|failed|error/i);

// ✅ CORRECT - Mantine PasswordInput with show/hide
await page.locator('[data-testid="password-input"]').fill('password');
await page.locator('button[aria-label="Toggle password visibility"]').click(); // If needed
```

### 4. Consistent Test Data
```typescript
// ✅ CORRECT - Use existing test accounts
const testCredentials = {
  admin: { email: 'admin@witchcityrope.com', password: 'Test123!' },
  member: { email: 'member@witchcityrope.com', password: 'Test123!' },
  guest: { email: 'guest@witchcityrope.com', password: 'Test123!' }
};
```

## New Comprehensive E2E Test Plan

### 1. Authentication Flow Tests
```typescript
test.describe('Authentication', () => {
  test('Complete login-to-dashboard flow', async ({ page }) => {
    await page.goto('/login');
    
    // Verify login page loads with correct title
    await expect(page.locator('h1')).toContainText('Welcome Back');
    
    // Fill form using data-testids
    await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    
    // Submit using correct button text
    await page.locator('[data-testid="login-button"]').click();
    
    // Verify successful redirect
    await page.waitForURL('/dashboard');
    await expect(page.locator('h1')).toContainText('Dashboard'); // Update with actual dashboard title
  });
  
  test('Invalid credentials show error', async ({ page }) => {
    await page.goto('/login');
    
    await page.locator('[data-testid="email-input"]').fill('invalid@test.com');
    await page.locator('[data-testid="password-input"]').fill('wrongpassword');
    await page.locator('[data-testid="login-button"]').click();
    
    // Verify error alert is shown
    await expect(page.locator('[data-testid="login-error"]')).toBeVisible();
    await expect(page.locator('[data-testid="login-error"]')).toContainText(/login failed/i);
  });
});
```

### 2. Event Browsing Tests
```typescript
test.describe('Public Event Access', () => {
  test('Browse events without authentication', async ({ page }) => {
    await page.goto('/events');
    
    // Verify events page loads
    await expect(page.locator('h1')).toContainText(/events/i);
    
    // Verify event cards are displayed
    await expect(page.locator('[data-testid="event-card"]')).toHaveCount(3, { timeout: 10000 });
    
    // Test event details
    await page.locator('[data-testid="event-card"]').first().click();
    await expect(page.locator('[data-testid="event-details"]')).toBeVisible();
  });
});
```

### 3. Registration and User Management Tests  
```typescript
test.describe('User Registration', () => {
  test('Complete registration flow', async ({ page }) => {
    const uniqueEmail = `test-${Date.now()}@example.com`;
    
    await page.goto('/register');
    
    // Fill registration form
    await page.locator('[data-testid="email-input"]').fill(uniqueEmail);
    await page.locator('[data-testid="scene-name-input"]').fill(`TestUser${Date.now()}`);
    await page.locator('[data-testid="password-input"]').fill('StrongPass123!');
    
    // Submit registration
    await page.locator('[data-testid="register-button"]').click();
    
    // Verify success (update with actual success page)
    await page.waitForURL('/welcome');
    await expect(page.locator('h1')).toContainText('Welcome');
  });
});
```

## Test Utilities and Helpers

### 1. Authentication Helper
```typescript
// File: /apps/web/tests/playwright/helpers/auth.helpers.ts
export class AuthHelpers {
  static async loginAs(page: Page, role: 'admin' | 'member' | 'guest') {
    const credentials = {
      admin: { email: 'admin@witchcityrope.com', password: 'Test123!' },
      member: { email: 'member@witchcityrope.com', password: 'Test123!' },
      guest: { email: 'guest@witchcityrope.com', password: 'Test123!' }
    };
    
    const creds = credentials[role];
    
    await page.goto('/login');
    await page.locator('[data-testid="email-input"]').fill(creds.email);
    await page.locator('[data-testid="password-input"]').fill(creds.password);
    await page.locator('[data-testid="login-button"]').click();
    
    // Wait for successful authentication
    await page.waitForURL('/dashboard');
  }
  
  static async logout(page: Page) {
    await page.locator('[data-testid="logout-button"]').click();
    await page.waitForURL('/login');
  }
}
```

### 2. Form Interaction Helper
```typescript
// File: /apps/web/tests/playwright/helpers/form.helpers.ts
export class FormHelpers {
  static async fillFormData(page: Page, data: Record<string, string>) {
    for (const [field, value] of Object.entries(data)) {
      await page.locator(`[data-testid="${field}-input"]`).fill(value);
    }
  }
  
  static async waitForFormError(page: Page, errorTestId: string) {
    await expect(page.locator(`[data-testid="${errorTestId}"]`)).toBeVisible();
    return page.locator(`[data-testid="${errorTestId}"]`).textContent();
  }
}
```

### 3. Wait Strategy Helpers
```typescript
// File: /apps/web/tests/playwright/helpers/wait.helpers.ts
export class WaitHelpers {
  static async waitForPageLoad(page: Page, expectedUrl?: string) {
    await page.waitForLoadState('networkidle');
    if (expectedUrl) {
      await page.waitForURL(expectedUrl);
    }
  }
  
  static async waitForApiResponse(page: Page, apiPath: string) {
    return page.waitForResponse(response => 
      response.url().includes(apiPath) && response.status() < 400
    );
  }
}
```

## Implementation Priority

### Phase 1: Critical Authentication Tests (IMMEDIATE)
1. Fix all login/authentication test failures
2. Update text expectations from "Login" to "Welcome Back"
3. Update button text from "Login" to "Sign In"
4. Verify data-testid selectors work

### Phase 2: Core Functionality Tests (WEEK 1)
1. Events browsing and filtering
2. Event detail viewing  
3. User registration flow
4. Dashboard navigation

### Phase 3: Advanced Feature Tests (WEEK 2)
1. Event registration process
2. User profile management
3. Admin functionality
4. Error handling scenarios

### Phase 4: Cross-browser and Responsive Tests (WEEK 3)
1. Mobile viewport testing
2. Cross-browser compatibility
3. Performance testing
4. Visual regression testing

## Success Metrics

### Before Fix
- Authentication tests: **0% passing** (expecting "Login", getting "Welcome Back")
- Button interactions: **Failing** (expecting "Login" button, actual is "Sign In")
- Form submissions: **Intermittent** (selector issues)

### After Fix Target
- Authentication tests: **100% passing**
- All critical user journeys: **95% passing**
- Cross-browser compatibility: **90% passing**
- Test execution time: **Under 5 minutes for full suite**

## Maintenance Strategy

### 1. Data-TestId Requirements
- **MANDATORY**: All interactive elements must have data-testid attributes
- **NAMING**: Use kebab-case: `data-testid="submit-button"`
- **CONSISTENCY**: Follow pattern: `{element-type}-{purpose}`

### 2. Test Review Process
1. Every new component must include data-testid attributes
2. Every UI text change requires test update review
3. Monthly test health check for flaky tests
4. Quarterly cross-browser validation

### 3. Documentation Updates
- Update TEST_CATALOG.md with all new test patterns
- Document component-specific testing approaches
- Maintain selector change log for UI updates

## Next Steps

1. **IMMEDIATE**: Update critical authentication tests
2. **Day 1**: Implement auth helpers and core test utilities  
3. **Day 2-3**: Update events and dashboard tests
4. **Week 1**: Complete comprehensive E2E test coverage
5. **Week 2**: Cross-browser and performance validation

This plan will transform the failing test suite into a reliable, maintainable testing foundation that properly validates the React + Mantine UI implementation.