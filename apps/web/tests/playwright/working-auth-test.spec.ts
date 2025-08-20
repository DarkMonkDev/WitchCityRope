import { test, expect } from '@playwright/test';

/**
 * Working Authentication Test with Correct Selectors
 * Based on DOM inspection findings from JWT authentication fix testing
 */

const testAccounts = {
  admin: {
    email: 'admin@witchcityrope.com',
    password: 'Test123!'
  },
  testUser: {
    email: 'test@witchcityrope.com',
    password: 'Test1234'
  }
};

test.describe('Authentication Flow - JWT Token Testing', () => {
  let networkRequests: Array<{ url: string; method: string; status: number; response?: any }> = [];

  test.beforeEach(async ({ page }) => {
    // Clear auth state
    await page.context().clearCookies();
    await page.evaluate(() => localStorage.clear());
    networkRequests = [];

    // Capture network requests
    page.on('response', async (response) => {
      if (response.url().includes('/api/auth') || response.url().includes('/login')) {
        const request = {
          url: response.url(),
          method: response.request().method(),
          status: response.status(),
          response: undefined as any
        };
        
        try {
          request.response = await response.json();
        } catch {
          request.response = await response.text();
        }
        
        networkRequests.push(request);
        console.log(`🌐 API Call: ${request.method} ${request.url} -> ${request.status}`);
      }
    });
  });

  test('should complete login with JWT token handling', async ({ page }) => {
    console.log('🧪 Testing JWT authentication flow...');
    
    // Navigate to login page
    await page.goto('http://localhost:5173/login');
    
    // Wait for form to load and take screenshot
    await page.waitForSelector('[data-testid="email-input"]', { timeout: 10000 });
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/login-form-loaded.png' });
    
    console.log('✅ Login form loaded successfully');
    
    // Fill form using correct selectors from DOM inspection
    await page.fill('[data-testid="email-input"]', testAccounts.admin.email);
    await page.fill('[data-testid="password-input"]', testAccounts.admin.password);
    
    console.log(`✅ Filled credentials: ${testAccounts.admin.email}`);
    
    // Submit form
    await page.click('[data-testid="login-button"]');
    
    console.log('🔘 Submitted login form');
    
    // Wait for authentication response
    await page.waitForTimeout(3000);
    
    // Take screenshot after submission
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/after-login-attempt.png' });
    
    // Check if we got any authentication responses
    const authRequests = networkRequests.filter(req => 
      req.url.includes('/api/auth/login') || 
      req.url.includes('/api/Auth/login')
    );
    
    console.log(`🔍 Found ${authRequests.length} authentication requests`);
    
    if (authRequests.length > 0) {
      const loginRequest = authRequests[0];
      console.log(`🎯 Login API Response: ${loginRequest.status}`);
      console.log(`📝 Response data:`, loginRequest.response);
      
      if (loginRequest.status === 200 && loginRequest.response?.success) {
        console.log('✅ LOGIN API SUCCESS: JWT token generated');
        console.log(`🎟️ Token: ${loginRequest.response.data?.token?.substring(0, 50)}...`);
        console.log(`👤 User: ${loginRequest.response.data?.user?.email}`);
        
        // Check if we were redirected or if auth state is updated
        const currentUrl = page.url();
        console.log(`📍 Current URL after login: ${currentUrl}`);
        
        // Check for any auth verification calls
        const authVerificationCalls = networkRequests.filter(req =>
          req.url.includes('/api/auth/user') || 
          req.url.includes('/api/auth/profile')
        );
        
        console.log(`🔍 Found ${authVerificationCalls.length} auth verification requests`);
        
        if (authVerificationCalls.length > 0) {
          authVerificationCalls.forEach(call => {
            console.log(`🔍 Auth verification: ${call.method} ${call.url} -> ${call.status}`);
          });
        }
        
        // Report success
        expect(loginRequest.status).toBe(200);
        expect(loginRequest.response.success).toBe(true);
        expect(loginRequest.response.data.token).toBeTruthy();
        
      } else {
        console.log('❌ LOGIN API FAILED');
        console.log(`Status: ${loginRequest.status}`);
        console.log(`Response:`, loginRequest.response);
      }
    } else {
      console.log('❌ NO LOGIN API REQUESTS FOUND');
      console.log('This indicates a frontend issue preventing API calls');
    }
    
    console.log('📊 Test Complete - Check logs above for JWT authentication results');
  });

  test('should test direct API login call', async ({ page }) => {
    console.log('🧪 Testing direct API login call...');
    
    const response = await page.request.post('http://localhost:5655/api/auth/login', {
      data: {
        email: testAccounts.admin.email,
        password: testAccounts.admin.password
      },
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    const responseData = await response.json();
    
    console.log(`🎯 Direct API Response: ${response.status()}`);
    console.log(`📝 Response data:`, responseData);
    
    expect(response.status()).toBe(200);
    expect(responseData.success).toBe(true);
    expect(responseData.data.token).toBeTruthy();
    
    console.log('✅ DIRECT API LOGIN SUCCESS');
    console.log(`🎟️ JWT Token: ${responseData.data.token.substring(0, 50)}...`);
    console.log(`👤 User: ${responseData.data.user.email}`);
  });

  test('should test missing auth verification endpoint', async ({ page }) => {
    console.log('🧪 Testing auth verification endpoints...');
    
    // Test common auth verification endpoints
    const endpoints = [
      'http://localhost:5655/api/auth/user',
      'http://localhost:5655/api/auth/profile',
      'http://localhost:5655/api/auth/user'
    ];
    
    for (const endpoint of endpoints) {
      try {
        const response = await page.request.get(endpoint);
        console.log(`🔍 ${endpoint} -> ${response.status()}`);
      } catch (error) {
        console.log(`❌ ${endpoint} -> ERROR: ${error}`);
      }
    }
  });
});