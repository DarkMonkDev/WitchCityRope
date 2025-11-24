import { test, expect } from '@playwright/test';

test('verify event policies section', async ({ page }) => {
  await page.goto('http://localhost:5173/events/756edc2a-4c53-4c47-bcfe-04a7435266a2');

  await page.waitForLoadState('networkidle');
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
