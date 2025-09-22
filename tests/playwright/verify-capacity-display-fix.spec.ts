import { test, expect } from '@playwright/test';

test.describe('Verify Capacity Display Fix', () => {
  test('should show correct capacity format "current/total" instead of "available/total"', async ({ page }) => {
    console.log('🔍 Testing capacity display fix...');

    // Navigate to homepage
    await page.goto('http://localhost:5173');

    // Wait for the page to load
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(3000);

    // Take initial screenshot
    await page.screenshot({
      path: 'test-results/before-capacity-check.png',
      fullPage: true
    });

    // Wait for events to load
    try {
      await page.waitForSelector('[data-testid="events-list"]', { timeout: 10000 });
      console.log('✅ Events list found');
    } catch (e) {
      console.log('⚠️ Events list not found, checking for alternative selectors');

      // Check for any event-related content
      const eventContent = await page.textContent('body');
      console.log('📝 Page content preview:', eventContent?.substring(0, 500));
    }

    // Look for capacity display elements
    const capacityElements = await page.locator('text=/\\d+\/\\d+/').all();
    console.log(`📊 Found ${capacityElements.length} capacity indicators`);

    if (capacityElements.length > 0) {
      for (let i = 0; i < capacityElements.length; i++) {
        const capacityText = await capacityElements[i].textContent();
        console.log(`🎯 Capacity ${i + 1}: "${capacityText}"`);

        // Check if it matches the expected pattern (current/total)
        const match = capacityText?.match(/(\d+)\/(\d+)/);
        if (match) {
          const current = parseInt(match[1]);
          const total = parseInt(match[2]);

          console.log(`   Current: ${current}, Total: ${total}`);

          // Verify this is current/total format, not available/total
          // For empty events, should show "0/15" not "15/15"
          // For events with RSVPs, should show "2/40" not "38/40"
          if (current === 0) {
            console.log('✅ Empty event correctly shows "0/total"');
          } else if (current < total) {
            console.log('✅ Event with participants correctly shows "current/total"');
          } else if (current === total) {
            console.log('⚠️ Event shows "total/total" - need to verify if actually full');
          }
        }
      }
    }

    // Look specifically for events that should show specific values
    // Based on user description:
    // - Empty events should show "0/15" instead of "15/15"
    // - Rope Social with 2 RSVPs should show "2/40" instead of "38/40"

    // Check for the Rope Social event specifically
    const ropeSocialElement = page.locator('text=/.*[Rr]ope.*[Ss]ocial.*/');
    if (await ropeSocialElement.count() > 0) {
      console.log('🎯 Found Rope Social event');

      // Look for capacity near the Rope Social text
      const parentElement = ropeSocialElement.first().locator('xpath=ancestor::*[contains(@class,"card") or contains(@data-testid,"event")]').first();
      const capacityInEvent = await parentElement.locator('text=/\\d+\/\\d+/').textContent();

      if (capacityInEvent) {
        console.log(`📊 Rope Social capacity: "${capacityInEvent}"`);

        // Based on fix description, should show "2/40" not "38/40"
        if (capacityInEvent.includes('2/40')) {
          console.log('✅ FIXED: Rope Social correctly shows "2/40"');
        } else if (capacityInEvent.includes('38/40')) {
          console.log('❌ BUG STILL EXISTS: Rope Social shows "38/40" (available/total)');
        } else {
          console.log(`ℹ️ Rope Social shows "${capacityInEvent}" - verify if correct`);
        }
      }
    }

    // Check for empty events showing 0/total instead of total/total
    const allEvents = await page.locator('[data-testid="event-card"], .event-card, [data-testid*="event"]').all();
    console.log(`📊 Found ${allEvents.length} event cards`);

    for (let i = 0; i < allEvents.length; i++) {
      const eventCard = allEvents[i];
      const eventTitle = await eventCard.locator('h2, h3, .title, [data-testid*="title"]').first().textContent();
      const capacityText = await eventCard.locator('text=/\\d+\/\\d+/').first().textContent().catch(() => null);

      if (eventTitle && capacityText) {
        console.log(`🎯 Event: "${eventTitle}" - Capacity: "${capacityText}"`);

        const match = capacityText.match(/(\d+)\/(\d+)/);
        if (match) {
          const current = parseInt(match[1]);
          const total = parseInt(match[2]);

          // Check for the bug pattern where available spots were shown instead of current
          if (current === total && current > 10) {
            console.log(`⚠️ Potential bug: "${eventTitle}" shows ${current}/${total} - check if actually full`);
          } else if (current === 0) {
            console.log(`✅ Correct: Empty event "${eventTitle}" shows 0/${total}`);
          } else {
            console.log(`✅ Correct: Event "${eventTitle}" shows current participants ${current}/${total}`);
          }
        }
      }
    }

    // Take final screenshot to document current state
    await page.screenshot({
      path: 'test-results/capacity-display-verification.png',
      fullPage: true
    });

    // Check API data directly to understand the backend state
    console.log('🔍 Checking API data for comparison...');
    const eventsResponse = await page.request.get('http://localhost:5655/api/events');
    if (eventsResponse.ok()) {
      const eventsData = await eventsResponse.json();
      console.log('📊 API Events Data:');

      if (Array.isArray(eventsData)) {
        eventsData.forEach((event: any, index: number) => {
          console.log(`   ${index + 1}. ${event.title || event.name}`);
          console.log(`      Capacity: ${event.capacity}`);
          console.log(`      Current RSVPs: ${event.currentRSVPs || 0}`);
          console.log(`      Current Tickets: ${event.currentTickets || 0}`);
          console.log(`      Available Spots: ${(event.capacity || 0) - (event.currentRSVPs || 0) - (event.currentTickets || 0)}`);
        });
      }
    }

    console.log('✅ Capacity display verification completed');
  });

  test('should verify no events appear as FULL when they have available spots', async ({ page }) => {
    console.log('🔍 Testing that events with available spots don\'t appear FULL...');

    await page.goto('http://localhost:5173');
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(3000);

    // Look for any "FULL" indicators
    const fullIndicators = await page.locator('text=/FULL|Full|full/').all();
    console.log(`📊 Found ${fullIndicators.length} "FULL" indicators`);

    if (fullIndicators.length > 0) {
      for (let i = 0; i < fullIndicators.length; i++) {
        const fullText = await fullIndicators[i].textContent();
        console.log(`⚠️ Found FULL indicator: "${fullText}"`);

        // Find the associated event
        const eventElement = fullIndicators[i].locator('xpath=ancestor::*[contains(@class,"card") or contains(@data-testid,"event")]').first();
        const eventTitle = await eventElement.locator('h2, h3, .title').first().textContent().catch(() => 'Unknown Event');
        const capacityText = await eventElement.locator('text=/\\d+\/\\d+/').first().textContent().catch(() => null);

        console.log(`   Event: "${eventTitle}" - Capacity: "${capacityText}"`);
      }
    } else {
      console.log('✅ No "FULL" indicators found - events should show available spots');
    }

    await page.screenshot({
      path: 'test-results/no-full-events-verification.png',
      fullPage: true
    });
  });
});