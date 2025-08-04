import { Page, Locator } from '@playwright/test';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { testConfig } from '../helpers/test.config';

/**
 * Page Object Model for Member Dashboard
 * Handles member dashboard interactions including viewing RSVPs and tickets
 */
export class MemberDashboardPage {
  readonly page: Page;
  
  // Dashboard sections
  readonly upcomingEventsSection: Locator;
  readonly pastEventsSection: Locator;
  readonly myTicketsSection: Locator;
  readonly emptyState: Locator;
  
  // Event cards
  readonly eventCards: Locator;
  readonly eventTitles: Locator;
  readonly eventDates: Locator;
  readonly eventStatuses: Locator;
  readonly eventButtons: Locator;
  
  // RSVP specific elements
  readonly rsvpCards: Locator;
  readonly rsvpStatuses: Locator;
  readonly viewEventButtons: Locator;
  readonly cancelRsvpButtons: Locator;
  
  // Ticket specific elements
  readonly ticketCards: Locator;
  readonly ticketNumbers: Locator;
  readonly ticketStatuses: Locator;
  readonly viewTicketButtons: Locator;
  
  // Navigation elements
  readonly eventsLink: Locator;
  readonly profileLink: Locator;
  readonly logoutButton: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Dashboard sections
    this.upcomingEventsSection = page.locator('.upcoming-events, [class*="upcoming"]');
    this.pastEventsSection = page.locator('.past-events, [class*="past"]');
    this.myTicketsSection = page.locator('[class*="tickets"], [class*="registrations"]');
    this.emptyState = page.locator('.empty-state, [class*="empty"], [class*="no-events"]');
    
    // Event cards - generic selectors that work for both RSVPs and tickets
    this.eventCards = page.locator('.event-card, .rsvp-card, .ticket-card, [class*="event-item"]');
    this.eventTitles = page.locator('.event-title, .event-name, h3, h4, h5').filter({ 
      has: page.locator('.event-card, .rsvp-card, .ticket-card') 
    });
    this.eventDates = page.locator('.event-date, .date, time');
    this.eventStatuses = page.locator('.registration-status, .rsvp-status, [class*="status"]');
    this.eventButtons = page.locator('.event-card button, .event-card a.btn');
    
    // RSVP specific
    this.rsvpCards = this.eventCards.filter({ 
      hasText: /RSVP|Jam|Social|Meetup/i 
    });
    this.rsvpStatuses = page.locator('.rsvp-status, .registration-status').filter({ 
      hasText: /RSVP/i 
    });
    this.viewEventButtons = page.locator('button, a').filter({ 
      hasText: /View Event|Event Details/i 
    });
    this.cancelRsvpButtons = page.locator('button').filter({ 
      hasText: /Cancel RSVP/i 
    });
    
    // Ticket specific
    this.ticketCards = this.eventCards.filter({ 
      hasText: /TKT-|Workshop|Class/i 
    });
    this.ticketNumbers = page.locator('text=/TKT-\\w+/');
    this.ticketStatuses = page.locator('.ticket-status').filter({ 
      hasText: /Confirmed|Paid/i 
    });
    this.viewTicketButtons = page.locator('button, a').filter({ 
      hasText: /View Ticket|Ticket Details/i 
    });
    
    // Navigation
    this.eventsLink = page.locator('a[href*="/events"], a:has-text("Events")');
    this.profileLink = page.locator('a[href*="/profile"], a:has-text("Profile")');
    this.logoutButton = page.locator('button[form="logoutForm"], button:has-text("Logout")');
  }

  /**
   * Navigate to member dashboard
   */
  async goto(): Promise<void> {
    await this.page.goto(testConfig.urls.memberDashboard);
    await BlazorHelpers.waitForBlazorReady(this.page);
    
    // Wait for dashboard content to load
    await this.page.waitForSelector('.dashboard-content, .member-dashboard', { 
      timeout: testConfig.timeouts.navigation 
    });
  }

  /**
   * Get count of upcoming events (RSVPs + Tickets)
   */
  async getUpcomingEventCount(): Promise<number> {
    // Check for empty state first
    if (await this.emptyState.isVisible({ timeout: 2000 })) {
      return 0;
    }
    
    return await this.eventCards.count();
  }

  /**
   * Get count of RSVPs only
   */
  async getRsvpCount(): Promise<number> {
    return await this.rsvpCards.count();
  }

  /**
   * Get count of tickets only
   */
  async getTicketCount(): Promise<number> {
    return await this.ticketCards.count();
  }

  /**
   * Find event by title
   */
  async findEventByTitle(title: string): Promise<Locator | null> {
    const card = this.eventCards.filter({ hasText: title });
    if (await card.count() > 0) {
      return card.first();
    }
    return null;
  }

  /**
   * Get event details from dashboard
   */
  async getEventDetails(eventTitle: string): Promise<{
    title: string;
    date: string;
    status: string;
    buttonText: string;
    isRsvp: boolean;
  } | null> {
    const card = await this.findEventByTitle(eventTitle);
    if (!card) return null;
    
    const title = await card.locator('.event-title, h3, h4, h5').first().textContent() || '';
    const date = await card.locator('.event-date, .date, time').first().textContent() || '';
    const status = await card.locator('[class*="status"]').first().textContent() || '';
    const button = card.locator('button, a.btn').first();
    const buttonText = await button.textContent() || '';
    
    // Determine if this is an RSVP or ticket
    const isRsvp = status.toLowerCase().includes('rsvp') || 
                   buttonText.toLowerCase().includes('event') ||
                   !buttonText.toLowerCase().includes('ticket');
    
    return {
      title: title.trim(),
      date: date.trim(),
      status: status.trim(),
      buttonText: buttonText.trim(),
      isRsvp
    };
  }

  /**
   * Check if a specific event appears in RSVPs
   */
  async hasRsvpForEvent(eventTitle: string): Promise<boolean> {
    const event = await this.getEventDetails(eventTitle);
    return event !== null && event.isRsvp;
  }

  /**
   * Check if a specific ticket exists
   */
  async hasTicketForEvent(eventTitle: string): Promise<boolean> {
    const event = await this.getEventDetails(eventTitle);
    return event !== null && !event.isRsvp;
  }

  /**
   * Get all RSVP events
   */
  async getAllRsvps(): Promise<Array<{
    title: string;
    date: string;
    status: string;
  }>> {
    const rsvps = [];
    const cards = await this.rsvpCards.all();
    
    for (const card of cards) {
      const title = await card.locator('.event-title, h3, h4, h5').first().textContent() || '';
      const date = await card.locator('.event-date, .date, time').first().textContent() || '';
      const status = await card.locator('[class*="status"]').first().textContent() || '';
      
      rsvps.push({
        title: title.trim(),
        date: date.trim(),
        status: status.trim()
      });
    }
    
    return rsvps;
  }

  /**
   * Get all tickets
   */
  async getAllTickets(): Promise<Array<{
    title: string;
    ticketNumber: string;
    status: string;
  }>> {
    const tickets = [];
    const cards = await this.ticketCards.all();
    
    for (const card of cards) {
      const title = await card.locator('.event-title, h3, h4, h5').first().textContent() || '';
      const ticketNumber = await card.locator('text=/TKT-\\w+/').first().textContent() || '';
      const status = await card.locator('[class*="status"]').first().textContent() || '';
      
      tickets.push({
        title: title.trim(),
        ticketNumber: ticketNumber.trim(),
        status: status.trim()
      });
    }
    
    return tickets;
  }

  /**
   * Click view event for a specific RSVP
   */
  async viewRsvpEvent(eventTitle: string): Promise<void> {
    const card = await this.findEventByTitle(eventTitle);
    if (!card) throw new Error(`Event "${eventTitle}" not found`);
    
    const viewButton = card.locator('button, a').filter({ 
      hasText: /View Event|Event Details/i 
    }).first();
    
    await BlazorHelpers.clickAndWait(this.page, viewButton);
  }

  /**
   * Cancel RSVP from dashboard
   */
  async cancelRsvp(eventTitle: string): Promise<boolean> {
    const card = await this.findEventByTitle(eventTitle);
    if (!card) return false;
    
    const cancelButton = card.locator('button').filter({ 
      hasText: /Cancel RSVP/i 
    }).first();
    
    if (await cancelButton.isVisible()) {
      await cancelButton.click();
      
      // Handle confirmation modal if it appears
      const confirmButton = this.page.locator('button').filter({ 
        hasText: /Confirm Cancel/i 
      });
      if (await confirmButton.isVisible({ timeout: 2000 })) {
        await confirmButton.click();
      }
      
      await this.page.waitForTimeout(2000);
      return true;
    }
    
    return false;
  }

  /**
   * Navigate to events page from dashboard
   */
  async navigateToEvents(): Promise<void> {
    await BlazorHelpers.clickAndWait(this.page, this.eventsLink);
  }

  /**
   * Get empty state message
   */
  async getEmptyStateMessage(): Promise<string | null> {
    if (await this.emptyState.isVisible({ timeout: 2000 })) {
      return await this.emptyState.textContent();
    }
    return null;
  }

  /**
   * Check if dashboard is showing upcoming events
   */
  async isShowingUpcomingEvents(): Promise<boolean> {
    return await this.upcomingEventsSection.isVisible({ timeout: 2000 });
  }

  /**
   * Check if dashboard is showing past events
   */
  async isShowingPastEvents(): Promise<boolean> {
    return await this.pastEventsSection.isVisible({ timeout: 2000 });
  }

  /**
   * Wait for dashboard data to load
   */
  async waitForDashboardData(timeout: number = 7000): Promise<void> {
    // Wait for either event cards or empty state
    await this.page.waitForSelector(
      '.event-card, .empty-state, [class*="no-events"]',
      { timeout }
    );
    
    // Additional wait for dynamic content
    await this.page.waitForTimeout(1000);
  }

  /**
   * Get dashboard summary
   */
  async getDashboardSummary(): Promise<{
    hasUpcomingSection: boolean;
    eventCount: number;
    rsvpCount: number;
    ticketCount: number;
    emptyStateMessage: string | null;
  }> {
    await this.waitForDashboardData();
    
    return {
      hasUpcomingSection: await this.isShowingUpcomingEvents(),
      eventCount: await this.getUpcomingEventCount(),
      rsvpCount: await this.getRsvpCount(),
      ticketCount: await this.getTicketCount(),
      emptyStateMessage: await this.getEmptyStateMessage()
    };
  }
}