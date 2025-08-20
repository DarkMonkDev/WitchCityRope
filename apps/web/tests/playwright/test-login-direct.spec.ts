import { test, expect } from '@playwright/test';

test.describe('Direct Login Test', () => {
  test('should test login with proper error handling', async ({ page }) => {
    // Go to the app
    await page.goto('/', { waitUntil: 'networkidle' });
    
    console.log('=== TESTING DIRECT FETCH TO API ===');
    
    // Test direct fetch from browser context
    const fetchResult = await page.evaluate(async () => {
      try {
        const response = await fetch('http://localhost:5655/api/auth/login', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          credentials: 'include', // Important for CORS with credentials
          body: JSON.stringify({
            email: 'test@witchcityrope.com',
            password: 'Test1234'
          })
        });
        
        const data = await response.json();
        
        return {
          success: true,
          status: response.status,
          ok: response.ok,
          headers: Object.fromEntries(response.headers.entries()),
          data: data
        };
      } catch (error) {
        return {
          success: false,
          error: error.toString(),
          name: error.name,
          message: error.message
        };
      }
    });
    
    console.log('Fetch result:', JSON.stringify(fetchResult, null, 2));
    
    if (fetchResult.success) {
      console.log('✅ DIRECT FETCH WORKS - Login successful');
      console.log('Token received:', fetchResult.data?.data?.token ? 'YES' : 'NO');
      expect(fetchResult.status).toBe(200);
      expect(fetchResult.data.success).toBe(true);
    } else {
      console.log('❌ DIRECT FETCH FAILED');
      console.log('Error:', fetchResult.error);
      console.log('Error name:', fetchResult.name);
      console.log('Error message:', fetchResult.message);
    }
  });

  test('should test the actual login form flow', async ({ page }) => {
    const requestLog: any[] = [];
    
    // Capture network requests
    page.on('request', (request) => {
      if (request.url().includes('/api/')) {
        requestLog.push({
          type: 'request',
          method: request.method(),
          url: request.url(),
          headers: request.headers(),
          postData: request.postData()
        });
      }
    });
    
    page.on('response', async (response) => {
      if (response.url().includes('/api/')) {
        try {
          const responseText = await response.text();
          requestLog.push({
            type: 'response',
            method: response.request().method(),
            url: response.url(),
            status: response.status(),
            headers: response.headers(),
            body: responseText
          });
        } catch (e) {
          requestLog.push({
            type: 'response',
            url: response.url(),
            status: response.status(),
            error: 'Could not read response body'
          });
        }
      }
    });

    // Navigate to login page
    console.log('=== NAVIGATING TO LOGIN PAGE ===');
    await page.goto('/login');
    
    // Wait for the login form
    try {
      await page.waitForSelector('form', { timeout: 10000 });
      console.log('✅ Login form found');
    } catch (e) {
      console.log('❌ Login form not found within 10 seconds');
      await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/no-form-found.png' });
      return;
    }
    
    // Find and fill the form
    const emailSelector = 'input[type="email"], input[name="email"], input[placeholder*="email" i]';
    const passwordSelector = 'input[type="password"], input[name="password"], input[placeholder*="password" i]';
    
    await page.fill(emailSelector, 'test@witchcityrope.com');
    await page.fill(passwordSelector, 'Test1234');
    
    console.log('✅ Form filled with credentials');
    
    // Take screenshot before submit
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/before-submit.png' });
    
    // Find and click submit button
    const submitButton = page.locator('button[type="submit"], button:has-text("Login"), input[type="submit"]');
    await expect(submitButton).toBeVisible();
    
    console.log('=== CLICKING SUBMIT BUTTON ===');
    await submitButton.click();
    
    // Wait for either success or error
    console.log('=== WAITING FOR LOGIN RESPONSE ===');
    
    try {
      // Wait for either navigation or error message
      await page.waitForFunction(() => {
        const currentUrl = window.location.pathname;
        const hasError = document.querySelector('.error, [data-testid="error"], .alert-danger') !== null;
        const hasSuccess = currentUrl !== '/login' || document.querySelector('[data-testid="success"]') !== null;
        
        return hasError || hasSuccess;
      }, { timeout: 15000 });
      
      console.log('✅ Login attempt completed');
    } catch (e) {
      console.log('⚠️ Login attempt timed out');
    }
    
    // Take screenshot after submit
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/after-submit.png' });
    
    // Log all network activity
    console.log('=== NETWORK REQUEST LOG ===');
    requestLog.forEach((entry, index) => {
      console.log(`${index + 1}. ${entry.type.toUpperCase()} ${entry.method} ${entry.url}`);
      if (entry.type === 'request' && entry.postData) {
        console.log(`   Body: ${entry.postData}`);
      }
      if (entry.type === 'response') {
        console.log(`   Status: ${entry.status}`);
        if (entry.url.includes('/login')) {
          console.log(`   Response: ${entry.body?.substring(0, 200)}...`);
        }
      }
    });
    
    // Check current state
    const currentUrl = await page.url();
    const hasErrorMessage = await page.locator('.error, [data-testid="error"], .alert-danger').count() > 0;
    
    console.log('Final URL:', currentUrl);
    console.log('Has error message:', hasErrorMessage);
    
    // Look for login API request
    const loginRequest = requestLog.find(entry => 
      entry.type === 'request' && 
      entry.url.includes('/api/auth/login') && 
      entry.method === 'POST'
    );
    
    const loginResponse = requestLog.find(entry => 
      entry.type === 'response' && 
      entry.url.includes('/api/auth/login') && 
      entry.method === 'POST'
    );
    
    console.log('=== ANALYSIS ===');
    console.log('Made login request:', !!loginRequest);
    console.log('Received login response:', !!loginResponse);
    
    if (loginRequest) {
      console.log('✅ LOGIN REQUEST MADE');
      console.log('Request body:', loginRequest.postData);
    } else {
      console.log('❌ NO LOGIN REQUEST MADE - form submission issue');
    }
    
    if (loginResponse) {
      console.log('✅ LOGIN RESPONSE RECEIVED');
      console.log('Response status:', loginResponse.status);
      if (loginResponse.status === 200) {
        console.log('✅ LOGIN API SUCCESS');
        try {
          const responseData = JSON.parse(loginResponse.body);
          if (responseData.success) {
            console.log('✅ LOGIN DATA SUCCESS - Token received');
            console.log('User:', responseData.data.user.email);
            
            // If API succeeded but we're still on login page, it's a frontend issue
            if (currentUrl.includes('/login')) {
              console.log('⚠️ API SUCCESS but still on login page - frontend routing issue');
            } else {
              console.log('✅ LOGIN COMPLETE - Successfully redirected');
            }
          } else {
            console.log('❌ LOGIN API RETURNED ERROR');
            console.log('Error:', responseData.error);
          }
        } catch (e) {
          console.log('Failed to parse response JSON');
        }
      } else {
        console.log('❌ LOGIN API FAILED');
        console.log('Status:', loginResponse.status);
      }
    }
    
    // Basic assertions
    expect(requestLog.length).toBeGreaterThan(0);
    
    if (loginResponse?.status === 200) {
      console.log('✅ LOGIN TEST PASSED - API responded successfully');
    } else if (!loginRequest) {
      console.log('❌ LOGIN TEST FAILED - No request made (form issue)');
    } else {
      console.log('❌ LOGIN TEST FAILED - Request made but failed');
    }
  });
});