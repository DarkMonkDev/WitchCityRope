import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test('events navigation comparison', async ({ page }) => {
  page.on('console', msg => console.log('BROWSER:', msg.text()));
  
  await AuthHelpers.loginAs(page, 'admin');
  
  // Navigate to events list
  await page.goto('/admin/events');
  await page.waitForLoadState('networkidle');
  
  const urlBefore = page.url();
  console.log('Events list URL:', urlBefore);
  
  // Click first event (if any)
  const firstRow = page.locator('[data-testid="event-row"]').first();
  if (await firstRow.isVisible()) {
    console.log('Clicking first event...');
    await firstRow.click();
    await page.waitForLoadState('networkidle');
    
    const urlAfter = page.url();
    console.log('Detail URL:', urlAfter);
    console.log('URL pattern match:', /\/admin\/events\/\d+$/.test(urlAfter));
  } else {
    console.log('No events found');
  }
});
