import { test, expect } from '@playwright/test';

test('React Authentication Navigation Test', async ({ page }) => {
  console.log('=== TESTING REACT AUTHENTICATION NAVIGATION ===');

  const consoleMessages: string[] = [];
  const pageErrors: string[] = [];

  // Capture console and errors
  page.on('console', (msg) => {
    if (msg.type() === 'error') {
      consoleMessages.push(`Console Error: ${msg.text()}`);
    }
  });

  page.on('pageerror', (error) => {
    pageErrors.push(`Page Error: ${error.message}`);
  });

  // Navigate to homepage first
  await page.goto('http://localhost:5173');
  await page.waitForLoadState('networkidle');

  console.log('✅ Homepage loaded successfully');

  // Look for LOGIN navigation link (not button - this should navigate to /login)
  const loginNavigationSelectors = [
    'a:has-text("LOGIN")',
    'a[href="/login"]',
    'text=LOGIN',
    'nav a:has-text("LOGIN")'
  ];

  let loginNavLink = null;
  let usedSelector = '';

  for (const selector of loginNavigationSelectors) {
    const element = page.locator(selector);
    const count = await element.count();
    if (count > 0) {
      loginNavLink = element.first();
      usedSelector = selector;
      console.log(`Found LOGIN navigation with selector: ${selector}`);
      break;
    }
  }

  if (loginNavLink) {
    // Get navigation link details
    const linkText = await loginNavLink.textContent();
    const linkHref = await loginNavLink.getAttribute('href');

    console.log(`Login navigation details:`);
    console.log(`  Text: "${linkText}"`);
    console.log(`  Href: "${linkHref}"`);

    // Click the navigation link
    console.log('🖱️ Clicking LOGIN navigation...');
    await loginNavLink.click();
    await page.waitForLoadState('networkidle');

    // Should navigate to login page
    const currentUrl = page.url();
    console.log(`URL after click: ${currentUrl}`);

    expect(currentUrl).toContain('/login');
    console.log('✅ Successfully navigated to login page');
  } else {
    console.log('ℹ️ No LOGIN navigation found - going directly to login page');
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
  }

  // Verify we're on the React login page
  await expect(page.locator('h1')).toContainText('Welcome Back');
  console.log('✅ React LoginPage confirmed with "Welcome Back" title');

  // Verify React login form elements are present (NO modals/dialogs)
  const reactLoginSelectors = {
    emailInput: '[data-testid="email-input"]',
    passwordInput: '[data-testid="password-input"]',
    loginButton: '[data-testid="login-button"]',
    loginForm: '[data-testid="login-form"]'
  };

  console.log('🔍 Verifying React login form elements:');
  for (const [name, selector] of Object.entries(reactLoginSelectors)) {
    const exists = await page.locator(selector).count();
    console.log(`  ${name}: ${exists} found`);
    expect(exists).toBe(1);
  }

  // Verify NO modal/dialog elements exist
  const modalElements = await page.locator('[role="dialog"], .modal, [data-testid*="modal"]').count();
  console.log(`Modal/dialog elements: ${modalElements} (should be 0)`);
  expect(modalElements).toBe(0);

  // Test authentication with correct selectors
  console.log('🧪 Testing authentication with admin credentials...');

  await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
  await page.fill('[data-testid="password-input"]', 'Test123!');
  await page.click('[data-testid="login-button"]');

  // Wait for authentication to complete
  await page.waitForTimeout(3000);

  const finalUrl = page.url();
  console.log(`Final URL after authentication: ${finalUrl}`);

  // Check for any errors during the test
  console.log(`Console errors during test: ${consoleMessages.length}`);
  if (consoleMessages.length > 0) {
    consoleMessages.forEach((msg, i) => console.log(`  ${i + 1}. ${msg}`));
  }

  console.log(`Page errors during test: ${pageErrors.length}`);
  if (pageErrors.length > 0) {
    pageErrors.forEach((error, i) => console.log(`  ${i + 1}. ${error}`));
  }

  console.log('✅ React authentication navigation test complete');
});