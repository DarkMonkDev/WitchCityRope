import { Page, Locator } from '@playwright/test';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { testConfig } from '../helpers/test.config';

/**
 * Page Object Model for RSVP functionality
 * Handles RSVP actions on event pages and admin RSVP management
 */
export class RsvpPage {
  readonly page: Page;
  
  // Event page RSVP elements
  readonly rsvpButton: Locator;
  readonly cancelRsvpButton: Locator;
  readonly rsvpModal: Locator;
  readonly rsvpConfirmButton: Locator;
  readonly rsvpSuccessMessage: Locator;
  readonly alreadyRegisteredMessage: Locator;
  readonly vettingRequiredMessage: Locator;
  
  // Admin RSVP management elements
  readonly rsvpTab: Locator;
  readonly rsvpTable: Locator;
  readonly rsvpRows: Locator;
  readonly confirmButtons: Locator;
  readonly cancelButtons: Locator;
  readonly emptyStateMessage: Locator;
  readonly rsvpStats: Locator;
  
  // Check-in page elements
  readonly checkInHeader: Locator;
  readonly attendeesTable: Locator;
  readonly hasTicketColumn: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Event page RSVP elements
    this.rsvpButton = page.locator('button').filter({ hasText: /^RSVP|Register$/i });
    this.cancelRsvpButton = page.locator('button').filter({ hasText: /Cancel RSVP/i });
    this.rsvpModal = page.locator('.modal, .dialog, [role="dialog"]');
    this.rsvpConfirmButton = page.locator('.modal button, .dialog button').filter({ 
      hasText: /Confirm|Submit|RSVP/i 
    });
    this.rsvpSuccessMessage = page.locator('.alert-success, .success-message, [class*="success"]');
    this.alreadyRegisteredMessage = page.locator('text=/already registered|You\'re registered|RSVP confirmed/i');
    this.vettingRequiredMessage = page.locator('text=/vetting required|must be vetted/i');
    
    // Admin RSVP management elements
    this.rsvpTab = page.locator('button.tab-button, button[role="tab"]').filter({ 
      hasText: /RSVPs|Registrations|Attendees/i 
    });
    this.rsvpTable = page.locator('table.data-table, .attendees-table');
    this.rsvpRows = page.locator('table.data-table tbody tr, .attendees-table tbody tr');
    this.confirmButtons = page.locator('button').filter({ hasText: /Confirm/i });
    this.cancelButtons = page.locator('button').filter({ hasText: /Cancel/i });
    this.emptyStateMessage = page.locator('.empty-state').filter({ hasText: /No RSVPs yet/i });
    this.rsvpStats = page.locator('.stat-card');
    
    // Check-in page elements
    this.checkInHeader = page.locator('h1').filter({ hasText: /Event Check-In/i });
    this.attendeesTable = page.locator('.attendees-table');
    this.hasTicketColumn = page.locator('th').filter({ hasText: /Has Ticket/i });
  }

  /**
   * RSVP to an event from the event details page
   */
  async rsvpToEvent(): Promise<boolean> {
    try {
      // Check if already registered
      if (await this.alreadyRegisteredMessage.isVisible({ timeout: 2000 })) {
        console.log('User is already registered for this event');
        return true;
      }
      
      // Check if vetting is required
      if (await this.vettingRequiredMessage.isVisible({ timeout: 2000 })) {
        console.log('Event requires vetting - member may not be eligible');
        return false;
      }
      
      // Click RSVP button
      await BlazorHelpers.clickAndWait(this.page, this.rsvpButton);
      
      // Handle modal if it appears
      if (await this.rsvpModal.isVisible({ timeout: 2000 })) {
        await BlazorHelpers.clickAndWait(this.page, this.rsvpConfirmButton);
      }
      
      // Wait for success confirmation
      await this.rsvpSuccessMessage.waitFor({ state: 'visible', timeout: 5000 });
      return true;
    } catch (error) {
      console.error('Failed to RSVP:', error);
      return false;
    }
  }

  /**
   * Cancel an existing RSVP
   */
  async cancelRsvp(): Promise<boolean> {
    try {
      await BlazorHelpers.clickAndWait(this.page, this.cancelRsvpButton);
      
      // Handle confirmation modal if it appears
      const confirmButton = this.page.locator('button').filter({ hasText: /Confirm Cancel/i });
      if (await confirmButton.isVisible({ timeout: 2000 })) {
        await confirmButton.click();
      }
      
      await this.page.waitForTimeout(2000);
      return true;
    } catch (error) {
      console.error('Failed to cancel RSVP:', error);
      return false;
    }
  }

  /**
   * Navigate to admin RSVP management tab
   */
  async navigateToRsvpTab(): Promise<void> {
    await BlazorHelpers.clickAndWait(this.page, this.rsvpTab);
    await this.page.waitForTimeout(1000);
  }

  /**
   * Get the count of RSVPs in admin view
   */
  async getRsvpCount(): Promise<number> {
    await this.navigateToRsvpTab();
    
    // Check for empty state first
    if (await this.emptyStateMessage.isVisible({ timeout: 2000 })) {
      return 0;
    }
    
    return await this.rsvpRows.count();
  }

  /**
   * Get RSVP stats from admin dashboard
   */
  async getRsvpStats(): Promise<{ [key: string]: string }> {
    const stats: { [key: string]: string } = {};
    
    const statCards = await this.rsvpStats.all();
    for (const card of statCards) {
      const label = await card.locator('.stat-label').textContent() || '';
      const value = await card.locator('.stat-value').textContent() || '';
      if (label && value) {
        stats[label.trim()] = value.trim();
      }
    }
    
    return stats;
  }

  /**
   * Find RSVP by member email in admin view
   */
  async findRsvpByEmail(email: string): Promise<Locator | null> {
    await this.navigateToRsvpTab();
    
    const row = this.rsvpRows.filter({ hasText: email });
    if (await row.count() > 0) {
      return row.first();
    }
    
    return null;
  }

  /**
   * Confirm an RSVP in admin view
   */
  async confirmRsvp(email: string): Promise<boolean> {
    const row = await this.findRsvpByEmail(email);
    if (!row) return false;
    
    const confirmButton = row.locator('button').filter({ hasText: /Confirm/i });
    if (await confirmButton.isVisible()) {
      await confirmButton.click();
      await this.page.waitForTimeout(1000);
      return true;
    }
    
    return false;
  }

  /**
   * Cancel an RSVP in admin view
   */
  async cancelRsvpAdmin(email: string): Promise<boolean> {
    const row = await this.findRsvpByEmail(email);
    if (!row) return false;
    
    const cancelButton = row.locator('button').filter({ hasText: /Cancel/i });
    if (await cancelButton.isVisible()) {
      await cancelButton.click();
      await this.page.waitForTimeout(1000);
      return true;
    }
    
    return false;
  }

  /**
   * Navigate to event check-in page
   */
  async navigateToCheckIn(eventId: string): Promise<void> {
    // Use relative URL - Playwright will prepend the baseURL from config
    await this.page.goto(`/admin/events/${eventId}/checkin`);
    await this.checkInHeader.waitFor({ state: 'visible', timeout: 5000 });
  }

  /**
   * Get attendee count from check-in page
   */
  async getCheckInAttendeeCount(): Promise<number> {
    if (!await this.attendeesTable.isVisible({ timeout: 2000 })) {
      return 0;
    }
    
    return await this.page.locator('.attendees-table tbody tr').count();
  }

  /**
   * Check if "Has Ticket" column exists in check-in view
   */
  async hasTicketColumnExists(): Promise<boolean> {
    return await this.hasTicketColumn.isVisible({ timeout: 2000 });
  }

  /**
   * Check if RSVP tab is visible (for social events)
   */
  async isRsvpTabVisible(): Promise<boolean> {
    return await this.rsvpTab.isVisible({ timeout: 2000 });
  }

  /**
   * Get RSVP button text
   */
  async getRsvpButtonText(): Promise<string | null> {
    if (await this.rsvpButton.isVisible({ timeout: 2000 })) {
      return await this.rsvpButton.textContent();
    }
    return null;
  }

  /**
   * Check if user can RSVP (button is visible and enabled)
   */
  async canRsvp(): Promise<boolean> {
    if (!await this.rsvpButton.isVisible({ timeout: 2000 })) {
      return false;
    }
    
    return await this.rsvpButton.isEnabled();
  }
}