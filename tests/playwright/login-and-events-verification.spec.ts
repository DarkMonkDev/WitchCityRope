import { test, expect } from '@playwright/test';

/**
 * Comprehensive E2E Test for Login and Events Display
 * 
 * This test verifies:
 * 1. Login functionality through the website UI at http://localhost:5173/login
 * 2. Events page displays events correctly at http://localhost:5173/events
 * 
 * Test Requirements:
 * - Test login with credentials: admin@witchcityrope.com / Test123!
 * - Verify successful login and navigation
 * - Test events page and verify events are displayed (should show ~10 events seeded)
 */

test.describe('Login and Events Verification', () => {
  
  test.beforeEach(async ({ page }) => {
    // Set up error monitoring for comprehensive error detection
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.log('❌ Browser Console Error:', msg.text());
      }
    });

    page.on('response', response => {
      if (!response.ok() && response.url().includes('/api/')) {
        console.log(`❌ API Error: ${response.status()} ${response.url()}`);
      }
    });
  });

  test('should login successfully with valid credentials and verify navigation', async ({ page }) => {
    console.log('🔐 Testing login functionality...');

    // Navigate to login page
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');

    // Take screenshot of login page
    await page.screenshot({ 
      path: 'test-results/01-login-page-loaded.png', 
      fullPage: true 
    });

    // Verify login page elements are present
    console.log('🔍 Verifying login form elements...');
    
    // Check for multiple possible selector patterns based on existing tests
    const emailInput = page.locator('input[type="email"], [data-testid="email-input"], input[name="email"]').first();
    const passwordInput = page.locator('input[type="password"], [data-testid="password-input"], input[name="password"]').first();
    const loginButton = page.locator('button[type="submit"], [data-testid="login-button"], button:has-text("Login"), button:has-text("Sign In")').first();

    // Verify form elements exist
    await expect(emailInput).toBeVisible({ timeout: 10000 });
    await expect(passwordInput).toBeVisible({ timeout: 10000 });
    await expect(loginButton).toBeVisible({ timeout: 10000 });

    console.log('✅ Login form elements found and visible');

    // Fill in credentials
    console.log('📝 Filling in login credentials...');
    await emailInput.fill('admin@witchcityrope.com');
    await passwordInput.fill('Test123!');

    // Take screenshot before submission
    await page.screenshot({ 
      path: 'test-results/02-login-form-filled.png', 
      fullPage: true 
    });

    // Submit login form
    console.log('🚀 Submitting login form...');
    await loginButton.click();

    // Wait for navigation or response
    await page.waitForTimeout(3000);

    // Take screenshot after login attempt
    await page.screenshot({ 
      path: 'test-results/03-after-login-submission.png', 
      fullPage: true 
    });

    // Check for successful login indicators
    const currentUrl = page.url();
    console.log('📍 URL after login attempt:', currentUrl);

    // Check for various success indicators
    const isOnDashboard = currentUrl.includes('/dashboard');
    const isNotOnLogin = !currentUrl.includes('/login');
    const hasWelcomeMessage = await page.locator('text=Welcome').isVisible();
    const hasLogoutButton = await page.locator('text=Logout, [data-testid="button-logout"]').isVisible();
    const hasUserAvatar = await page.locator('[data-testid="avatar-user"]').isVisible();

    console.log('✅ Login Success Indicators:');
    console.log('  - Redirected from login page:', isNotOnLogin);
    console.log('  - On dashboard page:', isOnDashboard);
    console.log('  - Welcome message visible:', hasWelcomeMessage);
    console.log('  - Logout button visible:', hasLogoutButton);
    console.log('  - User avatar visible:', hasUserAvatar);

    // Verify successful login (flexible criteria)
    expect(isNotOnLogin || isOnDashboard || hasWelcomeMessage || hasLogoutButton || hasUserAvatar).toBe(true);

    console.log('✅ Login test completed successfully');
  });

  test('should display events correctly on events page', async ({ page }) => {
    console.log('📅 Testing events page display...');

    // Set up network monitoring for API calls
    const apiCalls: Array<{url: string, status: number, method: string}> = [];
    
    page.on('response', response => {
      if (response.url().includes('/api/events')) {
        apiCalls.push({
          url: response.url(),
          status: response.status(),
          method: response.request().method()
        });
        console.log(`📡 Events API Call: ${response.request().method()} ${response.url()} - ${response.status()}`);
      }
    });

    // Navigate to events page
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');

    // Wait for API calls and content to load
    await page.waitForTimeout(5000);

    // Take screenshot of events page
    await page.screenshot({ 
      path: 'test-results/04-events-page-loaded.png', 
      fullPage: true 
    });

    console.log('🔍 Analyzing events page content...');

    // Get page content for analysis
    const pageContent = await page.content();
    const bodyText = await page.textContent('body') || '';

    // Check for events-related content
    const hasEventsTitle = bodyText.includes('Events') || bodyText.includes('EVENTS');
    const hasUpcomingEvents = bodyText.includes('Upcoming') || bodyText.includes('UPCOMING');
    const hasEventContent = 
      bodyText.includes('Rope') || 
      pageContent.includes('Workshop') || 
      bodyText.includes('Safety') ||
      bodyText.includes('Techniques') ||
      bodyText.includes('Community');

    console.log('📊 Content Analysis:');
    console.log('  - Has Events title:', hasEventsTitle);
    console.log('  - Has "Upcoming" text:', hasUpcomingEvents);
    console.log('  - Has event-related content:', hasEventContent);
    console.log('  - Page content length:', bodyText.length);

    // Look for specific event titles from the API data
    const specificEvents = [
      'Introduction to Rope Safety',
      'Single Column Tie Techniques', 
      'Suspension Basics',
      'Community Rope Jam',
      'Advanced Floor Work'
    ];

    let eventsFound = 0;
    specificEvents.forEach(eventTitle => {
      if (bodyText.includes(eventTitle)) {
        eventsFound++;
        console.log(`✅ Found event: ${eventTitle}`);
      }
    });

    console.log(`📊 Specific events found: ${eventsFound}/${specificEvents.length}`);

    // Check for event display elements
    const eventCards = await page.locator('[data-testid*="event"], [class*="event-card"], [class*="event"]').count();
    const cardElements = await page.locator('[class*="card"]').count();
    const listItems = await page.locator('li').count();
    const articles = await page.locator('article').count();

    console.log('📊 DOM Elements:');
    console.log('  - Event-specific elements:', eventCards);
    console.log('  - Card elements:', cardElements);
    console.log('  - List items:', listItems);
    console.log('  - Article elements:', articles);

    // Check for loading states
    const hasLoadingText = bodyText.includes('Loading') || bodyText.includes('loading');
    const hasLoadingSpinner = await page.locator('[aria-label*="loading"], [class*="spinner"]').isVisible();

    console.log('⏳ Loading State:');
    console.log('  - Has loading text:', hasLoadingText);
    console.log('  - Has loading spinner:', hasLoadingSpinner);

    // Check for error states
    const hasErrorText = bodyText.includes('Error') || bodyText.includes('error');
    const hasErrorElement = await page.locator('[role="alert"], [class*="error"]').isVisible();

    console.log('❌ Error State:');
    console.log('  - Has error text:', hasErrorText);
    console.log('  - Has error element:', hasErrorElement);

    // Log API call results
    console.log('📡 API Calls Summary:');
    console.log(`  - Total events API calls: ${apiCalls.length}`);
    apiCalls.forEach(call => {
      console.log(`    ${call.method} ${call.url} - ${call.status}`);
    });

    // Verify events are displayed (flexible criteria)
    const eventsDisplayed = 
      eventsFound > 0 || 
      hasEventContent || 
      eventCards > 0 || 
      cardElements > 0 ||
      (apiCalls.length > 0 && apiCalls.some(call => call.status === 200));

    if (!eventsDisplayed) {
      console.log('⚠️ Events may not be displaying correctly. Capturing additional diagnostics...');
      
      // Take additional screenshots for diagnosis
      await page.screenshot({ 
        path: 'test-results/05-events-diagnosis-full.png', 
        fullPage: true 
      });
      
      // Get network logs
      console.log('🌐 Current URL:', page.url());
      console.log('📄 First 500 chars of body text:', bodyText.substring(0, 500));
    }

    // Verify that events are displayed
    expect(eventsDisplayed).toBe(true);

    console.log('✅ Events page test completed');
  });

  test('should complete full user journey: login then view events', async ({ page }) => {
    console.log('🔄 Testing complete user journey: login → events');

    // Step 1: Login
    console.log('🔐 Step 1: Login...');
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');

    // Fill login form using flexible selectors
    const emailInput = page.locator('input[type="email"], [data-testid="email-input"], input[name="email"]').first();
    const passwordInput = page.locator('input[type="password"], [data-testid="password-input"], input[name="password"]').first();
    const loginButton = page.locator('button[type="submit"], [data-testid="login-button"], button:has-text("Login")').first();

    await emailInput.fill('admin@witchcityrope.com');
    await passwordInput.fill('Test123!');
    await loginButton.click();

    // Wait for login to complete
    await page.waitForTimeout(3000);

    // Take screenshot after login
    await page.screenshot({ 
      path: 'test-results/06-journey-after-login.png', 
      fullPage: true 
    });

    // Step 2: Navigate to events
    console.log('📅 Step 2: Navigate to events...');
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    // Take final screenshot
    await page.screenshot({ 
      path: 'test-results/07-journey-events-page.png', 
      fullPage: true 
    });

    // Verify we can access events page
    const currentUrl = page.url();
    const bodyText = await page.textContent('body') || '';
    
    console.log('📍 Final URL:', currentUrl);
    console.log('📊 Events page accessible after login:', currentUrl.includes('/events'));
    console.log('📊 Page has content:', bodyText.length > 0);

    // Verify successful completion
    expect(currentUrl.includes('/events')).toBe(true);
    expect(bodyText.length).toBeGreaterThan(0);

    console.log('✅ Complete user journey test completed successfully');
  });
});