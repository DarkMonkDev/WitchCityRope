# E2E Testing Patterns and Best Practices

## Overview

This document outlines the patterns, best practices, and conventions for E2E testing in the WitchCityRope project using Puppeteer.

## Test Organization

### 1. Test Suite Structure

Tests are organized by authentication requirements:

```
tests/e2e/
├── test-helpers.js                 # Shared helper functions
├── test-suite-organizer.js         # Test suite management
├── test-public-forms.js            # No auth required
├── test-authenticated-forms.js     # Login required
├── test-admin-forms.js             # Admin role required
└── test-special-conditions.js      # Token/2FA required
```

### 2. Running Tests by Category

```bash
# Run all tests
node test-suite-organizer.js

# Run specific suite
node test-suite-organizer.js --suite public
node test-suite-organizer.js --suite authenticated
node test-suite-organizer.js --suite admin

# Run with visible browser
node test-suite-organizer.js --headed

# Run multiple suites
node test-suite-organizer.js --suite public --suite authenticated
```

## Helper Functions

### Core Helpers (test-helpers.js)

The project includes a comprehensive set of helper functions for common E2E operations:

#### Authentication
```javascript
// Login with automatic navigation
await helpers.login(page, email, password);

// Navigate to protected page (auto-login if needed)
await helpers.navigateWithAuth(page, '/admin/events', 'admin@witchcityrope.com', 'Test123!');

// Logout
await helpers.logout(page);
```

#### Form Operations
```javascript
// Fill multiple form fields
await helpers.fillForm(page, {
    'input[name="Email"]': 'test@example.com',
    'select[name="Subject"]': 'General Question',
    'textarea[name="Message"]': 'Test message',
    'input[type="checkbox"]': true
});

// Wait for validation messages
await helpers.waitForValidation(page);

// Check specific validation errors
const errors = await helpers.checkValidationErrors(page, {
    email: 'Email is required',
    password: 'Password must be at least 8 characters'
});
```

#### Utilities
```javascript
// Take timestamped screenshot
await helpers.captureScreenshot(page, 'test-name');

// Wait for Blazor initialization
await helpers.waitForBlazor(page);

// Retry with exponential backoff
await helpers.retry(async () => {
    await page.click('button');
}, 3, 1000);

// Check element visibility
const isVisible = await helpers.isElementVisible(page, '.alert-success');
```

## Testing Patterns

### 1. Form Validation Testing

```javascript
async function testFormValidation(page) {
    // 1. Test empty form submission
    await page.click('button[type="submit"]');
    await helpers.waitForValidation(page);
    
    const errors = await page.$$('.text-danger');
    expect(errors.length).toBeGreaterThan(0);
    
    // 2. Test invalid data
    await helpers.fillForm(page, {
        'input[type="email"]': 'invalid-email'
    });
    
    await page.keyboard.press('Tab');
    await helpers.waitForValidation(page);
    
    // 3. Test valid submission
    await helpers.fillForm(page, {
        'input[type="email"]': 'valid@example.com',
        'input[type="password"]': 'ValidPass123!'
    });
    
    await page.click('button[type="submit"]');
    await page.waitForNavigation();
}
```

### 2. Authentication Flow Testing

```javascript
async function testAuthenticatedFeature(page) {
    // Use navigateWithAuth for protected pages
    await helpers.navigateWithAuth(
        page,
        '/member/dashboard',
        'member@witchcityrope.com',
        'Test123!'
    );
    
    // Test authenticated functionality
    await page.click('button.rsvp-btn');
    await page.waitForSelector('.modal');
}
```

### 3. Async Operations Testing

```javascript
async function testAsyncOperation(page) {
    // Click button that triggers async operation
    await page.click('button.save-btn');
    
    // Wait for loading indicator
    await page.waitForSelector('.spinner', { visible: true });
    
    // Wait for operation to complete
    await page.waitForSelector('.spinner', { hidden: true });
    
    // Check for success message
    await page.waitForSelector('.alert-success', {
        visible: true,
        timeout: 10000
    });
}
```

### 4. Modal Dialog Testing

```javascript
async function testModal(page) {
    // Open modal
    await page.click('button[data-toggle="modal"]');
    
    // Wait for modal to be visible
    await page.waitForSelector('.modal.show', { visible: true });
    
    // Interact with modal content
    await helpers.fillForm(page, {
        '.modal input[name="title"]': 'Test Title'
    });
    
    // Submit modal
    await page.click('.modal button[type="submit"]');
    
    // Wait for modal to close
    await page.waitForSelector('.modal.show', { hidden: true });
}
```

## Best Practices

### 1. Element Selection

**DO:**
- Use data attributes for test selectors: `data-test="submit-btn"`
- Use semantic selectors: `button[type="submit"]`, `input[name="email"]`
- Use ARIA attributes: `[aria-label="Close"]`

**DON'T:**
- Rely on CSS classes that might change
- Use overly specific selectors
- Use text content that might be localized

### 2. Waiting Strategies

**DO:**
- Wait for specific elements/conditions
- Use appropriate timeouts
- Wait for network idle when needed

```javascript
// Good
await page.waitForSelector('.success-message', { visible: true });
await page.waitForFunction(() => document.querySelectorAll('.item').length > 0);

// Avoid
await helpers.delay(2000); // Arbitrary delays
```

### 3. Error Handling

**DO:**
- Capture screenshots on failure
- Provide descriptive error messages
- Clean up resources in finally blocks

```javascript
try {
    await testFunction(page);
} catch (error) {
    await helpers.captureScreenshot(page, 'error-state');
    throw new Error(`Test failed at step X: ${error.message}`);
} finally {
    await cleanup();
}
```

### 4. Test Data Management

**DO:**
- Use unique data for each test run
- Clean up test data after tests
- Use test-specific email addresses

```javascript
const testEmail = `test-${Date.now()}@example.com`;
const testName = `Test User ${Date.now()}`;
```

### 5. Browser Context

**DO:**
- Use fresh browser context for each test suite
- Clear cookies/storage when needed
- Set viewport for consistent testing

```javascript
const context = await browser.createIncognitoBrowserContext();
const page = await context.newPage();
await page.setViewport({ width: 1280, height: 800 });
```

## Common Issues and Solutions

### 1. Stale Element References

**Problem:** Element references become invalid after DOM changes

**Solution:** Re-query elements after DOM updates
```javascript
let submitBtn = await page.$('button[type="submit"]');
await submitBtn.click();
// After DOM update...
submitBtn = await page.$('button[type="submit"]'); // Re-query
await submitBtn.click();
```

### 2. Timing Issues

**Problem:** Tests fail due to race conditions

**Solution:** Use proper wait conditions
```javascript
// Wait for Blazor to be ready
await helpers.waitForBlazor(page);

// Wait for specific state
await page.waitForFunction(() => {
    const btn = document.querySelector('button');
    return btn && !btn.disabled;
});
```

### 3. Authentication State

**Problem:** Tests fail because they're not authenticated

**Solution:** Use helper functions
```javascript
// For single protected page
await helpers.navigateWithAuth(page, '/protected-page', email, password);

// For multiple protected pages in same test
await helpers.login(page, email, password);
// ... navigate to multiple pages
await helpers.logout(page);
```

## Debugging Tests

### 1. Run in Headed Mode
```bash
HEADLESS=false node test-script.js
```

### 2. Add Debugging Pauses
```javascript
await page.evaluate(() => debugger);
// or
await page.waitForTimeout(0); // Pause indefinitely
```

### 3. Enable Verbose Logging
```javascript
page.on('console', msg => console.log('Browser:', msg.text()));
page.on('pageerror', error => console.log('Page error:', error));
```

### 4. Save HTML on Failure
```javascript
const html = await page.content();
await fs.writeFile('debug-page.html', html);
```

## Performance Considerations

### 1. Parallel Execution

Run independent tests in parallel:
```javascript
await Promise.all([
    testPublicForm1(page1),
    testPublicForm2(page2),
    testPublicForm3(page3)
]);
```

### 2. Resource Optimization

- Close unused pages
- Limit screenshot sizes
- Use headless mode in CI
- Reuse browser contexts when appropriate

### 3. Network Optimization

```javascript
// Block unnecessary resources
await page.setRequestInterception(true);
page.on('request', (req) => {
    if (req.resourceType() === 'image') {
        req.abort();
    } else {
        req.continue();
    }
});
```

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Run E2E Tests
  run: |
    npm install
    node test-suite-organizer.js
  env:
    BASE_URL: ${{ secrets.TEST_URL }}
    HEADLESS: true
```

### Test Reporting

Generate and save test reports:
```javascript
const report = await helpers.createTestReport({
    title: 'E2E Test Results',
    results: testResults,
    summary: { total, passed, failed }
});
```

## Maintenance

### 1. Keep Tests DRY

- Use helper functions for repeated operations
- Extract common selectors to constants
- Share test data setup functions

### 2. Update Tests with UI Changes

- Run tests before making UI changes
- Update selectors when HTML structure changes
- Maintain backwards compatibility when possible

### 3. Regular Test Audits

- Remove obsolete tests
- Update deprecated patterns
- Optimize slow tests
- Add tests for new features

## Conclusion

Following these patterns ensures consistent, maintainable, and reliable E2E tests. Always prioritize test clarity and reliability over cleverness or brevity.