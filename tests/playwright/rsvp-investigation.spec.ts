import { test, expect, Page } from '@playwright/test';
import path from 'path';

const TEST_RESULTS_DIR = '/home/chad/repos/witchcityrope-react./test-results/rsvp-investigation-' + new Date().toISOString().split('T')[0].replace(/-/g, '');

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
  await page.click('text=LOGIN');
  await page.waitForSelector('[data-testid="login-modal"]', { timeout: 10000 });

  // Fill credentials
  await page.fill('input[name="email"]', user.email);
  await page.fill('input[name="password"]', user.password);

  // Submit login
  await page.click('button[type="submit"]');

  // Wait for successful login (should redirect or show logged in state)
  await page.waitForTimeout(3000);
}

// Helper function to take screenshot with description
async function takeScreenshot(page: Page, filename: string, description: string) {
  const fullPath = path.join(TEST_RESULTS_DIR, filename);
  await page.screenshot({
    path: fullPath,
    fullPage: true
  });
  console.log(`📸 Screenshot saved: ${filename} - ${description}`);
}

// Helper function to capture API response data
async function captureAPIData(page: Page, endpoint: string, description: string) {
  try {
    const response = await page.request.get(`http://localhost:5655${endpoint}`);
    const data = await response.json();

    const filename = `api-${endpoint.replace(/[\/\?]/g, '-')}.json`;
    const fullPath = path.join(TEST_RESULTS_DIR, filename);

    await page.evaluate(
      ({ path, data }) => {
        const fs = require('fs');
        fs.writeFileSync(path, JSON.stringify(data, null, 2));
      },
      { path: fullPath, data }
    );

    console.log(`📊 API data captured: ${description}`);
    return data;
  } catch (error) {
    console.log(`❌ Failed to capture API data for ${endpoint}: ${error}`);
    return null;
  }
}

test.describe('RSVP and Ticketing System Investigation', () => {

  test.beforeEach(async ({ page }) => {
    // Ensure test results directory exists
    await page.evaluate((dir) => {
      const fs = require('fs');
      fs.mkdirSync(dir, { recursive: true });
    }, TEST_RESULTS_DIR);
  });

  test('1. Admin Events List - Capacity Column Investigation', async ({ page }) => {
    console.log('🔍 Testing Admin Events List Capacity Display');

    await loginAs(page, 'admin');

    // Navigate to admin events
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Take screenshot of admin events list
    await takeScreenshot(page, 'admin-events-list.png', 'Admin events list showing capacity column');

    // Capture events API data
    const eventsData = await captureAPIData(page, '/api/events', 'Events API response for admin');

    // Check if capacity column exists and has data
    const capacityHeaders = await page.locator('th:has-text("Capacity"), th:has-text("Participants"), th:has-text("Count")').count();
    console.log(`📊 Found ${capacityHeaders} capacity-related headers`);

    // Look for participant counts in the table
    const participantCounts = await page.locator('td').allTextContents();
    const numericCounts = participantCounts.filter(text => /^\d+\/\d+$|^\d+$/.test(text.trim()));
    console.log(`📊 Found ${numericCounts.length} numeric participant counts: ${numericCounts.slice(0, 5).join(', ')}`);

    // Take screenshot of any capacity data found
    if (capacityHeaders > 0) {
      await takeScreenshot(page, 'admin-events-capacity-column.png', 'Close-up of capacity column data');
    }

    // Check console for any errors
    const consoleErrors = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    await page.waitForTimeout(1000);
    console.log(`🚨 Console errors found: ${consoleErrors.length}`);
    if (consoleErrors.length > 0) {
      console.log('❌ Console errors:', consoleErrors);
    }
  });

  test('2. Admin RSVP/Tickets Tab Investigation', async ({ page }) => {
    console.log('🔍 Testing Admin RSVP/Tickets Tab Data Display');

    await loginAs(page, 'admin');

    // Navigate to admin area
    await page.goto('http://localhost:5173/admin');
    await page.waitForLoadState('networkidle');

    // Look for RSVP/Tickets navigation
    const rsvpTab = page.locator('text=RSVP, text=Tickets, text=Participation').first();
    const tabExists = await rsvpTab.isVisible({ timeout: 5000 }).catch(() => false);

    if (tabExists) {
      await rsvpTab.click();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000);

      await takeScreenshot(page, 'admin-rsvp-tickets-tab.png', 'Admin RSVP/Tickets tab showing participation data');

      // Capture participation API data
      await captureAPIData(page, '/api/admin/rsvps', 'Admin RSVP data');
      await captureAPIData(page, '/api/admin/participations', 'Admin participation data');
    } else {
      await takeScreenshot(page, 'admin-navigation-no-rsvp-tab.png', 'Admin navigation - RSVP tab not found');
      console.log('⚠️ RSVP/Tickets tab not found in admin navigation');
    }

    // Try alternative admin paths
    const alternativePaths = [
      '/admin/rsvps',
      '/admin/tickets',
      '/admin/participations',
      '/admin/events/rsvps'
    ];

    for (const path of alternativePaths) {
      try {
        await page.goto(`http://localhost:5173${path}`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(1000);

        const hasContent = await page.locator('table, .rsvp, .ticket, .participation').count() > 0;
        if (hasContent) {
          await takeScreenshot(page, `admin-rsvp-${path.replace(/[\/]/g, '-')}.png`, `Admin RSVP data at ${path}`);
          console.log(`✅ Found RSVP content at ${path}`);
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

    await takeScreenshot(page, 'dashboard-member-view.png', 'Member dashboard showing RSVP counts');

    // Capture dashboard API data
    await captureAPIData(page, '/api/dashboard/user', 'User dashboard data');
    await captureAPIData(page, '/api/dashboard/rsvps', 'User RSVP data');

    // Look for RSVP counts on dashboard
    const rsvpElements = await page.locator('text=/RSVP|rsvp|Attending|attending/i').allTextContents();
    console.log(`📊 Found ${rsvpElements.length} RSVP-related elements:`, rsvpElements);

    // Look for numeric counts
    const allText = await page.locator('body').textContent();
    const numbers = allText.match(/\b\d+\b/g) || [];
    console.log(`📊 Numbers found on dashboard: ${numbers.slice(0, 10).join(', ')}`);

    // Check for "My RSVPs" or similar sections
    const myRsvpSection = page.locator('text=/My RSVP|My Events|Upcoming Events/i').first();
    const sectionExists = await myRsvpSection.isVisible({ timeout: 5000 }).catch(() => false);

    if (sectionExists) {
      await takeScreenshot(page, 'dashboard-my-rsvps-section.png', 'My RSVPs section on dashboard');
    }
  });

  test('4. Cancel RSVP Functionality Investigation', async ({ page }) => {
    console.log('🔍 Testing Cancel RSVP Functionality');

    await loginAs(page, 'vetted');

    // First, try to find and RSVP to an event
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    await takeScreenshot(page, 'events-list-before-rsvp.png', 'Events list before attempting RSVP');

    // Look for RSVP buttons
    const rsvpButtons = await page.locator('button:has-text("RSVP"), button:has-text("Join"), button:has-text("Register")').count();
    console.log(`📊 Found ${rsvpButtons} potential RSVP buttons`);

    if (rsvpButtons > 0) {
      // Try to RSVP to first available event
      const firstRsvpButton = page.locator('button:has-text("RSVP"), button:has-text("Join"), button:has-text("Register")').first();

      await firstRsvpButton.click();
      await page.waitForTimeout(2000);

      await takeScreenshot(page, 'after-rsvp-attempt.png', 'Page state after RSVP attempt');

      // Now look for cancel buttons
      const cancelButtons = await page.locator('button:has-text("Cancel"), button:has-text("Remove"), button:has-text("Withdraw")').count();
      console.log(`📊 Found ${cancelButtons} potential cancel buttons`);

      if (cancelButtons > 0) {
        const firstCancelButton = page.locator('button:has-text("Cancel"), button:has-text("Remove"), button:has-text("Withdraw")').first();

        await firstCancelButton.click();
        await page.waitForTimeout(2000);

        await takeScreenshot(page, 'after-cancel-attempt.png', 'Page state after cancel RSVP attempt');
      }
    }

    // Check user's RSVP history
    await page.goto('http://localhost:5173/profile');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    await takeScreenshot(page, 'profile-rsvp-history.png', 'User profile showing RSVP history');

    // Capture user participation data
    await captureAPIData(page, '/api/user/participations', 'User participation history');
  });

  test('5. API Direct Testing - RSVP Data Comparison', async ({ page }) => {
    console.log('🔍 Testing API Endpoints Directly for RSVP Data');

    // Test various API endpoints to see what data is actually returned
    const apiEndpoints = [
      '/api/events',
      '/api/dashboard/events',
      '/api/admin/events',
      '/api/participations',
      '/api/rsvps',
      '/api/tickets',
      '/api/events/1/participants',
      '/api/events/1/rsvps'
    ];

    for (const endpoint of apiEndpoints) {
      await captureAPIData(page, endpoint, `Direct API test for ${endpoint}`);
    }

    // Test with authentication headers
    await loginAs(page, 'admin');

    // Capture authenticated API responses
    const authenticatedEndpoints = [
      '/api/admin/dashboard',
      '/api/admin/users',
      '/api/admin/events/stats'
    ];

    for (const endpoint of authenticatedEndpoints) {
      await captureAPIData(page, endpoint, `Authenticated API test for ${endpoint}`);
    }
  });

  test('6. Comprehensive Visual Documentation', async ({ page }) => {
    console.log('🔍 Creating Comprehensive Visual Documentation');

    // Test as admin
    await loginAs(page, 'admin');

    // Capture all admin pages
    const adminPages = [
      '/admin',
      '/admin/events',
      '/admin/users',
      '/admin/settings'
    ];

    for (const path of adminPages) {
      try {
        await page.goto(`http://localhost:5173${path}`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(2000);

        const filename = `admin-${path.replace(/[\/]/g, '-')}.png`;
        await takeScreenshot(page, filename, `Admin page: ${path}`);
      } catch (error) {
        console.log(`❌ Failed to capture admin page ${path}: ${error}`);
      }
    }

    // Test as regular member
    await loginAs(page, 'member');

    const memberPages = [
      '/dashboard',
      '/events',
      '/profile'
    ];

    for (const path of memberPages) {
      try {
        await page.goto(`http://localhost:5173${path}`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(2000);

        const filename = `member-${path.replace(/[\/]/g, '-')}.png`;
        await takeScreenshot(page, filename, `Member page: ${path}`);
      } catch (error) {
        console.log(`❌ Failed to capture member page ${path}: ${error}`);
      }
    }
  });

});