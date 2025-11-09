/**
 * Console Error Capture Test
 *
 * Captures exact console error messages from page load to help diagnose
 * the 3 console errors detected in footer tests.
 */

import { test } from '@playwright/test';

const BASE_URL = 'http://localhost:5173';

test('Capture all console messages on page load', async ({ page }) => {
  const consoleMessages: Array<{ type: string; text: string }> = [];

  page.on('console', (msg) => {
    consoleMessages.push({
      type: msg.type(),
      text: msg.text()
    });
  });

  await page.goto(BASE_URL);
  await page.waitForLoadState('networkidle');

  // Print all console messages
  console.log('\n========== CONSOLE MESSAGES ==========\n');

  const errors = consoleMessages.filter(m => m.type === 'error');
  const warnings = consoleMessages.filter(m => m.type === 'warning');
  const logs = consoleMessages.filter(m => m.type === 'log');

  console.log(`Total messages: ${consoleMessages.length}`);
  console.log(`Errors: ${errors.length}`);
  console.log(`Warnings: ${warnings.length}`);
  console.log(`Logs: ${logs.length}`);

  if (errors.length > 0) {
    console.log('\n========== ERRORS ==========\n');
    errors.forEach((err, index) => {
      console.log(`Error ${index + 1}:`);
      console.log(err.text);
      console.log('---');
    });
  }

  if (warnings.length > 0) {
    console.log('\n========== WARNINGS ==========\n');
    warnings.forEach((warn, index) => {
      console.log(`Warning ${index + 1}:`);
      console.log(warn.text);
      console.log('---');
    });
  }

  console.log('\n========== END CONSOLE CAPTURE ==========\n');
});
