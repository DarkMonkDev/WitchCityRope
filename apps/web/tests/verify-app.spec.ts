import { test, expect } from '@playwright/test';

test('verify app loads on localhost:5173', async ({ page }) => {
  // Navigate to the app
  await page.goto('http://localhost:5173', { waitUntil: 'networkidle' });
  
  // Take a screenshot for debugging
  await page.screenshot({ path: 'app-state.png', fullPage: true });
  
  // Check if React root exists
  const root = await page.locator('#root');
  await expect(root).toBeVisible();
  
  // Check what's actually in the root
  const rootContent = await root.innerHTML();
  console.log('Root content:', rootContent.substring(0, 500));
  
  // Check for any console errors
  page.on('console', msg => {
    if (msg.type() === 'error') {
      console.error('Console error:', msg.text());
    }
  });
  
  // Wait a bit to see if anything loads
  await page.waitForTimeout(3000);
  
  // Check if there's a login form or any content
  const hasLoginForm = await page.locator('form').count() > 0;
  const hasAnyText = await page.locator('h1, h2, h3, p').count() > 0;
  
  console.log('Has login form:', hasLoginForm);
  console.log('Has any text content:', hasAnyText);
  
  // Try to check for events page
  await page.goto('http://localhost:5173/events', { waitUntil: 'networkidle' });
  await page.screenshot({ path: 'events-page.png', fullPage: true });
  
  const eventsContent = await page.locator('body').innerHTML();
  console.log('Events page content:', eventsContent.substring(0, 500));
});