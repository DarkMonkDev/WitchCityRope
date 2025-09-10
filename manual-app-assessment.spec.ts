import { test, expect } from '@playwright/test';

test.describe('Manual App Assessment', () => {
  test('capture app state and navigation', async ({ page }) => {
    console.log('🔍 Starting manual app assessment...');

    // Navigate to homepage
    await page.goto('http://localhost:5173');
    
    // Capture homepage
    await page.screenshot({ path: 'test-results/homepage-current-state.png', fullPage: true });
    console.log('📸 Homepage screenshot captured');

    // Try to navigate to login
    try {
      await page.click('text=Login', { timeout: 5000 });
      await page.screenshot({ path: 'test-results/login-page-current-state.png', fullPage: true });
      console.log('📸 Login page screenshot captured');
    } catch (e) {
      console.log('⚠️ No Login link found on homepage');
      
      // Try direct navigation to login
      await page.goto('http://localhost:5173/login');
      await page.screenshot({ path: 'test-results/login-direct-navigation.png', fullPage: true });
      console.log('📸 Direct login navigation screenshot captured');
    }

    // Try to navigate to events
    await page.goto('http://localhost:5173');
    try {
      await page.click('text=Events', { timeout: 5000 });
      await page.screenshot({ path: 'test-results/events-page-current-state.png', fullPage: true });
      console.log('📸 Events page screenshot captured');
    } catch (e) {
      console.log('⚠️ No Events link found on homepage');
      
      // Try direct navigation to events
      await page.goto('http://localhost:5173/events');
      await page.screenshot({ path: 'test-results/events-direct-navigation.png', fullPage: true });
      console.log('📸 Direct events navigation screenshot captured');
    }

    // Check for navigation menu
    await page.goto('http://localhost:5173');
    const navElements = await page.locator('nav, header, .navbar, .menu').all();
    console.log(`📋 Found ${navElements.length} navigation elements`);

    // Check for common UI elements
    const loginButtons = await page.locator('text=Login, text=Sign In').all();
    const eventsLinks = await page.locator('text=Events, text=Classes').all();
    const menuButtons = await page.locator('[role="button"], button').all();
    
    console.log(`🔍 Found ${loginButtons.length} login-related elements`);
    console.log(`🔍 Found ${eventsLinks.length} events-related elements`);
    console.log(`🔍 Found ${menuButtons.length} buttons/interactive elements`);

    // Try to find any dropdown menus
    try {
      await page.click('[data-testid="user-menu"], .user-menu, .dropdown', { timeout: 2000 });
      await page.screenshot({ path: 'test-results/dropdown-menu-state.png', fullPage: true });
      console.log('📸 Dropdown menu screenshot captured');
    } catch (e) {
      console.log('ℹ️ No dropdown menu found or accessible');
    }

    // Get page title and URL
    const title = await page.title();
    const url = page.url();
    console.log(`📝 Page title: ${title}`);
    console.log(`📝 Current URL: ${url}`);

    // Check for API calls
    page.on('response', response => {
      if (response.url().includes('/api/')) {
        console.log(`📡 API call detected: ${response.url()} - Status: ${response.status()}`);
      }
    });

    // Final assessment screenshot
    await page.screenshot({ path: 'test-results/app-final-assessment.png', fullPage: true });
    console.log('✅ Manual assessment completed');
  });

  test('test authentication flow', async ({ page }) => {
    console.log('🔐 Testing authentication flow...');

    // Go to login page
    await page.goto('http://localhost:5173/login');
    await page.screenshot({ path: 'test-results/auth-login-page.png', fullPage: true });

    // Check for login form elements
    const emailInput = page.locator('[data-testid="email-input"], input[type="email"], input[name="email"]');
    const passwordInput = page.locator('[data-testid="password-input"], input[type="password"], input[name="password"]');
    const loginButton = page.locator('[data-testid="login-button"], button[type="submit"], text=Login');

    const emailExists = await emailInput.count() > 0;
    const passwordExists = await passwordInput.count() > 0;
    const buttonExists = await loginButton.count() > 0;

    console.log(`📧 Email input found: ${emailExists}`);
    console.log(`🔒 Password input found: ${passwordExists}`);
    console.log(`🔘 Login button found: ${buttonExists}`);

    if (emailExists && passwordExists && buttonExists) {
      // Try to fill and submit login form
      await emailInput.fill('admin@witchcityrope.com');
      await passwordInput.fill('Test123!');
      await page.screenshot({ path: 'test-results/auth-form-filled.png', fullPage: true });
      
      await loginButton.click();
      await page.waitForTimeout(3000); // Wait for response
      await page.screenshot({ path: 'test-results/auth-after-submit.png', fullPage: true });
      
      // Check if we're redirected or see an error
      const currentUrl = page.url();
      console.log(`📍 URL after login attempt: ${currentUrl}`);
      
      // Check for any error messages or success indicators
      const errorMessages = await page.locator('text=error, text=Error, .error, .alert').all();
      const successMessages = await page.locator('text=welcome, text=Welcome, text=dashboard, text=Dashboard').all();
      
      console.log(`❌ Error messages found: ${errorMessages.length}`);
      console.log(`✅ Success indicators found: ${successMessages.length}`);
    }
  });

  test('check for user dashboard features', async ({ page }) => {
    console.log('👤 Checking for user dashboard features...');

    // Try common dashboard routes
    const dashboardRoutes = [
      '/dashboard',
      '/profile', 
      '/my-events',
      '/account'
    ];

    for (const route of dashboardRoutes) {
      try {
        await page.goto(`http://localhost:5173${route}`);
        await page.screenshot({ path: `test-results/dashboard-route-${route.replace('/', '')}.png`, fullPage: true });
        console.log(`📸 Captured ${route} page`);
        
        const title = await page.title();
        const content = await page.textContent('body');
        console.log(`📝 ${route} - Title: ${title.slice(0, 50)}...`);
        console.log(`📝 ${route} - Content preview: ${content?.slice(0, 100)}...`);
      } catch (e) {
        console.log(`⚠️ Could not access ${route}: ${e.message.slice(0, 100)}`);
      }
    }
  });
});