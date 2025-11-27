import { test, expect, Page } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

test.describe('Login 401 Investigation', () => {
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

    // Capture network requests with full details
    page.on('request', (request) => {
      if (request.url().includes('/api/auth') || request.url().includes('/login')) {
        console.log(`🌐 REQUEST: ${request.method()} ${request.url()}`);
        if (request.postData()) {
          console.log(`   POST DATA: ${request.postData()}`);
        }
        console.log(`   HEADERS: ${JSON.stringify(request.headers())}`);
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

        // Log auth requests immediately
        if (request.url().includes('/api/auth') || request.url().includes('/login')) {
          console.log(`🔴 RESPONSE: ${request.method()} ${request.url()}`);
          console.log(`   Status: ${response.status()}`);
          console.log(`   Headers: ${JSON.stringify(response.headers())}`);
          console.log(`   Response: ${JSON.stringify(parsedResponse)}`);
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
    });

    page.on('pageerror', (error) => {
      consoleMessages.push({
        type: 'pageerror',
        text: error.toString(),
        timestamp: Date.now()
      });
    });
  });

  test('should test direct API login call to identify 401 cause', async ({ page }) => {
    console.log('=== TESTING DIRECT API LOGIN ===');
    console.log(`Testing login for: ${AuthHelpers.accounts.admin.email}`);
    
    // Test API health first
    const healthResponse = await page.evaluate(async () => {
      try {
        const response = await fetch('/health');
        return {
          status: response.status,
          ok: response.ok,
          text: await response.text()
        };
      } catch (error) {
        return { error: error.toString() };
      }
    });
    
    console.log('API Health Check:', healthResponse);
    
    // Test login endpoint directly
    const loginResponse = await page.evaluate(async (credentials) => {
      try {
        const response = await fetch('/api/auth/login', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(credentials),
          credentials: 'include'
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
    }, AuthHelpers.accounts.admin);
    
    console.log('=== DIRECT LOGIN API CALL RESULT ===');
    console.log('Status:', loginResponse.status);
    console.log('OK:', loginResponse.ok);
    console.log('Headers:', JSON.stringify(loginResponse.headers, null, 2));
    console.log('Response:', JSON.stringify(loginResponse.response, null, 2));
    
    if (loginResponse.error) {
      console.log('❌ API Call Error:', loginResponse.error);
    } else if (loginResponse.status === 401) {
      console.log('❌ 401 UNAUTHORIZED - FOUND THE ISSUE!');
      console.log('This confirms the API is returning 401 for these credentials');
      console.log('Response body:', loginResponse.response);
    } else if (loginResponse.status === 200) {
      console.log('✅ Login API call successful');
    } else {
      console.log(`⚠️ Unexpected status: ${loginResponse.status}`);
    }
    
    expect(healthResponse.ok).toBe(true);
  });

  test('should test login flow through the UI with correct selectors', async ({ page }) => {
    console.log('=== TESTING LOGIN THROUGH UI ===');
    
    // Navigate to login page
    await page.goto('/login');
    await page.waitForLoadState('networkidle');
    
    // Take screenshot before filling
    await page.screenshot({
      path: './test-results/login-before-fill.png',
      fullPage: true
    });
    
    // Use the correct selectors we discovered
    const emailInput = page.locator('input[placeholder="your@email.com"]');
    const passwordInput = page.locator('input[type="password"]');
    const loginButton = page.locator('button[type="submit"]:has-text("Login")');
    
    // Verify elements are visible
    await expect(emailInput).toBeVisible();
    await expect(passwordInput).toBeVisible();
    await expect(loginButton).toBeVisible();
    
    console.log('✅ All form elements found and visible');

    // Login using AuthHelpers
    await AuthHelpers.loginAs(page, 'admin');

    console.log(`✅ Login attempt completed`);
    
    // Take screenshot after filling
    await page.screenshot({
      path: './test-results/login-after-fill.png',
      fullPage: true
    });
    
    // Clear previous network requests
    networkRequests.length = 0;
    
    // Click login button
    console.log('🔘 Clicking login button...');
    await loginButton.click();
    
    // Wait for network request to complete
    await page.waitForTimeout(3000);
    
    // Take screenshot after click
    await page.screenshot({
      path: './test-results/login-after-click.png',
      fullPage: true
    });
    
    console.log('=== NETWORK REQUESTS AFTER LOGIN CLICK ===');
    networkRequests.forEach((req, index) => {
      console.log(`${index + 1}. ${req.method} ${req.url}`);
      console.log(`   Status: ${req.status}`);
      if (req.url.includes('/login') || req.url.includes('/auth')) {
        console.log(`   POST Data: ${req.postData}`);
        console.log(`   Response: ${JSON.stringify(req.response)}`);
      }
    });
    
    console.log('=== CONSOLE MESSAGES ===');
    consoleMessages.forEach((msg, index) => {
      if (msg.type === 'error' || msg.text.includes('401') || msg.text.includes('auth')) {
        console.log(`${index + 1}. [${msg.type}] ${msg.text}`);
      }
    });
    
    // Check if we got a login request
    const loginRequest = networkRequests.find(req => 
      req.url.includes('/api/auth/login') && req.method === 'POST'
    );
    
    if (loginRequest) {
      console.log('✅ Login API request was made');
      console.log('Status:', loginRequest.status);
      console.log('Request data:', loginRequest.postData);
      console.log('Response:', JSON.stringify(loginRequest.response, null, 2));
      
      if (loginRequest.status === 401) {
        console.log('❌ CONFIRMED: UI login also returns 401');
        console.log('The issue is with the API authentication, not the frontend');
      }
    } else {
      console.log('❌ No login API request was made');
      console.log('This indicates a frontend issue preventing the API call');
    }
    
    // Save all data for analysis
    const finalReport = {
      timestamp: new Date().toISOString(),
      testUser: AuthHelpers.accounts.admin,
      apiBaseUrl: API_BASE_URL,
      networkRequests: networkRequests,
      consoleMessages: consoleMessages,
      summary: {
        loginRequestMade: !!loginRequest,
        loginStatus: loginRequest?.status || 'No request',
        loginResponse: loginRequest?.response || 'No response'
      }
    };
    
    console.log('=== FINAL ANALYSIS ===');
    console.log('Login request made:', !!loginRequest);
    if (loginRequest) {
      console.log('Login status:', loginRequest.status);
      console.log('Login response:', JSON.stringify(loginRequest.response, null, 2));
    }
    
    // Don't fail the test - just analyze
    expect(emailInput).toBeVisible();
  });
});