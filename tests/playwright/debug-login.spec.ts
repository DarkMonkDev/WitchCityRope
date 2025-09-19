import { test, expect } from '@playwright/test';

test('debug login process step by step', async ({ page }) => {
  console.log('🔍 Starting login debug...');

  // Navigate to the app
  await page.goto('http://localhost:5173');
  await expect(page).toHaveTitle(/Witch City Rope/);
  console.log('✅ Navigated to home page');

  // Click LOGIN button
  await page.click('text=LOGIN');
  console.log('✅ Clicked LOGIN button');

  // Wait for login page
  await expect(page.locator('text=Welcome Back')).toBeVisible({ timeout: 10000 });
  console.log('✅ Login page loaded');

  // Fill in credentials
  await page.fill('input[placeholder="your@email.com"]', 'admin@witchcityrope.com');
  console.log('✅ Filled email');

  await page.fill('input[placeholder="Enter your password"]', 'Test123!');
  console.log('✅ Filled password');

  // Take screenshot before clicking Sign In
  await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/before-signin.png' });

  // Click the Sign In button (not submit button)
  await page.click('text=Sign In');
  console.log('✅ Clicked Sign In button');

  // Wait for some response
  await page.waitForTimeout(3000);

  // Take screenshot after clicking Sign In
  await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/after-signin.png' });

  // Check current URL
  const currentUrl = page.url();
  console.log(`Current URL: ${currentUrl}`);

  // Check if still on login page
  const stillOnLogin = await page.locator('text=Welcome Back').isVisible({ timeout: 2000 });
  console.log(`Still on login page: ${stillOnLogin}`);

  // Check for any error messages
  const errorSelectors = [
    '.error',
    '.alert-error',
    '[role="alert"]',
    '.text-red',
    '.text-danger',
    '[class*="error"]',
    'text=Invalid',
    'text=Error',
    'text=Failed'
  ];

  for (const selector of errorSelectors) {
    const errorElement = page.locator(selector);
    if (await errorElement.isVisible({ timeout: 1000 })) {
      const errorText = await errorElement.textContent();
      console.log(`Error found (${selector}): ${errorText}`);
    }
  }

  // Check console errors
  page.on('console', msg => {
    if (msg.type() === 'error') {
      console.log('Console error:', msg.text());
    }
  });

  // Check network requests
  page.on('response', response => {
    if (response.url().includes('/auth/') || response.url().includes('/login')) {
      console.log(`Network: ${response.status()} ${response.url()}`);
    }
  });

  // Wait longer to see if anything happens
  await page.waitForTimeout(5000);

  // Check final state
  const finalUrl = page.url();
  const finalOnLogin = await page.locator('text=Welcome Back').isVisible({ timeout: 2000 });

  console.log(`Final URL: ${finalUrl}`);
  console.log(`Final on login page: ${finalOnLogin}`);

  // Take final screenshot
  await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/final-state.png' });

  // Check what user-related elements are visible
  const userElements = [
    'text=Logout',
    'text=Profile',
    'text=Dashboard',
    'text=Admin',
    'text=Welcome',
    '[data-testid="user-menu"]'
  ];

  console.log('User elements visibility:');
  for (const selector of userElements) {
    const isVisible = await page.locator(selector).isVisible({ timeout: 1000 });
    console.log(`  ${selector}: ${isVisible}`);
  }
});