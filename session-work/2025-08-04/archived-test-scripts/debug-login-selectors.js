const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({
    headless: false // Show browser for debugging
  });

  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    ignoreHTTPSErrors: true
  });

  const page = await context.newPage();

  try {
    console.log('Navigating to login page...');
    await page.goto('http://localhost:5651/login', {
      waitUntil: 'networkidle'
    });

    // Wait for the page to load
    await page.waitForTimeout(2000);

    // Try different selectors for email input
    const emailSelectors = [
      'input[type="email"]',
      'input[name="email"]',
      'input[id="email"]',
      '#Email',
      'input[placeholder*="email" i]',
      'input[formcontrolname="email" i]',
      '.form-control[type="email"]',
      'input.form-control'
    ];

    console.log('Checking for email input selectors:');
    for (const selector of emailSelectors) {
      const count = await page.locator(selector).count();
      if (count > 0) {
        console.log(`✓ Found ${count} element(s) with selector: ${selector}`);
        // Get more info about the first element
        const element = page.locator(selector).first();
        const attributes = await element.evaluate(el => {
          return {
            type: el.type,
            name: el.name,
            id: el.id,
            placeholder: el.placeholder,
            className: el.className
          };
        });
        console.log('  Attributes:', attributes);
      }
    }

    // Try different selectors for password input
    const passwordSelectors = [
      'input[type="password"]',
      'input[name="password"]',
      'input[id="password"]',
      '#Password',
      'input[placeholder*="password" i]',
      'input[formcontrolname="password" i]',
      '.form-control[type="password"]'
    ];

    console.log('\nChecking for password input selectors:');
    for (const selector of passwordSelectors) {
      const count = await page.locator(selector).count();
      if (count > 0) {
        console.log(`✓ Found ${count} element(s) with selector: ${selector}`);
      }
    }

    // Check for submit button
    const buttonSelectors = [
      'button[type="submit"]',
      'button:has-text("Login")',
      'button:has-text("Sign in")',
      'input[type="submit"]',
      '.btn-primary'
    ];

    console.log('\nChecking for submit button selectors:');
    for (const selector of buttonSelectors) {
      const count = await page.locator(selector).count();
      if (count > 0) {
        console.log(`✓ Found ${count} element(s) with selector: ${selector}`);
      }
    }

    console.log('\nKeeping browser open for 10 seconds for manual inspection...');
    await page.waitForTimeout(10000);

  } catch (error) {
    console.error('Error occurred:', error);
  } finally {
    await browser.close();
  }
})();