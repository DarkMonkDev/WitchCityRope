import { test, expect } from '@playwright/test';

test('React Login Page Selector Validation', async ({ page }) => {
  console.log('🔍 Validating React LoginPage selectors...');

  // Navigate directly to login page (React full-page component)
  await page.goto('http://localhost:5173/login');
  await page.waitForLoadState('networkidle');

  console.log('✅ Navigated to React login page');

  // Take screenshot of login page
  await page.screenshot({
    path: '/home/chad/repos/witchcityrope-react/test-results/react-login-page.png',
    fullPage: true
  });

  // Verify React LoginPage title (NOT "Login")
  await expect(page.locator('h1')).toContainText('Welcome Back');
  console.log('✅ Page title "Welcome Back" found');

  // Verify correct React selectors exist
  const correctSelectors = {
    emailInput: '[data-testid="email-input"]',
    passwordInput: '[data-testid="password-input"]',
    loginButton: '[data-testid="login-button"]',
    loginForm: '[data-testid="login-form"]'
  };

  for (const [name, selector] of Object.entries(correctSelectors)) {
    const exists = await page.locator(selector).count();
    console.log(`📍 ${name} ("${selector}"): ${exists} elements found`);
    expect(exists).toBe(1); // Each selector should find exactly 1 element
  }

  // Verify NO modal/dialog elements (React uses full page)
  const modalCount = await page.locator('[role="dialog"], .modal, .login-modal').count();
  console.log(`📱 Modal elements found: ${modalCount} (should be 0)`);
  expect(modalCount).toBe(0);

  // Verify button text is "Sign In" not "Login"
  const buttonText = await page.locator('[data-testid="login-button"]').textContent();
  console.log(`🔘 Button text: "${buttonText}"`);
  expect(buttonText).toContain('Sign In');

  // Test form functionality with correct selectors
  console.log('🧪 Testing form functionality...');

  await page.fill('[data-testid="email-input"]', 'test@example.com');
  await page.fill('[data-testid="password-input"]', 'testpassword');

  // Verify inputs are filled
  const emailValue = await page.locator('[data-testid="email-input"]').inputValue();
  const passwordValue = await page.locator('[data-testid="password-input"]').inputValue();

  expect(emailValue).toBe('test@example.com');
  expect(passwordValue).toBe('testpassword');

  console.log('✅ React LoginPage selectors validation complete');
});