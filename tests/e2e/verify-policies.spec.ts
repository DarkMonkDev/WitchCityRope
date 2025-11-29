import { test, expect } from '@playwright/test';

const baseUrl = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';

test('verify event policies section', async ({ page }) => {
  await page.goto(`${baseUrl}/events/756edc2a-4c53-4c47-bcfe-04a7435266a2`);

  await page.waitForLoadState('domcontentloaded');
  await page.waitForTimeout(2000);

  await page.screenshot({ path: './test-results/event-policies-test.png', fullPage: true });

  const policiesSection = await page.locator('text=Important Policies').count();
  console.log('Policies section count:', policiesSection);

  if (policiesSection > 0) {
    console.log('✅ Policies section FOUND');
  } else {
    console.log('❌ Policies section NOT FOUND');
  }
});
