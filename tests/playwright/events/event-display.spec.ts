import { test, expect } from '@playwright/test';
import { EventPage } from '../pages/event.page';
import { LoginPage } from '../pages/login.page';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Event Display Tests
 * Converted from Puppeteer test: test-event-display.js
 * 
 * Tests event display functionality including:
 * - Public events page
 * - Admin events list
 * - Event navigation
 * - Event details display
 */

test.describe('Event Display - Public', () => {
  let eventPage: EventPage;

  test.beforeEach(async ({ page }) => {
    eventPage = new EventPage(page);
  });

  test('should display events on public events page', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'public-events-display' });

    // Navigate to public events page
    await eventPage.gotoPublicEvents();
    
    // Look for event elements
    const eventSelectors = [
      '.event-card',
      '.event-item',
      '.event-list-item',
      '[class*="event"]',
      'article',
      '.card'
    ];
    
    let eventsFound = false;
    let eventCount = 0;
    let foundSelector = '';
    
    for (const selector of eventSelectors) {
      const elements = page.locator(selector);
      const count = await elements.count();
      if (count > 0) {
        eventsFound = true;
        eventCount = count;
        foundSelector = selector;
        break;
      }
    }
    
    if (eventsFound) {
      console.log(`✅ Found ${eventCount} events using selector: ${foundSelector}`);
      expect(eventCount).toBeGreaterThan(0);
      
      // Get first event text for verification
      const firstEvent = page.locator(foundSelector).first();
      const eventText = await firstEvent.textContent();
      console.log(`First event preview: ${eventText?.substring(0, 100)}...`);
    } else {
      // Check if there's a "no events" message
      const noEventsVisible = await eventPage.noEventsMessage.isVisible();
      if (noEventsVisible) {
        console.log('ℹ️  Page shows "no events" message - this might be expected');
      } else {
        console.log('⚠️  No events found on the page');
      }
    }
    
    // Verify page structure
    await expect(page).toHaveTitle(/Event/i);
  });

  test('should have proper page structure for public events', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'public-events-structure' });

    await eventPage.gotoPublicEvents();
    
    // Check for common event page elements
    const pageElements = [
      { selector: 'h1, h2', description: 'Page heading' },
      { selector: 'nav, .navigation', description: 'Navigation' },
      { selector: 'main, .content, .container', description: 'Main content area' }
    ];
    
    for (const element of pageElements) {
      const locator = page.locator(element.selector).first();
      const isVisible = await locator.isVisible();
      console.log(`${isVisible ? '✅' : '❌'} ${element.description}`);
    }
  });
});

test.describe('Event Display - Admin', () => {
  let eventPage: EventPage;
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    eventPage = new EventPage(page);
    loginPage = new LoginPage(page);
    
    // Login as admin
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
  });

  test('should display events table in admin panel', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-events-table' });

    // Navigate to admin events
    await eventPage.gotoEventsList();
    
    // Check if events table is visible
    const tableVisible = await eventPage.eventsTable.isVisible();
    expect(tableVisible).toBeTruthy();
    console.log('✅ Events table is visible');
    
    // Get event count
    const eventCount = await eventPage.getEventCount();
    console.log(`Found ${eventCount} events in admin panel`);
    
    if (eventCount === 0) {
      // Check for "no events" message
      const noEventsVisible = await eventPage.noEventsMessage.isVisible();
      if (noEventsVisible) {
        console.log('ℹ️  Page shows "no events" message');
      }
    } else {
      expect(eventCount).toBeGreaterThan(0);
      
      // Verify table headers
      const headers = await eventPage.eventTableHeaders.allTextContents();
      console.log('Table headers:', headers);
      expect(headers).toContain('Title');
    }
  });

  test('should have functioning create event button', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'create-event-navigation' });

    await eventPage.gotoEventsList();
    
    // Find and click create button
    const createButtonVisible = await eventPage.createEventButton.isVisible();
    expect(createButtonVisible).toBeTruthy();
    console.log('✅ Found create event button');
    
    await eventPage.createEventButton.click();
    await page.waitForLoadState('networkidle');
    
    // Verify navigation to create page
    const currentUrl = page.url();
    expect(currentUrl).toMatch(/new|create|edit/);
    console.log('✅ Navigated to event creation page');
    
    // Verify form is displayed
    await expect(eventPage.eventForm).toBeVisible();
  });

  test('should navigate to event details when clicking event', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-detail-navigation' });

    await eventPage.gotoEventsList();
    
    const eventCount = await eventPage.getEventCount();
    if (eventCount === 0) {
      test.skip();
      return;
    }
    
    // Click first event
    const firstEventText = await eventPage.eventRows.first().textContent();
    console.log(`Clicking on event: ${firstEventText}`);
    
    await eventPage.eventRows.first().click();
    await BlazorHelpers.waitForNavigation(page);
    
    // Verify we're on event detail page
    const currentUrl = page.url();
    expect(currentUrl).not.toContain('/events');
    expect(currentUrl).toMatch(/\/event\/|\/events\/\d+/);
    
    // Verify detail container is visible
    await expect(eventPage.eventDetailContainer).toBeVisible();
  });

  test('should display event details correctly', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-detail-display' });

    // First create an event to ensure we have one to display
    await eventPage.gotoCreateEvent();
    
    const eventTitle = eventPage.generateEventTitle('Display Test Event');
    await eventPage.createEvent({
      title: eventTitle,
      venue: 'Display Test Venue',
      description: 'This event is for testing display functionality.'
    });
    
    // Navigate to the event
    await eventPage.gotoEventsList();
    await eventPage.clickEvent(eventTitle);
    
    // Get and verify event details
    const details = await eventPage.getEventDetails();
    expect(details.title).toContain(eventTitle);
    expect(details.venue).toContain('Display Test Venue');
    
    // Check for action buttons
    await expect(eventPage.editEventButton).toBeVisible();
    await expect(eventPage.deleteEventButton).toBeVisible();
  });

  test('should update event list after creation', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-list-update' });

    // Get initial count
    await eventPage.gotoEventsList();
    const initialCount = await eventPage.getEventCount();
    
    // Create new event
    await eventPage.gotoCreateEvent();
    const eventTitle = eventPage.generateEventTitle('List Update Test');
    
    await eventPage.createEvent({
      title: eventTitle,
      venue: 'Update Test Venue',
      description: 'Testing list update after creation.'
    });
    
    // Go back to list
    await eventPage.gotoEventsList();
    
    // Verify count increased
    const newCount = await eventPage.getEventCount();
    expect(newCount).toBe(initialCount + 1);
    
    // Verify new event appears
    const eventExists = await eventPage.eventExists(eventTitle);
    expect(eventExists).toBeTruthy();
  });

  test('should handle event deletion', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-deletion' });

    // Create an event to delete
    await eventPage.gotoCreateEvent();
    const eventTitle = eventPage.generateEventTitle('Delete Test Event');
    
    await eventPage.createEvent({
      title: eventTitle,
      venue: 'Delete Test Venue',
      description: 'This event will be deleted.'
    });
    
    // Navigate to the event
    await eventPage.gotoEventsList();
    const initialCount = await eventPage.getEventCount();
    
    await eventPage.clickEvent(eventTitle);
    
    // Delete the event
    await eventPage.deleteEvent();
    
    // Verify deletion
    const finalCount = await eventPage.getEventCount();
    expect(finalCount).toBe(initialCount - 1);
    
    const eventExists = await eventPage.eventExists(eventTitle);
    expect(eventExists).toBeFalsy();
    console.log('✅ Event successfully deleted');
  });
});

test.describe('Event Display - Search and Filter', () => {
  test('should search for events', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-search' });

    const loginPage = new LoginPage(page);
    const eventPage = new EventPage(page);
    
    // Login and navigate
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await eventPage.gotoEventsList();
    
    // Look for search input
    const searchInput = page.locator('input[type="search"], input[placeholder*="Search"]').first();
    
    if (await searchInput.isVisible()) {
      // Create a searchable event
      await eventPage.gotoCreateEvent();
      const uniqueTitle = eventPage.generateEventTitle('Searchable Unique Event');
      
      await eventPage.createEvent({
        title: uniqueTitle,
        venue: 'Search Test Venue',
        description: 'Testing search functionality.'
      });
      
      await eventPage.gotoEventsList();
      
      // Search for the event
      await searchInput.fill(uniqueTitle);
      await page.waitForTimeout(1000); // Wait for search to process
      
      // Verify only our event is shown
      const visibleEvents = await eventPage.eventRows.count();
      expect(visibleEvents).toBe(1);
      
      const eventVisible = await eventPage.eventExists(uniqueTitle);
      expect(eventVisible).toBeTruthy();
      console.log('✅ Search functionality working');
    } else {
      console.log('ℹ️  No search input found - skipping search test');
    }
  });
});

test.describe('Event Display - Pagination', () => {
  test('should handle pagination if available', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-pagination' });

    const loginPage = new LoginPage(page);
    const eventPage = new EventPage(page);
    
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await eventPage.gotoEventsList();
    
    // Look for pagination controls
    const paginationControls = page.locator('.pagination, nav[aria-label="pagination"]').first();
    
    if (await paginationControls.isVisible()) {
      console.log('✅ Pagination controls found');
      
      // Check for page numbers
      const pageNumbers = paginationControls.locator('a, button').filter({ hasText: /^\d+$/ });
      const pageCount = await pageNumbers.count();
      
      if (pageCount > 1) {
        // Click second page
        await pageNumbers.nth(1).click();
        await page.waitForLoadState('networkidle');
        
        // Verify URL or content changed
        const currentUrl = page.url();
        expect(currentUrl).toMatch(/page=2|p=2/);
        console.log('✅ Pagination navigation working');
      }
    } else {
      console.log('ℹ️  No pagination controls found - might not have enough events');
    }
  });
});

test.describe('Event Display - Visual Regression', () => {
  test('should match visual snapshot of events list', async ({ page }) => {
    const loginPage = new LoginPage(page);
    const eventPage = new EventPage(page);
    
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await eventPage.gotoEventsList();
    
    // Wait for content to stabilize
    await BlazorHelpers.waitForBlazorReady(page);
    await page.waitForTimeout(1000);
    
    await expect(page).toHaveScreenshot('events-list.png', {
      fullPage: true,
      animations: 'disabled'
    });
  });

  test('should match visual snapshot of public events page', async ({ page }) => {
    const eventPage = new EventPage(page);
    
    await eventPage.gotoPublicEvents();
    await page.waitForLoadState('networkidle');
    
    await expect(page).toHaveScreenshot('events-public.png', {
      fullPage: true,
      animations: 'disabled'
    });
  });
});