import { Page, Locator } from '@playwright/test';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { testConfig } from '../helpers/test.config';

/**
 * Page Object Model for the Admin Events Management page
 * Extends regular events functionality with admin-specific features
 */
export class AdminEventsPage {
  readonly page: Page;
  
  // Page elements
  readonly pageTitle: Locator;
  readonly createEventButton: Locator;
  readonly eventsTable: Locator;
  readonly searchInput: Locator;
  readonly searchButton: Locator;
  readonly pagination: Locator;
  
  // Filter controls
  readonly statusFilter: Locator;
  readonly typeFilter: Locator;
  readonly dateRangeFilter: Locator;
  readonly sortDropdown: Locator;
  
  // Event form elements (for create/edit)
  readonly eventModal: Locator;
  readonly titleInput: Locator;
  readonly descriptionInput: Locator;
  readonly startDateInput: Locator;
  readonly endDateInput: Locator;
  readonly locationInput: Locator;
  readonly maxAttendeesInput: Locator;
  readonly ticketPriceInput: Locator;
  readonly eventTypeSelect: Locator;
  readonly eventStatusSelect: Locator;
  readonly showInPublicListCheckbox: Locator;
  readonly saveEventButton: Locator;
  readonly cancelButton: Locator;
  readonly deleteEventButton: Locator;
  
  // Table headers
  readonly titleHeader: Locator;
  readonly dateHeader: Locator;
  readonly locationHeader: Locator;
  readonly attendeesHeader: Locator;
  readonly statusHeader: Locator;
  readonly actionsHeader: Locator;
  
  // Bulk actions
  readonly selectAllCheckbox: Locator;
  readonly bulkActionsDropdown: Locator;
  readonly applyBulkActionButton: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Initialize locators
    this.pageTitle = page.locator('h1:has-text("Events"), .page-title:has-text("Event Management")').first();
    this.createEventButton = page.locator('button:has-text("Create New Event"), button.btn.btn-primary:has-text("Create New Event"), button:has-text("+ Create New Event")').first();
    this.eventsTable = page.locator('table.events-table, .wcr-table, table').filter({ has: page.locator('th:has-text("Title")') }).first();
    this.searchInput = page.locator('input[placeholder*="Search events"], #eventSearch').first();
    this.searchButton = page.locator('button:has-text("Search")').filter({ hasText: /search/i }).first();
    this.pagination = page.locator('.pagination, .wcr-pagination').first();
    
    // Filter controls
    this.statusFilter = page.locator('select#statusFilter, select[name="status"], .status-filter').first();
    this.typeFilter = page.locator('select#typeFilter, select[name="eventType"], .type-filter').first();
    this.dateRangeFilter = page.locator('select#dateRange, .date-range-filter').first();
    this.sortDropdown = page.locator('select#sortBy, .sort-dropdown').first();
    
    // Event form elements
    this.eventModal = page.locator('.modal:has-text("Event"), .event-form-modal, #eventModal').first();
    this.titleInput = page.locator('input[name="title"], #eventTitle').first();
    this.descriptionInput = page.locator('textarea[name="description"], #eventDescription, .rich-text-editor').first();
    this.startDateInput = page.locator('input[name="startDate"], #startDate').first();
    this.endDateInput = page.locator('input[name="endDate"], #endDate').first();
    this.locationInput = page.locator('input[name="location"], #eventLocation').first();
    this.maxAttendeesInput = page.locator('input[name="maxAttendees"], #maxAttendees').first();
    this.ticketPriceInput = page.locator('input[name="ticketPrice"], #ticketPrice').first();
    this.eventTypeSelect = page.locator('select[name="eventType"], #eventType').first();
    this.eventStatusSelect = page.locator('select[name="status"], #eventStatus').first();
    this.showInPublicListCheckbox = page.locator('input[name="showInPublicList"], #showInPublicList').first();
    this.saveEventButton = page.locator('button:has-text("Save"), button[type="submit"]:has-text("Create")').first();
    this.cancelButton = page.locator('button:has-text("Cancel"), button[data-dismiss="modal"]').first();
    this.deleteEventButton = page.locator('button:has-text("Delete Event"), .btn-danger:has-text("Delete")').first();
    
    // Table headers
    this.titleHeader = page.locator('th:has-text("Title")').first();
    this.dateHeader = page.locator('th:has-text("Date")').first();
    this.locationHeader = page.locator('th:has-text("Location")').first();
    this.attendeesHeader = page.locator('th:has-text("Attendees")').first();
    this.statusHeader = page.locator('th:has-text("Status")').first();
    this.actionsHeader = page.locator('th:has-text("Actions")').first();
    
    // Bulk actions
    this.selectAllCheckbox = page.locator('input[type="checkbox"].select-all, th input[type="checkbox"]').first();
    this.bulkActionsDropdown = page.locator('select#bulkActions, .bulk-actions-dropdown').first();
    this.applyBulkActionButton = page.locator('button:has-text("Apply"), button:has-text("Apply Action")').first();
  }

  /**
   * Navigate to the admin events page
   */
  async goto(): Promise<void> {
    // Ensure we're using a relative URL that works with the baseURL
    const eventsUrl = testConfig.urls.adminEvents.startsWith('/') ? testConfig.urls.adminEvents : `/${testConfig.urls.adminEvents}`;
    
    try {
      await this.page.goto(eventsUrl);
      await BlazorHelpers.waitForBlazorReady(this.page);
    } catch (error) {
      console.error(`Failed to navigate to admin events: ${eventsUrl}`, error);
      // Try with full URL as fallback
      const fullUrl = `${testConfig.baseUrl}${eventsUrl}`;
      console.log(`Attempting navigation with full URL: ${fullUrl}`);
      await this.page.goto(fullUrl);
      await BlazorHelpers.waitForBlazorReady(this.page);
    }
    
    // Wait for events table to load
    await this.pageTitle.waitFor({ state: 'visible', timeout: testConfig.timeouts.navigation });
  }

  /**
   * Check if user has access to admin events
   */
  async hasAccess(): Promise<boolean> {
    try {
      await this.goto();
      const currentUrl = this.page.url();
      
      // If not redirected to login and create button is visible
      if (!currentUrl.includes('/login')) {
        await this.createEventButton.waitFor({ state: 'visible', timeout: 5000 });
        return true;
      }
      return false;
    } catch {
      return false;
    }
  }

  /**
   * Open create event modal
   */
  async openCreateEventModal(): Promise<void> {
    await BlazorHelpers.clickAndWait(this.page, this.createEventButton);
    
    // Check if navigated to a new page or opened modal
    const currentUrl = this.page.url();
    if (currentUrl.includes('/create') || currentUrl.includes('/new')) {
      // Navigated to create page
      await this.titleInput.waitFor({ state: 'visible', timeout: 5000 });
    } else {
      // Modal should open
      await this.eventModal.waitFor({ state: 'visible', timeout: 5000 });
    }
  }

  /**
   * Create a new event
   */
  async createEvent(eventData: {
    title: string;
    description: string;
    startDate: string;
    endDate: string;
    location: string;
    maxAttendees?: number;
    ticketPrice?: number;
    eventType?: string;
    status?: string;
    showInPublicList?: boolean;
  }): Promise<void> {
    await this.openCreateEventModal();
    
    // Fill event form
    await BlazorHelpers.fillAndWait(this.page, this.titleInput, eventData.title);
    await BlazorHelpers.fillAndWait(this.page, this.descriptionInput, eventData.description);
    await BlazorHelpers.fillAndWait(this.page, this.startDateInput, eventData.startDate);
    await BlazorHelpers.fillAndWait(this.page, this.endDateInput, eventData.endDate);
    await BlazorHelpers.fillAndWait(this.page, this.locationInput, eventData.location);
    
    if (eventData.maxAttendees !== undefined) {
      await BlazorHelpers.fillAndWait(this.page, this.maxAttendeesInput, eventData.maxAttendees.toString());
    }
    
    if (eventData.ticketPrice !== undefined) {
      await BlazorHelpers.fillAndWait(this.page, this.ticketPriceInput, eventData.ticketPrice.toString());
    }
    
    if (eventData.eventType) {
      await this.eventTypeSelect.selectOption(eventData.eventType);
    }
    
    if (eventData.status) {
      await this.eventStatusSelect.selectOption(eventData.status);
    }
    
    if (eventData.showInPublicList !== undefined) {
      if (eventData.showInPublicList) {
        await this.showInPublicListCheckbox.check();
      } else {
        await this.showInPublicListCheckbox.uncheck();
      }
    }
    
    // Save event
    await BlazorHelpers.clickAndWait(this.page, this.saveEventButton);
    
    // Wait for navigation or modal close
    const currentUrl = this.page.url();
    if (currentUrl.includes('/create') || currentUrl.includes('/new')) {
      // Should navigate back to events list
      await BlazorHelpers.waitForNavigation(this.page, /.*\/admin\/events/);
    } else {
      // Modal should close
      await this.eventModal.waitFor({ state: 'hidden', timeout: 5000 });
    }
  }

  /**
   * Search for events
   */
  async searchEvents(query: string): Promise<void> {
    await BlazorHelpers.fillAndWait(this.page, this.searchInput, query);
    await BlazorHelpers.clickAndWait(this.page, this.searchButton);
    
    // Wait for table to update
    await this.page.waitForTimeout(500);
  }

  /**
   * Filter events by status
   */
  async filterByStatus(status: 'All' | 'Published' | 'Draft' | 'Cancelled'): Promise<void> {
    await this.statusFilter.selectOption(status);
    await this.page.waitForTimeout(500);
  }

  /**
   * Filter events by type
   */
  async filterByType(type: 'All' | 'Social' | 'Workshop' | 'Performance' | 'Vetting'): Promise<void> {
    await this.typeFilter.selectOption(type);
    await this.page.waitForTimeout(500);
  }

  /**
   * Edit event by title
   */
  async editEvent(title: string): Promise<void> {
    const eventRow = await this.getEventRow(title);
    if (!eventRow) {
      throw new Error(`Event with title "${title}" not found`);
    }
    
    const editButton = eventRow.locator('button:has-text("Edit"), a:has-text("Edit")').first();
    await BlazorHelpers.clickAndWait(this.page, editButton);
    
    // Check if navigated to edit page or opened modal
    const currentUrl = this.page.url();
    if (currentUrl.includes('/edit')) {
      await this.titleInput.waitFor({ state: 'visible', timeout: 5000 });
    } else {
      await this.eventModal.waitFor({ state: 'visible', timeout: 5000 });
    }
  }

  /**
   * Delete event by title
   */
  async deleteEvent(title: string, confirm: boolean = true): Promise<void> {
    await this.editEvent(title);
    
    // Click delete button
    await BlazorHelpers.clickAndWait(this.page, this.deleteEventButton);
    
    if (confirm) {
      // Handle confirmation dialog
      const confirmButton = this.page.locator('button:has-text("Confirm"), button:has-text("Yes")').first();
      await BlazorHelpers.clickAndWait(this.page, confirmButton);
    }
    
    // Wait for navigation or modal close
    const currentUrl = this.page.url();
    if (currentUrl.includes('/edit')) {
      await BlazorHelpers.waitForNavigation(this.page, /.*\/admin\/events/);
    } else {
      await this.eventModal.waitFor({ state: 'hidden', timeout: 5000 });
    }
  }

  /**
   * Get event row by title
   */
  async getEventRow(title: string): Promise<Locator | null> {
    const rows = this.eventsTable.locator('tbody tr');
    const count = await rows.count();
    
    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const rowText = await row.textContent();
      if (rowText?.includes(title)) {
        return row;
      }
    }
    
    return null;
  }

  /**
   * Get all events from current page
   */
  async getEvents(): Promise<Array<{
    title: string;
    date: string;
    location: string;
    attendees: string;
    status: string;
  }>> {
    const events = [];
    const rows = this.eventsTable.locator('tbody tr');
    const count = await rows.count();
    
    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const cells = row.locator('td');
      
      events.push({
        title: await cells.nth(0).textContent() || '',
        date: await cells.nth(1).textContent() || '',
        location: await cells.nth(2).textContent() || '',
        attendees: await cells.nth(3).textContent() || '',
        status: await cells.nth(4).textContent() || ''
      });
    }
    
    return events;
  }

  /**
   * View event details
   */
  async viewEventDetails(title: string): Promise<void> {
    const eventRow = await this.getEventRow(title);
    if (!eventRow) {
      throw new Error(`Event with title "${title}" not found`);
    }
    
    const viewButton = eventRow.locator('button:has-text("View"), a:has-text("View"), a:has-text("Details")').first();
    await BlazorHelpers.clickAndWait(this.page, viewButton);
  }

  /**
   * Check if event exists
   */
  async eventExists(title: string): Promise<boolean> {
    const eventRow = await this.getEventRow(title);
    return eventRow !== null;
  }

  /**
   * Get available action buttons on the page
   */
  async getAvailableActions(): Promise<string[]> {
    const buttons = await this.page.locator('button, a.btn').allTextContents();
    return buttons.filter(text => text.trim().length > 0);
  }

  /**
   * Check if create event button is visible
   */
  async canCreateEvents(): Promise<boolean> {
    return await this.createEventButton.isVisible();
  }

  /**
   * Apply bulk action to selected events
   */
  async applyBulkAction(action: 'delete' | 'publish' | 'unpublish' | 'cancel'): Promise<void> {
    await this.bulkActionsDropdown.selectOption(action);
    await BlazorHelpers.clickAndWait(this.page, this.applyBulkActionButton);
    
    // Handle confirmation if needed
    const confirmButton = this.page.locator('button:has-text("Confirm"), button:has-text("Yes")');
    if (await confirmButton.isVisible()) {
      await BlazorHelpers.clickAndWait(this.page, confirmButton);
    }
  }

  /**
   * Select events for bulk action
   */
  async selectEventsForBulkAction(titles: string[]): Promise<void> {
    for (const title of titles) {
      const eventRow = await this.getEventRow(title);
      if (eventRow) {
        const checkbox = eventRow.locator('input[type="checkbox"]').first();
        await checkbox.check();
      }
    }
  }

  /**
   * Navigate to a specific page number in pagination
   */
  async goToPage(pageNumber: number): Promise<void> {
    const pageLink = this.pagination.locator(`a:has-text("${pageNumber}")`);
    if (await pageLink.isVisible()) {
      await BlazorHelpers.clickAndWait(this.page, pageLink);
      // Wait for table to update
      await this.page.waitForTimeout(500);
    } else {
      throw new Error(`Page ${pageNumber} is not available in pagination`);
    }
  }

  /**
   * Sort events by specified criteria
   */
  async sortBy(criteria: 'date' | 'name' | 'location' | 'status'): Promise<void> {
    if (await this.sortDropdown.isVisible()) {
      await this.sortDropdown.selectOption(criteria);
      // Wait for table to update
      await this.page.waitForTimeout(500);
    } else {
      throw new Error('Sort dropdown is not available');
    }
  }

  /**
   * Take a screenshot for debugging
   */
  async screenshot(name: string): Promise<void> {
    await this.page.screenshot({ 
      path: `test-results/screenshots/admin-events-${name}.png`,
      fullPage: true 
    });
  }
}