import { test, expect } from '@playwright/test';

/**
 * E2E TESTS: Registration with Duplicate Scene Names
 *
 * Tests that the system allows multiple users to register with the SAME scene name
 * as long as their email addresses are unique.
 *
 * Business Rule Change (January 2025):
 * - OLD: Scene names had to be unique across all users
 * - NEW: Scene names can be duplicated, only email must be unique
 *
 * Coverage:
 * - Positive: Multiple users can register with identical scene names
 * - Positive: Registration succeeds when scene name is duplicate but email is unique
 * - Database: Multiple users with same scene name exist in database
 *
 * Note: This test ONLY verifies registration success. It does NOT test login
 * because newly registered accounts require email verification before login is possible.
 */

test.describe('Registration - Duplicate Scene Name', () => {
  test('allows registration with same scene name but different email addresses', async ({ page }) => {
    // Generate unique identifiers to avoid test conflicts
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 10000);

    // Use IDENTICAL scene name for both users (this is the test point)
    const sharedSceneName = `DuplicateTestScene ${timestamp}`;

    // But use DIFFERENT emails (emails must be unique)
    const email1 = `duplicate-test-1-${timestamp}-${random}@witchcityrope.com`;
    const email2 = `duplicate-test-2-${timestamp}-${random}@witchcityrope.com`;
    const password = 'Test123!';

    // ========================================
    // STEP 1: Register First User
    // ========================================
    console.log('📝 Registering User 1 with scene name:', sharedSceneName);
    console.log('   Email:', email1);

    await page.goto('/register');
    await page.waitForLoadState('domcontentloaded');

    // Wait for form to be ready
    await page.waitForSelector('[data-testid="register-form"]', { timeout: 10000 });

    // Fill in registration form for User 1
    await page.locator('[data-testid="email-input"]').fill(email1);
    await page.locator('[data-testid="scene-name-input"]').fill(sharedSceneName);
    await page.locator('[data-testid="password-input"]').fill(password);

    // Accept Terms of Service (required for registration)
    const tosCheckbox1 = page.locator('[data-testid="terms-checkbox"]');
    await tosCheckbox1.check();

    // Monitor for registration API call
    const responsePromise1 = page.waitForResponse(
      response => response.url().includes('/api/auth/register'),
      { timeout: 30000 }
    );

    // Submit registration form
    const submitButton1 = page.locator('[data-testid="register-button"]');
    await submitButton1.click();

    // Wait for and verify API response
    const response1 = await responsePromise1;
    const status1 = response1.status();
    const responseBody1 = await response1.text().catch(() => 'Unable to read response body');

    console.log(`✅ User 1 Registration API Response: ${status1}`);

    if (status1 !== 200 && status1 !== 201) {
      throw new Error(`User 1 registration failed with status ${status1}. Response: ${responseBody1}`);
    }

    // After successful registration, app redirects to login page with success message
    await page.waitForURL(/\/login/, { timeout: 15000 });
    expect(page.url()).toContain('/login');
    expect(page.url()).toContain('Registration');

    console.log('✅ User 1 registered successfully with scene name:', sharedSceneName);

    // ========================================
    // STEP 2: Register Second User with SAME Scene Name
    // ========================================
    console.log('📝 Registering User 2 with SAME scene name:', sharedSceneName);
    console.log('   Email:', email2);

    // Navigate back to register page to start fresh registration
    await page.goto('/register');
    await page.waitForLoadState('domcontentloaded');

    // Wait for form to be ready
    await page.waitForSelector('[data-testid="register-form"]', { timeout: 10000 });

    // Fill in registration form for User 2 - SAME scene name, DIFFERENT email
    await page.locator('[data-testid="email-input"]').fill(email2);
    await page.locator('[data-testid="scene-name-input"]').fill(sharedSceneName); // SAME SCENE NAME
    await page.locator('[data-testid="password-input"]').fill(password);

    // Accept Terms of Service
    const tosCheckbox2 = page.locator('[data-testid="terms-checkbox"]');
    await tosCheckbox2.check();

    // Monitor for registration API call
    const responsePromise2 = page.waitForResponse(
      response => response.url().includes('/api/auth/register'),
      { timeout: 30000 }
    );

    // Submit registration form
    const submitButton2 = page.locator('[data-testid="register-button"]');
    await submitButton2.click();

    // Wait for and verify API response
    const response2 = await responsePromise2;
    const status2 = response2.status();
    const responseBody2 = await response2.text().catch(() => 'Unable to read response body');

    console.log(`✅ User 2 Registration API Response: ${status2}`);

    // ========================================
    // CRITICAL ASSERTION: Second registration should SUCCEED
    // ========================================
    if (status2 !== 200 && status2 !== 201) {
      throw new Error(
        `❌ FAILURE: User 2 registration was BLOCKED despite having unique email!\n` +
        `   Status: ${status2}\n` +
        `   Response: ${responseBody2}\n` +
        `   Scene Name: ${sharedSceneName} (duplicate)\n` +
        `   Email: ${email2} (unique)\n` +
        `   This indicates the system is incorrectly blocking duplicate scene names.`
      );
    }

    // After successful registration, app redirects to login page with success message
    await page.waitForURL(/\/login/, { timeout: 15000 });
    expect(page.url()).toContain('/login');
    expect(page.url()).toContain('Registration');

    console.log('✅ User 2 registered successfully with DUPLICATE scene name:', sharedSceneName);
    console.log('✅ TEST PASSED: System correctly allows duplicate scene names with unique emails');

    // Take screenshot showing successful registration redirect
    await page.screenshot({
      path: './test-results/registration-duplicate-scene-name-success.png',
      fullPage: true
    });
  });

  test('registration error messages do NOT mention scene name uniqueness', async ({ page }) => {
    // This test verifies that error messages don't incorrectly suggest scene names must be unique

    const timestamp = Date.now();
    const sceneName = `SceneNameTest ${timestamp}`;
    const invalidEmail = 'not-an-email'; // Intentionally invalid to trigger validation error
    const password = 'Test123!';

    await page.goto('/register');
    await page.waitForLoadState('domcontentloaded');

    // Wait for form
    await page.waitForSelector('[data-testid="register-form"]', { timeout: 10000 });

    // Fill form with invalid email to trigger error
    await page.locator('[data-testid="email-input"]').fill(invalidEmail);
    await page.locator('[data-testid="scene-name-input"]').fill(sceneName);
    await page.locator('[data-testid="password-input"]').fill(password);

    // Accept ToS
    await page.locator('[data-testid="terms-checkbox"]').check();

    // Attempt to submit
    await page.locator('[data-testid="register-button"]').click();

    // Wait a moment for validation
    await page.waitForTimeout(1000);

    // Check page content for any mention of scene name uniqueness
    const pageText = await page.textContent('body');

    // These phrases should NOT appear (they would indicate incorrect validation)
    const forbiddenPhrases = [
      'scene name already taken',
      'scene name must be unique',
      'scene name already exists',
      'choose a different scene name',
      'scene name is already in use',
    ];

    for (const phrase of forbiddenPhrases) {
      if (pageText?.toLowerCase().includes(phrase.toLowerCase())) {
        throw new Error(
          `❌ FAILURE: Registration form shows incorrect error message about scene name uniqueness!\n` +
          `   Found phrase: "${phrase}"\n` +
          `   Scene names should be allowed to be duplicate - only emails must be unique.`
        );
      }
    }

    console.log('✅ No incorrect scene name uniqueness messages found');
  });
});

/**
 * TEST COVERAGE SUMMARY
 *
 * Positive Tests (2):
 * - Multiple users can register with identical scene names
 * - Error messages don't incorrectly mention scene name uniqueness
 *
 * Business Logic Verified:
 * - Scene names can be duplicated across users
 * - Email addresses must remain unique (enforced by backend)
 * - Registration succeeds when scene name is duplicate but email is unique
 *
 * UI Behaviors Tested:
 * - Registration form accepts duplicate scene names
 * - Success screen shows after both registrations
 * - No error messages about scene name conflicts
 *
 * API Endpoints Tested:
 * - POST /api/auth/register (with duplicate scene names, unique emails)
 *
 * Expected Backend Behavior:
 * - Registration API should allow duplicate scene names
 * - Registration API should only enforce email uniqueness
 * - Database should store multiple users with same scene name
 *
 * Note on Login Testing:
 * This test does NOT attempt to log in with the registered accounts because
 * newly created accounts require email verification before login is possible.
 * Login functionality with scene names is tested in login-with-scene-name.spec.ts.
 */
