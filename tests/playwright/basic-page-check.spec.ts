import { test, expect } from '@playwright/test';

test('Basic page load and screenshot', async ({ page }) => {
  console.log('🔍 Checking basic page load...');

  // Navigate to React app
  await page.goto('http://localhost:5173');
  console.log('📍 Navigated to React app');

  // Wait for page to load
  await page.waitForTimeout(3000);

  // Take screenshot
  await page.screenshot({ path: '/tmp/basic-page-screenshot.png', fullPage: true });
  console.log('📸 Screenshot saved');

  // Get page content
  const content = await page.content();
  console.log('📋 Page content length:', content.length);

  // Check what's in the root element
  const rootContent = await page.locator('#root').textContent();
  console.log('📋 Root element content:', rootContent ? rootContent.substring(0, 200) + '...' : 'Empty');

  // Check for any visible text
  const visibleText = await page.locator('body').textContent();
  console.log('📋 Visible text:', visibleText ? visibleText.substring(0, 200) + '...' : 'No visible text');

  // Check console errors
  const errors = [];
  page.on('console', msg => {
    if (msg.type() === 'error') {
      errors.push(msg.text());
    }
  });

  console.log('🔍 Console errors:', errors);
});