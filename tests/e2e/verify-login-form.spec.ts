import { test, expect } from '@playwright/test';

test('Verify login form has both email and password fields', async ({ page }) => {
  // Navigate to login page
  await page.goto('http://localhost:5173/login');

  // Wait for page to load
  await page.waitForLoadState('networkidle');

  // Take a screenshot for inspection
  await page.screenshot({ path: 'login-form-inspection.png', fullPage: true });

  // Check for email input
  const emailInput = page.locator('[data-testid="email-input"]');
  await expect(emailInput).toBeVisible();
  console.log('✅ Email input found');

  // Check for password input
  const passwordInput = page.locator('[data-testid="password-input"]');
  await expect(passwordInput).toBeVisible();
  console.log('✅ Password input found');

  // Check for login button
  const loginButton = page.locator('[data-testid="login-button"]');
  await expect(loginButton).toBeVisible();
  console.log('✅ Login button found');

  // Log all form inputs for debugging
  const allInputs = await page.locator('input').all();
  console.log(`📝 Total inputs found: ${allInputs.length}`);

  for (let i = 0; i < allInputs.length; i++) {
    const input = allInputs[i];
    const type = await input.getAttribute('type') || 'text';
    const placeholder = await input.getAttribute('placeholder') || '';
    const testId = await input.getAttribute('data-testid') || '';
    const name = await input.getAttribute('name') || '';
    console.log(`   Input ${i + 1}: type="${type}", placeholder="${placeholder}", testId="${testId}", name="${name}"`);
  }

  // Try to interact with the form
  await emailInput.fill('test@example.com');
  await passwordInput.fill('testpassword');

  console.log('✅ Successfully filled both email and password fields');
});