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
  await page.waitForURL(/\/admin\/vetting\/[a-f0-9-]+$/);
  await page.waitForLoadState('networkidle');
  
  const h1 = await page.locator('h1').first().textContent();
  console.log('H1 after navigation:', h1);
  
  expect(h1).not.toBe('Vetting Applications');
});
