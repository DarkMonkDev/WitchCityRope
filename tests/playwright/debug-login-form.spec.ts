import { test, expect } from '@playwright/test';

test('Debug login form modal', async ({ page }) => {
  console.log('🔍 Debugging login form modal...');

  await page.goto('http://localhost:5173');
  await page.waitForTimeout(3000);

  // Take screenshot of initial page
  await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/before-login-click.png' });

  // Click LOGIN button
  console.log('🔑 Clicking LOGIN button...');
  await page.click('text=LOGIN');
  await page.waitForTimeout(3000);

  // Take screenshot after clicking LOGIN
  await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/after-login-click.png' });

  // Check what's visible on the page
  const bodyText = await page.textContent('body');
  console.log('📋 Page content includes email input:', bodyText?.includes('email') ? 'YES' : 'NO');
  console.log('📋 Page content includes password input:', bodyText?.includes('password') ? 'YES' : 'NO');

  // Try different selectors for login form
  const emailSelectors = [
    'input[name="email"]',
    'input[type="email"]',
    'input[placeholder*="email" i]',
    '[data-testid="email"]',
    '#email'
  ];

  for (const selector of emailSelectors) {
    const exists = await page.locator(selector).count();
    console.log(`📍 Selector "${selector}": ${exists} elements found`);
  }

  // Check for any modal or form elements
  const modalCount = await page.locator('[role="dialog"], .modal, .login-modal').count();
  console.log(`📱 Modal elements found: ${modalCount}`);

  // Check for any input elements
  const inputCount = await page.locator('input').count();
  console.log(`📝 Total input elements: ${inputCount}`);

  if (inputCount > 0) {
    console.log('📝 Available input elements:');
    const inputs = await page.locator('input').all();
    for (let i = 0; i < inputs.length; i++) {
      const input = inputs[i];
      const type = await input.getAttribute('type');
      const name = await input.getAttribute('name');
      const placeholder = await input.getAttribute('placeholder');
      console.log(`  Input ${i}: type="${type}", name="${name}", placeholder="${placeholder}"`);
    }
  }
});