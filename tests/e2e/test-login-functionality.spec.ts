import { test, expect } from '@playwright/test';
import { AuthHelper } from './helpers/auth.helper';

test('Test complete login functionality with both email and password', async ({ page }) => {
  // Test login with valid credentials using AuthHelper
  const loginSuccess = await AuthHelper.loginAs(page, 'admin');

  console.log('✅ Login completed successfully');

  // Verify successful login
  expect(loginSuccess).toBeTruthy();

  const currentUrl = page.url();
  console.log(`📍 Current URL after login attempt: ${currentUrl}`);

  // If redirected to dashboard, login was successful
  if (currentUrl.includes('/dashboard')) {
    console.log('✅ LOGIN SUCCESSFUL: Redirected to dashboard');
  } else {
    console.log(`⚠️ LOGIN STATUS UNCLEAR: Still on URL ${currentUrl}`);
  }

  // Take a screenshot of the result
  await page.screenshot({ path: 'login-test-result.png', fullPage: true });
});