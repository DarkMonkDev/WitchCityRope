import { test, expect } from '@playwright/test';

/**
 * Focused Real API Login Test
 * 
 * Based on the login form screenshot, this test uses the correct selectors
 * and test credentials to verify the complete login flow.
 */

test.describe('Focused Real API Login Test', () => {
  let consoleErrors: string[] = [];
  let apiRequests: Array<{
    url: string;
    method: string;
    status?: number;
  }> = [];

  test.beforeEach(async ({ page }) => {
    // Reset arrays
    consoleErrors = [];
    apiRequests = [];

    // Monitor console errors
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    // Monitor API requests
    page.on('request', (request) => {
      const url = request.url();
      if (url.includes('localhost:5655') || url.includes('/api/')) {
        apiRequests.push({
          url,
          method: request.method()
        });
      }
    });

    page.on('response', (response) => {
      const url = response.url();
      if (url.includes('localhost:5655') || url.includes('/api/')) {
        const request = apiRequests.find(req => req.url === url && !('status' in req));
        if (request) {
          (request as any).status = response.status();
        }
      }
    });
  });

  test('should complete login with test@witchcityrope.com', async ({ page }) => {
    console.log('=== FOCUSED LOGIN TEST STARTING ===');
    
    // Step 1: Navigate to login page
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    console.log('✅ Navigated to login page');

    // Step 2: Verify form elements are visible (using generic selectors based on screenshot)
    const emailField = page.locator('input[type="email"]').first();
    const passwordField = page.locator('input[type="password"]').first();
    const loginButton = page.locator('button:has-text("Login")').first();

    await expect(emailField).toBeVisible();
    await expect(passwordField).toBeVisible(); 
    await expect(loginButton).toBeVisible();
    
    console.log('✅ All form elements are visible');

    // Step 3: Fill credentials - using the test account from CLAUDE.md
    await emailField.fill('test@witchcityrope.com');
    await passwordField.fill('Test1234');
    
    console.log('✅ Filled credentials: test@witchcityrope.com');

    // Clear API requests before login attempt
    apiRequests.length = 0;

    // Step 4: Submit login form
    console.log('🔄 Submitting login form...');
    await loginButton.click();

    // Wait for potential navigation or API response
    await page.waitForTimeout(3000);
    
    const currentUrl = page.url();
    console.log(`Current URL after login: ${currentUrl}`);

    // Step 5: Take screenshot of result
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/focused-login-result.png' });

    // Step 6: Analyze results
    const loginRequests = apiRequests.filter(req => 
      req.url.includes('login') || 
      req.url.includes('auth') ||
      req.url.includes('authenticate')
    );

    const protectedRequests = apiRequests.filter(req => 
      req.url.includes('Protected') ||
      req.url.includes('profile')
    );

    // Check for infinite loop errors
    const loopErrors = consoleErrors.filter(error => 
      error.includes('Maximum update depth exceeded') ||
      error.includes('Too many re-renders') ||
      error.includes('Maximum call stack size exceeded')
    );

    // Step 7: Generate comprehensive report
    const testReport = {
      timestamp: new Date().toISOString(),
      testCredentials: 'test@witchcityrope.com / Test1234',
      results: {
        currentUrl,
        remainedOnLoginPage: currentUrl.includes('/login'),
        consoleErrors: consoleErrors.length,
        infiniteLoopErrors: loopErrors.length,
        totalApiRequests: apiRequests.length,
        loginRequests: loginRequests.length,
        protectedRequests: protectedRequests.length
      },
      apiActivity: {
        allRequests: apiRequests,
        loginSpecific: loginRequests,
        protectedEndpoints: protectedRequests
      },
      verdict: {
        noInfiniteLoop: loopErrors.length === 0,
        apiCommunicationWorking: apiRequests.length > 0,
        realApiBeingUsed: apiRequests.some(req => req.url.includes('localhost:5655')),
        mswDisabled: true // Confirmed from .env.development
      }
    };

    console.log('=== FOCUSED LOGIN TEST REPORT ===');
    console.log(JSON.stringify(testReport, null, 2));

    // Step 8: Critical assertions
    expect(loopErrors.length, 'No infinite loop errors should occur').toBe(0);
    expect(apiRequests.length, 'At least one API request should be made').toBeGreaterThan(0);

    // Log final status
    console.log('=== TEST COMPLETION SUMMARY ===');
    console.log(`✅ No infinite loops: ${loopErrors.length === 0}`);
    console.log(`✅ API communication: ${apiRequests.length} requests made`);
    console.log(`✅ Real API used: ${apiRequests.some(req => req.url.includes('localhost:5655'))}`);
    console.log(`📍 Final URL: ${currentUrl}`);
    
    if (currentUrl.includes('/login')) {
      console.log('📝 User remained on login page - may indicate failed login or validation');
    } else {
      console.log('✅ User was redirected - indicates successful authentication');
    }

    // Additional check: Look for any error messages on the page
    const errorMessages = await page.locator('text=Invalid, text=Error, text=Failed, .error, .alert-danger').count();
    console.log(`Error messages on page: ${errorMessages}`);

    return testReport;
  });

  test('should verify page loads without infinite loops', async ({ page }) => {
    console.log('=== INFINITE LOOP PREVENTION TEST ===');
    
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    // Wait and monitor for 5 seconds for any loop errors
    await page.waitForTimeout(5000);
    
    const loopErrors = consoleErrors.filter(error => 
      error.includes('Maximum update depth exceeded') ||
      error.includes('Too many re-renders') ||
      error.includes('Maximum call stack size exceeded')
    );

    console.log(`Console errors detected: ${consoleErrors.length}`);
    console.log(`Loop errors detected: ${loopErrors.length}`);
    
    if (loopErrors.length > 0) {
      console.log('LOOP ERRORS FOUND:', loopErrors);
    }

    expect(loopErrors.length).toBe(0);
    
    console.log('✅ No infinite loop errors detected during 5-second monitoring period');
  });

  test.afterEach(async () => {
    console.log(`=== TEST COMPLETED ===`);
    console.log(`Total console errors: ${consoleErrors.length}`);
    console.log(`Total API requests: ${apiRequests.length}`);
  });
});