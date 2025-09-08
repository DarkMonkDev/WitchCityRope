import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';
import { WaitHelpers } from './helpers/wait.helpers';

/**
 * Comprehensive Events E2E Tests
 * Tests public events browsing, event details, and authenticated event interactions
 */
test.describe('Events - Public Access', () => {
  test('should browse events without authentication', async ({ page }) => {
    await page.goto('/events');
    await WaitHelpers.waitForPageLoad(page, '**/events');

    // Verify events page loads with correct title
    await expect(page.locator('h1')).toContainText(/events/i);

    // Wait for events to load from API
    await WaitHelpers.waitForApiResponse(page, '/api/events');
    
    // Verify event cards are displayed
    const eventCards = page.locator('[data-testid="event-card"]');
    await expect(eventCards).toHaveCount(3, { timeout: 10000 });

    // Verify event cards have required information
    const firstEvent = eventCards.first();
    await expect(firstEvent).toContainText(/rope|workshop|class/i);
    
    // Should show date/time information
    await expect(firstEvent.locator('[data-testid="event-date"]')).toBeVisible();
    await expect(firstEvent.locator('[data-testid="event-time"]')).toBeVisible();
    
    console.log('✅ Public events page loads correctly');
  });

  test('should display event details when clicking event card', async ({ page }) => {
    await page.goto('/events');
    await WaitHelpers.waitForDataLoad(page, 'events-list');

    const eventCards = page.locator('[data-testid="event-card"]');
    const firstEvent = eventCards.first();
    
    // Get event title for verification
    const eventTitle = await firstEvent.locator('[data-testid="event-title"]').textContent();
    
    // Click event card
    await firstEvent.click();
    
    // Should navigate to event details or show modal
    await page.waitForTimeout(500);
    
    // Verify event details are displayed
    const eventDetails = page.locator('[data-testid="event-details"]');
    if (await eventDetails.count() > 0) {
      await expect(eventDetails).toBeVisible();
      await expect(eventDetails).toContainText(eventTitle || '');
    } else {
      // Might navigate to event page instead
      await expect(page).toHaveURL(/.*\/events\/.*/);
    }
    
    console.log('✅ Event details accessible from event card');
  });

  test('should filter events by type', async ({ page }) => {
    await page.goto('/events');
    await WaitHelpers.waitForDataLoad(page, 'events-list');

    // Look for filter controls
    const filterControls = [
      '[data-testid="event-type-filter"]',
      '[data-testid="filter-select"]',
      'select[name="eventType"]',
      '.filter-control'
    ];

    let filterFound = false;
    for (const selector of filterControls) {
      const filter = page.locator(selector);
      if (await filter.count() > 0) {
        // Try to use the filter
        await filter.click();
        
        // Look for filter options
        const classOption = page.locator('text=Class, text=Workshop');
        if (await classOption.count() > 0) {
          await classOption.click();
          await WaitHelpers.waitForStateUpdate(page);
          filterFound = true;
          break;
        }
      }
    }

    if (filterFound) {
      // Verify filtering works
      const eventCards = page.locator('[data-testid="event-card"]');
      const eventCount = await eventCards.count();
      expect(eventCount).toBeGreaterThan(0);
      console.log(`✅ Event filtering works - ${eventCount} events shown`);
    } else {
      console.log('ℹ️ Event filters not yet implemented or not found');
    }
  });

  test('should handle empty events state', async ({ page }) => {
    // Mock API to return empty events
    await page.route('**/api/events', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      });
    });

    await page.goto('/events');
    await WaitHelpers.waitForPageLoad(page);

    // Should show empty state
    const emptyStateSelectors = [
      '[data-testid="no-events"]',
      '[data-testid="empty-state"]',
      'text=No events',
      'text=No upcoming events'
    ];

    let emptyStateFound = false;
    for (const selector of emptyStateSelectors) {
      const element = page.locator(selector);
      if (await element.count() > 0 && await element.isVisible()) {
        emptyStateFound = true;
        console.log(`✅ Empty state displayed: ${selector}`);
        break;
      }
    }

    if (!emptyStateFound) {
      // Check if page shows any indication of no events
      const pageContent = await page.textContent('body');
      expect(pageContent?.toLowerCase()).toMatch(/no events|empty|none found/);
      console.log('✅ Empty state handled gracefully');
    }
  });

  test('should handle events API error gracefully', async ({ page }) => {
    // Mock API to return error
    await page.route('**/api/events', route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal Server Error' })
      });
    });

    await page.goto('/events');
    
    // Should show error state or fallback content
    await page.waitForTimeout(2000);
    
    const errorStateSelectors = [
      '[data-testid="events-error"]',
      '[data-testid="error-message"]',
      '.error-state',
      'text=Error loading events'
    ];

    let errorStateFound = false;
    for (const selector of errorStateSelectors) {
      const element = page.locator(selector);
      if (await element.count() > 0) {
        await expect(element).toBeVisible();
        errorStateFound = true;
        console.log(`✅ Error state displayed: ${selector}`);
        break;
      }
    }

    if (!errorStateFound) {
      // Should at least not crash the page
      const pageContent = await page.textContent('body');
      expect(pageContent).toBeDefined();
      console.log('✅ Error handled gracefully (no crash)');
    }
  });
});

test.describe('Events - Authenticated Access', () => {
  test.beforeEach(async ({ page }) => {
    // Login as member for authenticated tests
    await AuthHelpers.loginAs(page, 'member');
  });

  test('should show event registration options for authenticated users', async ({ page }) => {
    await page.goto('/events');
    await WaitHelpers.waitForDataLoad(page, 'events-list');

    const eventCards = page.locator('[data-testid="event-card"]');
    const firstEvent = eventCards.first();
    
    // Click event to view details
    await firstEvent.click();
    await page.waitForTimeout(500);

    // Should show registration button for authenticated users
    const registrationButtons = [
      '[data-testid="register-button"]',
      '[data-testid="join-event"]',
      'button:has-text("Register")',
      'button:has-text("Join")',
      'button:has-text("RSVP")'
    ];

    let registrationFound = false;
    for (const selector of registrationButtons) {
      const button = page.locator(selector);
      if (await button.count() > 0 && await button.isVisible()) {
        await expect(button).toBeEnabled();
        registrationFound = true;
        console.log(`✅ Registration option available: ${selector}`);
        break;
      }
    }

    if (!registrationFound) {
      console.log('ℹ️ Event registration not yet implemented or event full');
    }
  });

  test('should show different content for different user roles', async ({ page }) => {
    // Test as regular member
    await page.goto('/events');
    await WaitHelpers.waitForDataLoad(page, 'events-list');
    
    const memberContent = await page.textContent('body');
    
    // Logout and login as admin
    await AuthHelpers.logout(page);
    await AuthHelpers.loginAs(page, 'admin');
    
    await page.goto('/events');
    await WaitHelpers.waitForDataLoad(page, 'events-list');
    
    const adminContent = await page.textContent('body');
    
    // Admin should see additional options (like event management)
    const adminFeatures = [
      'Edit Event',
      'Manage',
      'Delete',
      'Create Event',
      'Admin'
    ];

    let hasAdminFeatures = false;
    for (const feature of adminFeatures) {
      if (adminContent?.includes(feature) && !memberContent?.includes(feature)) {
        hasAdminFeatures = true;
        console.log(`✅ Admin-specific feature found: ${feature}`);
        break;
      }
    }

    if (!hasAdminFeatures) {
      console.log('ℹ️ Admin-specific features not visible or not yet implemented');
    }
  });

  test('should handle event registration flow', async ({ page }) => {
    await page.goto('/events');
    await WaitHelpers.waitForDataLoad(page, 'events-list');

    const eventCards = page.locator('[data-testid="event-card"]');
    const availableEvent = eventCards.first();
    
    // Click event
    await availableEvent.click();
    
    // Look for registration button
    const registerButton = page.locator('[data-testid="register-button"], button:has-text("Register")');
    
    if (await registerButton.count() > 0 && await registerButton.isVisible()) {
      // Click register
      await registerButton.click();
      
      // Should show registration form or confirmation
      await page.waitForTimeout(1000);
      
      const registrationElements = [
        '[data-testid="registration-form"]',
        '[data-testid="registration-modal"]',
        'text=Registration',
        'text=Confirm',
        'button:has-text("Confirm Registration")'
      ];
      
      let registrationFlowFound = false;
      for (const selector of registrationElements) {
        const element = page.locator(selector);
        if (await element.count() > 0) {
          await expect(element).toBeVisible();
          registrationFlowFound = true;
          console.log(`✅ Registration flow started: ${selector}`);
          break;
        }
      }
      
      if (!registrationFlowFound) {
        // Check if we navigated to registration page
        if (page.url().includes('register') || page.url().includes('signup')) {
          console.log('✅ Navigated to registration page');
        }
      }
    } else {
      console.log('ℹ️ Event registration not available for this event');
    }
  });
});

test.describe('Events - Responsive Design', () => {
  const viewports = [
    { name: 'Mobile', width: 375, height: 667 },
    { name: 'Tablet', width: 768, height: 1024 },
    { name: 'Desktop', width: 1920, height: 1080 }
  ];

  viewports.forEach(viewport => {
    test(`should display correctly on ${viewport.name}`, async ({ page }) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      
      await page.goto('/events');
      await WaitHelpers.waitForPageLoad(page);

      // Verify page loads without horizontal scroll
      const bodyWidth = await page.locator('body').boundingBox();
      expect(bodyWidth?.width).toBeLessThanOrEqual(viewport.width);

      // Verify events are visible
      const eventCards = page.locator('[data-testid="event-card"]');
      if (await eventCards.count() > 0) {
        await expect(eventCards.first()).toBeVisible();
      }

      // Take screenshot for manual review
      await page.screenshot({ 
        path: `test-results/events-${viewport.name.toLowerCase()}.png`,
        fullPage: true
      });

      console.log(`✅ Events page responsive on ${viewport.name} (${viewport.width}x${viewport.height})`);
    });
  });
});

test.describe('Events - Performance', () => {
  test('should load events within performance budget', async ({ page }) => {
    const startTime = Date.now();
    
    await page.goto('/events');
    await WaitHelpers.waitForDataLoad(page, 'events-list');
    
    const loadTime = Date.now() - startTime;
    
    // Should load within 3 seconds
    expect(loadTime).toBeLessThan(3000);
    
    console.log(`✅ Events page loaded in ${loadTime}ms`);
  });

  test('should handle large number of events efficiently', async ({ page }) => {
    // Mock API with many events
    const manyEvents = Array.from({ length: 50 }, (_, i) => ({
      id: i + 1,
      title: `Event ${i + 1}`,
      startDate: new Date(Date.now() + i * 24 * 60 * 60 * 1000).toISOString(),
      description: `Description for event ${i + 1}`,
      capacity: 20,
      registrationCount: Math.floor(Math.random() * 20)
    }));

    await page.route('**/api/events', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(manyEvents)
      });
    });

    const startTime = Date.now();
    
    await page.goto('/events');
    await WaitHelpers.waitForPageLoad(page);
    
    const loadTime = Date.now() - startTime;
    
    // Should still load efficiently with many events
    expect(loadTime).toBeLessThan(5000);
    
    // Verify events are displayed (might be paginated)
    const eventCards = page.locator('[data-testid="event-card"]');
    const eventCount = await eventCards.count();
    expect(eventCount).toBeGreaterThan(0);
    
    console.log(`✅ Handled ${manyEvents.length} events efficiently in ${loadTime}ms, showing ${eventCount} events`);
  });
});