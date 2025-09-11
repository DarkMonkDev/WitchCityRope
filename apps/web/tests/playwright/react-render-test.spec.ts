import { test, expect } from '@playwright/test';

test('React app renders', async ({ page }) => {
  // Navigate to test page
  await page.goto('http://localhost:5173/test');
  
  // Wait for any content
  await page.waitForTimeout(2000);
  
  // Check if React rendered
  const heading = await page.locator('h1').first();
  const isVisible = await heading.isVisible().catch(() => false);
  
  if (isVisible) {
    console.log('✅ React is rendering! Found heading:', await heading.textContent());
  } else {
    console.log('❌ React is NOT rendering - page is blank');
    
    // Check for any content in body
    const bodyText = await page.locator('body').textContent();
    console.log('Body content:', bodyText?.substring(0, 200));
    
    // Check console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.log('Console error:', msg.text());
      }
    });
    
    // Take screenshot for debugging
    await page.screenshot({ path: 'test-results/react-render-issue.png' });
  }
  
  expect(isVisible).toBe(true);
});