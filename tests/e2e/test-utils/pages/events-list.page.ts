import { Page, Locator } from '@playwright/test';
import { SelectorHelpers } from '../helpers/selector.helpers';
import { WaitHelpers, TIMEOUTS } from '../helpers/wait.helpers';

/**
 * Page Object Model for Events List Page
 *
 * PURPOSE: Encapsulate events page interactions and provide
 * robust event discovery for tests.
 *
 * CRITICAL: This addresses event discovery failures where tests
 * couldn't find events on the page due to timing/selector issues.
 */

export class EventsListPage {
  constructor(private page: Page) {}

  /**
   * Navigate to events page
   */
  async goto(): Promise<void> {
    console.log('📍 Navigating to events list page...');
    await this.page.goto('http://localhost:5173/events');
    await this.waitForPageLoad();
  }

  /**
   * Wait for events page to fully load
   *
   * Handles multiple scenarios:
   * - Events loaded and displayed
   * - Empty state (no events)
   * - Error state (API failed)
   */
  async waitForPageLoad(): Promise<void> {
    // Wait for network to stabilize
    await this.page.waitForLoadState('networkidle', { timeout: TIMEOUTS.PAGE_LOAD });

    console.log('⏳ Waiting for events page content to render...');

    // Wait for either event cards or empty state
    const hasEvents = await this.waitForEventsOrEmptyState();

    if (hasEvents) {
      console.log('✅ Events page loaded with events');
    } else {
      console.log('ℹ️ Events page loaded but shows no events (empty state)');
    }
  }

  /**
   * Wait for either event cards or empty state message
   *
   * Returns true if events found, false if empty state
   */
  private async waitForEventsOrEmptyState(): Promise<boolean> {
    const timeout = 10000;
    const startTime = Date.now();

    while (Date.now() - startTime < timeout) {
      // Check for event cards
      const eventCards = this.page.locator('[data-testid="event-card"]');
      const cardCount = await eventCards.count();

      if (cardCount > 0) {
        console.log(`✅ Found ${cardCount} event cards`);
        return true;
      }

      // Check for empty state
      const emptyState = this.page.locator('[data-testid="events-empty-state"]');
      const hasEmptyState = await emptyState.count() > 0;

      if (hasEmptyState) {
        console.log('ℹ️ Found empty state message');
        return false;
      }

      // Check for error state
      const errorState = this.page.locator('[role="alert"]');
      const hasError = await errorState.count() > 0;

      if (hasError) {
        const errorText = await errorState.textContent();
        throw new Error(`Events page shows error: ${errorText}`);
      }

      await this.page.waitForTimeout(100);
    }

    // Timeout - neither events nor empty state found
    const currentUrl = this.page.url();
    const pageTitle = await this.page.title();

    throw new Error(
      `Events page failed to load properly:\n` +
      `- No event cards found\n` +
      `- No empty state message found\n` +
      `- Current URL: ${currentUrl}\n` +
      `- Page title: ${pageTitle}\n` +
      `\n` +
      `Possible causes:\n` +
      `1. User not authenticated\n` +
      `2. API endpoint failing\n` +
      `3. React component not rendering\n` +
      `4. Missing data-testid attributes\n` +
      `\n` +
      `Check browser console for JavaScript errors.`
    );
  }

  /**
   * Check if page has any events
   */
  async hasEvents(): Promise<boolean> {
    const eventCards = this.page.locator('[data-testid="event-card"]');
    const count = await eventCards.count();
    return count > 0;
  }

  /**
   * Get count of events on page
   */
  async getEventCount(): Promise<number> {
    const eventCards = this.page.locator('[data-testid="event-card"]');
    return await eventCards.count();
  }

  /**
   * Get first event ID from the page
   *
   * CRITICAL: This is the primary method failing in tests.
   * Now includes proper waits and clear error messages.
   *
   * @throws Error if no events found with clear diagnostic message
   */
  async getFirstEventId(): Promise<string> {
    console.log('🔍 Looking for first event on page...');

    // Ensure we have events
    const hasEvents = await this.hasEvents();
    if (!hasEvents) {
      throw new Error(
        'No events found on events page.\n' +
        '\n' +
        'Please verify:\n' +
        '1. Database has seeded events: curl http://localhost:5655/api/events\n' +
        '2. User is authenticated and has permission to view events\n' +
        '3. Events page component is rendering correctly\n' +
        '\n' +
        'To seed events, run: npm run db:seed'
      );
    }

    // Wait for first event card to be visible
    const eventCards = this.page.locator('[data-testid="event-card"]');
    await eventCards.first().waitFor({ state: 'visible', timeout: 5000 });

    // Find link within first event card
    const firstEventCard = eventCards.first();
    const eventLink = firstEventCard.locator('a[href*="/events/"]');

    // Wait for link to be attached
    await eventLink.waitFor({ state: 'attached', timeout: 5000 })
      .catch(() => {
        throw new Error(
          'Event card found but has no link.\n' +
          'Check that EventCard component renders link with href="/events/{id}"'
        );
      });

    const href = await eventLink.getAttribute('href');

    if (!href) {
      throw new Error('Event link found but href attribute is empty');
    }

    const eventId = href.split('/events/')[1];

    if (!eventId) {
      throw new Error(`Could not extract event ID from href: ${href}`);
    }

    console.log(`✅ Found event ID: ${eventId}`);
    return eventId;
  }

  /**
   * Get all event IDs from the page
   *
   * @returns Array of event IDs in display order
   */
  async getAllEventIds(): Promise<string[]> {
    console.log('🔍 Getting all event IDs from page...');

    const hasEvents = await this.hasEvents();
    if (!hasEvents) {
      console.log('ℹ️ No events on page');
      return [];
    }

    const eventLinks = this.page.locator('[data-testid="event-card"] a[href*="/events/"]');
    const count = await eventLinks.count();

    console.log(`📊 Found ${count} event links`);

    const ids: string[] = [];

    for (let i = 0; i < count; i++) {
      const link = eventLinks.nth(i);
      const href = await link.getAttribute('href');

      if (href) {
        const id = href.split('/events/')[1];
        if (id) {
          ids.push(id);
        }
      }
    }

    console.log(`✅ Extracted ${ids.length} event IDs`);
    return ids;
  }

  /**
   * Get event ID by event type (Class, Workshop, Social, etc.)
   *
   * Useful for finding specific types of events for testing:
   * ```typescript
   * const socialEventId = await eventsPage.getEventIdByType('Social');
   * ```
   */
  async getEventIdByType(eventType: string): Promise<string | null> {
    console.log(`🔍 Looking for ${eventType} event...`);

    const hasEvents = await this.hasEvents();
    if (!hasEvents) {
      return null;
    }

    const eventCards = this.page.locator('[data-testid="event-card"]');
    const count = await eventCards.count();

    for (let i = 0; i < count; i++) {
      const card = eventCards.nth(i);

      // Check if card shows this event type
      const typeText = await card.locator('[data-testid="event-type"]')
        .textContent()
        .catch(() => '');

      if (typeText?.includes(eventType)) {
        const link = card.locator('a[href*="/events/"]');
        const href = await link.getAttribute('href');

        if (href) {
          const id = href.split('/events/')[1];
          console.log(`✅ Found ${eventType} event: ${id}`);
          return id;
        }
      }
    }

    console.log(`ℹ️ No ${eventType} events found on page`);
    return null;
  }

  /**
   * Filter events by view (cards/list toggle)
   */
  async switchToListView(): Promise<void> {
    const listViewButton = await SelectorHelpers.waitForElementWithError(
      this.page,
      '[data-testid="button-view-toggle"] input[value="list"]',
      {
        errorMessage: 'List view button not found - check EventsPage has view toggle'
      }
    );

    await listViewButton.click();
    await this.page.waitForTimeout(300); // Allow animation
  }

  async switchToCardView(): Promise<void> {
    const cardViewButton = await SelectorHelpers.waitForElementWithError(
      this.page,
      '[data-testid="button-view-toggle"] input[value="cards"]',
      {
        errorMessage: 'Card view button not found - check EventsPage has view toggle'
      }
    );

    await cardViewButton.click();
    await this.page.waitForTimeout(300); // Allow animation
  }

  /**
   * Search for events
   */
  async searchEvents(query: string): Promise<void> {
    const searchInput = await SelectorHelpers.waitForElementWithError(
      this.page,
      'input[placeholder*="Search"]',
      {
        errorMessage: 'Search input not found on events page'
      }
    );

    await searchInput.fill(query);

    // Wait for search results to update
    await this.page.waitForTimeout(500);
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Toggle "Show Past Classes" switch
   */
  async togglePastClasses(showPast: boolean): Promise<void> {
    const pastClassesSwitch = await SelectorHelpers.waitForElementWithError(
      this.page,
      'input[type="checkbox"]',
      {
        errorMessage: 'Past classes toggle not found'
      }
    );

    const isCurrentlyChecked = await pastClassesSwitch.isChecked();

    if (isCurrentlyChecked !== showPast) {
      await pastClassesSwitch.click();
      await this.page.waitForTimeout(300);
      await this.page.waitForLoadState('networkidle');
    }
  }

  /**
   * Click on an event card to navigate to details
   */
  async clickEvent(eventId: string): Promise<void> {
    console.log(`🖱️ Clicking on event: ${eventId}`);

    const eventLink = this.page.locator(`a[href="/events/${eventId}"]`);

    await eventLink.waitFor({ state: 'visible', timeout: 5000 })
      .catch(() => {
        throw new Error(`Event link not found for event ID: ${eventId}`);
      });

    await eventLink.click();

    // Wait for navigation to event detail page
    await this.page.waitForURL(`**/events/${eventId}`, { timeout: 10000 });
    await this.page.waitForLoadState('networkidle');

    console.log(`✅ Navigated to event detail page`);
  }
}
