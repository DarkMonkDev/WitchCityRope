import { test, expect } from '@playwright/test';

test.describe('Debug Homepage', () => {
  test('Check homepage login button', async ({ page }) => {
    console.log('🔍 Debugging homepage...');

    await page.goto('http://localhost:5173/');
    await page.waitForLoadState('networkidle');

    // Take screenshot
    await page.screenshot({ path: 'test-results/homepage-debug.png', fullPage: true });

    // Check for login buttons with various selectors
    const loginButtons = [
      '[data-testid="login-button"]',
      'button:has-text("LOGIN")',
      'button:has-text("Login")',
      'button:has-text("Sign In")',
      '.login',
      '[class*="login"]',
      'a[href*="login"]'
    ];

    for (const selector of loginButtons) {
      const count = await page.locator(selector).count();
      const visible = count > 0 ? await page.locator(selector).first().isVisible() : false;
      console.log(`🔍 Selector "${selector}": ${count} found, visible: ${visible}`);
    }

    // Get all buttons
    const allButtons = await page.locator('button').all();
    console.log(`📝 Total buttons found: ${allButtons.length}`);

    for (let i = 0; i < Math.min(allButtons.length, 10); i++) {
      const button = allButtons[i];
      const text = await button.textContent();
      const visible = await button.isVisible();
      console.log(`   Button ${i}: "${text}" (visible: ${visible})`);
    }

    // Check page content
    const bodyText = await page.textContent('body');
    const hasLogin = bodyText?.toLowerCase().includes('login') || false;
    const hasSignIn = bodyText?.toLowerCase().includes('sign in') || false;

    console.log(`📋 Page contains:`);
    console.log(`   - "login": ${hasLogin}`);
    console.log(`   - "sign in": ${hasSignIn}`);
  });
});