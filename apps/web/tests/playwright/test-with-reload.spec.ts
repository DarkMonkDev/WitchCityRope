import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test('vetting navigation with hard reload', async ({ page }) => {
  page.on('console', msg => console.log('BROWSER:', msg.text()));
  
  await AuthHelpers.loginAs(page, 'admin');
  await page.goto('/admin/vetting');
  
  // HARD RELOAD to clear any cached JavaScript
  await page.reload({ waitUntil: 'networkidle' });
  
  // Click first row
  const firstRow = page.locator('table tbody tr').first();
  await firstRow.click();
  await page.waitForURL(/\/admin\/vetting\/applications\/[a-f0-9-]+$/);
  await page.waitForLoadState('networkidle');
  
  // Verify we navigated to the detail page
  const currentUrl = page.url();
  console.log('Current URL after navigation:', currentUrl);

  // Check that we're on the detail page (URL should contain application ID)
  expect(currentUrl).toMatch(/\/admin\/vetting\/applications\/[a-f0-9-]+$/);
});
