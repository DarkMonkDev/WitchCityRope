import { test, expect } from '@playwright/test';
import { setupConsoleErrorFiltering } from './helpers/console.helpers';

test('Check console errors', async ({ page }) => {
  // Set up console error filtering with helper
  const { getErrors, printSummary } = setupConsoleErrorFiltering(page, {
    filter401Errors: true,
    logFilteredMessages: true,
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

  // Report findings using helper
  printSummary();

  // Take screenshot
  await page.screenshot({ path: 'test-results/console-error-check.png' });

  // Use helper to get errors
  const errors = getErrors();
  expect(errors.length).toBe(0);
});