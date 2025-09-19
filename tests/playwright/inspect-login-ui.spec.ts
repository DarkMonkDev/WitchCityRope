import { test, expect } from '@playwright/test';

test('inspect login UI structure', async ({ page }) => {
  // Navigate to the app
  await page.goto('http://localhost:5173');

  // Wait for page to load
  await expect(page).toHaveTitle(/Witch City Rope/);

  // Take a screenshot of the initial page
  await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/initial-page.png' });

  // Check if LOGIN button exists
  const loginButton = page.locator('text=LOGIN');
  console.log('LOGIN button visible:', await loginButton.isVisible());

  // Click the LOGIN button
  await loginButton.click();

  // Wait a moment for any modal to appear
  await page.waitForTimeout(2000);

  // Take a screenshot after clicking login
  await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/after-login-click.png' });

  // Check for various possible login modal selectors
  const possibleSelectors = [
    '[data-testid="login-modal"]',
    '.modal',
    '[role="dialog"]',
    '.login-modal',
    '#login-modal',
    'form',
    'input[name="email"]',
    'input[type="email"]',
    '[placeholder*="email" i]',
    '[placeholder*="password" i]'
  ];

  console.log('Checking possible selectors:');
  for (const selector of possibleSelectors) {
    const element = page.locator(selector);
    const isVisible = await element.isVisible();
    const count = await element.count();
    console.log(`  ${selector}: visible=${isVisible}, count=${count}`);
  }

  // Get all visible input elements
  const inputs = page.locator('input:visible');
  const inputCount = await inputs.count();
  console.log(`Visible input count: ${inputCount}`);

  for (let i = 0; i < inputCount; i++) {
    const input = inputs.nth(i);
    const type = await input.getAttribute('type');
    const name = await input.getAttribute('name');
    const placeholder = await input.getAttribute('placeholder');
    console.log(`  Input ${i}: type=${type}, name=${name}, placeholder=${placeholder}`);
  }

  // Check page content for debugging
  const pageContent = await page.content();
  console.log('Page contains "email":', pageContent.includes('email'));
  console.log('Page contains "password":', pageContent.includes('password'));
  console.log('Page contains "login":', pageContent.toLowerCase().includes('login'));

  // Log any console errors from the page
  page.on('console', msg => {
    if (msg.type() === 'error') {
      console.log('Page error:', msg.text());
    }
  });

  // Check for any error messages
  const errorElements = page.locator('.error, .alert, [role="alert"]');
  const errorCount = await errorElements.count();
  if (errorCount > 0) {
    console.log('Found error elements:', errorCount);
    for (let i = 0; i < errorCount; i++) {
      const errorText = await errorElements.nth(i).textContent();
      console.log(`  Error ${i}: ${errorText}`);
    }
  }
});