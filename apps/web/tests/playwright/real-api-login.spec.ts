import { test, expect, Page } from '@playwright/test';

// Test configuration
const TEST_USER = {
  email: 'test@witchcityrope.com',
  password: 'Test1234'
};

const API_BASE_URL = 'http://localhost:5655';
const WEB_BASE_URL = 'http://localhost:5173';

test.describe('Real API Login Testing', () => {
  let networkRequests: Array<{
    url: string;
    method: string;
    status: number;
    response: any;
    timestamp: number;
  }> = [];

  let consoleMessages: Array<{
    type: string;
    text: string;
    timestamp: number;
  }> = [];

  test.beforeEach(async ({ page }) => {
    // Clear tracking arrays
    networkRequests = [];
    consoleMessages = [];

    // Track network requests
    page.on('response', async (response) => {
      try {
        const responseBody = await response.text();
        let parsedResponse = responseBody;
        try {
          parsedResponse = JSON.parse(responseBody);
        } catch {
          // Keep as string if not JSON
        }

        networkRequests.push({
          url: response.url(),
          method: response.request().method(),
          status: response.status(),
          response: parsedResponse,
          timestamp: Date.now()
        });
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

    // Track page errors
    page.on('pageerror', (error) => {
      consoleMessages.push({
        type: 'pageerror',
        text: error.toString(),
        timestamp: Date.now()
      });
    });
  });

  test('should verify MSW is disabled and API is accessible', async ({ page }) => {
    // Go to the app
    await page.goto('/');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Check console for MSW status
    const mswStatus = consoleMessages.find(msg => 
      msg.text.includes('MSW') || msg.text.includes('Mock Service Worker')
    );
    
    console.log('MSW Status Message:', mswStatus);
    
    // Direct API test
    const apiResponse = await page.evaluate(async (apiUrl) => {
      try {
        const response = await fetch(`${apiUrl}/health`);
        return {
          status: response.status,
          ok: response.ok,
          text: await response.text()
        };
      } catch (error) {
        return {
          error: error.toString()
        };
      }
    }, API_BASE_URL);
    
    console.log('Direct API Health Check:', apiResponse);
    
    expect(apiResponse.ok).toBe(true);
    expect(apiResponse.status).toBe(200);
  });

  test('should perform real login with test account', async ({ page }) => {
    // Go to login page
    await page.goto('/login');
    
    // Wait for login form to be visible
    await page.waitForSelector('form', { timeout: 10000 });
    
    console.log('=== LOGIN TEST START ===');
    console.log('Test user:', TEST_USER.email);
    console.log('API Base URL:', API_BASE_URL);
    
    // Fill in credentials
    await page.fill('input[type="email"], input[name="email"]', TEST_USER.email);
    await page.fill('input[type="password"], input[name="password"]', TEST_USER.password);
    
    // Take screenshot before clicking login
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/before-login-click.png' });
    
    // Click login button
    const loginButton = page.locator('button:has-text("Login"), button[type="submit"]');
    await expect(loginButton).toBeVisible();
    await loginButton.click();
    
    console.log('Login button clicked at:', new Date().toISOString());
    
    // Wait up to 15 seconds for either success or failure
    try {
      await page.waitForFunction(() => {
        // Check for success indicators
        const isLoggedIn = window.location.pathname === '/dashboard' || 
                          document.querySelector('[data-testid="welcome-message"]') !== null ||
                          document.querySelector('[data-testid="logout-button"]') !== null;
        
        // Check for error indicators  
        const hasError = document.querySelector('.error') !== null ||
                        document.querySelector('[data-testid="error-message"]') !== null ||
                        document.querySelector('.alert-danger') !== null;
        
        return isLoggedIn || hasError;
      }, { timeout: 15000 });
    } catch (timeoutError) {
      console.log('Timeout waiting for login result');
      
      // Take screenshot on timeout
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/login-timeout.png' });
    }
    
    // Wait a bit more for any network requests to complete
    await page.waitForTimeout(2000);
    
    // Take final screenshot
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/after-login-attempt.png' });
    
    console.log('=== NETWORK REQUESTS ===');
    networkRequests.forEach((req, index) => {
      console.log(`${index + 1}. ${req.method} ${req.url}`);
      console.log(`   Status: ${req.status}`);
      if (req.url.includes('/login') || req.url.includes('/auth')) {
        console.log(`   Response: ${JSON.stringify(req.response).substring(0, 200)}...`);
      }
    });
    
    console.log('=== CONSOLE MESSAGES ===');
    consoleMessages.forEach((msg, index) => {
      console.log(`${index + 1}. [${msg.type}] ${msg.text}`);
    });
    
    // Check if we got a login request to the API
    const loginRequest = networkRequests.find(req => 
      req.url.includes('/api/auth/login') && req.method === 'POST'
    );
    
    console.log('=== LOGIN REQUEST ANALYSIS ===');
    console.log('Login API request found:', !!loginRequest);
    
    if (loginRequest) {
      console.log('Login request status:', loginRequest.status);
      console.log('Login response:', JSON.stringify(loginRequest.response, null, 2));
      
      // If we got a successful login response, check if UI updated
      if (loginRequest.status === 200 && loginRequest.response?.success) {
        console.log('✅ LOGIN API SUCCESS - checking UI state');
        
        // Check current URL
        const currentUrl = page.url();
        console.log('Current URL:', currentUrl);
        
        // Check for success indicators
        const isOnDashboard = currentUrl.includes('/dashboard');
        const hasWelcomeMessage = await page.locator('[data-testid="welcome-message"]').count() > 0;
        const hasLogoutButton = await page.locator('[data-testid="logout-button"]').count() > 0;
        
        console.log('Is on dashboard:', isOnDashboard);
        console.log('Has welcome message:', hasWelcomeMessage);
        console.log('Has logout button:', hasLogoutButton);
        
        if (isOnDashboard || hasWelcomeMessage || hasLogoutButton) {
          console.log('✅ LOGIN SUCCESSFUL - User authenticated and UI updated');
        } else {
          console.log('⚠️ LOGIN API SUCCESS but UI not updated - possible frontend issue');
        }
      } else {
        console.log('❌ LOGIN API FAILED');
        console.log('Status:', loginRequest.status);
        console.log('Response:', loginRequest.response);
      }
    } else {
      console.log('❌ NO LOGIN API REQUEST - possible frontend routing issue');
      
      // Check if there are any errors preventing the request
      const errors = consoleMessages.filter(msg => msg.type === 'error' || msg.type === 'pageerror');
      if (errors.length > 0) {
        console.log('Frontend errors found:');
        errors.forEach(error => console.log(' -', error.text));
      }
      
      // Check if MSW is still enabled
      const mswMessages = consoleMessages.filter(msg => 
        msg.text.toLowerCase().includes('msw') || msg.text.toLowerCase().includes('mock')
      );
      if (mswMessages.length > 0) {
        console.log('MSW-related messages:');
        mswMessages.forEach(msg => console.log(' -', msg.text));
      }
    }
    
    // Final assertions based on what we found
    expect(networkRequests.length).toBeGreaterThan(0);
    
    if (loginRequest) {
      expect(loginRequest.status).toBe(200);
      expect(loginRequest.response?.success).toBe(true);
    }
  });
  
  test('should test login button behavior specifically', async ({ page }) => {
    await page.goto('/login');
    
    // Wait for form
    await page.waitForSelector('form');
    
    // Fill credentials
    await page.fill('input[type="email"], input[name="email"]', TEST_USER.email);
    await page.fill('input[type="password"], input[name="password"]', TEST_USER.password);
    
    // Find the login button
    const loginButton = page.locator('button:has-text("Login"), button[type="submit"]');
    await expect(loginButton).toBeVisible();
    
    // Check button initial state
    const initialText = await loginButton.textContent();
    console.log('Login button initial text:', initialText);
    
    // Check if button becomes disabled or shows loading
    await loginButton.click();
    
    // Wait a moment and check button state
    await page.waitForTimeout(1000);
    
    const afterClickText = await loginButton.textContent();
    const isDisabled = await loginButton.isDisabled();
    const hasSpinner = await page.locator('.spinner, .loading, [data-testid="loading"]').count() > 0;
    
    console.log('Button text after click:', afterClickText);
    console.log('Button disabled:', isDisabled);
    console.log('Has loading spinner:', hasSpinner);
    
    // Check if button is stuck in loading state
    await page.waitForTimeout(3000);
    
    const finalText = await loginButton.textContent();
    const finalDisabled = await loginButton.isDisabled();
    
    console.log('Button final text:', finalText);
    console.log('Button final disabled:', finalDisabled);
    
    if (finalText?.includes('Loading') || finalText?.includes('...') || finalDisabled) {
      console.log('⚠️ BUTTON STUCK IN LOADING STATE - this indicates the issue');
    }
    
    // Look for network requests
    const hasLoginRequest = networkRequests.some(req => 
      req.url.includes('/login') && req.method === 'POST'
    );
    
    console.log('Made login API request:', hasLoginRequest);
    
    if (!hasLoginRequest) {
      console.log('❌ NO LOGIN REQUEST MADE - button spinning but no API call');
    }
  });
});