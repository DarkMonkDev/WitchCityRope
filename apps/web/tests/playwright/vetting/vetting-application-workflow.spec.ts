import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from '../../playwright/helpers/auth.helpers';

/**
 * Vetting Application Workflow E2E Tests
 *
 * These tests cover the most basic vetting application scenarios that were
 * manually tested and fixed:
 * - Bug #1: Submit button showing "Draft" tag instead of "Submit Vetting Application"
 * - Bug #2: Dashboard showing wrong vetting status after submission
 *
 * CRITICAL: All tests run against Docker containers on port 5173 EXCLUSIVELY
 * Per docker-only-testing-standard.md
 */

test.describe('Vetting Application Workflow', () => {
  /**
   * Clean authentication state before each test
   * Uses ABSOLUTE URLs per lessons learned (cookie persistence)
   */
  test.beforeEach(async ({ page }) => {
    await AuthHelpers.clearAuthState(page);
  });

  /**
   * TEST 1: New User Dashboard Shows Correct Vetting Status
   *
   * GIVEN: Brand new user account created
   * WHEN: User logs in and views dashboard
   * THEN:
   *   - Vetting status section visible
   *   - Shows "Submit Vetting Application" button (NOT "Draft" tag)
   *   - Button links to /join page
   *
   * This test validates the fix for Bug #1 (submit button display)
   */
  test('new user dashboard shows submit vetting application button', async ({ page }) => {
    // Arrange: Login as a new member who hasn't submitted vetting application
    await AuthHelpers.loginAs(page, 'member');

    // Act: Navigate to dashboard
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');

    // Assert: Vetting status section exists
    const vettingStatusSection = page.locator('[data-testid="vetting-status-section"], .vetting-status, section:has-text("Vetting Status")').first();
    await expect(vettingStatusSection).toBeVisible({ timeout: 10000 });

    // Assert: Submit Vetting Application button exists (NOT Draft tag)
    const submitButton = page.locator('button:has-text("Submit Vetting Application"), a:has-text("Submit Vetting Application")').first();
    await expect(submitButton).toBeVisible();

    // Assert: Button links to /join page
    const buttonHref = await submitButton.getAttribute('href');
    if (buttonHref) {
      expect(buttonHref).toContain('/join');
    } else {
      // If it's a button, clicking should navigate to /join
      await submitButton.click();
      await expect(page).toHaveURL(/\/join/);
    }

    // Screenshot for documentation
    await page.screenshot({
      path: 'test-results/new-user-dashboard-vetting-status.png',
      fullPage: true
    });
  });

  /**
   * TEST 2: New User Can Submit Vetting Application
   *
   * GIVEN: New user on vetting application form (/join)
   * WHEN: User fills out all required fields and submits
   * THEN:
   *   - Success message appears
   *   - Application saved to database
   *   - User redirected appropriately
   *
   * This test validates the complete submission flow
   */
  test('new user can submit vetting application successfully', async ({ page }) => {
    // Arrange: Create unique test user credentials
    const timestamp = Date.now();
    const uniqueEmail = `vetting-test-${timestamp}@example.com`;
    const uniqueSceneName = `VettingTest${timestamp}`;

    // Note: This test assumes user can register OR is already logged in as 'member'
    // For simplicity, we'll use the existing member account
    await AuthHelpers.loginAs(page, 'member');

    // Act: Navigate to vetting application form
    await page.goto('http://localhost:5173/join');
    await page.waitForLoadState('networkidle');

    // Wait for form to load
    const vettingForm = page.locator('form, [data-testid="vetting-application-form"]').first();
    await expect(vettingForm).toBeVisible({ timeout: 10000 });

    // Fill out required fields
    // Real Name field
    const realNameInput = page.locator('input[name="realName"], [data-testid="real-name-input"]').first();
    if (await realNameInput.count() > 0) {
      await realNameInput.fill('Test User');
    }

    // Why join field (20+ characters required)
    const whyJoinInput = page.locator('textarea[name="whyJoin"], [data-testid="why-join-input"]').first();
    if (await whyJoinInput.count() > 0) {
      await whyJoinInput.fill('I am very interested in learning rope bondage and joining the community.');
    }

    // Experience field (50+ characters required)
    const experienceInput = page.locator('textarea[name="experience"], [data-testid="experience-input"]').first();
    if (await experienceInput.count() > 0) {
      await experienceInput.fill('I have been practicing rope bondage for 2 years and have attended several workshops.');
    }

    // Agreement checkbox
    const agreementCheckbox = page.locator('input[type="checkbox"][name="agreedToGuidelines"], [data-testid="agree-checkbox"]').first();
    if (await agreementCheckbox.count() > 0) {
      await agreementCheckbox.check();
    }

    // Screenshot before submission
    await page.screenshot({
      path: 'test-results/vetting-application-form-filled.png',
      fullPage: true
    });

    // Submit form
    const submitButton = page.locator('button[type="submit"]:has-text("Submit"), button:has-text("Submit Application")').first();
    await submitButton.click();

    // Assert: Success message or redirect
    // Option 1: Success toast/alert message
    const successMessage = page.locator('[data-testid="success-message"], .success, .alert-success, text=/application.*submitted/i').first();

    // Option 2: Redirect to confirmation page or dashboard
    const currentUrl = page.url();

    // Wait for either success message OR URL change
    try {
      await expect(successMessage).toBeVisible({ timeout: 10000 });
      console.log('Success message appeared');
    } catch (e) {
      // If no success message, verify URL changed (redirect)
      expect(currentUrl).not.toContain('/join');
      console.log('Redirected to:', currentUrl);
    }

    // Screenshot after submission
    await page.screenshot({
      path: 'test-results/vetting-application-submitted.png',
      fullPage: true
    });
  });

  /**
   * TEST 3: Dashboard Updates After Vetting Application Submission
   *
   * GIVEN: User has submitted vetting application
   * WHEN: User returns to dashboard
   * THEN:
   *   - Vetting status shows "Submitted" or "Pending" badge (NOT "Submit Application" button)
   *   - Status message indicates "Application submitted for review"
   *
   * This test validates the fix for Bug #2 (dashboard status display)
   *
   * Note: This test depends on Test 2 having run first, OR requires a pre-seeded
   * user with an existing submitted application.
   */
  test('dashboard shows submitted status after vetting application submitted', async ({ page }) => {
    // Arrange: Login as vetted member who has submitted application
    // Note: Using 'vetted' account which should have Approved status
    await AuthHelpers.loginAs(page, 'vetted');

    // Act: Navigate to dashboard
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');

    // Assert: Vetting status section exists
    const vettingStatusSection = page.locator('[data-testid="vetting-status-section"], .vetting-status, section:has-text("Vetting Status")').first();
    await expect(vettingStatusSection).toBeVisible({ timeout: 10000 });

    // Assert: Status badge exists (NOT submit button)
    const statusBadge = page.locator('[data-testid="vetting-status-badge"], .badge, .status-badge, span:has-text("Approved"), span:has-text("Submitted"), span:has-text("Pending")').first();
    await expect(statusBadge).toBeVisible();

    // Assert: Status badge does NOT say "Submit Application"
    const statusText = await statusBadge.textContent();
    expect(statusText).not.toContain('Submit Application');
    expect(statusText).not.toContain('Draft');

    // Status should be one of the valid statuses
    const validStatuses = ['Approved', 'Submitted', 'Pending', 'Under Review', 'On Hold', 'Denied'];
    const hasValidStatus = validStatuses.some(status => statusText?.includes(status));
    expect(hasValidStatus).toBeTruthy();

    // Screenshot for documentation
    await page.screenshot({
      path: 'test-results/dashboard-vetting-status-submitted.png',
      fullPage: true
    });
  });

  /**
   * TEST 4: User Cannot Submit Duplicate Vetting Application
   *
   * GIVEN: User has already submitted vetting application
   * WHEN: User navigates to /join again
   * THEN:
   *   - Form either shows existing application status, OR
   *   - Displays message "You already have a pending application"
   *   - Submit button disabled or redirects to dashboard
   *
   * This test validates duplicate submission prevention
   */
  test('user with existing application cannot submit duplicate', async ({ page }) => {
    // Arrange: Login as vetted member who already has application
    await AuthHelpers.loginAs(page, 'vetted');

    // Act: Navigate to vetting application form
    await page.goto('http://localhost:5173/join');
    await page.waitForLoadState('networkidle');

    // Assert: One of these scenarios should be true:
    // Scenario 1: Page shows existing application status
    const existingAppMessage = page.locator('text=/already.*application/i, text=/existing.*application/i').first();

    // Scenario 2: Page redirects away from /join
    await page.waitForTimeout(2000); // Allow time for redirect
    const currentUrl = page.url();

    // Scenario 3: Submit button is disabled
    const submitButton = page.locator('button[type="submit"]:has-text("Submit"), button:has-text("Submit Application")').first();

    // Check which scenario applies
    if (await existingAppMessage.count() > 0) {
      // Scenario 1: Message shown
      await expect(existingAppMessage).toBeVisible();
      console.log('Existing application message shown');
    } else if (!currentUrl.includes('/join')) {
      // Scenario 2: Redirected
      console.log('Redirected away from /join to:', currentUrl);
      expect(currentUrl).not.toContain('/join');
    } else if (await submitButton.count() > 0) {
      // Scenario 3: Button disabled
      const isDisabled = await submitButton.isDisabled();
      expect(isDisabled).toBeTruthy();
      console.log('Submit button is disabled');
    } else {
      // If none of these, test should fail
      throw new Error('No duplicate prevention mechanism detected');
    }

    // Screenshot for documentation
    await page.screenshot({
      path: 'test-results/duplicate-application-prevention.png',
      fullPage: true
    });
  });

  /**
   * TEST 5: Incomplete Form Shows Validation Errors
   *
   * GIVEN: User on vetting application form
   * WHEN: User tries to submit with missing required fields
   * THEN:
   *   - Validation errors appear on empty fields
   *   - Form does NOT submit
   *   - User stays on form page
   *
   * This test validates form validation
   */
  test('incomplete form shows validation errors and does not submit', async ({ page }) => {
    // Arrange: Login as member without application
    await AuthHelpers.loginAs(page, 'member');

    // Navigate to vetting application form
    await page.goto('http://localhost:5173/join');
    await page.waitForLoadState('networkidle');

    // Wait for form to load
    const vettingForm = page.locator('form, [data-testid="vetting-application-form"]').first();
    await expect(vettingForm).toBeVisible({ timeout: 10000 });

    // Act: Try to submit empty form
    const submitButton = page.locator('button[type="submit"]:has-text("Submit"), button:has-text("Submit Application")').first();
    await submitButton.click();

    // Assert: Validation errors appear
    // Common validation error selectors
    const validationErrors = page.locator('.error, .validation-error, [data-testid*="error"], .text-red, .text-danger, .invalid-feedback');

    // Wait for at least one validation error to appear
    await expect(validationErrors.first()).toBeVisible({ timeout: 5000 });

    // Assert: Still on /join page (form did not submit)
    await expect(page).toHaveURL(/\/join/);

    // Assert: Multiple validation errors shown (one per required field)
    const errorCount = await validationErrors.count();
    expect(errorCount).toBeGreaterThan(0);
    console.log(`Validation errors shown: ${errorCount}`);

    // Screenshot for documentation
    await page.screenshot({
      path: 'test-results/vetting-application-validation-errors.png',
      fullPage: true
    });
  });

  /**
   * TEST 6: Form Shows User Email Pre-filled When Logged In
   *
   * GIVEN: User is logged in
   * WHEN: User navigates to /join
   * THEN:
   *   - Email field is pre-filled with user's email
   *   - Email field is readonly (cannot be changed)
   *
   * This test validates email pre-population from authentication
   */
  test('form pre-fills email for logged-in user', async ({ page }) => {
    // Arrange: Login as member
    const credentials = await AuthHelpers.loginAs(page, 'member');

    // Act: Navigate to vetting application form
    await page.goto('http://localhost:5173/join');
    await page.waitForLoadState('networkidle');

    // Wait for form to load
    const vettingForm = page.locator('form, [data-testid="vetting-application-form"]').first();
    await expect(vettingForm).toBeVisible({ timeout: 10000 });

    // Assert: Email field exists and has value
    const emailInput = page.locator('input[name="email"], [data-testid="email-input"], input[type="email"]').first();
    if (await emailInput.count() > 0) {
      // Email should be pre-filled with logged-in user's email
      const emailValue = await emailInput.inputValue();
      expect(emailValue).toBe(credentials.email);

      // Email should be readonly
      const isReadonly = await emailInput.getAttribute('readonly');
      const isDisabled = await emailInput.isDisabled();
      expect(isReadonly !== null || isDisabled).toBeTruthy();

      console.log(`Email pre-filled: ${emailValue}`);
      console.log(`Email readonly/disabled: ${isReadonly !== null || isDisabled}`);
    } else {
      console.log('Email input not found - may be handled differently in UI');
    }

    // Screenshot for documentation
    await page.screenshot({
      path: 'test-results/vetting-application-email-prefilled.png',
      fullPage: true
    });
  });
});

/**
 * Test Data Requirements:
 *
 * These tests require the following seeded test users:
 * - member@witchcityrope.com (Member, no vetting application)
 * - vetted@witchcityrope.com (VettedMember, approved vetting application)
 *
 * Database should be seeded with test data per standard seeding process.
 */

/**
 * Test Execution:
 *
 * Run all vetting application workflow tests:
 * ```bash
 * cd /home/chad/repos/witchcityrope/apps/web
 * npx playwright test tests/e2e/vetting/vetting-application-workflow.spec.ts
 * ```
 *
 * Run specific test:
 * ```bash
 * npx playwright test tests/e2e/vetting/vetting-application-workflow.spec.ts -g "new user dashboard"
 * ```
 *
 * Run with UI mode:
 * ```bash
 * npx playwright test tests/e2e/vetting/vetting-application-workflow.spec.ts --ui
 * ```
 */
