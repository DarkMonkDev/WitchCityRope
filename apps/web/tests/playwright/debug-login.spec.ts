import { test, expect } from '@playwright/test';

test('debug login issue', async ({ page }) => {
  // Listen for console errors
  const errors: string[] = [];
  page.on('console', msg => {
    if (msg.type() === 'error') {
      errors.push(msg.text());
    }
  });

  // Listen for page errors
  page.on('pageerror', error => {
    errors.push(`Page error: ${error.message}`);
  });

  // Listen for network requests and responses
  const requests: any[] = [];
  page.on('request', request => {
    requests.push({
      method: request.method(),
      url: request.url(),
      headers: request.headers()
    });
  });

  const responses: any[] = [];
  page.on('response', response => {
    responses.push({
      status: response.status(),
      url: response.url(),
      headers: response.headers()
    });
  });

  console.log('🔍 Starting login debug test...');
  
  // Navigate to login
  console.log('📍 Navigating to login page...');
  await page.goto('http://localhost:5174/login');
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/before-login.png' });
  
  console.log('📍 Current URL after navigation:', page.url());

  // Check what form elements are actually available
  console.log('🔍 Analyzing form elements on page...');
  const allInputs = page.locator('input');
  const inputCount = await allInputs.count();
  console.log(`Found ${inputCount} input elements`);
  
  for (let i = 0; i < inputCount; i++) {
    const input = allInputs.nth(i);
    const type = await input.getAttribute('type');
    const name = await input.getAttribute('name');
    const id = await input.getAttribute('id');
    const placeholder = await input.getAttribute('placeholder');
    const testId = await input.getAttribute('data-testid');
    console.log(`Input ${i}: type="${type}", name="${name}", id="${id}", placeholder="${placeholder}", data-testid="${testId}"`);
  }

  // Try different selectors for email and password fields
  let emailInput;
  let passwordInput;
  
  console.log('🔍 Looking for email field...');
  // Try different selectors
  const emailSelectors = [
    'input[type="email"]',
    'input[name="email"]',
    'input[placeholder*="email" i]',
    '[data-testid="email"]',
    '[data-testid="email-input"]',
    'input:nth-of-type(1)'  // First input field
  ];
  
  for (const selector of emailSelectors) {
    const element = page.locator(selector);
    if (await element.count() > 0) {
      console.log(`✅ Found email field with selector: ${selector}`);
      emailInput = element.first();
      break;
    }
  }
  
  console.log('🔍 Looking for password field...');
  const passwordSelectors = [
    'input[type="password"]',
    'input[name="password"]',
    'input[placeholder*="password" i]',
    '[data-testid="password"]',
    '[data-testid="password-input"]',
    'input:nth-of-type(2)'  // Second input field
  ];
  
  for (const selector of passwordSelectors) {
    const element = page.locator(selector);
    if (await element.count() > 0) {
      console.log(`✅ Found password field with selector: ${selector}`);
      passwordInput = element.first();
      break;
    }
  }

  if (emailInput && passwordInput) {
    console.log('📝 Filling in credentials...');
    await emailInput.fill('admin@witchcityrope.com');
    await passwordInput.fill('Test123!');

    // Look for submit button
    console.log('🔍 Looking for submit button...');
    const submitSelectors = [
      'button[type="submit"]',
      'button:has-text("Login")',
      'button:has-text("Sign In")',
      '[data-testid="login-button"]',
      '[data-testid="submit"]',
      'form button'
    ];
    
    let submitButton;
    for (const selector of submitSelectors) {
      const element = page.locator(selector);
      if (await element.count() > 0) {
        console.log(`✅ Found submit button with selector: ${selector}`);
        submitButton = element.first();
        break;
      }
    }

    if (submitButton) {
      // Intercept the login API call
      console.log('🔍 Setting up API call interception...');
      const responsePromise = page.waitForResponse(
        response => response.url().includes('/auth/login') || response.url().includes('/api/auth'),
        { timeout: 10000 }
      ).catch(() => null);

      // Click login
      console.log('🔘 Clicking login button...');
      await submitButton.click();

      // Wait for response
      console.log('⏳ Waiting for API response...');
      const response = await responsePromise;
      if (response) {
        console.log('✅ API Status:', response.status());
        try {
          const body = await response.json();
          console.log('📝 API Response:', JSON.stringify(body, null, 2));
        } catch (e) {
          console.log('📝 API Response (text):', await response.text().catch(() => 'Could not read response'));
        }
      } else {
        console.log('❌ No API response intercepted');
      }
    } else {
      console.log('❌ Could not find submit button');
    }
  } else {
    console.log('❌ Could not find email or password fields');
    console.log(`Email field found: ${!!emailInput}`);
    console.log(`Password field found: ${!!passwordInput}`);
  }

  // Wait a bit for any errors
  await page.waitForTimeout(2000);
  await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/after-login.png' });

  // Report errors
  if (errors.length > 0) {
    console.log('🚨 Console errors found:');
    errors.forEach(err => console.log('  -', err));
  } else {
    console.log('✅ No console errors detected');
  }

  // Check current URL
  console.log('📍 Current URL after login attempt:', page.url());

  // Log relevant network activity
  console.log('🌐 Auth-related network requests:');
  requests.filter(req => req.url.includes('auth') || req.url.includes('login')).forEach(req => {
    console.log(`  ${req.method} ${req.url}`);
  });

  console.log('📡 Auth-related network responses:');
  responses.filter(res => res.url.includes('auth') || res.url.includes('login')).forEach(res => {
    console.log(`  ${res.status} ${res.url}`);
  });

  // Check for any error messages on the page
  const errorElements = page.locator('[role="alert"], .error, .alert, .notification, .mantine-notification');
  const errorCount = await errorElements.count();
  console.log('🚨 Error elements on page:', errorCount);
  if (errorCount > 0) {
    for (let i = 0; i < errorCount; i++) {
      const errorText = await errorElements.nth(i).textContent();
      console.log(`  Error ${i + 1}: ${errorText}`);
    }
  }

  // Final assessment
  console.log('📊 Debug Summary:');
  console.log(`  - Console errors: ${errors.length}`);
  console.log(`  - Current URL: ${page.url()}`);
  console.log(`  - Form elements found: ${!!emailInput && !!passwordInput}`);
});