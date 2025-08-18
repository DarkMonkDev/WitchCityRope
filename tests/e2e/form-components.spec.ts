import { test, expect } from '@playwright/test';

test.describe('Form Components Test Page', () => {
  
  test.beforeEach(async ({ page }) => {
    // Capture console errors for debugging
    const consoleErrors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    // Capture network failures
    const networkErrors: string[] = [];
    page.on('requestfailed', request => {
      networkErrors.push(`${request.method()} ${request.url()} - ${request.failure()?.errorText}`);
    });

    // Store errors for later access
    page.consoleErrors = consoleErrors;
    page.networkErrors = networkErrors;
  });

  test('should load the form test page successfully', async ({ page }) => {
    // Navigate to the form test page with extended timeout
    await page.goto('/form-test', { timeout: 30000 });

    // Take initial screenshot for debugging
    await page.screenshot({ path: 'test-results/form-test-page-loaded.png', fullPage: true });

    // Verify page title/heading
    await expect(page.locator('h1')).toContainText('Mantine v7 Form Components Test', { timeout: 10000 });

    // Verify page description is visible
    await expect(page.locator('text=Comprehensive testing page for all WitchCityRope form components')).toBeVisible();

    // Check for console errors
    const errors = page.consoleErrors || [];
    if (errors.length > 0) {
      console.log('Console errors detected:', errors);
    }

    // Check for network errors
    const networkErrors = page.networkErrors || [];
    if (networkErrors.length > 0) {
      console.log('Network errors detected:', networkErrors);
    }
  });

  test('should display all test control buttons', async ({ page }) => {
    await page.goto('/form-test');

    // Wait for test controls section to load
    await expect(page.locator('text=Test Controls')).toBeVisible();

    // Check all test control buttons are visible and properly labeled
    const fillTestDataBtn = page.locator('[data-testid="fill-test-data"]');
    await expect(fillTestDataBtn).toBeVisible();
    await expect(fillTestDataBtn).toContainText('Fill Test Data');

    const fillConflictDataBtn = page.locator('[data-testid="fill-conflict-data"]');
    await expect(fillConflictDataBtn).toBeVisible();
    await expect(fillConflictDataBtn).toContainText('Fill Conflict Data');

    const toggleErrorsBtn = page.locator('[data-testid="toggle-errors"]');
    await expect(toggleErrorsBtn).toBeVisible();
    await expect(toggleErrorsBtn).toContainText(/Show|Hide.*Validation Errors/);

    const toggleDisabledBtn = page.locator('[data-testid="toggle-disabled"]');
    await expect(toggleDisabledBtn).toBeVisible();
    await expect(toggleDisabledBtn).toContainText(/Enable|Disable.*All Fields/);

    // Take screenshot of controls section
    await page.locator('text=Test Controls').locator('..').screenshot({ 
      path: 'test-results/test-controls-section.png' 
    });
  });

  test('should display all form input components', async ({ page }) => {
    await page.goto('/form-test');

    // Wait for form to load
    await page.waitForLoadState('networkidle');

    // Check basic form components
    await expect(page.locator('[data-testid="basic-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="basic-select"]')).toBeVisible();
    await expect(page.locator('[data-testid="basic-textarea"]')).toBeVisible();

    // Check specialized WitchCityRope components
    await expect(page.locator('[data-testid="email-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="scene-name-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="password-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="phone-input"]')).toBeVisible();

    // Check emergency contact section
    await expect(page.locator('[data-testid="emergency-contact"]')).toBeVisible();

    // Check submit button
    await expect(page.locator('[data-testid="submit-button"]')).toBeVisible();

    // Take screenshot of the form sections
    await page.screenshot({ path: 'test-results/form-components-visible.png', fullPage: true });
  });

  test('should fill test data when Fill Test Data button is clicked', async ({ page }) => {
    await page.goto('/form-test');

    // Wait for page to be fully loaded
    await page.waitForLoadState('networkidle');

    // Click Fill Test Data button
    await page.locator('[data-testid="fill-test-data"]').click();

    // Wait a moment for data to populate
    await page.waitForTimeout(500);

    // Verify test data was filled in the fields
    await expect(page.locator('[data-testid="basic-input"]')).toHaveValue('Test Input Value');
    await expect(page.locator('[data-testid="email-input"] input')).toHaveValue('test@example.com');
    await expect(page.locator('[data-testid="scene-name-input"] input')).toHaveValue('TestUser123');
    await expect(page.locator('[data-testid="phone-input"] input')).toHaveValue('(555) 123-4567');

    // Check password field has value (but don't check actual password for security)
    await expect(page.locator('[data-testid="password-input"] input')).not.toHaveValue('');

    // Check textarea
    await expect(page.locator('[data-testid="basic-textarea"]')).toContainText('This is a test textarea');

    // Take screenshot after filling test data
    await page.screenshot({ path: 'test-results/form-test-data-filled.png', fullPage: true });
  });

  test('should show validation errors when Toggle Errors button is clicked', async ({ page }) => {
    await page.goto('/form-test');
    await page.waitForLoadState('networkidle');

    // First clear any existing data to ensure validation errors will show
    await page.locator('[data-testid="basic-input"]').clear();
    await page.locator('[data-testid="email-input"] input').clear();

    // Click Toggle Errors button to show validation
    await page.locator('[data-testid="toggle-errors"]').click();

    // Wait for validation to run
    await page.waitForTimeout(1000);

    // Check if validation errors are displayed
    // Look for error indicators or messages (adapt based on actual Mantine error styling)
    const errorElements = page.locator('[role="alert"], .mantine-InputBase-error, .mantine-Text-root[data-variant="error"]');
    
    // Check if at least some validation errors are showing
    const errorCount = await errorElements.count();
    expect(errorCount).toBeGreaterThan(0);

    // Verify button text changed to "Hide"
    await expect(page.locator('[data-testid="toggle-errors"]')).toContainText('Hide');

    // Take screenshot showing validation errors
    await page.screenshot({ path: 'test-results/form-validation-errors.png', fullPage: true });
  });

  test('should disable all fields when Disable All button is clicked', async ({ page }) => {
    await page.goto('/form-test');
    await page.waitForLoadState('networkidle');

    // Click Disable All button
    await page.locator('[data-testid="toggle-disabled"]').click();

    // Wait for state change
    await page.waitForTimeout(500);

    // Check that input fields are disabled
    await expect(page.locator('[data-testid="basic-input"]')).toBeDisabled();
    await expect(page.locator('[data-testid="email-input"] input')).toBeDisabled();
    await expect(page.locator('[data-testid="scene-name-input"] input')).toBeDisabled();
    await expect(page.locator('[data-testid="password-input"] input')).toBeDisabled();
    await expect(page.locator('[data-testid="phone-input"] input')).toBeDisabled();
    await expect(page.locator('[data-testid="submit-button"]')).toBeDisabled();

    // Verify button text changed to "Enable"
    await expect(page.locator('[data-testid="toggle-disabled"]')).toContainText('Enable');

    // Take screenshot of disabled state
    await page.screenshot({ path: 'test-results/form-fields-disabled.png', fullPage: true });
  });

  test('should test conflict data and validation responses', async ({ page }) => {
    await page.goto('/form-test');
    await page.waitForLoadState('networkidle');

    // Click Fill Conflict Data button
    await page.locator('[data-testid="fill-conflict-data"]').click();

    // Wait for data to populate and async validation to potentially run
    await page.waitForTimeout(2000);

    // Check that conflict data was filled
    await expect(page.locator('[data-testid="email-input"] input')).toHaveValue('taken@example.com');
    await expect(page.locator('[data-testid="scene-name-input"] input')).toHaveValue('admin');

    // Look for any async validation feedback (this may take time)
    // The test data mentions these will trigger uniqueness validation
    
    // Take screenshot to capture any validation states
    await page.screenshot({ path: 'test-results/form-conflict-data.png', fullPage: true });

    // Try triggering validation
    await page.locator('[data-testid="toggle-errors"]').click();
    await page.waitForTimeout(1000);

    // Take final screenshot
    await page.screenshot({ path: 'test-results/form-conflict-validation.png', fullPage: true });
  });

  test('should display form state information', async ({ page }) => {
    await page.goto('/form-test');
    await page.waitForLoadState('networkidle');

    // Check that Form State section exists
    await expect(page.locator('text=Form State')).toBeVisible();

    // Look for form state badges
    const formStateBadges = page.locator('.mantine-Badge-root');
    await expect(formStateBadges).toHaveCount(3, { timeout: 5000 });

    // Check for accordion sections
    await expect(page.locator('text=View Form Values')).toBeVisible();
    await expect(page.locator('text=View Form Errors')).toBeVisible();

    // Test accordion interaction
    await page.locator('text=View Form Values').click();
    await page.waitForTimeout(500);
    
    // Should show JSON data
    await expect(page.locator('code')).toBeVisible();

    // Take screenshot of form state section
    await page.screenshot({ path: 'test-results/form-state-section.png', fullPage: true });
  });

  test('should handle form submission', async ({ page }) => {
    await page.goto('/form-test');
    await page.waitForLoadState('networkidle');

    // Fill test data first
    await page.locator('[data-testid="fill-test-data"]').click();
    await page.waitForTimeout(1000);

    // Submit the form
    await page.locator('[data-testid="submit-button"]').click();

    // Check for loading state
    const submitButton = page.locator('[data-testid="submit-button"]');
    
    // Wait for either loading state or completion
    await page.waitForTimeout(2000);

    // Look for success notification (Mantine notifications)
    // This may appear as a toast/notification
    const notifications = page.locator('.mantine-Notification-root, [role="alert"]');
    
    // Take screenshot after submission attempt
    await page.screenshot({ path: 'test-results/form-submission-result.png', fullPage: true });
  });

  test('should verify responsive layout on mobile viewport', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    await page.goto('/form-test');
    await page.waitForLoadState('networkidle');

    // Verify page still loads properly on mobile
    await expect(page.locator('h1')).toBeVisible();
    await expect(page.locator('[data-testid="fill-test-data"]')).toBeVisible();

    // Check that form components are visible on mobile
    await expect(page.locator('[data-testid="basic-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="email-input"]')).toBeVisible();

    // Take mobile screenshot
    await page.screenshot({ path: 'test-results/form-test-mobile.png', fullPage: true });
  });

  test('should capture network requests and errors', async ({ page }) => {
    const requests: string[] = [];
    const responses: string[] = [];

    // Monitor network activity
    page.on('request', request => {
      requests.push(`${request.method()} ${request.url()}`);
    });

    page.on('response', response => {
      responses.push(`${response.status()} ${response.url()}`);
    });

    await page.goto('/form-test');
    await page.waitForLoadState('networkidle');

    // Try some interactions that might make API calls
    await page.locator('[data-testid="fill-test-data"]').click();
    await page.waitForTimeout(1000);

    // Try conflict data (might trigger async validation)
    await page.locator('[data-testid="fill-conflict-data"]').click();
    await page.waitForTimeout(2000);

    // Log network activity for debugging
    console.log('Network Requests:', requests);
    console.log('Network Responses:', responses);
    console.log('Console Errors:', page.consoleErrors || []);
    console.log('Network Errors:', page.networkErrors || []);

    // Take final screenshot
    await page.screenshot({ path: 'test-results/form-test-network-complete.png', fullPage: true });
  });

  test('should verify all component sections are present', async ({ page }) => {
    await page.goto('/form-test');
    await page.waitForLoadState('networkidle');

    // Verify all major sections are present
    await expect(page.locator('text=Test Controls')).toBeVisible();
    await expect(page.locator('text=Testing Instructions')).toBeVisible();
    await expect(page.locator('text=Basic Form Components')).toBeVisible();
    await expect(page.locator('text=Specialized WitchCityRope Components')).toBeVisible();
    await expect(page.locator('text=Emergency Contact Group')).toBeVisible();
    await expect(page.locator('text=Form State')).toBeVisible();
    await expect(page.locator('text=Component Features Demonstrated')).toBeVisible();

    // Take comprehensive screenshot
    await page.screenshot({ path: 'test-results/form-test-all-sections.png', fullPage: true });
  });

});

// Add custom types for page object to store error arrays
declare global {
  namespace PlaywrightTest {
    interface Page {
      consoleErrors?: string[];
      networkErrors?: string[];
    }
  }
}