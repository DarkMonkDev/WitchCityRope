import { test } from '@playwright/test';

test('Simple admin screenshot test', async ({ page }) => {
  console.log('🔍 Taking screenshots of admin interface');

  try {
    // Go to the React app
    await page.goto('http://localhost:5173/', { timeout: 10000 });
    await page.waitForTimeout(2000);

    // Take initial screenshot
    await page.screenshot({ path: 'test-results/homepage-initial.png', fullPage: true });
    console.log('✅ Homepage screenshot taken');

    // Check if login button is visible
    const loginButton = page.locator('text=LOGIN');
    if (await loginButton.isVisible({ timeout: 5000 })) {
      console.log('✅ Login button found, clicking...');
      await loginButton.click();
      await page.waitForTimeout(1000);
      await page.screenshot({ path: 'test-results/login-modal.png', fullPage: true });
      console.log('✅ Login modal screenshot taken');
    } else {
      console.log('❌ Login button not found');
    }

  } catch (error) {
    console.log('❌ Error:', error);
    await page.screenshot({ path: 'test-results/error-state.png', fullPage: true });
  }
});