import { test, expect } from '@playwright/test';

// Test user credentials
const TEST_USERS = {
  admin: { email: 'admin@witchcityrope.com', password: 'Test123!' },
  member: { email: 'member@witchcityrope.com', password: 'Test123!' }
};

// Helper function to login
async function loginAs(page, userType) {
  const user = TEST_USERS[userType];
  console.log(`🔐 Logging in as ${userType}: ${user.email}`);

  await page.goto('http://localhost:5173');
  await page.waitForLoadState('networkidle');

  // Click LOGIN button
  await page.click('text=LOGIN');
  await page.waitForTimeout(2000);

  // Fill in login form (look for any email/password inputs)
  await page.fill('input[type="email"], input[name="email"]', user.email);
  await page.fill('input[type="password"], input[name="password"]', user.password);

  // Submit form
  await page.click('button[type="submit"], button:has-text("Login"), button:has-text("Sign In")');
  await page.waitForTimeout(3000);
}

test.describe('RSVP System Working Investigation', () => {

  test('1. Events List - Verify Capacity Display', async ({ page }) => {
    console.log('🔍 Testing Events List Capacity Display');

    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    await page.screenshot({
      path: '/home/chad/repos/witchcityrope-react./test-results/events-capacity-display.png',
      fullPage: true
    });

    // Look for capacity indicators in the page
    const bodyText = await page.textContent('body');

    // Check for patterns like "0/15", "5/12", etc.
    const capacityMatches = bodyText.match(/\d+\/\d+/g) || [];
    console.log(`📊 Found ${capacityMatches.length} capacity indicators: ${capacityMatches.join(', ')}`);

    // Check for "tickets" text
    const ticketMatches = bodyText.match(/\d+\s+of\s+\d+\s+tickets/gi) || [];
    console.log(`📊 Found ${ticketMatches.length} ticket indicators: ${ticketMatches.join(', ')}`);

    // Check if all show zero participants
    const zeroParticipants = capacityMatches.filter(match => match.startsWith('0/'));
    console.log(`📊 Events with zero participants: ${zeroParticipants.length}/${capacityMatches.length}`);

    if (zeroParticipants.length === capacityMatches.length && capacityMatches.length > 0) {
      console.log('⚠️ ALL events show zero participants - this matches the user's report');
    }
  });

  test('2. Attempt Admin Login and Check Admin Interface', async ({ page }) => {
    console.log('🔍 Testing Admin Login and Interface');

    try {
      await loginAs(page, 'admin');

      // Check if login was successful by looking for user-specific content
      await page.waitForTimeout(2000);

      // Take screenshot after login attempt
      await page.screenshot({
        path: '/home/chad/repos/witchcityrope-react./test-results/after-admin-login-attempt.png',
        fullPage: true
      });

      // Try to navigate to admin area
      await page.goto('http://localhost:5173/admin');
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000);

      await page.screenshot({
        path: '/home/chad/repos/witchcityrope-react./test-results/admin-interface.png',
        fullPage: true
      });

      // Check for admin-specific content
      const bodyText = await page.textContent('body');
      const hasAdminContent = bodyText.toLowerCase().includes('admin') ||
                             bodyText.toLowerCase().includes('manage') ||
                             bodyText.toLowerCase().includes('dashboard');

      console.log(`📊 Admin interface accessible: ${hasAdminContent}`);

      // Look for event management or RSVP management sections
      const hasEventManagement = bodyText.toLowerCase().includes('event') &&
                                 (bodyText.toLowerCase().includes('manage') ||
                                  bodyText.toLowerCase().includes('admin'));

      console.log(`📊 Event management interface found: ${hasEventManagement}`);

      // Try admin/events path
      await page.goto('http://localhost:5173/admin/events');
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000);

      await page.screenshot({
        path: '/home/chad/repos/witchcityrope-react./test-results/admin-events-page.png',
        fullPage: true
      });

      const eventsPageText = await page.textContent('body');
      const hasEventsTable = eventsPageText.includes('capacity') ||
                            eventsPageText.includes('participants') ||
                            eventsPageText.includes('attendees');

      console.log(`📊 Admin events page has capacity info: ${hasEventsTable}`);

    } catch (error) {
      console.log(`❌ Admin login failed: ${error}`);

      await page.screenshot({
        path: '/home/chad/repos/witchcityrope-react./test-results/admin-login-failed.png',
        fullPage: true
      });
    }
  });

  test('3. Test RSVP Functionality with Regular User', async ({ page }) => {
    console.log('🔍 Testing RSVP Functionality');

    try {
      await loginAs(page, 'member');

      await page.waitForTimeout(2000);

      await page.screenshot({
        path: '/home/chad/repos/witchcityrope-react./test-results/after-member-login.png',
        fullPage: true
      });

      // Go to events page to try RSVP
      await page.goto('http://localhost:5173/events');
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000);

      await page.screenshot({
        path: '/home/chad/repos/witchcityrope-react./test-results/events-logged-in-member.png',
        fullPage: true
      });

      // Look for RSVP, Register, or Join buttons
      const buttons = await page.locator('button').allTextContents();
      const rsvpButtons = buttons.filter(text =>
        text.toLowerCase().includes('rsvp') ||
        text.toLowerCase().includes('register') ||
        text.toLowerCase().includes('join') ||
        text.toLowerCase().includes('learn more')
      );

      console.log(`📊 Found potential RSVP buttons: ${rsvpButtons.join(', ')}`);

      // Try clicking on "LEARN MORE" for first event to see if RSVP options appear
      if (await page.locator('button:has-text("LEARN MORE")').count() > 0) {
        await page.locator('button:has-text("LEARN MORE")').first().click();
        await page.waitForTimeout(2000);

        await page.screenshot({
          path: '/home/chad/repos/witchcityrope-react./test-results/event-details-modal.png',
          fullPage: true
        });

        // Check if RSVP options appear in modal/details
        const modalText = await page.textContent('body');
        const hasRSVPOptions = modalText.toLowerCase().includes('rsvp') ||
                              modalText.toLowerCase().includes('register') ||
                              modalText.toLowerCase().includes('book') ||
                              modalText.toLowerCase().includes('reserve');

        console.log(`📊 Event details show RSVP options: ${hasRSVPOptions}`);
      }

      // Check dashboard for user's RSVPs
      await page.goto('http://localhost:5173/dashboard');
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000);

      await page.screenshot({
        path: '/home/chad/repos/witchcityrope-react./test-results/member-dashboard.png',
        fullPage: true
      });

      const dashboardText = await page.textContent('body');
      const hasRSVPSection = dashboardText.toLowerCase().includes('rsvp') ||
                            dashboardText.toLowerCase().includes('my events') ||
                            dashboardText.toLowerCase().includes('upcoming');

      console.log(`📊 Dashboard shows RSVP section: ${hasRSVPSection}`);

    } catch (error) {
      console.log(`❌ Member login failed: ${error}`);

      await page.screenshot({
        path: '/home/chad/repos/witchcityrope-react./test-results/member-login-failed.png',
        fullPage: true
      });
    }
  });

  test('4. Compare API Data vs UI Display', async ({ page }) => {
    console.log('🔍 Comparing API Data vs UI Display');

    // Get API data
    const eventsResponse = await page.request.get('http://localhost:5655/api/events');
    const eventsData = await eventsResponse.json();

    console.log('📊 API Data Analysis:');
    if (eventsData.success && Array.isArray(eventsData.data)) {
      eventsData.data.forEach((event, index) => {
        console.log(`   Event ${index + 1}: "${event.title}"`);
        console.log(`     Capacity: ${event.capacity}`);
        console.log(`     Current Attendees: ${event.currentAttendees}`);
        console.log(`     Current RSVPs: ${event.currentRSVPs}`);
        console.log(`     Current Tickets: ${event.currentTickets}`);
      });
    }

    // Check UI display
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    const bodyText = await page.textContent('body');
    const capacityMatches = bodyText.match(/\d+\/\d+/g) || [];

    console.log('📊 UI Display Analysis:');
    console.log(`   Found ${capacityMatches.length} capacity displays: ${capacityMatches.join(', ')}`);

    // Compare API vs UI
    const apiHasParticipants = eventsData.data?.some(event =>
      event.currentAttendees > 0 || event.currentRSVPs > 0 || event.currentTickets > 0
    );

    const uiShowsParticipants = capacityMatches.some(match => !match.startsWith('0/'));

    console.log('📊 Comparison Results:');
    console.log(`   API shows participants: ${apiHasParticipants}`);
    console.log(`   UI shows participants: ${uiShowsParticipants}`);

    if (!apiHasParticipants && !uiShowsParticipants) {
      console.log('✅ API and UI are consistent - both show zero participants');
      console.log('💡 The issue is that no actual RSVPs/tickets exist in the database');
    } else if (apiHasParticipants && !uiShowsParticipants) {
      console.log('❌ MISMATCH: API has participants but UI shows zero');
    } else if (!apiHasParticipants && uiShowsParticipants) {
      console.log('❌ MISMATCH: UI shows participants but API has zero');
    }
  });

});