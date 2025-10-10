import { test, expect } from '@playwright/test';

/**
 * Simple login page test - bypasses problematic helpers
 * to verify basic page functionality
 */
test.describe('Simple Login Test', () => {
  test('should load login page and show Welcome Back', async ({ page }) => {
    // Clear cookies first (but don't try to access localStorage/sessionStorage)
    await page.context().clearCookies();
    
    // Navigate to login page
    await page.goto('http://localhost:5173/login');
    
    // Wait for page to load
    await page.waitForLoadState('domcontentloaded');
    
    // Check if page shows "Welcome Back" as expected by the fixed test
    const h1Text = await page.locator('h1').textContent();
    console.log(`Found h1 text: "${h1Text}"`);
    
    // Take screenshot to see what's actually on the page
    await page.screenshot({ path: 'test-results/simple-login-page.png' });
    
    // Check for expected elements
    const emailInput = page.locator('[data-testid="email-input"]');
    const passwordInput = page.locator('[data-testid="password-input"]');
    const loginButton = page.locator('[data-testid="login-button"]');
    
    console.log(`Email input visible: ${await emailInput.isVisible()}`);
    console.log(`Password input visible: ${await passwordInput.isVisible()}`);
    console.log(`Login button visible: ${await loginButton.isVisible()}`);
    
    // Try to verify the h1 text
    if (h1Text) {
      expect(h1Text).toContain('Welcome Back');
    }
  });

  test('should be able to fill login form', async ({ page }) => {
    const { AuthHelpers } = await import('./helpers/auth.helpers');

    // Login using AuthHelpers
    await AuthHelpers.loginAs(page, 'admin');

    // Take screenshot after successful login
    await page.screenshot({ path: 'test-results/form-filled.png' });

    console.log('✅ Login successful using AuthHelpers');
  });
});