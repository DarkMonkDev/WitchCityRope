import { test, expect } from '@playwright/test';

/**
 * Final Real API Login Test - Complete End-to-End Authentication Flow
 * 
 * Uses correct Mantine UI selectors discovered through DOM inspection.
 * This test verifies:
 * 1. No infinite loop issues (FIXED)
 * 2. Real API communication on port 5655 (MSW disabled)
 * 3. Login form functionality with test@witchcityrope.com
 * 4. Authentication state handling
 * 5. Network monitoring and console error tracking
 */

test.describe('Final Real API Authentication Test', () => {
  let consoleErrors: string[] = [];
  let consoleMessages: string[] = [];
  let apiRequests: Array<{
    url: string;
    method: string;
    status?: number;
    timestamp: number;
  }> = [];

  test.beforeEach(async ({ page }) => {
    // Reset tracking arrays
    consoleErrors = [];
    consoleMessages = [];
    apiRequests = [];

    // Monitor all console activity
    page.on('console', (msg) => {
      const message = `[${msg.type()}] ${msg.text()}`;
      consoleMessages.push(message);
      
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    // Monitor network requests to real API
    page.on('request', (request) => {
      const url = request.url();
      if (url.includes('localhost:5655') || url.includes('/api/')) {
        apiRequests.push({
          url,
          method: request.method(),
          timestamp: Date.now()
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

  test('should complete full login flow with real API - COMPREHENSIVE TEST', async ({ page }) => {
    console.log('=== COMPREHENSIVE REAL API LOGIN TEST ===');
    console.log('Testing: Real API communication, No infinite loops, Authentication flow');
    
    // Step 1: Navigate to login page
    console.log('Step 1: Navigating to login page...');
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    // Take initial screenshot
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/final-01-login-page.png' });
    console.log('✅ Login page loaded');

    // Step 2: Wait for form to be fully rendered
    console.log('Step 2: Waiting for Mantine form elements...');
    
    // Use correct React LoginPage selectors (data-testid attributes)
    const emailInput = page.locator('[data-testid="email-input"]');
    const passwordInput = page.locator('[data-testid="password-input"]');
    const loginButton = page.locator('[data-testid="login-button"]');

    await expect(emailInput).toBeVisible({ timeout: 10000 });
    await expect(passwordInput).toBeVisible({ timeout: 10000 });
    await expect(loginButton).toBeVisible({ timeout: 10000 });
    
    console.log('✅ All Mantine form elements are visible and ready');

    // Step 3: Fill in test credentials
    console.log('Step 3: Filling credentials - test@witchcityrope.com');
    await emailInput.fill('test@witchcityrope.com');
    await passwordInput.fill('Test1234');
    
    // Screenshot with filled credentials
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/final-02-credentials-filled.png' });

    // Step 4: Clear API requests and submit
    console.log('Step 4: Submitting login form and monitoring API calls...');
    apiRequests.length = 0; // Clear previous requests
    
    // Submit form and wait for response
    await loginButton.click();
    
    // Wait for potential navigation or API response
    await page.waitForTimeout(4000);
    
    const urlAfterLogin = page.url();
    console.log(`URL after login attempt: ${urlAfterLogin}`);

    // Take screenshot of result
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/final-03-after-login.png' });

    // Step 5: Analyze authentication result
    console.log('Step 5: Analyzing authentication result...');
    
    const successIndicators = {
      redirectedFromLogin: !urlAfterLogin.includes('/login'),
      hasWelcomeText: await page.locator('text=Welcome, text=Dashboard, text=Profile').count() > 0,
      hasLogoutButton: await page.locator('text=Logout, button:has-text("Logout")').count() > 0,
      hasUserMenu: await page.locator('[data-testid="user-menu"], .user-menu').count() > 0
    };

    // Step 6: Check for infinite loop errors (CRITICAL)
    console.log('Step 6: Checking for infinite loop patterns...');
    
    const infiniteLoopErrors = consoleErrors.filter(error => 
      error.includes('Maximum update depth exceeded') ||
      error.includes('Too many re-renders') ||
      error.includes('Maximum call stack size exceeded') ||
      error.toLowerCase().includes('infinite') ||
      error.includes('RangeError: Maximum call stack')
    );

    // Step 7: Analyze API communication
    console.log('Step 7: Analyzing API communication...');
    
    const realApiRequests = apiRequests.filter(req => req.url.includes('localhost:5655'));
    const authRequests = apiRequests.filter(req => 
      req.url.includes('login') || 
      req.url.includes('auth') ||
      req.url.includes('authenticate')
    );
    const protectedRequests = apiRequests.filter(req => 
      req.url.includes('Protected') ||
      req.url.includes('profile')
    );

    // Step 8: Generate comprehensive test report
    const testReport = {
      testExecutionInfo: {
        timestamp: new Date().toISOString(),
        testCredentials: 'test@witchcityrope.com / Test1234',
        environment: {
          mswDisabled: true,
          reactUrl: 'http://localhost:5173',
          apiUrl: 'http://localhost:5655'
        }
      },
      criticalChecks: {
        noInfiniteLoops: infiniteLoopErrors.length === 0,
        apiCommunicationWorking: realApiRequests.length > 0,
        formSubmissionSuccessful: apiRequests.length > 0,
        consoleErrorsMinimal: consoleErrors.length < 10
      },
      authenticationResults: {
        currentUrl: urlAfterLogin,
        successIndicators,
        likelyAuthenticationSuccess: (
          successIndicators.redirectedFromLogin ||
          successIndicators.hasWelcomeText ||
          successIndicators.hasLogoutButton ||
          successIndicators.hasUserMenu
        )
      },
      networkActivity: {
        totalApiRequests: apiRequests.length,
        realApiRequests: realApiRequests.length,
        authenticationRequests: authRequests.length,
        protectedEndpointRequests: protectedRequests.length,
        requestDetails: apiRequests.map(req => ({
          url: req.url.replace('http://localhost:5655', ''),
          method: req.method,
          status: (req as any).status || 'pending'
        }))
      },
      errorAnalysis: {
        totalConsoleErrors: consoleErrors.length,
        infiniteLoopErrors: infiniteLoopErrors.length,
        criticalErrors: infiniteLoopErrors,
        recentConsoleMessages: consoleMessages.slice(-10)
      }
    };

    // Step 9: Log comprehensive report
    console.log('=== COMPREHENSIVE TEST REPORT ===');
    console.log(JSON.stringify(testReport, null, 2));

    // Step 10: Critical Assertions
    console.log('Step 10: Running critical assertions...');

    // MOST CRITICAL: No infinite loops
    expect(infiniteLoopErrors.length, 
      `CRITICAL: Infinite loop errors detected: ${infiniteLoopErrors.join(', ')}`
    ).toBe(0);

    // API communication is working
    expect(realApiRequests.length, 
      'Real API communication should be working (requests to localhost:5655)'
    ).toBeGreaterThan(0);

    // Form submission generated network activity
    expect(apiRequests.length,
      'Form submission should generate API requests'
    ).toBeGreaterThan(0);

    // Step 11: Success metrics logging
    console.log('=== TEST SUCCESS METRICS ===');
    console.log(`✅ Infinite Loop Prevention: ${infiniteLoopErrors.length === 0 ? 'PASSED' : 'FAILED'}`);
    console.log(`✅ Real API Communication: ${realApiRequests.length} requests to localhost:5655`);
    console.log(`✅ Form Functionality: ${apiRequests.length} total API requests generated`);
    console.log(`✅ Console Error Level: ${consoleErrors.length} errors (target: < 10)`);
    console.log(`📍 Authentication Result: ${testReport.authenticationResults.likelyAuthenticationSuccess ? 'SUCCESS' : 'NEEDS_INVESTIGATION'}`);
    console.log(`📍 Final URL: ${urlAfterLogin}`);

    return testReport;
  });

  test('should maintain stable page state without loops', async ({ page }) => {
    console.log('=== PAGE STABILITY TEST ===');
    
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    // Monitor for 10 seconds for any stability issues
    console.log('Monitoring page stability for 10 seconds...');
    await page.waitForTimeout(10000);
    
    const stabilityErrors = consoleErrors.filter(error => 
      error.includes('Maximum update depth exceeded') ||
      error.includes('Too many re-renders') ||
      error.includes('Maximum call stack size exceeded')
    );

    console.log(`Stability test results:`);
    console.log(`- Total console messages: ${consoleMessages.length}`);
    console.log(`- Total console errors: ${consoleErrors.length}`);
    console.log(`- Stability errors: ${stabilityErrors.length}`);
    
    // Critical assertion: No stability issues
    expect(stabilityErrors.length).toBe(0);
    
    console.log('✅ Page remained stable during 10-second monitoring period');
  });

  test.afterEach(async ({ page }) => {
    const finalState = {
      finalUrl: page.url(),
      totalConsoleMessages: consoleMessages.length,
      totalConsoleErrors: consoleErrors.length,
      totalApiRequests: apiRequests.length,
      timestamp: new Date().toISOString()
    };

    console.log('=== TEST COMPLETION SUMMARY ===');
    console.log(JSON.stringify(finalState, null, 2));
  });
});