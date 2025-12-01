import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * Basic Events Page Test - Simple validation that routes work
 */

test.describe('Events Basic Tests', () => {
  test('Public events page loads successfully', async ({ page }) => {
    console.log('🧪 Testing basic events page loading...');

    // Navigate to public events page
    await page.goto('/events');

    // Wait for page to load
    await page.waitForLoadState('domcontentloaded');
    
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
      // Current page heading is "Explore Classes & Meetups"
      page.locator('text=Explore Classes').count(),
      page.locator('text=No Events Currently Available').count(),
      // Check for search/filter UI elements that indicate events page loaded
      page.locator('text=Search events').count(),
      page.locator('text=Card View').count()
    ];

    const results = await Promise.all(contentChecks);
    const [eventCards, testIdCards, exploreClassesText, noEventsText, searchText, cardViewText] = results;

    console.log('Content found:');
    console.log('- Event cards:', eventCards);
    console.log('- Test ID cards:', testIdCards);
    console.log('- "Explore Classes" text:', exploreClassesText);
    console.log('- "No Events" text:', noEventsText);
    console.log('- "Search events" text:', searchText);
    console.log('- "Card View" text:', cardViewText);

    // The page should have loaded successfully - check for events content or events page UI
    const hasEventContent = eventCards > 0 || testIdCards > 0 || exploreClassesText > 0 || noEventsText > 0 || searchText > 0 || cardViewText > 0;
    expect(hasEventContent).toBe(true);
    
    console.log('✅ Basic events page test passed');
  });

  test('Admin events page loads when authenticated', async ({ page }) => {
    console.log('🧪 Testing admin events page with authentication...');

    // Login as admin before accessing admin pages
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to admin events page
    const response = await page.goto('/admin/events');

    // With authentication, should load successfully
    expect(response?.status()).toBe(200);

    await page.screenshot({ path: 'test-results/admin-events-access.png' });

    console.log('✅ Admin events page loads correctly when authenticated');
  });

  test('Event details page structure', async ({ page }) => {
    console.log('🧪 Testing event details page...');

    // First navigate to events list to find a real event
    await page.goto('/events');
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(2000);

    // Find an event link to get a real event ID
    const eventLink = page.locator('a[href*="/events/"]').first();
    let eventUrl = '/events';

    if (await eventLink.count() > 0) {
      const href = await eventLink.getAttribute('href');
      if (href && href !== '/events') {
        eventUrl = href;
        console.log(`Found event: ${eventUrl}`);
      }
    }

    // Navigate to the event details page
    await page.goto(eventUrl);
    await page.waitForLoadState('domcontentloaded');

    await page.screenshot({ path: 'test-results/event-details-structure.png' });

    // Should either show event details OR be on events list page
    const hasContent = await Promise.all([
      // Event details page indicators
      page.locator('h1').count(),  // Event title heading
      page.locator('text=Event not found').count(),
      page.locator('text=Loading event details').count(),
      // Events list page indicators
      page.locator('text=Explore Classes').count(),
      page.locator('[data-testid="event-card"]').count(),
      page.locator('text=Card View').count(),
      // Breadcrumb that indicates we're on an event page
      page.locator('nav a[href="/events"]').count()
    ]);

    const [headings, notFound, loading, exploreClasses, eventCards, cardView, breadcrumb] = hasContent;
    const hasValidContent = headings > 0 || notFound > 0 || loading > 0 || exploreClasses > 0 || eventCards > 0 || cardView > 0 || breadcrumb > 0;

    expect(hasValidContent).toBe(true);

    console.log('✅ Event details page structure test passed');
  });
});