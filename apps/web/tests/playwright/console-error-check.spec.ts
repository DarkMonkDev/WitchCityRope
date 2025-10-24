import { test, expect } from '@playwright/test';

test('Check console errors', async ({ page }) => {
  const errors: string[] = [];

  // Listen for console errors
  page.on('console', msg => {
    if (msg.type() === 'error') {
      const errorText = msg.text();

      // Filter out expected 401 errors on public pages
      // These occur when unauthenticated users visit public routes (homepage, events)
      // The app checks auth status on load, which returns 401 - this is EXPECTED behavior
      const is401Error = errorText.includes('401') || errorText.includes('Unauthorized');
      const isFailedResource = errorText.includes('Failed to load resource');

      if ((is401Error && isFailedResource) || is401Error) {
        console.log('ℹ️  Expected auth check (401):', errorText);
        return; // Skip this error - it's expected behavior on public pages
      }

      errors.push(errorText);
      console.log('❌ Console Error:', errorText);
    }
  });

  page.on('pageerror', error => {
    errors.push(error.message);
    console.log('❌ Page Error:', error.message);
  });

  // Navigate to the app
  await page.goto('http://localhost:5173');

  // Wait a bit for any errors to appear
  await page.waitForTimeout(3000);

  // Check if root element exists
  const rootExists = await page.locator('#root').count() > 0;
  console.log('Root element exists:', rootExists);

  // Check root element content
  const rootContent = await page.locator('#root').innerHTML();
  console.log('Root element content:', rootContent ? rootContent.substring(0, 100) : 'EMPTY');

  // Try to execute JavaScript in the page context
  const jsCheck = await page.evaluate(() => {
    return {
      hasReact: typeof (window as any).React !== 'undefined',
      hasReactDOM: typeof (window as any).ReactDOM !== 'undefined',
      rootElement: document.getElementById('root'),
      rootChildren: document.getElementById('root')?.children.length || 0,
      documentTitle: document.title,
      bodyText: document.body.innerText.substring(0, 100)
    };
  });

  console.log('JavaScript check:', jsCheck);

  // Report findings
  if (errors.length > 0) {
    console.log('\n=== CONSOLE ERRORS FOUND ===');
    errors.forEach((error, i) => console.log(`${i + 1}. ${error}`));
  } else {
    console.log('\n✅ No console errors found');
  }

  // Take screenshot
  await page.screenshot({ path: 'test-results/console-error-check.png' });

  expect(errors.length).toBe(0);
});