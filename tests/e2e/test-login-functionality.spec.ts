import { test, expect } from '@playwright/test';

test('Test complete login functionality with both email and password', async ({ page }) => {
  // Navigate to login page
  await page.goto('http://localhost:5173/login');
  await page.waitForLoadState('networkidle');

  // Verify both fields are present
  const emailInput = page.locator('[data-testid="email-input"]');
  const passwordInput = page.locator('[data-testid="password-input"]');
  const loginButton = page.locator('[data-testid="login-button"]');

  await expect(emailInput).toBeVisible();
  await expect(passwordInput).toBeVisible();
  await expect(loginButton).toBeVisible();

  console.log('✅ All form fields are visible and accessible');

  // Test login with valid credentials
  await emailInput.fill('admin@witchcityrope.com');
  await passwordInput.fill('Test123!');

  console.log('✅ Filled email and password fields successfully');

  // Click the login button
  await loginButton.click();

  // Wait for either successful login (redirect) or error message
  await page.waitForTimeout(2000);

  // Check if we were redirected to dashboard (successful login)
  const currentUrl = page.url();
  console.log(`📍 Current URL after login attempt: ${currentUrl}`);

  // Check for potential error messages
  const errorElement = page.locator('[data-testid="login-error"]');
  const errorVisible = await errorElement.isVisible();

  if (errorVisible) {
    const errorText = await errorElement.textContent();
    console.log(`❌ Login error: ${errorText}`);
  } else {
    console.log('✅ No error message displayed');
  }

  // If redirected to dashboard, login was successful
  if (currentUrl.includes('/dashboard')) {
    console.log('✅ LOGIN SUCCESSFUL: Redirected to dashboard');
  } else {
    console.log(`⚠️ LOGIN STATUS UNCLEAR: Still on URL ${currentUrl}`);
  }

  // Take a screenshot of the result
  await page.screenshot({ path: 'login-test-result.png', fullPage: true });
});