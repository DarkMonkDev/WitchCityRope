import { test, expect } from '@playwright/test';

/**
 * Comprehensive JWT Authentication Testing
 * Tests the complete authentication flow after JWT token handling fixes
 */

const testAccounts = {
  admin: {
    email: 'admin@witchcityrope.com',
    password: 'Test123!'
  }
};

test.describe('JWT Authentication Comprehensive Test', () => {
  let networkRequests: Array<{ url: string; method: string; status: number; response?: any }> = [];

  test.beforeEach(async ({ page }) => {
    // Clear auth state
    await page.context().clearCookies();
    networkRequests = [];

    // Capture network requests
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
          request.response = `Error reading response: ${error}`;
        }
        
        networkRequests.push(request);
        console.log(`🌐 API Call: ${request.method} ${request.url} -> ${request.status}`);
      }
    });
  });

  test('should verify login API is working with JWT tokens', async ({ page }) => {
    console.log('🧪 Testing JWT authentication API...');
    
    // Test direct API call first
    const directApiResponse = await page.request.post('http://localhost:5655/api/auth/login', {
      data: {
        email: testAccounts.admin.email,
        password: testAccounts.admin.password
      },
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    const directApiData = await directApiResponse.json();
    
    console.log(`🎯 Direct API Test: ${directApiResponse.status()}`);
    console.log(`✅ Response:`, directApiData);
    
    // Verify JWT token structure
    if (directApiData.success && directApiData.data?.token) {
      console.log('✅ JWT TOKEN GENERATED SUCCESSFULLY');
      console.log(`🎟️ Token: ${directApiData.data.token.substring(0, 50)}...`);
      console.log(`👤 User: ${directApiData.data.user?.email}`);
      console.log(`🕐 Expires: ${directApiData.data.expiresAt}`);
      
      // Verify JWT token format (should have 3 parts separated by dots)
      const tokenParts = directApiData.data.token.split('.');
      expect(tokenParts.length).toBe(3);
      console.log(`✅ JWT token has correct format (3 parts)`);
    }
    
    expect(directApiResponse.status()).toBe(200);
    expect(directApiData.success).toBe(true);
    expect(directApiData.data.token).toBeTruthy();
    
    console.log('✅ DIRECT API LOGIN WITH JWT: SUCCESS');
  });

  test('should test UI form submission with JWT handling', async ({ page }) => {
    console.log('🧪 Testing UI form submission...');
    
    // Navigate to login page
    await page.goto('http://localhost:5173/login');
    
    // Wait for form elements to load
    await page.waitForSelector('[data-testid="email-input"]', { timeout: 10000 });
    await page.waitForSelector('[data-testid="password-input"]', { timeout: 10000 });
    await page.waitForSelector('[data-testid="login-button"]', { timeout: 10000 });
    
    console.log('✅ Login form elements loaded');
    
    // Fill the form using correct selectors
    await page.fill('[data-testid="email-input"]', testAccounts.admin.email);
    await page.fill('[data-testid="password-input"]', testAccounts.admin.password);
    
    console.log(`✅ Filled form with: ${testAccounts.admin.email}`);
    
    // Take screenshot before submission
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/before-login-submit.png',
      fullPage: true
    });
    
    // Submit the form
    await page.click('[data-testid="login-button"]');
    console.log('🔘 Submitted login form');
    
    // Wait for network requests
    await page.waitForTimeout(5000);
    
    // Take screenshot after submission
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/test-results/after-login-submit.png',
      fullPage: true 
    });
    
    // Analyze network requests
    console.log(`🔍 Total auth requests captured: ${networkRequests.length}`);
    
    const loginRequests = networkRequests.filter(req => 
      req.url.includes('/login')
    );
    
    if (loginRequests.length > 0) {
      const loginRequest = loginRequests[0];
      console.log(`🎯 Login Request: ${loginRequest.method} ${loginRequest.url}`);
      console.log(`📊 Status: ${loginRequest.status}`);
      
      if (loginRequest.status === 200 && loginRequest.response?.success) {
        console.log('✅ UI LOGIN SUCCESS WITH JWT');
        console.log(`🎟️ JWT Token: ${loginRequest.response.data?.token?.substring(0, 50)}...`);
        
        // Check current URL for redirects
        const currentUrl = page.url();
        console.log(`📍 Current URL: ${currentUrl}`);
        
        if (currentUrl.includes('/dashboard')) {
          console.log('✅ Successfully redirected to dashboard');
        } else if (currentUrl.includes('/login')) {
          console.log('⚠️ Still on login page - possible auth state issue');
        }
        
      } else {
        console.log('❌ UI LOGIN FAILED');
        console.log(`Status: ${loginRequest.status}`);
        console.log(`Response:`, loginRequest.response);
      }
    } else {
      console.log('❌ NO LOGIN REQUESTS FOUND FROM UI');
      console.log('This indicates form submission is not triggering API calls');
    }
    
    // Check for auth verification attempts
    const authVerifyRequests = networkRequests.filter(req => 
      req.url.includes('/me') || req.url.includes('/profile')
    );
    
    if (authVerifyRequests.length > 0) {
      console.log(`🔍 Auth verification attempts: ${authVerifyRequests.length}`);
      authVerifyRequests.forEach((req, index) => {
        console.log(`  ${index + 1}. ${req.method} ${req.url} -> ${req.status}`);
      });
    } else {
      console.log('ℹ️ No auth verification requests found');
    }
  });

  test('should test protected route access with JWT', async ({ page }) => {
    console.log('🧪 Testing protected route access...');
    
    // First login via API to get JWT token
    const loginResponse = await page.request.post('http://localhost:5655/api/auth/login', {
      data: {
        email: testAccounts.admin.email,
        password: testAccounts.admin.password
      },
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    const loginData = await loginResponse.json();
    
    if (loginData.success) {
      console.log('✅ Got JWT token from API');
      
      // Try accessing protected endpoints with the token
      const protectedEndpoints = [
        'http://localhost:5655/api/auth/user',
        'http://localhost:5655/api/auth/profile',
        'http://localhost:5655/api/users/profile'
      ];
      
      for (const endpoint of protectedEndpoints) {
        try {
          const response = await page.request.get(endpoint, {
            headers: {
              'Authorization': `Bearer ${loginData.data.token}`
            }
          });
          
          console.log(`🔍 ${endpoint} -> ${response.status()}`);
          
          if (response.status() === 404) {
            console.log(`   ⚠️ Endpoint not implemented`);
          } else if (response.status() === 200) {
            console.log(`   ✅ Endpoint working with JWT`);
          } else {
            console.log(`   ❌ Unexpected status: ${response.status()}`);
          }
          
        } catch (error) {
          console.log(`❌ ${endpoint} -> ERROR: ${error}`);
        }
      }
    }
  });

  test.afterEach(async ({ page }) => {
    // Print final summary
    console.log('\n📊 AUTHENTICATION TEST SUMMARY:');
    console.log(`Total API requests: ${networkRequests.length}`);
    
    const successful = networkRequests.filter(req => req.status >= 200 && req.status < 300);
    const failed = networkRequests.filter(req => req.status >= 400);
    
    console.log(`✅ Successful requests: ${successful.length}`);
    console.log(`❌ Failed requests: ${failed.length}`);
    
    if (failed.length > 0) {
      console.log('\n❌ Failed requests details:');
      failed.forEach(req => {
        console.log(`  ${req.method} ${req.url} -> ${req.status}`);
      });
    }
  });
});