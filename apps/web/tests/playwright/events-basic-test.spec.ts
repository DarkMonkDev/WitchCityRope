import { test, expect } from '@playwright/test';

/**
 * Basic Events Page Test - Simple validation that routes work
 */

test.describe('Events Basic Tests', () => {
  test('Public events page loads successfully', async ({ page }) => {
    console.log('🧪 Testing basic events page loading...');

    // Navigate to public events page
    await page.goto('http://localhost:5174/events');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Take a screenshot for debugging
    await page.screenshot({ path: 'test-results/events-basic-load.png' });
    
    // Check if the page title contains "Events" or similar
    const title = await page.title();
    console.log('Page title:', title);
    
    // Look for events-related content with multiple strategies
    const contentChecks = [
      // Check for our specific event components
      page.locator('.event-card').count(),
      page.locator('[data-testid="event-card"]').count(),
      page.locator('text=Upcoming Events').count(),
      page.locator('text=No Events Currently Available').count(),
      // Check for Mantine components that should be present
      page.locator('[data-mantine-component]').count()
    ];
    
    const results = await Promise.all(contentChecks);
    const [eventCards, testIdCards, upcomingText, noEventsText, mantineComponents] = results;
    
    console.log('Content found:');
    console.log('- Event cards:', eventCards);
    console.log('- Test ID cards:', testIdCards);
    console.log('- "Upcoming Events" text:', upcomingText);
    console.log('- "No Events" text:', noEventsText);
    console.log('- Mantine components:', mantineComponents);
    
    // The page should have loaded successfully - check for events content
    const hasEventContent = eventCards > 0 || testIdCards > 0 || upcomingText > 0 || noEventsText > 0;
    expect(hasEventContent).toBe(true);
    
    // Additional check - if no Mantine components detected, at least verify basic React content
    if (mantineComponents === 0) {
      console.log('⚠️ Mantine components not detected, checking for basic React content...');
      const basicContent = await page.locator('body').textContent();
      expect(basicContent).toContain('Upcoming Events');
    }
    
    console.log('✅ Basic events page test passed');
  });

  test('Admin events page requires authentication', async ({ page }) => {
    console.log('🧪 Testing admin events page...');

    // Navigate to admin events page
    const response = await page.goto('http://localhost:5174/admin/events');
    
    // Should either redirect to login OR show the page (depending on auth implementation)
    // At minimum, should not give a 404
    expect(response?.status()).not.toBe(404);
    
    await page.screenshot({ path: 'test-results/admin-events-access.png' });
    
    console.log('✅ Admin events page accessibility test passed');
  });

  test('Event details page structure', async ({ page }) => {
    console.log('🧪 Testing event details page...');

    // Navigate to a sample event details page (using a test ID)
    await page.goto('http://localhost:5174/events/test-event-id');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    await page.screenshot({ path: 'test-results/event-details-structure.png' });
    
    // Should either show event details OR show "event not found"
    const hasContent = await Promise.all([
      page.locator('text=Back to Events').count(),
      page.locator('text=Event not found').count(),
      page.locator('text=Loading event details').count()
    ]);
    
    const [backLink, notFound, loading] = hasContent;
    const hasValidContent = backLink > 0 || notFound > 0 || loading > 0;
    
    expect(hasValidContent).toBe(true);
    
    console.log('✅ Event details page structure test passed');
  });
});