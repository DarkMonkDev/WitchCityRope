import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * Password Reset Workflow Test
 * Tests the complete password reset request flow (unauthenticated user)
 *
 * This test verifies:
 * 1. Navigation to forgot password page
 * 2. Email input validation
 * 3. Form submission
 * 4. Success confirmation message appears
 * 5. Security: Always shows success (no email enumeration)
 *
 * CRITICAL: This test does NOT authenticate - it tests the forgot password flow
 * for unauthenticated users. AuthHelpers is imported for structure but not used for login.
 */

test.describe('Password Reset Workflow', () => {
  test.beforeEach(async ({ page }) => {
    // Clear authentication state - this is an unauthenticated flow
    await AuthHelpers.clearAuthState(page);
  });

  test('User can request password reset and receive confirmation', async ({ page }) => {
    console.log('🧪 Starting password reset workflow test...');

    // Step 1: Navigate to forgot password page
    console.log('📍 Step 1: Navigate to forgot password page');
    await page.goto('/forgot-password');
    await page.waitForLoadState('domcontentloaded');

    // Verify page loaded correctly
    const pageContainer = page.locator('[data-testid="page-forgot-password"]');
    await expect(pageContainer).toBeVisible({ timeout: 10000 });
    console.log('✅ Forgot password page loaded successfully');

    // Verify page title
    await expect(page.locator('h1, [role="heading"]')).toContainText('Reset Password', {
      timeout: 5000
    });

    // Step 2: Verify form is visible
    console.log('📍 Step 2: Verify form elements are present');
    const form = page.locator('[data-testid="forgot-password-form"]');
    await expect(form).toBeVisible({ timeout: 5000 });

    const emailInput = page.locator('[data-testid="email-input"]');
    await expect(emailInput).toBeVisible({ timeout: 5000 });

    const submitButton = page.locator('[data-testid="submit-button"]');
    await expect(submitButton).toBeVisible({ timeout: 5000 });
    console.log('✅ Form elements found');

    // Step 3: Enter email address (using a test account)
    console.log('📍 Step 3: Enter email address');
    const testEmail = 'admin@witchcityrope.com'; // Using existing test account
    await emailInput.fill(testEmail);
    await expect(emailInput).toHaveValue(testEmail);
    console.log(`✅ Email entered: ${testEmail}`);

    // Step 4: Submit the form
    console.log('📍 Step 4: Submit password reset request');
    await submitButton.click();

    // Step 5: Wait for and verify success message
    console.log('📍 Step 5: Verify success confirmation appears');

    // Wait for success message to appear
    const successMessage = page.locator('[data-testid="success-message"]');
    await expect(successMessage).toBeVisible({ timeout: 10000 });

    // Verify message content (security: always shows success, no email enumeration)
    await expect(successMessage).toContainText('If an account exists with this email', {
      timeout: 5000
    });
    console.log('✅ Success confirmation message displayed');

    // Step 6: Verify form is replaced with success state
    console.log('📍 Step 6: Verify form is hidden after success');
    await expect(form).not.toBeVisible({ timeout: 5000 });
    console.log('✅ Form hidden after successful submission');

    // Step 7: Verify "Back to Login" link is present
    console.log('📍 Step 7: Verify navigation options');
    const backToLoginLink = page.locator('[data-testid="link-back-to-login"]');
    await expect(backToLoginLink).toBeVisible({ timeout: 5000 });
    console.log('✅ "Back to Login" link is present');

    console.log('🎉 Password reset workflow test completed successfully');
  });

  test('Form validates email format before submission', async ({ page }) => {
    console.log('🧪 Testing email validation...');

    // Navigate to forgot password page
    await page.goto('/forgot-password');
    await page.waitForLoadState('domcontentloaded');

    const emailInput = page.locator('[data-testid="email-input"]');
    const submitButton = page.locator('[data-testid="submit-button"]');

    // Test invalid email format
    console.log('📍 Testing invalid email format');
    await emailInput.fill('invalid-email');
    await submitButton.click();

    // Should show validation error (Mantine form validation)
    // Note: Mantine may display inline validation or prevent submission
    // We verify that success message does NOT appear
    const successMessage = page.locator('[data-testid="success-message"]');
    await expect(successMessage).not.toBeVisible({ timeout: 2000 });
    console.log('✅ Invalid email rejected');

    // Test valid email format
    console.log('📍 Testing valid email format');
    await emailInput.clear();
    await emailInput.fill('valid@example.com');
    await submitButton.click();

    // Should show success message
    await expect(successMessage).toBeVisible({ timeout: 10000 });
    console.log('✅ Valid email accepted');

    console.log('🎉 Email validation test completed');
  });

  test('Back to Login link navigates correctly', async ({ page }) => {
    console.log('🧪 Testing Back to Login navigation...');

    // Navigate to forgot password page
    await page.goto('/forgot-password');
    await page.waitForLoadState('domcontentloaded');

    // Click "Back to Login" link (before submission)
    const backToLoginLink = page.locator('[data-testid="link-back-to-login"]').first();
    await expect(backToLoginLink).toBeVisible({ timeout: 5000 });
    await backToLoginLink.click();

    // Verify navigation to login page
    await page.waitForURL('**/login', { timeout: 10000 });
    await page.waitForLoadState('domcontentloaded');

    const currentUrl = page.url();
    expect(currentUrl).toContain('/login');
    console.log('✅ Successfully navigated to login page');

    console.log('🎉 Navigation test completed');
  });
});
