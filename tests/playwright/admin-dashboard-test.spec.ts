import { test, expect } from '@playwright/test';

test('Admin Dashboard Connection Investigation', async ({ page }) => {
  // Enable console logging to capture errors
  const messages: string[] = [];
  const errors: string[] = [];

  page.on('console', msg => {
    messages.push(`${msg.type()}: ${msg.text()}`);
  });

  page.on('pageerror', error => {
    errors.push(`Page Error: ${error.message}`);
  });

  console.log('🔍 Starting admin dashboard investigation...');

  // Step 1: Navigate to the React app
  await page.goto('http://localhost:5173');
  await page.waitForTimeout(2000); // Allow React to initialize

  // Check if the app loaded
  const title = await page.title();
  console.log(`📍 Page title: ${title}`);

  const rootContent = await page.locator('#root').innerHTML();
  console.log(`📍 Root element has content: ${rootContent.length > 0}`);

  // Step 2: Try to find and click login
  const loginButton = page.locator('[data-testid="login-button"]', { timeout: 5000 }).or(
    page.locator('text=LOGIN').first()
  ).or(
    page.locator('text=Sign In').first()
  );

  try {
    await loginButton.waitFor({ timeout: 5000 });
    console.log('✅ Login button found');

    await loginButton.click();
    await page.waitForTimeout(1000);

    // Step 3: Try to login as admin
    const emailField = page.locator('[data-testid="email-input"]').or(
      page.locator('input[type="email"]')
    );
    const passwordField = page.locator('[data-testid="password-input"]').or(
      page.locator('input[type="password"]')
    );

    if (await emailField.isVisible({ timeout: 2000 })) {
      console.log('✅ Email field found');
      await emailField.fill('admin@witchcityrope.com');

      if (await passwordField.isVisible({ timeout: 2000 })) {
        console.log('✅ Password field found');
        await passwordField.fill('Test123!');

        // Find submit button
        const submitButton = page.locator('[data-testid="login-submit"]').or(
          page.locator('button[type="submit"]')
        ).or(
          page.locator('text=Sign In')
        );

        if (await submitButton.isVisible({ timeout: 2000 })) {
          console.log('✅ Submit button found');
          await submitButton.click();
          await page.waitForTimeout(3000); // Wait for login processing

          // Step 4: Check if we're redirected to dashboard
          const currentUrl = page.url();
          console.log(`📍 Current URL after login: ${currentUrl}`);

          // Check for admin dashboard elements
          const adminElements = [
            'text=Dashboard',
            'text=Admin',
            'text=Events',
            'text=Members',
            '[data-testid="admin-menu"]',
            '[data-testid="dashboard-content"]'
          ];

          for (const selector of adminElements) {
            const element = page.locator(selector);
            const isVisible = await element.isVisible({ timeout: 2000 }).catch(() => false);
            console.log(`📍 ${selector} visible: ${isVisible}`);
          }

        } else {
          console.log('❌ Submit button not found');
        }
      } else {
        console.log('❌ Password field not found');
      }
    } else {
      console.log('❌ Email field not found');
    }

  } catch (error) {
    console.log(`❌ Login button not found: ${error}`);
  }

  // Step 5: Report all console messages and errors
  console.log('\n🔍 Console Messages:');
  messages.forEach(msg => console.log(`  ${msg}`));

  if (errors.length > 0) {
    console.log('\n❌ Page Errors:');
    errors.forEach(err => console.log(`  ${err}`));
  }

  // Take a screenshot for evidence
  await page.screenshot({ path: '/tmp/admin-dashboard-investigation.png', fullPage: true });
  console.log('📸 Screenshot saved to /tmp/admin-dashboard-investigation.png');
});