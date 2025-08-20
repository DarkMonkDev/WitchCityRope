import { test, expect } from '@playwright/test';

/**
 * Real API Authentication Test
 * Testing with actual accounts that exist in the database
 */

const testAccounts = {
  testUser: {
    email: 'test@witchcityrope.com',
    password: 'Test1234'
  },
  member: {
    email: 'member@witchcityrope.com', 
    password: 'Test123!'
  }
};

test.describe('Real API Authentication Testing', () => {
  let networkRequests: Array<{ url: string; method: string; status: number; response?: any }> = [];

  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    networkRequests = [];

    page.on('response', async (response) => {
      if (response.url().includes('/api/auth') || response.url().includes('/api/Auth')) {
        const request = {
          url: response.url(),
          method: response.request().method(),
          status: response.status(),
          response: undefined as any
        };
        
        try {
          if (response.headers()['content-type']?.includes('application/json')) {
            request.response = await response.json();
          } else {
            request.response = await response.text();
          }
        } catch (error) {
          request.response = `Error: ${error}`;
        }
        
        networkRequests.push(request);
        console.log(`🌐 ${request.method} ${request.url} -> ${request.status}`);
      }
    });
  });

  test('should successfully login with existing test account', async ({ page }) => {
    console.log('🧪 Testing with known working account: test@witchcityrope.com');
    
    const response = await page.request.post('http://localhost:5655/api/auth/login', {
      data: {
        email: testAccounts.testUser.email,
        password: testAccounts.testUser.password
      },
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    const responseData = await response.json();
    
    console.log(`🎯 API Response: ${response.status()}`);
    console.log(`📝 Response:`, responseData);
    
    if (response.status() === 200 && responseData.success) {
      console.log('✅ LOGIN SUCCESS WITH JWT');
      console.log(`🎟️ JWT Token: ${responseData.data.token.substring(0, 50)}...`);
      console.log(`👤 User: ${responseData.data.user.email} (${responseData.data.user.sceneName})`);
      console.log(`🕐 Expires: ${responseData.data.expiresAt}`);
      
      // Verify JWT token structure
      const tokenParts = responseData.data.token.split('.');
      expect(tokenParts.length).toBe(3);
      
      expect(response.status()).toBe(200);
      expect(responseData.success).toBe(true);
      expect(responseData.data.token).toBeTruthy();
      expect(responseData.data.user.email).toBe(testAccounts.testUser.email);
      
    } else {
      console.log('❌ LOGIN FAILED');
      console.log(`Status: ${response.status()}`);
      console.log(`Error: ${responseData.error}`);
      expect(response.status()).toBe(200); // This will fail and show the actual issue
    }
  });

  test('should complete full UI login flow with existing account', async ({ page }) => {
    console.log('🧪 Testing UI login flow...');
    
    await page.goto('http://localhost:5173/login');
    
    // Wait for form
    await page.waitForSelector('[data-testid="email-input"]', { timeout: 10000 });
    
    // Fill form with working account
    await page.fill('[data-testid="email-input"]', testAccounts.testUser.email);
    await page.fill('[data-testid="password-input"]', testAccounts.testUser.password);
    
    console.log(`✅ Filled form with: ${testAccounts.testUser.email}`);
    
    // Take screenshot before submit
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/before-real-login.png' });
    
    // Submit
    await page.click('[data-testid="login-button"]');
    console.log('🔘 Form submitted');
    
    // Wait for response
    await page.waitForTimeout(5000);
    
    // Take screenshot after submit  
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/after-real-login.png' });
    
    console.log(`🔍 Network requests: ${networkRequests.length}`);
    
    const loginRequests = networkRequests.filter(req => req.url.includes('/login'));
    
    if (loginRequests.length > 0) {
      const loginRequest = loginRequests[0];
      console.log(`🎯 Login API: ${loginRequest.status}`);
      
      if (loginRequest.status === 200 && loginRequest.response?.success) {
        console.log('✅ UI LOGIN SUCCESS');
        console.log(`🎟️ JWT Token generated: ${loginRequest.response.data?.token?.substring(0, 30)}...`);
        
        const currentUrl = page.url();
        console.log(`📍 Current URL: ${currentUrl}`);
        
        if (currentUrl.includes('/dashboard')) {
          console.log('✅ Successfully redirected to dashboard');
        } else {
          console.log('⚠️ Authentication succeeded but no redirect - possible state management issue');
        }
        
      } else {
        console.log('❌ UI LOGIN FAILED');
        console.log(`Status: ${loginRequest.status}`);
        console.log(`Error:`, loginRequest.response);
      }
    } else {
      console.log('❌ NO API CALLS MADE');
      console.log('Frontend form submission not working');
    }
  });

  test('should test authentication state verification', async ({ page }) => {
    console.log('🧪 Testing auth state verification...');
    
    // First get a valid JWT token
    const loginResponse = await page.request.post('http://localhost:5655/api/auth/login', {
      data: {
        email: testAccounts.testUser.email,
        password: testAccounts.testUser.password
      },
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    const loginData = await loginResponse.json();
    
    if (loginData.success) {
      console.log('✅ Got JWT token for testing');
      
      // Test common auth verification endpoints
      const endpoints = [
        { url: 'http://localhost:5655/api/auth/user', description: 'Current user info' },
        { url: 'http://localhost:5655/api/auth/profile', description: 'User profile' },
        { url: 'http://localhost:5655/api/users/profile', description: 'User profile (alt)' },
        { url: 'http://localhost:5655/api/user/me', description: 'User me endpoint' }
      ];
      
      console.log('🔍 Testing auth verification endpoints:');
      
      for (const endpoint of endpoints) {
        try {
          const response = await page.request.get(endpoint.url, {
            headers: {
              'Authorization': `Bearer ${loginData.data.token}`
            }
          });
          
          console.log(`  ${endpoint.description}: ${response.status()}`);
          
          if (response.status() === 200) {
            console.log(`    ✅ Working endpoint found!`);
          } else if (response.status() === 404) {
            console.log(`    ❌ Not implemented`);
          } else {
            console.log(`    ⚠️ Status: ${response.status()}`);
          }
          
        } catch (error) {
          console.log(`    ❌ Error: ${error}`);
        }
      }
      
      // Test without Authorization header
      console.log('\n🔍 Testing endpoints without auth:');
      const testUrl = 'http://localhost:5655/api/auth/user';
      
      try {
        const response = await page.request.get(testUrl);
        console.log(`  No auth header: ${response.status()}`);
      } catch (error) {
        console.log(`  No auth header: ERROR - ${error}`);
      }
      
    } else {
      console.log('❌ Could not get JWT token for verification testing');
    }
  });

  test.afterEach(async () => {
    console.log('\n📊 TEST SUMMARY:');
    console.log(`Total requests: ${networkRequests.length}`);
    
    networkRequests.forEach((req, i) => {
      console.log(`  ${i + 1}. ${req.method} ${req.url} -> ${req.status}`);
    });
  });
});