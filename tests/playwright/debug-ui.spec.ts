import { test, expect } from '@playwright/test';

test('Debug UI State', async ({ page }) => {
  console.log('=== DEBUGGING UI STATE ===');

  // Navigate to app
  await page.goto('http://localhost:5173');
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(3000);

  // Check what's actually on the page
  const title = await page.title();
  console.log(`Page title: "${title}"`);

  // Check if LOGIN button exists and is visible
  const loginButton = page.locator('text=LOGIN');
  const loginExists = await loginButton.count();
  const loginVisible = await loginButton.isVisible().catch(() => false);
  console.log(`LOGIN button - exists: ${loginExists > 0}, visible: ${loginVisible}`);

  if (loginExists > 0) {
    console.log('LOGIN button found, attempting to click...');
    await loginButton.click();
    await page.waitForTimeout(2000);

    // Check for login form elements
    const emailInput = page.locator('input[name="email"]');
    const emailExists = await emailInput.count();
    const emailVisible = await emailInput.isVisible().catch(() => false);
    console.log(`Email input - exists: ${emailExists > 0}, visible: ${emailVisible}`);

    // Check for any modal or form containers
    const modals = await page.locator('[role="dialog"], .modal, [data-testid*="modal"]').count();
    console.log(`Modal containers found: ${modals}`);

    // Check for any login-related elements
    const loginElements = await page.locator('*:has-text("email"), *:has-text("password"), *:has-text("Email"), *:has-text("Password")').count();
    console.log(`Login-related elements found: ${loginElements}`);
  }

  // Check overall page state
  const bodyText = await page.locator('body').textContent();
  const bodyLength = bodyText?.length || 0;
  console.log(`Page body text length: ${bodyLength} characters`);

  if (bodyLength < 100) {
    console.log(`Page body content: "${bodyText}"`);
  } else {
    console.log(`Page body preview: "${bodyText?.substring(0, 200)}..."`);
  }

  // Check for React app mounting
  const rootContent = await page.locator('#root').innerHTML();
  const rootHasContent = rootContent.length > 0;
  console.log(`React root has content: ${rootHasContent} (${rootContent.length} chars)`);

  if (!rootHasContent) {
    console.log('❌ React app appears to not be mounted properly');
  }

  // Always pass - we're just debugging
  expect(true).toBe(true);
});