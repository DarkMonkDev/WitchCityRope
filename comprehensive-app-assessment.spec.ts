import { test, expect } from '@playwright/test';

test.describe('Comprehensive App Assessment', () => {
  test('comprehensive app state assessment', async ({ page }) => {
    console.log('🎯 Starting comprehensive app assessment...');

    // Navigate to homepage
    console.log('📍 Navigating to homepage...');
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(3000); // Wait for React to load
    
    // Capture homepage
    await page.screenshot({ path: 'test-results/01-homepage-loaded.png', fullPage: true });
    console.log('📸 Homepage screenshot captured');

    // Check page title and basic elements
    const title = await page.title();
    const url = page.url();
    console.log(`📝 Page title: ${title}`);
    console.log(`📝 Current URL: ${url}`);

    // Look for key navigation elements
    const logo = page.locator('text=WITCH CITY ROPE');
    const eventsLink = page.locator('[data-testid="link-events"]');
    const loginButton = page.locator('text=Login');
    
    console.log(`🔍 Logo visible: ${await logo.isVisible()}`);
    console.log(`🔍 Events link visible: ${await eventsLink.isVisible()}`);
    console.log(`🔍 Login button visible: ${await loginButton.isVisible()}`);

    // Test navigation to events page
    console.log('🚀 Testing navigation to events page...');
    await eventsLink.click();
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'test-results/02-events-page.png', fullPage: true });
    console.log('📸 Events page screenshot captured');
    
    const eventsUrl = page.url();
    console.log(`📝 Events page URL: ${eventsUrl}`);

    // Test navigation to login page
    console.log('🚀 Testing navigation to login page...');
    await page.goto('http://localhost:5173/login');
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'test-results/03-login-page.png', fullPage: true });
    console.log('📸 Login page screenshot captured');
    
    // Check for login form elements
    const emailField = page.locator('input[type="email"], [data-testid="email-input"]');
    const passwordField = page.locator('input[type="password"], [data-testid="password-input"]');
    const submitButton = page.locator('button[type="submit"], [data-testid="login-button"]');
    
    console.log(`📧 Email field found: ${await emailField.count() > 0}`);
    console.log(`🔒 Password field found: ${await passwordField.count() > 0}`);
    console.log(`🔘 Submit button found: ${await submitButton.count() > 0}`);

    // Test login flow if form exists
    if (await emailField.count() > 0 && await passwordField.count() > 0) {
      console.log('🔐 Testing login flow...');
      await emailField.fill('admin@witchcityrope.com');
      await passwordField.fill('Test123!');
      await page.screenshot({ path: 'test-results/04-login-form-filled.png', fullPage: true });
      
      await submitButton.click();
      await page.waitForTimeout(3000); // Wait for login response
      await page.screenshot({ path: 'test-results/05-after-login-attempt.png', fullPage: true });
      
      const postLoginUrl = page.url();
      console.log(`📝 URL after login attempt: ${postLoginUrl}`);
      
      // Check if user is now authenticated
      const welcomeMessage = page.locator('text=Welcome');
      const logoutButton = page.locator('text=Logout, [data-testid="button-logout"]');
      const dashboardLink = page.locator('text=Dashboard, [data-testid="avatar-user"]');
      
      console.log(`👋 Welcome message visible: ${await welcomeMessage.isVisible()}`);
      console.log(`🚪 Logout button visible: ${await logoutButton.isVisible()}`);
      console.log(`📊 Dashboard link visible: ${await dashboardLink.isVisible()}`);
      
      // If authenticated, test dashboard access
      if (await dashboardLink.isVisible()) {
        console.log('📊 Testing dashboard access...');
        await dashboardLink.click();
        await page.waitForTimeout(2000);
        await page.screenshot({ path: 'test-results/06-dashboard-page.png', fullPage: true });
        
        const dashboardUrl = page.url();
        console.log(`📝 Dashboard URL: ${dashboardUrl}`);
      }
    }

    // Test various dashboard routes
    const dashboardRoutes = ['/dashboard', '/dashboard/events', '/dashboard/profile'];
    for (const route of dashboardRoutes) {
      console.log(`🧭 Testing route: ${route}`);
      try {
        await page.goto(`http://localhost:5173${route}`);
        await page.waitForTimeout(2000);
        const routeName = route.replace('/dashboard', '').replace('/', '-') || 'main';
        await page.screenshot({ path: `test-results/07-dashboard${routeName}.png`, fullPage: true });
        
        const routeUrl = page.url();
        console.log(`📝 Route ${route} - Final URL: ${routeUrl}`);
      } catch (e) {
        console.log(`⚠️ Route ${route} failed: ${e.message.slice(0, 100)}`);
      }
    }

    console.log('✅ Comprehensive assessment completed');
  });

  test('API integration check', async ({ page }) => {
    console.log('📡 Testing API integration...');
    
    const apiCalls = [];
    
    // Monitor network requests
    page.on('response', response => {
      if (response.url().includes('/api/')) {
        apiCalls.push({
          url: response.url(),
          status: response.status(),
          method: response.request().method()
        });
        console.log(`📡 API call: ${response.request().method()} ${response.url()} - ${response.status()}`);
      }
    });

    // Navigate to pages that should make API calls
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(5000); // Wait for potential API calls
    
    await page.goto('http://localhost:5173/events');
    await page.waitForTimeout(5000); // Wait for events API call
    
    console.log(`📊 Total API calls detected: ${apiCalls.length}`);
    apiCalls.forEach(call => {
      console.log(`  ${call.method} ${call.url} - ${call.status}`);
    });
    
    await page.screenshot({ path: 'test-results/08-api-integration-test.png', fullPage: true });
  });
});