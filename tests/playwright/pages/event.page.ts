import { Page, Locator } from '@playwright/test';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { testConfig } from '../helpers/test.config';

/**
 * Page Object Model for Event management pages
 * Handles both event creation and event listing/display functionality
 */
export class EventPage {
  readonly page: Page;
  
  // Event list page elements
  readonly eventsTable: Locator;
  readonly eventRows: Locator;
  readonly createEventButton: Locator;
  readonly noEventsMessage: Locator;
  readonly eventTableHeaders: Locator;
  
  // Event form elements
  readonly eventForm: Locator;
  readonly titleInput: Locator;
  readonly leadTeacherSelect: Locator;
  readonly venueInput: Locator;
  readonly descriptionEditor: Locator;
  readonly descriptionRichTextContent: Locator;
  readonly descriptionTextarea: Locator; // Fallback
  readonly startDateInput: Locator;
  readonly endDateInput: Locator;
  readonly submitButton: Locator;
  readonly cancelButton: Locator;
  
  // Validation elements
  readonly validationSummary: Locator;
  readonly validationMessages: Locator;
  readonly titleValidation: Locator;
  readonly venueValidation: Locator;
  readonly descriptionValidation: Locator;
  
  // Event detail elements
  readonly eventDetailContainer: Locator;
  readonly editEventButton: Locator;
  readonly deleteEventButton: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Event list page elements
    this.eventsTable = page.locator('table').filter({ has: page.locator('th:has-text("Title")') });
    this.eventRows = page.locator('table tbody tr');
    this.createEventButton = page.locator('button:has-text("Create"), button:has-text("New Event"), a:has-text("Create Event")').first();
    this.noEventsMessage = page.locator('text=/no events/i');
    this.eventTableHeaders = page.locator('table thead th');
    
    // Event form elements
    this.eventForm = page.locator('.event-editor-container, form[class*="event"]').first();
    this.titleInput = page.locator('input[placeholder*="Workshop"], input[placeholder*="Event Title"], input[name="Title"]').first();
    this.leadTeacherSelect = page.locator('select').first(); // Usually the first select is the teacher
    this.venueInput = page.locator('input[placeholder*="venue"], input[placeholder*="Venue"], input[name="Venue"]').first();
    
    // Rich text editor elements
    this.descriptionEditor = page.locator('.e-rte-container, .rich-text-editor').first();
    this.descriptionRichTextContent = page.locator('.e-rte-content').first();
    this.descriptionTextarea = page.locator('textarea[name="Description"], textarea[placeholder*="description"]').first();
    
    // Date inputs
    this.startDateInput = page.locator('input[type="datetime-local"]').first();
    this.endDateInput = page.locator('input[type="datetime-local"]').nth(1);
    
    // Form buttons
    this.submitButton = page.locator('button:has-text("Create Event"), button:has-text("Save"), button[type="submit"]').first();
    this.cancelButton = page.locator('button:has-text("Cancel"), a:has-text("Cancel")').first();
    
    // Validation elements
    this.validationSummary = page.locator('.validation-summary, .wcr-validation-summary');
    this.validationMessages = page.locator('.validation-message, .wcr-validation-message, .text-danger');
    this.titleValidation = this.validationMessages.filter({ hasText: /title/i });
    this.venueValidation = this.validationMessages.filter({ hasText: /venue/i });
    this.descriptionValidation = this.validationMessages.filter({ hasText: /description/i });
    
    // Event detail elements
    this.eventDetailContainer = page.locator('.event-detail, .event-details, [class*="event-detail"]');
    this.editEventButton = page.locator('button:has-text("Edit"), a:has-text("Edit")').first();
    this.deleteEventButton = page.locator('button:has-text("Delete")').first();
  }

  /**
   * Navigate to events list page
   */
  async gotoEventsList(): Promise<void> {
    await this.page.goto(`${testConfig.urls.admin}/events`);
    await BlazorHelpers.waitForBlazorReady(this.page);
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Navigate to create event page
   */
  async gotoCreateEvent(): Promise<void> {
    await this.page.goto(`${testConfig.urls.admin}/events/new-standardized`);
    await BlazorHelpers.waitForBlazorReady(this.page);
    await this.eventForm.waitFor({ state: 'visible', timeout: testConfig.timeouts.navigation });
  }

  /**
   * Navigate to public events page
   */
  async gotoPublicEvents(): Promise<void> {
    await this.page.goto(`${testConfig.urls.base}/events`);
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Get count of events in the table
   */
  async getEventCount(): Promise<number> {
    try {
      await this.eventRows.first().waitFor({ state: 'visible', timeout: 5000 });
      return await this.eventRows.count();
    } catch {
      // No events in table
      return 0;
    }
  }

  /**
   * Check if an event with given title exists in the list
   */
  async eventExists(title: string): Promise<boolean> {
    const eventRow = this.eventRows.filter({ hasText: title });
    return await eventRow.count() > 0;
  }

  /**
   * Click on an event in the list to view details
   */
  async clickEvent(title: string): Promise<void> {
    const eventRow = this.eventRows.filter({ hasText: title });
    await eventRow.first().click();
    await BlazorHelpers.waitForNavigation(this.page);
  }

  /**
   * Fill the event creation form
   */
  async fillEventForm(eventData: {
    title: string;
    teacher?: string;
    venue: string;
    description: string;
    startDate?: Date;
    endDate?: Date;
  }): Promise<void> {
    // Title
    await BlazorHelpers.fillAndWait(this.page, this.titleInput, eventData.title);
    
    // Lead Teacher - select first non-empty option if not specified
    if (eventData.teacher) {
      await this.leadTeacherSelect.selectOption({ label: eventData.teacher });
    } else {
      const options = await this.leadTeacherSelect.locator('option').all();
      if (options.length > 1) {
        const firstValue = await options[1].getAttribute('value');
        if (firstValue) {
          await this.leadTeacherSelect.selectOption(firstValue);
        }
      }
    }
    
    // Venue
    await BlazorHelpers.fillAndWait(this.page, this.venueInput, eventData.venue);
    
    // Description - handle rich text editor
    await this.fillDescription(eventData.description);
    
    // Dates
    if (eventData.startDate) {
      await this.setDateTime(this.startDateInput, eventData.startDate);
    }
    if (eventData.endDate) {
      await this.setDateTime(this.endDateInput, eventData.endDate);
    }
  }

  /**
   * Fill description in rich text editor or textarea
   */
  private async fillDescription(description: string): Promise<void> {
    // Try rich text editor first
    if (await this.descriptionRichTextContent.isVisible()) {
      await this.page.evaluate((desc) => {
        const rteContent = document.querySelector('.e-rte-content') as HTMLElement;
        if (rteContent) {
          rteContent.click();
          rteContent.innerHTML = `<p>${desc}</p>`;
          // Fire events for Blazor
          rteContent.dispatchEvent(new Event('input', { bubbles: true }));
          rteContent.dispatchEvent(new Event('change', { bubbles: true }));
        }
      }, description);
    } else if (await this.descriptionTextarea.isVisible()) {
      // Fallback to textarea
      await BlazorHelpers.fillAndWait(this.page, this.descriptionTextarea, description);
    }
  }

  /**
   * Set datetime-local input value
   */
  private async setDateTime(input: Locator, date: Date): Promise<void> {
    const dateString = date.toISOString().slice(0, 16);
    await input.click();
    await input.fill('');
    await input.type(dateString);
  }

  /**
   * Create a new event with given data
   */
  async createEvent(eventData: {
    title: string;
    teacher?: string;
    venue: string;
    description: string;
    startDate?: Date;
    endDate?: Date;
  }): Promise<void> {
    await this.fillEventForm(eventData);
    
    // Wait a bit for form to be ready
    await this.page.waitForTimeout(2000);
    
    // Submit the form
    await BlazorHelpers.clickAndWait(this.page, this.submitButton);
    
    // Wait for navigation or validation
    await this.page.waitForTimeout(3000);
  }

  /**
   * Check if form submission was successful
   */
  async isSubmissionSuccessful(): Promise<boolean> {
    // Check if we navigated away from the form
    const currentUrl = this.page.url();
    return !currentUrl.includes('/new-standardized') && !currentUrl.includes('/create');
  }

  /**
   * Get validation errors
   */
  async getValidationErrors(): Promise<string[]> {
    const messages = await this.validationMessages.allTextContents();
    return messages.filter(msg => msg.trim().length > 0);
  }

  /**
   * Check if specific field has validation error
   */
  async hasFieldValidationError(field: 'title' | 'venue' | 'description'): Promise<boolean> {
    const validationLocator = {
      title: this.titleValidation,
      venue: this.venueValidation,
      description: this.descriptionValidation
    }[field];
    
    return await validationLocator.isVisible();
  }

  /**
   * Submit empty form to trigger validation
   */
  async submitEmptyForm(): Promise<void> {
    await BlazorHelpers.clickAndWait(this.page, this.submitButton);
    await BlazorHelpers.waitForValidation(this.page);
  }

  /**
   * Generate unique event title
   */
  generateEventTitle(prefix: string = 'Test Event'): string {
    return `${prefix} ${Date.now()}`;
  }

  /**
   * Get event details from detail page
   */
  async getEventDetails(): Promise<{
    title?: string;
    venue?: string;
    teacher?: string;
    description?: string;
  }> {
    await this.eventDetailContainer.waitFor({ state: 'visible' });
    
    const details: any = {};
    
    // Extract details from the page - adjust selectors based on actual structure
    const titleElement = this.page.locator('h1, h2').first();
    if (await titleElement.isVisible()) {
      details.title = await titleElement.textContent();
    }
    
    // Look for venue, teacher, etc. in common patterns
    const detailElements = await this.page.locator('dt, .detail-label').allTextContents();
    const detailValues = await this.page.locator('dd, .detail-value').allTextContents();
    
    detailElements.forEach((label, index) => {
      const value = detailValues[index];
      if (label.toLowerCase().includes('venue')) {
        details.venue = value;
      } else if (label.toLowerCase().includes('teacher')) {
        details.teacher = value;
      }
    });
    
    return details;
  }

  /**
   * Delete current event (from detail page)
   */
  async deleteEvent(): Promise<void> {
    await this.deleteEventButton.click();
    
    // Handle confirmation dialog
    this.page.on('dialog', dialog => dialog.accept());
    
    // Wait for navigation back to list
    await BlazorHelpers.waitForNavigation(this.page, /\/events$/);
  }

  /**
   * Click edit button on event detail page
   */
  async clickEditEvent(): Promise<void> {
    await BlazorHelpers.clickAndWait(this.page, this.editEventButton);
    await this.eventForm.waitFor({ state: 'visible' });
  }

  /**
   * Take a screenshot for debugging
   */
  async screenshot(name: string): Promise<void> {
    await this.page.screenshot({ 
      path: `test-results/screenshots/event-${name}.png`,
      fullPage: true 
    });
  }
}