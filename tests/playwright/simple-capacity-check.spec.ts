import { test, expect } from '@playwright/test';

test.describe('Simple Capacity Display Check', () => {
  test('take screenshot and check capacity display', async ({ page }) => {
    console.log('🔍 Taking screenshot to verify capacity display...');

    // Navigate to homepage
    await page.goto('http://localhost:5173');

    // Wait for page to load
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(5000);

    // Take screenshot of the homepage
    await page.screenshot({
      path: 'test-results/homepage-capacity-check.png',
      fullPage: true
    });

    // Get page content for analysis
    const bodyText = await page.textContent('body');
    console.log('📝 Page content length:', bodyText?.length || 0);

    // Look for any capacity-related text patterns
    const capacityMatches = bodyText?.match(/\d+\/\d+/g) || [];
    console.log('📊 Found capacity patterns:', capacityMatches);

    // Look for event-related text
    const eventMatches = bodyText?.match(/\b(event|workshop|social|class|rope)\b/gi) || [];
    console.log('🎯 Found event keywords:', eventMatches.slice(0, 10));

    // Check for any RSVP-related text
    const rsvpMatches = bodyText?.match(/\b(rsvp|ticket|register|join|attend)\b/gi) || [];
    console.log('🎫 Found participation keywords:', rsvpMatches.slice(0, 10));

    // Look for specific text that might indicate the bug
    if (bodyText?.includes('15/15')) {
      console.log('❌ POTENTIAL BUG: Found "15/15" which might indicate available/total instead of current/total');
    }

    if (bodyText?.includes('38/40')) {
      console.log('❌ POTENTIAL BUG: Found "38/40" which might indicate available/total instead of current/total');
    }

    if (bodyText?.includes('0/15')) {
      console.log('✅ GOOD: Found "0/15" which indicates correct current/total format for empty event');
    }

    if (bodyText?.includes('2/40')) {
      console.log('✅ GOOD: Found "2/40" which indicates correct current/total format');
    }

    // Check API response to understand backend data
    try {
      const apiResponse = await page.request.get('http://localhost:5655/api/events');
      if (apiResponse.ok()) {
        const events = await apiResponse.json();
        console.log('📊 API Events Summary:');

        if (Array.isArray(events)) {
          events.forEach((event: any, i: number) => {
            const title = event.title || event.name || `Event ${i + 1}`;
            const capacity = event.capacity || 0;
            const currentRSVPs = event.currentRSVPs || 0;
            const currentTickets = event.currentTickets || 0;
            const total = currentRSVPs + currentTickets;
            const available = capacity - total;

            console.log(`   ${title}:`);
            console.log(`     Capacity: ${capacity}`);
            console.log(`     Current Participants: ${total} (RSVPs: ${currentRSVPs}, Tickets: ${currentTickets})`);
            console.log(`     Available: ${available}`);
            console.log(`     Should show: ${total}/${capacity} (NOT ${available}/${capacity})`);
          });
        }
      } else {
        console.log('⚠️ API request failed:', apiResponse.status());
      }
    } catch (error) {
      console.log('⚠️ API request error:', error);
    }

    console.log('✅ Screenshot captured and analysis completed');
  });

  test('verify React app is functional', async ({ page }) => {
    console.log('🔍 Verifying React app functionality...');

    await page.goto('http://localhost:5173');
    await page.waitForLoadState('domcontentloaded');

    // Check page title
    const title = await page.title();
    console.log('📄 Page title:', title);

    // Check if React root has content
    const rootContent = await page.locator('#root').innerHTML();
    console.log('🔍 Root content length:', rootContent.length);

    if (rootContent.length === 0) {
      console.log('❌ React app not mounting - root element is empty');
    } else {
      console.log('✅ React app appears to be mounting - root has content');
    }

    // Check for any console errors
    const consoleErrors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    await page.waitForTimeout(3000);

    console.log('🚨 Console errors:', consoleErrors.length);
    consoleErrors.forEach(error => console.log('   Error:', error));

    expect(rootContent.length).toBeGreaterThan(0);
  });
});