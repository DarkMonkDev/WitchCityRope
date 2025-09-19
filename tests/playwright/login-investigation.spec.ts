import { test, expect } from '@playwright/test';

test('LOGIN Button Investigation', async ({ page }) => {
  console.log('=== INVESTIGATING LOGIN BUTTON BEHAVIOR ===');

  const consoleMessages: string[] = [];
  const pageErrors: string[] = [];

  // Capture console and errors
  page.on('console', (msg) => {
    if (msg.type() === 'error') {
      consoleMessages.push(`Console Error: ${msg.text()}`);
    }
  });

  page.on('pageerror', (error) => {
    pageErrors.push(`Page Error: ${error.message}`);
  });

  // Navigate to app
  await page.goto('http://localhost:5173');
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(2000);

  console.log('✅ Page loaded successfully');

  // Look for LOGIN button with multiple selectors
  const loginSelectors = [
    'text=LOGIN',
    'button:has-text("LOGIN")',
    '[data-testid="login-button"]',
    'a:has-text("LOGIN")',
    '.login-button',
    'button[type="button"]:has-text("LOGIN")'
  ];

  let loginButton = null;
  let usedSelector = '';

  for (const selector of loginSelectors) {
    const element = page.locator(selector);
    const count = await element.count();
    if (count > 0) {
      loginButton = element.first();
      usedSelector = selector;
      console.log(`Found LOGIN button with selector: ${selector}`);
      break;
    }
  }

  if (!loginButton) {
    console.log('❌ No LOGIN button found with any selector');
    return;
  }

  // Get button details before clicking
  const buttonText = await loginButton.textContent();
  const buttonTag = await loginButton.evaluate(el => el.tagName);
  const buttonType = await loginButton.getAttribute('type');
  const buttonClass = await loginButton.getAttribute('class');
  const buttonHref = await loginButton.getAttribute('href');

  console.log(`Button details:`);
  console.log(`  Text: "${buttonText}"`);
  console.log(`  Tag: ${buttonTag}`);
  console.log(`  Type: ${buttonType}`);
  console.log(`  Class: ${buttonClass}`);
  console.log(`  Href: ${buttonHref}`);

  // Take screenshot before click
  await page.screenshot({ path: 'before-login-click.png' });

  console.log('🖱️ Clicking LOGIN button...');
  await loginButton.click();
  await page.waitForTimeout(3000);

  // Take screenshot after click
  await page.screenshot({ path: 'after-login-click.png' });

  // Check what changed after clicking
  console.log('Checking page state after LOGIN click...');

  // Look for any form elements that might have appeared
  const formElements = await page.locator('form, input, [role="dialog"], .modal, [data-testid*="modal"], [data-testid*="login"]').count();
  console.log(`Form elements after click: ${formElements}`);

  if (formElements > 0) {
    const forms = await page.locator('form').count();
    const inputs = await page.locator('input').count();
    const dialogs = await page.locator('[role="dialog"]').count();
    const modals = await page.locator('.modal, [data-testid*="modal"]').count();

    console.log(`  Forms: ${forms}`);
    console.log(`  Inputs: ${inputs}`);
    console.log(`  Dialogs: ${dialogs}`);
    console.log(`  Modals: ${modals}`);

    // List all input elements
    const allInputs = await page.locator('input').all();
    for (let i = 0; i < allInputs.length; i++) {
      const input = allInputs[i];
      const inputName = await input.getAttribute('name');
      const inputType = await input.getAttribute('type');
      const inputPlaceholder = await input.getAttribute('placeholder');
      const inputVisible = await input.isVisible();
      console.log(`  Input ${i + 1}: name="${inputName}", type="${inputType}", placeholder="${inputPlaceholder}", visible=${inputVisible}`);
    }
  }

  // Check URL changes
  const currentUrl = page.url();
  console.log(`Current URL: ${currentUrl}`);

  // Check for navigation changes
  const bodyTextAfter = await page.locator('body').textContent();
  const newContentLength = bodyTextAfter?.length || 0;
  console.log(`Page content length after click: ${newContentLength} characters`);

  // Check specifically for login form indicators
  const emailFields = await page.locator('input[type="email"], input[name="email"], input[placeholder*="email" i]').count();
  const passwordFields = await page.locator('input[type="password"], input[name="password"], input[placeholder*="password" i]').count();
  const submitButtons = await page.locator('button[type="submit"], input[type="submit"]').count();

  console.log(`Login form elements found:`);
  console.log(`  Email fields: ${emailFields}`);
  console.log(`  Password fields: ${passwordFields}`);
  console.log(`  Submit buttons: ${submitButtons}`);

  // Check for any errors that occurred during click
  console.log(`Console errors during test: ${consoleMessages.length}`);
  consoleMessages.forEach((msg, i) => console.log(`  ${i + 1}. ${msg}`));

  console.log(`Page errors during test: ${pageErrors.length}`);
  pageErrors.forEach((error, i) => console.log(`  ${i + 1}. ${error}`));

  // Final determination
  if (emailFields > 0 && passwordFields > 0) {
    console.log('✅ LOGIN click opened a login form');
  } else if (currentUrl !== 'http://localhost:5173/') {
    console.log('✅ LOGIN click caused navigation');
  } else {
    console.log('❌ LOGIN click had no visible effect');
  }

  // Always pass - we're investigating
  expect(true).toBe(true);
});