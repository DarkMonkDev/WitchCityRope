import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test('check DOM after navigation', async ({ page }) => {
  page.on('console', msg => console.log('BROWSER:', msg.text()));
  
  await AuthHelpers.loginAs(page, 'admin');
  await page.goto('/admin/vetting');
  await page.waitForLoadState('networkidle');
  
  // Click first row
  const firstRow = page.locator('table tbody tr').first();
  await firstRow.click();
  await page.waitForURL(/\/admin\/vetting\/applications\/[a-f0-9-]+$/);
  await page.waitForLoadState('networkidle');
  
  // Check ALL h1 elements on the page
  const h1Count = await page.locator('h1').count();
  console.log('Number of h1 elements:', h1Count);
  
  for (let i = 0; i < h1Count; i++) {
    const text = await page.locator('h1').nth(i).textContent();
    console.log(`h1[${i}]:`, text);
  }
  
  // Check if multiple components are rendered
  const containerCount = await page.locator('body > div').count();
  console.log('Number of containers:', containerCount);
  
  // Take screenshot
  await page.screenshot({ path: 'test-results/dom-check.png', fullPage: true });
});
