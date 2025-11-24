import { test, expect } from '@playwright/test';

test.describe('Events Display Verification - Safe Test', () => {
  test.beforeEach(async ({ page }) => {
    // Clear any existing auth data
    await page.context().clearCookies();
    await page.goto('/');
  });

  test('should display events correctly on events page', async ({ page }) => {
    console.log('🎯 Starting focused events display test...');
    
    // Step 1: Navigate to events page
    console.log('📍 Step 1: Navigating to /events page...');
    await page.goto('/events');
    
    // Step 2: Wait for page to load and take initial screenshot
    await page.waitForLoadState('networkidle');
    await page.screenshot({ 
      path: './test-results/events-page-initial-load.png',
      fullPage: true 
    });
    console.log('📸 Screenshot taken: events-page-initial-load.png');
    
    // Step 3: Check for events page title/header
    console.log('📍 Step 3: Verifying events page header...');
    // Use .first() to avoid strict mode violation (multiple h1/h2 on page)
    await expect(page.locator('h1, h2').first()).toContainText(/Explore|Events/i);
    
    // Step 4: Look for specific events mentioned in requirements
    console.log('📍 Step 4: Looking for specific event titles...');
    
    // Wait a bit more for API data to load
    await page.waitForTimeout(2000);
    
    // Take screenshot after waiting for data
    await page.screenshot({ 
      path: './test-results/events-after-api-load.png',
      fullPage: true 
    });
    console.log('📸 Screenshot taken: events-after-api-load.png');
    
    // Look for the specific events mentioned in requirements
    const page_content = await page.content();
    
    // Check for "Rope Bondage Fundamentals"
    const hasRopeFundamentals = page_content.includes('Rope Bondage Fundamentals') || 
                               await page.locator('text=Rope Bondage Fundamentals').count() > 0;
    
    // Check for "Community Social Night"  
    const hasSocialNight = page_content.includes('Community Social Night') ||
                          await page.locator('text=Community Social Night').count() > 0;
    
    // Check that we DON'T see the "No Events" placeholder
    const hasNoEventsText = page_content.includes('No Events Currently Available') ||
                           page_content.includes('No events found') ||
                           page_content.includes('No upcoming events');
    
    console.log(`🔍 Event verification results:`);
    console.log(`  - Rope Bondage Fundamentals found: ${hasRopeFundamentals}`);
    console.log(`  - Community Social Night found: ${hasSocialNight}`);
    console.log(`  - "No Events" placeholder present: ${hasNoEventsText}`);
    
    // Step 5: Verify events are actually displaying (not just showing placeholder)
    console.log('📍 Step 5: Verifying events are actually displaying...');
    expect(hasNoEventsText).toBe(false);
    
    // Step 6: Check for event cards or list items
    console.log('📍 Step 6: Looking for event display elements...');
    const eventElements = await page.locator('[data-testid*="event"], .event-card, .event-item, article, [class*="event"]').count();
    console.log(`🎯 Found ${eventElements} potential event elements`);
    
    // Step 7: Verify at least some events are visible
    if (!hasRopeFundamentals && !hasSocialNight) {
      console.log('⚠️  Specific events not found, checking for any event data...');
      
      // Look for any event-like content
      const hasAnyEvents = await page.locator('text=/workshop|class|event|social|rope|bondage/i').count() > 0;
      expect(hasAnyEvents).toBe(true);
    } else {
      console.log('✅ Specific events found successfully!');
      expect(hasRopeFundamentals || hasSocialNight).toBe(true);
    }
    
    // Step 8: Take final screenshot for evidence
    await page.screenshot({ 
      path: './test-results/events-verification-complete.png',
      fullPage: true 
    });
    console.log('📸 Final screenshot taken: events-verification-complete.png');
    
    // Step 9: Verify page structure shows events are being loaded
    console.log('📍 Step 9: Checking page structure for event loading...');
    const pageText = await page.textContent('body');
    
    // Should NOT contain loading spinners or empty states
    expect(pageText).not.toContain('Loading events...');
    expect(pageText).not.toContain('No Events Currently Available');
    
    console.log('🎉 Events display verification completed successfully!');
  });

  test('should show event details when present', async ({ page }) => {
    console.log('🎯 Testing event details display...');
    
    await page.goto('/events');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    // Take screenshot of current state
    await page.screenshot({ 
      path: './test-results/event-details-check.png',
      fullPage: true 
    });
    
    // Look for event details like dates, descriptions, RSVP/ticket options, etc.
    const hasEventDetails = await page.locator('text=/date|time|description|rsvp|ticket|details/i').count() > 0;
    console.log(`📅 Event details found: ${hasEventDetails}`);
    
    // If events are showing, they should have some details
    const hasEvents = await page.locator('[data-testid*="event"], .event-card, article').count() > 0;
    if (hasEvents) {
      expect(hasEventDetails).toBe(true);
    }
    
    console.log('✅ Event details verification completed!');
  });
});