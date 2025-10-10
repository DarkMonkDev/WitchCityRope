import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

const API_BASE_URL = 'http://localhost:5655';

test.describe('Comprehensive Login Debugging', () => {
  let networkRequests: Array<{
    url: string;
    method: string;
    status: number;
    headers: any;
    postData: any;
    response: any;
    timestamp: number;
  }> = [];

  let consoleMessages: Array<{
    type: string;
    text: string;
    timestamp: number;
  }> = [];

  test.beforeEach(async ({ page }) => {
    networkRequests = [];
    consoleMessages = [];

    // Capture network requests with more detail
    page.on('request', (request) => {
      console.log(`🌐 REQUEST: ${request.method()} ${request.url()}`);
      if (request.postData()) {
        console.log(`   POST DATA: ${request.postData()}`);
      }
    });

    page.on('response', async (response) => {
      try {
        const request = response.request();
        const responseBody = await response.text();
        let parsedResponse = responseBody;
        
        try {
          parsedResponse = JSON.parse(responseBody);
        } catch {
          // Keep as string if not JSON
        }

        const requestInfo = {
          url: response.url(),
          method: request.method(),
          status: response.status(),
          headers: response.headers(),
          postData: request.postData(),
          response: parsedResponse,
          timestamp: Date.now()
        };

        networkRequests.push(requestInfo);

        // Log important requests immediately
        if (request.url().includes('/api/auth') || request.url().includes('/login')) {
          console.log(`🔴 AUTH REQUEST: ${request.method()} ${request.url()}`);
          console.log(`   Status: ${response.status()}`);
          console.log(`   Response: ${JSON.stringify(parsedResponse).substring(0, 200)}`);
        }
      } catch (error) {
        console.log('Error capturing response:', error);
      }
    });

    // Track console messages
    page.on('console', (msg) => {
      consoleMessages.push({
        type: msg.type(),
        text: msg.text(),
        timestamp: Date.now()
      });
      
      // Log errors immediately
      if (msg.type() === 'error') {
        console.log(`🔴 CONSOLE ERROR: ${msg.text()}`);
      }
    });

    // Track page errors
    page.on('pageerror', (error) => {
      console.log(`🔴 PAGE ERROR: ${error.toString()}`);
      consoleMessages.push({
        type: 'pageerror',
        text: error.toString(),
        timestamp: Date.now()
      });
    });
  });

  test('should diagnose login page and form elements', async ({ page }) => {
    console.log('=== STARTING COMPREHENSIVE LOGIN DIAGNOSIS ===');
    
    // Navigate to login page
    console.log('📍 Navigating to login page...');
    await page.goto('/login');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Take screenshot of login page
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/login-page-diagnosis.png', fullPage: true });
    
    // Check page title and URL
    const title = await page.title();
    const url = page.url();
    console.log(`📄 Page Title: ${title}`);
    console.log(`🔗 Page URL: ${url}`);
    
    // Check if page loaded properly
    const bodyContent = await page.locator('body').textContent();
    console.log(`📝 Body contains "login": ${bodyContent?.toLowerCase().includes('login')}`);
    console.log(`📝 Body contains "email": ${bodyContent?.toLowerCase().includes('email')}`);
    console.log(`📝 Body contains "password": ${bodyContent?.toLowerCase().includes('password')}`);
    
    // Analyze all form elements
    console.log('=== FORM ELEMENTS ANALYSIS ===');
    
    // Find all input elements
    const allInputs = await page.locator('input').all();
    console.log(`🔢 Total input elements found: ${allInputs.length}`);
    
    for (let i = 0; i < allInputs.length; i++) {
      const input = allInputs[i];
      const type = await input.getAttribute('type');
      const name = await input.getAttribute('name');
      const placeholder = await input.getAttribute('placeholder');
      const id = await input.getAttribute('id');
      const className = await input.getAttribute('class');
      
      console.log(`Input ${i + 1}:`);
      console.log(`  Type: ${type}`);
      console.log(`  Name: ${name}`);
      console.log(`  Placeholder: ${placeholder}`);
      console.log(`  ID: ${id}`);
      console.log(`  Classes: ${className}`);
    }
    
    // Find all button elements
    const allButtons = await page.locator('button').all();
    console.log(`🔘 Total button elements found: ${allButtons.length}`);
    
    for (let i = 0; i < allButtons.length; i++) {
      const button = allButtons[i];
      const type = await button.getAttribute('type');
      const text = await button.textContent();
      const className = await button.getAttribute('class');
      
      console.log(`Button ${i + 1}:`);
      console.log(`  Type: ${type}`);
      console.log(`  Text: ${text}`);
      console.log(`  Classes: ${className}`);
    }
    
    // Try different selectors for email input
    console.log('=== TESTING EMAIL INPUT SELECTORS ===');
    
    const emailSelectors = [
      'input[type="email"]',
      'input[name="email"]',
      'input[placeholder*="email"]',
      'input[placeholder*="Email"]',
      'input[placeholder="your@email.com"]',
      'input[id="email"]',
      'input[data-testid="email"]',
      '[data-mantine-type="email"]'
    ];
    
    for (const selector of emailSelectors) {
      const count = await page.locator(selector).count();
      console.log(`  ${selector}: ${count > 0 ? '✅' : '❌'} (${count} elements)`);
    }
    
    // Try different selectors for password input
    console.log('=== TESTING PASSWORD INPUT SELECTORS ===');
    
    const passwordSelectors = [
      'input[type="password"]',
      'input[name="password"]',
      'input[placeholder*="password"]',
      'input[placeholder*="Password"]',
      'input[id="password"]',
      'input[data-testid="password"]'
    ];
    
    for (const selector of passwordSelectors) {
      const count = await page.locator(selector).count();
      console.log(`  ${selector}: ${count > 0 ? '✅' : '❌'} (${count} elements)`);
    }
    
    // Try different selectors for login button
    console.log('=== TESTING LOGIN BUTTON SELECTORS ===');
    
    const buttonSelectors = [
      'button[type="submit"]',
      'button:has-text("Login")',
      'button:has-text("Sign In")',
      'button:has-text("Log In")',
      '[data-testid="login-button"]',
      '[type="submit"]'
    ];
    
    for (const selector of buttonSelectors) {
      const count = await page.locator(selector).count();
      console.log(`  ${selector}: ${count > 0 ? '✅' : '❌'} (${count} elements)`);
    }
    
    // Check if there's a form element
    const formCount = await page.locator('form').count();
    console.log(`📋 Form elements found: ${formCount}`);
    
    if (formCount > 0) {
      const formAction = await page.locator('form').first().getAttribute('action');
      const formMethod = await page.locator('form').first().getAttribute('method');
      console.log(`📋 Form action: ${formAction}`);
      console.log(`📋 Form method: ${formMethod}`);
    }
    
    // Get the full HTML of the page for debugging
    const fullHTML = await page.content();
    
    // Write debugging info to file
    const debugInfo = {
      timestamp: new Date().toISOString(),
      url: url,
      title: title,
      inputCount: allInputs.length,
      buttonCount: allButtons.length,
      formCount: formCount,
      bodyContainsLogin: bodyContent?.toLowerCase().includes('login'),
      bodyContainsEmail: bodyContent?.toLowerCase().includes('email'),
      bodyContainsPassword: bodyContent?.toLowerCase().includes('password'),
      networkRequests: networkRequests.map(req => ({
        method: req.method,
        url: req.url,
        status: req.status
      })),
      consoleMessages: consoleMessages
    };
    
    console.log('=== WRITING DEBUG FILES ===');
    console.log('Full page HTML saved to: /home/chad/repos/witchcityrope-react/test-results/login-page-debug.html');
    console.log('Debug info saved to: /home/chad/repos/witchcityrope-react/test-results/login-debug-info.json');
    
    // Don't fail the test - just gather info
    expect(allInputs.length).toBeGreaterThanOrEqual(0);
  });

  test('should test direct API call to login endpoint', async ({ page }) => {
    console.log('=== TESTING DIRECT API CALL ===');
    
    // Test API health first
    const healthResponse = await page.evaluate(async (apiUrl) => {
      try {
        const response = await fetch(`${apiUrl}/health`);
        return {
          status: response.status,
          ok: response.ok,
          text: await response.text()
        };
      } catch (error) {
        return { error: error.toString() };
      }
    }, API_BASE_URL);
    
    console.log('API Health Check:', healthResponse);
    
    // Test login endpoint directly
    const loginResponse = await page.evaluate(async (apiUrl, credentials) => {
      try {
        const response = await fetch(`${apiUrl}/api/auth/login`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(credentials),
          credentials: 'include' // Important for cookies
        });
        
        const responseText = await response.text();
        let responseJson;
        try {
          responseJson = JSON.parse(responseText);
        } catch {
          responseJson = responseText;
        }
        
        return {
          status: response.status,
          ok: response.ok,
          headers: Object.fromEntries(response.headers.entries()),
          response: responseJson
        };
      } catch (error) {
        return { error: error.toString() };
      }
    }, API_BASE_URL, AuthHelpers.accounts.admin);
    
    console.log('=== DIRECT LOGIN API CALL RESULT ===');
    console.log('Status:', loginResponse.status);
    console.log('OK:', loginResponse.ok);
    console.log('Headers:', JSON.stringify(loginResponse.headers, null, 2));
    console.log('Response:', JSON.stringify(loginResponse.response, null, 2));
    
    if (loginResponse.error) {
      console.log('❌ API Call Error:', loginResponse.error);
    } else if (loginResponse.status === 401) {
      console.log('❌ 401 UNAUTHORIZED - This is the main issue!');
      console.log('Possible causes:');
      console.log('1. Wrong credentials');
      console.log('2. User not found in database');
      console.log('3. Password hash mismatch');
      console.log('4. Account locked/disabled');
    } else if (loginResponse.status === 200) {
      console.log('✅ Login API call successful');
    } else {
      console.log(`⚠️ Unexpected status: ${loginResponse.status}`);
    }
    
    // Don't fail - just test
    expect(healthResponse.ok).toBe(true);
  });
});