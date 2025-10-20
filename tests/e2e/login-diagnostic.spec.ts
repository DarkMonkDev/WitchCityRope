import { test, expect } from '@playwright/test';

test.describe('Login Diagnostic', () => {
  test('basic login page navigation and form detection', async ({ page }) => {
    console.log('🔍 Testing basic login access...');
    
    // Navigate to login page
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    console.log(`Current URL: ${page.url()}`);
    
    // Take screenshot
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/login-page-diagnostic.png' });
    
    // Check for form elements with various selectors
    const formSelectors = [
      '[data-testid="login-form"]',
      'form',
      '[data-testid="email-input"]',
      'input[type="email"]',
      'input[name="email"]',
      '[data-testid="password-input"]',
      'input[type="password"]',
      'input[name="password"]',
      '[data-testid="login-button"]',
      'button[type="submit"]',
      'button:has-text("Login")',
      'button:has-text("Sign in")'
    ];
    
    console.log('🔍 Checking for form elements...');
    for (const selector of formSelectors) {
      const count = await page.locator(selector).count();
      console.log(`${selector}: ${count} elements found`);
    }
    
    // Get page content to see what's actually rendered
    const bodyText = await page.locator('body').textContent();
    console.log(`Page body text (first 500 chars): ${bodyText?.substring(0, 500)}`);
    
    // Check for any error messages
    const errorElements = await page.locator('*:has-text("error"), *:has-text("Error"), .error, [data-testid*="error"]').count();
    console.log(`Error elements found: ${errorElements}`);
    
    // Always pass so we can see results
    expect(true).toBeTruthy();
  });

  test('manual login attempt with flexible selectors', async ({ page }) => {
    console.log('🔍 Attempting manual login with flexible selectors...');
    
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    // Try multiple email field strategies
    const emailSelectors = [
      '[data-testid="email-input"]',
      'input[type="email"]',
      'input[name="email"]',
      'input[placeholder*="email" i]',
      'input[placeholder*="Email" i]'
    ];
    
    let emailInput = null;
    for (const selector of emailSelectors) {
      const element = page.locator(selector).first();
      if (await element.count() > 0) {
        emailInput = element;
        console.log(`✅ Found email input with selector: ${selector}`);
        break;
      }
    }
    
    // Try multiple password field strategies
    const passwordSelectors = [
      '[data-testid="password-input"]',
      'input[type="password"]',
      'input[name="password"]',
      'input[placeholder*="password" i]',
      'input[placeholder*="Password" i]'
    ];
    
    let passwordInput = null;
    for (const selector of passwordSelectors) {
      const element = page.locator(selector).first();
      if (await element.count() > 0) {
        passwordInput = element;
        console.log(`✅ Found password input with selector: ${selector}`);
        break;
      }
    }
    
    // Try multiple login button strategies
    const buttonSelectors = [
      '[data-testid="login-button"]',
      'button[type="submit"]',
      'button:has-text("Login")',
      'button:has-text("Sign in")',
      'button:has-text("Log in")',
      'input[type="submit"]'
    ];
    
    let loginButton = null;
    for (const selector of buttonSelectors) {
      const element = page.locator(selector).first();
      if (await element.count() > 0) {
        loginButton = element;
        console.log(`✅ Found login button with selector: ${selector}`);
        break;
      }
    }
    
    // Attempt login if all elements found
    if (emailInput && passwordInput && loginButton) {
      console.log('🚀 Attempting login...');
      
      await emailInput.fill('admin@witchcityrope.com');
      await passwordInput.fill('Test123!');
      
      // Verify values were set
      const emailValue = await emailInput.inputValue();
      const passwordValue = await passwordInput.inputValue();
      console.log(`Email value set: ${emailValue}`);
      console.log(`Password value set: ${passwordValue.length > 0 ? '[FILLED]' : '[EMPTY]'}`);
      
      // Click login
      await loginButton.click();
      
      // Wait a bit and check what happened
      await page.waitForTimeout(3000);
      console.log(`URL after login attempt: ${page.url()}`);
      
      // Take screenshot after login attempt
      await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/post-login-diagnostic.png' });
      
    } else {
      console.log('❌ Could not find all required form elements');
      console.log(`Email input found: ${emailInput !== null}`);
      console.log(`Password input found: ${passwordInput !== null}`);
      console.log(`Login button found: ${loginButton !== null}`);
    }
    
    // Always pass for diagnostic
    expect(true).toBeTruthy();
  });

  test('check if React app is actually rendering', async ({ page }) => {
    console.log('🔍 Checking React app rendering...');
    
    await page.goto('http://localhost:5173');
    await page.waitForLoadState('networkidle');
    
    // Check if React is loaded
    const reactElements = await page.locator('#root').count();
    console.log(`React root elements: ${reactElements}`);
    
    const rootContent = await page.locator('#root').textContent();
    console.log(`Root content length: ${rootContent?.length || 0}`);
    
    if (rootContent && rootContent.length > 0) {
      console.log(`Root content preview: ${rootContent.substring(0, 200)}`);
    }
    
    // Check for common React indicators
    const reactIndicators = [
      'div[data-reactroot]',
      '[data-testid]',
      '.mantine-',
      'div:has([data-testid])'
    ];
    
    for (const indicator of reactIndicators) {
      const count = await page.locator(indicator).count();
      console.log(`${indicator}: ${count} elements found`);
    }
    
    // Take screenshot
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/react-app-diagnostic.png' });
    
    expect(true).toBeTruthy();
  });
});