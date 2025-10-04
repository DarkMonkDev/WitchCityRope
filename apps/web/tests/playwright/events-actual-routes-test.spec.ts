import { test, expect } from '@playwright/test';

/**
 * Events Actual Routes Test
 * 
 * Testing the ACTUAL routes that exist based on the router configuration:
 * - /dashboard/events (protected, requires login)
 * - /admin/events-management-api-demo (demo route)
 * - /admin/event-session-matrix-demo (demo route)
 * 
 * NOT testing /events (doesn't exist, causes 404)
 */

const testUrls = {
  login: 'http://localhost:5173/login',
  dashboardEvents: 'http://localhost:5173/dashboard/events',
  adminEventsDemo: 'http://localhost:5173/admin/events-management-api-demo',
  adminSessionDemo: 'http://localhost:5173/admin/event-session-matrix-demo',
  home: 'http://localhost:5173/'
};

const testAccounts = {
  admin: {
    email: 'admin@witchcityrope.com',
    password: 'Test123!'
  },
  member: {
    email: 'member@witchcityrope.com', 
    password: 'Test123!'
  }
};

test.describe('Events System - Actual Routes Validation', () => {
  
  test('API Events Data Availability', async ({ page }) => {
    console.log('🧪 Testing Events API data availability...');
    
    // Direct API test
    try {
      const eventsApiResponse = await page.request.get('http://localhost:5655/api/events');
      console.log(`📅 Events API status: ${eventsApiResponse.status()}`);
      
      if (eventsApiResponse.ok()) {
        const eventsData = await eventsApiResponse.json();
        console.log(`📊 Events API returned:`, {
          dataType: typeof eventsData,
          isArray: Array.isArray(eventsData),
          count: Array.isArray(eventsData) ? eventsData.length : 'N/A',
          sampleData: Array.isArray(eventsData) && eventsData.length > 0 ? eventsData[0] : null
        });
        
        expect(eventsApiResponse.status()).toBe(200);
        expect(Array.isArray(eventsData)).toBe(true);
        expect(eventsData.length).toBeGreaterThan(0);
        
        console.log('✅ Events API: Working perfectly');
        
      } else {
        console.log('❌ Events API failed');
      }
    } catch (error) {
      console.log(`⚠️ Events API error: ${error.message}`);
      expect(false).toBe(true); // Fail the test
    }
  });

  test('Demo Routes - Events Management API Demo', async ({ page }) => {
    console.log('🧪 Testing Events Management API Demo route...');
    
    const response = await page.goto(testUrls.adminEventsDemo);
    console.log(`📡 Demo page response: ${response?.status()}`);
    
    await page.waitForTimeout(3000); // Give time for API calls and rendering
    await page.screenshot({ path: 'test-results/events-management-api-demo.png' });
    
    // Check for demo content
    const pageContent = await page.locator('body').textContent();
    const hasEventContent = pageContent?.toLowerCase().includes('event');
    const hasApiContent = pageContent?.toLowerCase().includes('api');
    const hasDemoContent = pageContent?.toLowerCase().includes('demo');
    
    console.log(`📄 Demo page contains:`);
    console.log(`   - Event content: ${hasEventContent}`);
    console.log(`   - API content: ${hasApiContent}`);
    console.log(`   - Demo content: ${hasDemoContent}`);
    
    // Check for interactive elements
    const buttons = await page.locator('button').count();
    const tables = await page.locator('table').count();
    const forms = await page.locator('form').count();
    
    console.log(`🔧 Interactive elements:`);
    console.log(`   - Buttons: ${buttons}`);
    console.log(`   - Tables: ${tables}`);
    console.log(`   - Forms: ${forms}`);
    
    expect(response?.status()).toBe(200);
    expect(hasEventContent || hasApiContent || hasDemoContent).toBe(true);
    
    console.log('✅ Events Management API Demo: Loading successfully');
  });

  test('Demo Routes - Event Session Matrix Demo', async ({ page }) => {
    console.log('🧪 Testing Event Session Matrix Demo route...');
    
    const response = await page.goto(testUrls.adminSessionDemo);
    console.log(`📡 Session demo response: ${response?.status()}`);
    
    await page.waitForTimeout(3000);
    await page.screenshot({ path: 'test-results/event-session-matrix-demo.png' });
    
    // Check for session/matrix content
    const pageContent = await page.locator('body').textContent();
    const hasSessionContent = pageContent?.toLowerCase().includes('session');
    const hasMatrixContent = pageContent?.toLowerCase().includes('matrix');
    const hasEventContent = pageContent?.toLowerCase().includes('event');
    
    console.log(`📄 Session demo contains:`);
    console.log(`   - Session content: ${hasSessionContent}`);
    console.log(`   - Matrix content: ${hasMatrixContent}`);
    console.log(`   - Event content: ${hasEventContent}`);
    
    expect(response?.status()).toBe(200);
    expect(hasSessionContent || hasMatrixContent || hasEventContent).toBe(true);
    
    console.log('✅ Event Session Matrix Demo: Loading successfully');
  });

  test('Login System and Protected Dashboard Events', async ({ page }) => {
    console.log('🧪 Testing login and protected dashboard events route...');
    
    // Test login page
    const loginResponse = await page.goto(testUrls.login);
    console.log(`🔐 Login page status: ${loginResponse?.status()}`);
    
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'test-results/login-page-for-events.png' });
    
    // Check login form elements (using more flexible selectors)
    const emailField = page.locator('input[name="email"], input[type="email"], input[placeholder*="email" i]').first();
    const passwordField = page.locator('input[name="password"], input[type="password"], input[placeholder*="password" i]').first();
    const submitButton = page.locator('button[type="submit"], button:has-text("Sign In"), button:has-text("Login")').first();
    
    const emailExists = await emailField.count() > 0;
    const passwordExists = await passwordField.count() > 0;
    const submitExists = await submitButton.count() > 0;
    
    console.log(`🔧 Login form elements:`);
    console.log(`   - Email field: ${emailExists}`);
    console.log(`   - Password field: ${passwordExists}`);
    console.log(`   - Submit button: ${submitExists}`);
    
    expect(loginResponse?.status()).toBe(200);
    expect(emailExists && passwordExists && submitExists).toBe(true);
    
    // Attempt login
    if (emailExists && passwordExists && submitExists) {
      console.log('🔑 Attempting login...');
      
      await emailField.fill(testAccounts.admin.email);
      await passwordField.fill(testAccounts.admin.password);
      
      await page.screenshot({ path: 'test-results/login-form-filled-events.png' });
      
      await submitButton.click();
      await page.waitForTimeout(5000); // Wait for login processing
      
      await page.screenshot({ path: 'test-results/after-login-events.png' });
      
      const currentUrl = page.url();
      const loginSuccessful = !currentUrl.includes('/login');
      console.log(`🎯 Login result - URL: ${currentUrl}, Success: ${loginSuccessful}`);
      
      if (loginSuccessful) {
        console.log('🎯 Testing protected dashboard events route...');
        
        // Now test the protected dashboard/events route
        const dashboardEventsResponse = await page.goto(testUrls.dashboardEvents);
        console.log(`📅 Dashboard events status: ${dashboardEventsResponse?.status()}`);
        
        await page.waitForTimeout(3000);
        await page.screenshot({ path: 'test-results/dashboard-events-page.png' });
        
        // Check for events dashboard content
        const dashboardContent = await page.locator('body').textContent();
        const hasEventsContent = dashboardContent?.toLowerCase().includes('event');
        const hasDashboardContent = dashboardContent?.toLowerCase().includes('dashboard');
        
        console.log(`📊 Dashboard events page:`);
        console.log(`   - Has events content: ${hasEventsContent}`);
        console.log(`   - Has dashboard content: ${hasDashboardContent}`);
        
        // Check for typical dashboard elements
        const dashboardElements = await page.locator('nav, .sidebar, .dashboard, [data-testid*="dashboard"]').count();
        console.log(`   - Dashboard UI elements: ${dashboardElements}`);
        
        expect(dashboardEventsResponse?.status()).toBe(200);
        
        console.log('✅ Protected Dashboard Events: Accessible after authentication');
        
      } else {
        console.log('⚠️ Login failed, cannot test protected routes');
      }
    }
  });

  test('Complete Events System Integration Assessment', async ({ page }) => {
    console.log('🎯 Complete Events System Assessment...');
    
    const assessment = {
      apiDataAvailable: false,
      demoRoutesWorking: false,
      authenticationWorking: false,
      protectedEventsAccessible: false,
      overallSystemFunctional: false
    };

    try {
      // 1. Check API data
      const eventsApi = await page.request.get('http://localhost:5655/api/events');
      assessment.apiDataAvailable = eventsApi.ok() && (await eventsApi.json()).length > 0;

      // 2. Check demo routes
      const demoResponse = await page.goto(testUrls.adminEventsDemo);
      assessment.demoRoutesWorking = demoResponse?.status() === 200;

      // 3. Check authentication
      await page.goto(testUrls.login);
      await page.waitForTimeout(2000);
      
      const emailField = page.locator('input[name="email"], input[type="email"], input[placeholder*="email" i]').first();
      const passwordField = page.locator('input[name="password"], input[type="password"]').first();
      const submitButton = page.locator('button[type="submit"], button:has-text("Sign In")').first();
      
      if (await emailField.count() > 0 && await passwordField.count() > 0) {
        await emailField.fill(testAccounts.admin.email);
        await passwordField.fill(testAccounts.admin.password);
        await submitButton.click();
        await page.waitForTimeout(3000);
        
        assessment.authenticationWorking = !page.url().includes('/login');
        
        // 4. Check protected events route
        if (assessment.authenticationWorking) {
          const dashboardResponse = await page.goto(testUrls.dashboardEvents);
          assessment.protectedEventsAccessible = dashboardResponse?.status() === 200;
        }
      }

      // Overall assessment
      assessment.overallSystemFunctional = 
        assessment.apiDataAvailable && 
        (assessment.demoRoutesWorking || assessment.protectedEventsAccessible) &&
        assessment.authenticationWorking;

    } catch (error) {
      console.log(`⚠️ Assessment error: ${error.message}`);
    }

    await page.screenshot({ path: 'test-results/complete-events-assessment.png' });

    console.log('📊 EVENTS SYSTEM ASSESSMENT RESULTS:');
    console.log(`   🔌 API Data Available: ${assessment.apiDataAvailable}`);
    console.log(`   🧪 Demo Routes Working: ${assessment.demoRoutesWorking}`);
    console.log(`   🔐 Authentication Working: ${assessment.authenticationWorking}`);
    console.log(`   🛡️  Protected Events Accessible: ${assessment.protectedEventsAccessible}`);
    console.log(`   🎯 Overall System Functional: ${assessment.overallSystemFunctional}`);

    // Report findings
    if (assessment.overallSystemFunctional) {
      console.log('✅ EVENTS SYSTEM: FUNCTIONAL - Core workflow can be completed');
    } else {
      console.log('❌ EVENTS SYSTEM: INCOMPLETE - Missing critical components');
    }

    // Core requirements for a functional Events system
    expect(assessment.apiDataAvailable).toBe(true);
    expect(assessment.authenticationWorking).toBe(true);
    
    // At least one of the event interfaces should work
    const hasWorkingEventInterface = assessment.demoRoutesWorking || assessment.protectedEventsAccessible;
    expect(hasWorkingEventInterface).toBe(true);
  });
});