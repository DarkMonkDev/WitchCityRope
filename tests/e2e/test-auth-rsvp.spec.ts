import { test, expect } from '@playwright/test';

test.describe('Authentication and RSVP Testing', () => {
  test('should navigate to login page and authenticate admin user', async ({ page }) => {
    // Navigate to the login page
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');

    // Take screenshot of login page
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/01-login-page.png', fullPage: true });

    // Check if login form is present using correct data-testid selectors
    const emailField = page.locator('[data-testid="email-input"]');
    const passwordField = page.locator('[data-testid="password-input"]');
    const loginButton = page.locator('[data-testid="login-button"]');

    // Verify form elements exist
    await expect(emailField).toBeVisible();
    await expect(passwordField).toBeVisible();
    await expect(loginButton).toBeVisible();

    console.log('✅ Login form elements found and visible');

    // Fill in admin credentials
    await emailField.fill('admin@witchcityrope.com');
    await passwordField.fill('Test123!');

    // Take screenshot before submitting
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/02-login-form-filled.png', fullPage: true });

    // Submit login form
    await loginButton.click();

    // Wait for navigation and check for successful login
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000); // Give time for auth state to update

    // Take screenshot after login attempt
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/03-after-login.png', fullPage: true });

    // Check current URL and page content
    const currentUrl = page.url();
    console.log('Current URL after login:', currentUrl);

    // Get page content to analyze what's displayed
    const pageContent = await page.textContent('body');
    console.log('Page contains "RopeMaster":', pageContent?.includes('RopeMaster'));
    console.log('Page contains "Welcome":', pageContent?.includes('Welcome'));
    console.log('Page contains "Admin":', pageContent?.includes('Admin'));
    console.log('Page contains "Dashboard":', pageContent?.includes('Dashboard'));

    // Look for admin-specific elements or RSVP/Tickets functionality
    const adminElements = await page.locator('text="Admin"').count();
    const rsvpElements = await page.locator('text="RSVP"').count();
    const ticketsElements = await page.locator('text="Tickets"').count();
    const eventsElements = await page.locator('text="Events"').count();

    console.log('Admin elements found:', adminElements);
    console.log('RSVP elements found:', rsvpElements);
    console.log('Tickets elements found:', ticketsElements);
    console.log('Events elements found:', eventsElements);

    // Check for navigation menu items
    const navItems = await page.locator('nav a, header a, [role="navigation"] a').count();
    console.log('Navigation items found:', navItems);

    // Log current page structure
    const headings = await page.locator('h1, h2, h3').allTextContents();
    console.log('Page headings:', headings);

    // Check for error messages
    const errorMessages = await page.locator('[data-testid*="error"], .error, .alert-error').count();
    if (errorMessages > 0) {
      const errorText = await page.locator('[data-testid*="error"], .error, .alert-error').first().textContent();
      console.log('Error message found:', errorText);
    } else {
      console.log('No error messages found');
    }

    // Check console for auth-related messages
    const logs: string[] = [];
    page.on('console', msg => {
      logs.push(msg.text());
    });

    // Wait a bit more and check logs
    await page.waitForTimeout(1000);
    const authLogs = logs.filter(log =>
      log.toLowerCase().includes('auth') ||
      log.toLowerCase().includes('login') ||
      log.toLowerCase().includes('token')
    );
    if (authLogs.length > 0) {
      console.log('Auth-related console logs:', authLogs);
    }
  });

  test('should test direct API authentication', async ({ page }) => {
    // Test API endpoints using page.request for more direct testing

    console.log('Testing API endpoints directly...');

    // Test health endpoint
    const healthResponse = await page.request.get('http://localhost:5655/health');
    expect(healthResponse.status()).toBe(200);
    console.log('✅ API health endpoint responding');

    // Test login endpoint directly
    const loginResponse = await page.request.post('http://localhost:5655/api/auth/login', {
      data: {
        email: 'admin@witchcityrope.com',
        password: 'Test123!'
      }
    });

    console.log('Login endpoint status:', loginResponse.status());

    if (loginResponse.ok()) {
      const loginData = await loginResponse.json();
      console.log('✅ Login successful via API');
      console.log('User role:', loginData.user?.role);
      console.log('User roles:', loginData.user?.roles);
      console.log('Is admin:', loginData.user?.role === 'Administrator');

      // Test protected endpoints with the session cookie
      const userResponse = await page.request.get('http://localhost:5655/api/auth/user');
      console.log('User info endpoint status:', userResponse.status());

      // Test dashboard events endpoint
      const dashboardResponse = await page.request.get('http://localhost:5655/api/dashboard/events?count=3');
      console.log('Dashboard events endpoint status:', dashboardResponse.status());

      // Test events endpoint
      const eventsResponse = await page.request.get('http://localhost:5655/api/events');
      console.log('Events endpoint status:', eventsResponse.status());

      if (eventsResponse.ok()) {
        const eventsData = await eventsResponse.json();
        console.log('Events found:', eventsData?.length || 'unknown');

        // Look for RSVP/ticket functionality in events
        if (Array.isArray(eventsData)) {
          const hasRsvpFeatures = eventsData.some(event =>
            event.allowRsvp || event.hasTickets || event.rsvpLimit || event.ticketPrice
          );
          console.log('Events have RSVP/ticket features:', hasRsvpFeatures);
        }
      }
    } else {
      console.log('❌ Login failed via API');
      const errorData = await loginResponse.text();
      console.log('Login error:', errorData);
    }
  });

  test('should check RSVP creation functionality', async ({ page }) => {
    // First login through the UI
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');

    // Login as regular member
    const emailField = page.locator('[data-testid="email-input"]');
    const passwordField = page.locator('[data-testid="password-input"]');
    const loginButton = page.locator('[data-testid="login-button"]');

    await emailField.fill('member@witchcityrope.com');
    await passwordField.fill('Test123!');
    await loginButton.click();

    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    // Take screenshot of member dashboard
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/04-member-dashboard.png', fullPage: true });

    // Look for events page or navigation
    const currentUrl = page.url();
    console.log('Member dashboard URL:', currentUrl);

    // Try to navigate to events page
    const eventsLink = page.locator('a[href*="events"], a:has-text("Events")');
    const eventsLinkCount = await eventsLink.count();
    console.log('Events links found:', eventsLinkCount);

    if (eventsLinkCount > 0) {
      await eventsLink.first().click();
      await page.waitForLoadState('networkidle');
      await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/05-events-page.png', fullPage: true });

      // Look for RSVP buttons or forms
      const rsvpButtons = await page.locator('button:has-text("RSVP"), button:has-text("Register"), [data-testid*="rsvp"]').count();
      console.log('RSVP buttons found:', rsvpButtons);

      // Check page content for event listings
      const pageContent = await page.textContent('body');
      console.log('Events page contains "workshop":', pageContent?.toLowerCase().includes('workshop'));
      console.log('Events page contains "class":', pageContent?.toLowerCase().includes('class'));
      console.log('Events page contains "rsvp":', pageContent?.toLowerCase().includes('rsvp'));
    } else {
      // Try direct navigation to events
      await page.goto('http://localhost:5173/events');
      await page.waitForLoadState('networkidle');
      await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/06-direct-events-page.png', fullPage: true });

      const pageContent = await page.textContent('body');
      console.log('Direct events page loaded:', !pageContent?.includes('404'));
    }
  });
});