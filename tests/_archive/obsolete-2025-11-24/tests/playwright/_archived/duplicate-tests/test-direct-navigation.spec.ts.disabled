import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test('direct navigation to detail page', async ({ page }) => {
  page.on('console', msg => console.log('BROWSER:', msg.text()));
  
  await AuthHelpers.loginAs(page, 'admin');
  
  // Navigate DIRECTLY to a detail page URL
  await page.goto('/admin/vetting/test-id-12345');
  await page.waitForLoadState('networkidle');
  
  console.log('URL:', page.url());
  await page.screenshot({ path: 'test-results/direct-navigation.png', fullPage: true });
  
  // Check what's on the page
  const title = await page.locator('h1').first().textContent();
  console.log('Page title:', title);
  
  const hasTable = await page.locator('table').count();
  console.log('Has table:', hasTable > 0);
});
