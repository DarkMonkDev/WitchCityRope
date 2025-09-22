import { test, expect, Page } from '@playwright/test';

// Test user credentials
const TEST_USERS = {
  admin: { email: 'admin@witchcityrope.com', password: 'Test123!' },
  member: { email: 'member@witchcityrope.com', password: 'Test123!' },
  vetted: { email: 'vetted@witchcityrope.com', password: 'Test123!' }
};

// Helper function to login as specific user
async function loginAs(page: Page, userType: keyof typeof TEST_USERS) {
  const user = TEST_USERS[userType];

  console.log(`🔐 Logging in as ${userType}: ${user.email}`);

  await page.goto('http://localhost:5173');
  await page.waitForLoadState('networkidle');

  // Click login button
  const loginButton = page.locator('text=LOGIN').first();
  if (await loginButton.isVisible({ timeout: 5000 })) {
    await loginButton.click();

    // Wait for login modal
    await page.waitForSelector('input[name="email"], input[type="email"]', { timeout: 10000 });

    // Fill credentials
    await page.fill('input[name="email"], input[type="email"]', user.email);
    await page.fill('input[name="password"], input[type="password"]', user.password);

    // Submit login
    await page.click('button[type="submit"]');
    await page.waitForTimeout(3000);
  }
}

test.describe('RSVP and Ticketing System Investigation', () => {

  test('1. Admin Events List - Capacity Column Investigation', async ({ page }) => {
    console.log('🔍 Testing Admin Events List Capacity Display');

    await loginAs(page, 'admin');

    // Navigate to admin events
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Take screenshot of admin events list
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope-react./test-results/admin-events-list.png',
      fullPage: true
    });

    // Check if capacity column exists and has data
    const capacityHeaders = await page.locator('th').allTextContents();
    console.log(`📊 Table headers found: ${capacityHeaders.join(', ')}`);

    // Look for participant counts in the table
    const participantCounts = await page.locator('td').allTextContents();
    const numericCounts = participantCounts.filter(text => /^\d+\/\d+$|^\d+$/.test(text.trim()));
    console.log(`📊 Found ${numericCounts.length} numeric counts: ${numericCounts.slice(0, 5).join(', ')}`);

    // Check for specific capacity-related text
    const capacityText = await page.textContent('body');
    const hasCapacityInfo = capacityText?.includes('capacity') || capacityText?.includes('participants');
    console.log(`📊 Page contains capacity/participant info: ${hasCapacityInfo}`);

    // Check for API calls and errors
    const consoleMessages: string[] = [];
    page.on('console', msg => {
      consoleMessages.push(`${msg.type()}: ${msg.text()}`);
    });

    await page.waitForTimeout(2000);
    console.log(`📊 Console messages: ${consoleMessages.length}`);
    if (consoleMessages.length > 0) {
      console.log('Console output:', consoleMessages.slice(-5));
    }
  });

  test('2. Admin RSVP/Tickets Tab Investigation', async ({ page }) => {
    console.log('🔍 Testing Admin RSVP/Tickets Tab Data Display');

    await loginAs(page, 'admin');

    // Navigate to admin area
    await page.goto('http://localhost:5173/admin');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    await page.screenshot({
      path: '/home/chad/repos/witchcityrope-react./test-results/admin-main-page.png',
      fullPage: true
    });

    // Look for RSVP/Tickets navigation
    const navigationText = await page.textContent('body');
    const hasRSVPNav = navigationText?.toLowerCase().includes('rsvp') ||
                      navigationText?.toLowerCase().includes('ticket') ||
                      navigationText?.toLowerCase().includes('participation');

    console.log(`📊 Admin page contains RSVP/Ticket navigation: ${hasRSVPNav}`);

    // Try clicking on any RSVP-related links
    const rsvpLinks = await page.locator('a, button').filter({ hasText: /rsvp|ticket|participation/i }).count();
    console.log(`📊 Found ${rsvpLinks} RSVP-related links/buttons`);

    if (rsvpLinks > 0) {
      try {
        await page.locator('a, button').filter({ hasText: /rsvp|ticket|participation/i }).first().click();
        await page.waitForTimeout(2000);
        await page.screenshot({
          path: '/home/chad/repos/witchcityrope-react./test-results/admin-rsvp-section.png',
          fullPage: true
        });
      } catch (error) {
        console.log(`❌ Failed to click RSVP link: ${error}`);
      }
    }

    // Try alternative admin paths
    const alternativePaths = ['/admin/rsvps', '/admin/tickets', '/admin/participations'];

    for (const path of alternativePaths) {
      try {
        await page.goto(`http://localhost:5173${path}`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(1000);

        const hasContent = await page.locator('table, .rsvp, .ticket, .participation').count() > 0;
        console.log(`📊 ${path} has RSVP content: ${hasContent}`);

        if (hasContent) {
          await page.screenshot({
            path: `/home/chad/repos/witchcityrope-react./test-results/admin-${path.replace(/[\/]/g, '-')}.png`,
            fullPage: true
          });
        }
      } catch (error) {
        console.log(`❌ Failed to access ${path}: ${error}`);
      }
    }
  });

  test('3. Dashboard RSVP Count Display Investigation', async ({ page }) => {
    console.log('🔍 Testing Dashboard RSVP Count Display for Regular User');

    await loginAs(page, 'member');

    // Navigate to dashboard
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    await page.screenshot({
      path: '/home/chad/repos/witchcityrope-react./test-results/dashboard-member-view.png',
      fullPage: true
    });

    // Look for RSVP counts on dashboard
    const bodyText = await page.textContent('body');
    const rsvpMatches = bodyText?.match(/rsvp|attending|joined|registered/gi) || [];
    console.log(`📊 Found ${rsvpMatches.length} RSVP-related terms: ${rsvpMatches.slice(0, 5).join(', ')}`);

    // Look for numeric counts
    const numbers = bodyText?.match(/\b\d+\b/g) || [];
    console.log(`📊 Numbers found on dashboard: ${numbers.slice(0, 10).join(', ')}`);

    // Check for "My RSVPs" or similar sections
    const hasRsvpSection = bodyText?.toLowerCase().includes('my rsvp') ||
                          bodyText?.toLowerCase().includes('my events') ||
                          bodyText?.toLowerCase().includes('upcoming events');

    console.log(`📊 Dashboard has RSVP section: ${hasRsvpSection}`);

    // Check for RSVP-related elements
    const rsvpElements = await page.locator('[data-testid*="rsvp"], [class*="rsvp"], [id*="rsvp"]').count();
    console.log(`📊 Found ${rsvpElements} RSVP-related elements by ID/class`);
  });

  test('4. Cancel RSVP Functionality Investigation', async ({ page }) => {
    console.log('🔍 Testing Cancel RSVP Functionality');

    await loginAs(page, 'vetted');

    // First, try to find and RSVP to an event
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    await page.screenshot({
      path: '/home/chad/repos/witchcityrope-react./test-results/events-list-before-rsvp.png',
      fullPage: true
    });

    // Look for RSVP buttons
    const rsvpButtons = await page.locator('button').filter({ hasText: /rsvp|join|register|attend/i }).count();
    console.log(`📊 Found ${rsvpButtons} potential RSVP buttons`);

    if (rsvpButtons > 0) {
      try {
        // Try to RSVP to first available event
        await page.locator('button').filter({ hasText: /rsvp|join|register|attend/i }).first().click();
        await page.waitForTimeout(2000);

        await page.screenshot({
          path: '/home/chad/repos/witchcityrope-react./test-results/after-rsvp-attempt.png',
          fullPage: true
        });

        // Now look for cancel buttons
        const cancelButtons = await page.locator('button').filter({ hasText: /cancel|remove|withdraw|leave/i }).count();
        console.log(`📊 Found ${cancelButtons} potential cancel buttons`);

        if (cancelButtons > 0) {
          await page.locator('button').filter({ hasText: /cancel|remove|withdraw|leave/i }).first().click();
          await page.waitForTimeout(2000);

          await page.screenshot({
            path: '/home/chad/repos/witchcityrope-react./test-results/after-cancel-attempt.png',
            fullPage: true
          });
        }
      } catch (error) {
        console.log(`❌ Error during RSVP/Cancel process: ${error}`);
      }
    }

    // Check user's RSVP history in profile
    try {
      await page.goto('http://localhost:5173/profile');
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000);

      await page.screenshot({
        path: '/home/chad/repos/witchcityrope-react./test-results/profile-rsvp-history.png',
        fullPage: true
      });

      const profileText = await page.textContent('body');
      const hasRsvpHistory = profileText?.toLowerCase().includes('rsvp') ||
                            profileText?.toLowerCase().includes('events') ||
                            profileText?.toLowerCase().includes('participation');

      console.log(`📊 Profile contains RSVP history: ${hasRsvpHistory}`);
    } catch (error) {
      console.log(`❌ Failed to access profile: ${error}`);
    }
  });

  test('5. API Direct Testing - Check What Data is Actually Returned', async ({ page }) => {
    console.log('🔍 Testing API Endpoints Directly for RSVP Data');

    // Test API endpoints without authentication first
    const publicEndpoints = ['/api/events', '/api/health'];

    for (const endpoint of publicEndpoints) {
      try {
        const response = await page.request.get(`http://localhost:5655${endpoint}`);
        const status = response.status();
        const data = await response.text();

        console.log(`📊 ${endpoint}: Status ${status}, Data length: ${data.length} chars`);
        if (status === 200 && data.length < 1000) {
          console.log(`📊 ${endpoint} response preview: ${data.substring(0, 200)}...`);
        }
      } catch (error) {
        console.log(`❌ Failed to test ${endpoint}: ${error}`);
      }
    }

    // Login and test authenticated endpoints
    await loginAs(page, 'admin');

    const authenticatedEndpoints = [
      '/api/admin/events',
      '/api/dashboard/events',
      '/api/user/participations',
      '/api/events/participations'
    ];

    for (const endpoint of authenticatedEndpoints) {
      try {
        const response = await page.request.get(`http://localhost:5655${endpoint}`);
        const status = response.status();
        const data = await response.text();

        console.log(`📊 ${endpoint}: Status ${status}, Data length: ${data.length} chars`);
        if (status === 200 && data.length < 1000) {
          console.log(`📊 ${endpoint} response preview: ${data.substring(0, 200)}...`);
        }
      } catch (error) {
        console.log(`❌ Failed to test ${endpoint}: ${error}`);
      }
    }
  });

  test('6. Visual Documentation of Current State', async ({ page }) => {
    console.log('🔍 Creating Visual Documentation of Current System State');

    // Test as admin first
    await loginAs(page, 'admin');

    const adminPages = ['/admin', '/admin/events', '/admin/users'];

    for (const path of adminPages) {
      try {
        await page.goto(`http://localhost:5173${path}`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(2000);

        const filename = `current-state-admin-${path.replace(/[\/]/g, '-')}.png`;
        await page.screenshot({
          path: `/home/chad/repos/witchcityrope-react./test-results/${filename}`,
          fullPage: true
        });

        console.log(`📸 Captured: ${filename}`);
      } catch (error) {
        console.log(`❌ Failed to capture admin page ${path}: ${error}`);
      }
    }

    // Test as regular member
    await loginAs(page, 'member');

    const memberPages = ['/dashboard', '/events', '/profile'];

    for (const path of memberPages) {
      try {
        await page.goto(`http://localhost:5173${path}`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(2000);

        const filename = `current-state-member-${path.replace(/[\/]/g, '-')}.png`;
        await page.screenshot({
          path: `/home/chad/repos/witchcityrope-react./test-results/${filename}`,
          fullPage: true
        });

        console.log(`📸 Captured: ${filename}`);
      } catch (error) {
        console.log(`❌ Failed to capture member page ${path}: ${error}`);
      }
    }

    console.log('✅ Visual documentation complete');
  });

});