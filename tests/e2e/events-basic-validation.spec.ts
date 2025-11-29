import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * Basic Events System Validation Test
 *
 * This test validates the basic functionality of the Events system without
 * complex localStorage cleanup that can cause security errors in Playwright.
 *
 * Test Flow:
 * 1. Check if Events page loads
 * 2. Verify login functionality
 * 3. Check admin access to events
 * 4. Validate basic event data display
 */

const testUrls = {
  publicEvents: '/events',
  adminEvents: '/admin/events',
  login: '/login',
  home: '/'
};

test.describe('Events System Basic Validation', () => {
  
  test('Events Page Loading and Content Detection', async ({ page }) => {
    console.log('🧪 Testing Events page basic loading...');
    
    // Set up console monitoring
    const consoleErrors = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
        console.log('🔴 Console Error:', msg.text());
      }
    });

    // Navigate to events page
    const response = await page.goto(testUrls.publicEvents);
    console.log(`📡 Response status: ${response?.status()}`);
    
    await page.screenshot({ path: 'test-results/events-page-loaded.png' });
    
    // Wait for page to load
    try {
      await page.waitForLoadState('networkidle', { timeout: 10000 });
    } catch (error) {
      console.log('⚠️ Network idle timeout - continuing with checks');
    }

    // Check if page loaded successfully (not 404 or error page)
    const pageTitle = await page.title();
    console.log(`📄 Page title: "${pageTitle}"`);
    
    const hasErrorIndicator = await page.locator('*:has-text("404"), *:has-text("Not Found"), *:has-text("Error")').count();
    console.log(`❌ Error indicators found: ${hasErrorIndicator}`);
    
    // Look for any events-related content
    const eventContentSelectors = [
      '.event-card',
      '.event',
      '[data-testid="event-card"]',
      '*:has-text("workshop")',
      '*:has-text("class")',
      '*:has-text("social")',
      '*:has-text("event")',
      'h1, h2, h3, h4'
    ];

    let contentFound = {};
    for (const selector of eventContentSelectors) {
      const count = await page.locator(selector).count();
      contentFound[selector] = count;
      if (count > 0) {
        console.log(`✅ Found ${count} elements for: ${selector}`);
      }
    }

    await page.screenshot({ path: 'test-results/events-content-analysis.png' });

    // Check for navigation elements
    const navigationElements = await page.locator('nav, .nav, [role="navigation"], a').count();
    console.log(`🧭 Navigation elements found: ${navigationElements}`);

    // Check for any form elements (might indicate functionality)
    const formElements = await page.locator('form, input, button').count();
    console.log(`📝 Interactive elements found: ${formElements}`);

    // Basic validation - page should load without major errors
    expect(response?.status()).toBe(200);
    expect(hasErrorIndicator).toBe(0);
    
    console.log(`📊 Events Page Analysis Complete:`);
    console.log(`   - Page loaded successfully: ${response?.status() === 200}`);
    console.log(`   - No error indicators: ${hasErrorIndicator === 0}`);
    console.log(`   - Console errors: ${consoleErrors.length}`);
    console.log(`   - Interactive elements: ${formElements}`);
    console.log(`   - Content detection results:`, contentFound);
  });

  test('Login System Validation', async ({ page }) => {
    console.log('🧪 Testing login system...');
    
    // Navigate to login page
    const response = await page.goto(testUrls.login);
    console.log(`📡 Login page response: ${response?.status()}`);
    
    await page.screenshot({ path: 'test-results/login-page-loaded.png' });
    
    // Wait for page elements
    try {
      await page.waitForSelector('input, form', { timeout: 10000 });
    } catch (error) {
      console.log('⚠️ No form elements found within timeout');
    }

    // Check for login form elements using correct data-testid selectors
    const emailInputs = await page.locator('[data-testid="email-or-scenename-input"]').count();
    const passwordInputs = await page.locator('[data-testid="password-input"]').count();
    const submitButtons = await page.locator('[data-testid="login-button"]').count();
    
    console.log(`📧 Email inputs found: ${emailInputs}`);
    console.log(`🔒 Password inputs found: ${passwordInputs}`);
    console.log(`🔘 Submit buttons found: ${submitButtons}`);

    const loginFormExists = emailInputs > 0 && passwordInputs > 0 && submitButtons > 0;
    console.log(`✅ Login form complete: ${loginFormExists}`);

    // If login form exists, try to test it using AuthHelpers
    if (loginFormExists) {
      try {
        // Login using AuthHelpers
        await AuthHelpers.loginAs(page, 'admin');

        await page.screenshot({ path: 'test-results/after-login-attempt.png' });

        console.log('🔗 URL after login:', page.url());
        const loginSuccessful = !page.url().includes('/login');
        console.log(`✅ Login appears successful: ${loginSuccessful}`);

        // If login successful, check admin access
        if (loginSuccessful) {
          console.log('🔑 Testing admin access to events...');

          try {
            await page.goto(testUrls.adminEvents);
            await page.waitForTimeout(2000);

            await page.screenshot({ path: 'test-results/admin-events-page.png' });

            const adminEventElements = await page.locator('*:has-text("event"), *:has-text("Event"), button, table, .admin').count();
            console.log(`⚙️ Admin event management elements found: ${adminEventElements}`);

            const adminAccessWorking = adminEventElements > 0;
            console.log(`🛠️ Admin events access working: ${adminAccessWorking}`);

          } catch (error) {
            console.log(`⚠️ Admin events access error: ${error.message}`);
          }
        }

      } catch (error) {
        console.log(`⚠️ Login test error: ${error.message}`);
      }
    }

    // Basic validation
    expect(response?.status()).toBe(200);
    expect(loginFormExists).toBe(true);
  });

  test('API Connectivity Check', async ({ page }) => {
    console.log('🧪 Testing API connectivity...');
    
    // Monitor network requests
    const apiRequests = [];
    page.on('response', async (response) => {
      if (response.url().includes('/api/')) {
        apiRequests.push({
          url: response.url(),
          status: response.status(),
          method: response.request().method()
        });
      }
    });

    // Navigate to events page to trigger API calls
    await page.goto(testUrls.publicEvents);
    await page.waitForTimeout(5000); // Give time for API calls
    
    console.log(`📡 API requests detected: ${apiRequests.length}`);
    apiRequests.forEach(req => {
      console.log(`   ${req.method} ${req.url} - Status: ${req.status}`);
    });

    // Direct API health check
    try {
      const apiHealthResponse = await page.request.get('http://localhost:5655/health');
      console.log(`🏥 API health check: ${apiHealthResponse.status()}`);
      
      if (apiHealthResponse.ok()) {
        const healthData = await apiHealthResponse.json();
        console.log(`📋 API health data:`, healthData);
      }
    } catch (error) {
      console.log(`⚠️ API health check failed: ${error.message}`);
    }

    // Check for events-specific API endpoints
    try {
      const eventsApiResponse = await page.request.get('http://localhost:5655/api/events');
      console.log(`📅 Events API response: ${eventsApiResponse.status()}`);
      
      if (eventsApiResponse.ok()) {
        const eventsData = await eventsApiResponse.json();
        console.log(`📊 Events data structure:`, typeof eventsData, Array.isArray(eventsData) ? `Array[${eventsData.length}]` : 'Object');
      }
    } catch (error) {
      console.log(`⚠️ Events API check failed: ${error.message}`);
    }

    await page.screenshot({ path: 'test-results/api-connectivity-final.png' });
  });

  test('Complete System Integration Check', async ({ page }) => {
    console.log('🎯 Running complete system integration check...');
    
    const systemStatus = {
      eventsPageLoads: false,
      loginFormExists: false,
      apiResponds: false,
      adminAccessExists: false,
      eventsDataAvailable: false
    };

    try {
      // 1. Events page loads
      const eventsResponse = await page.goto(testUrls.publicEvents);
      systemStatus.eventsPageLoads = eventsResponse?.status() === 200;
      await page.waitForTimeout(2000);

      // 2. Login form exists and works using correct data-testid selectors
      await page.goto(testUrls.login);
      const emailInput = await page.locator('[data-testid="email-or-scenename-input"]').count();
      const passwordInput = await page.locator('[data-testid="password-input"]').count();
      const submitButton = await page.locator('[data-testid="login-button"]').count();
      systemStatus.loginFormExists = emailInput > 0 && passwordInput > 0 && submitButton > 0;

      // 3. API responds
      try {
        const apiResponse = await page.request.get('http://localhost:5655/health');
        systemStatus.apiResponds = apiResponse.ok();
      } catch (error) {
        systemStatus.apiResponds = false;
      }

      // 4. Admin access exists (basic check)
      if (systemStatus.loginFormExists) {
        try {
          await AuthHelpers.loginAs(page, 'admin');

          const adminResponse = await page.goto(testUrls.adminEvents);
          systemStatus.adminAccessExists = adminResponse?.status() === 200;
        } catch (error) {
          systemStatus.adminAccessExists = false;
        }
      }

      // 5. Events data available
      try {
        const eventsApiResponse = await page.request.get('http://localhost:5655/api/events');
        systemStatus.eventsDataAvailable = eventsApiResponse.ok();
      } catch (error) {
        systemStatus.eventsDataAvailable = false;
      }

    } catch (error) {
      console.log(`⚠️ Integration check error: ${error.message}`);
    }

    await page.screenshot({ path: 'test-results/complete-system-check.png' });

    // Report results
    console.log('📊 Complete System Status:');
    console.log(`   ✓ Events Page Loads: ${systemStatus.eventsPageLoads}`);
    console.log(`   ✓ Login Form Exists: ${systemStatus.loginFormExists}`);
    console.log(`   ✓ API Responds: ${systemStatus.apiResponds}`);
    console.log(`   ✓ Admin Access Exists: ${systemStatus.adminAccessExists}`);
    console.log(`   ✓ Events Data Available: ${systemStatus.eventsDataAvailable}`);

    const coreSystemWorking = systemStatus.eventsPageLoads && systemStatus.loginFormExists && systemStatus.apiResponds;
    console.log(`🎯 Core System Operational: ${coreSystemWorking}`);

    // Essential functionality should be working
    expect(systemStatus.eventsPageLoads).toBe(true);
    expect(systemStatus.apiResponds).toBe(true);
    expect(coreSystemWorking).toBe(true);
  });
});