import { test, expect } from '@playwright/test';
import { EventPage } from '../pages/event.page';
import { LoginPage } from '../pages/login.page';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Event Creation Tests
 * Converted from Puppeteer test: test-event-creation-success.js
 * 
 * Tests event creation functionality including:
 * - Form loading and validation
 * - Successful event creation
 * - Field validation
 * - Rich text editor handling
 */

test.describe('Event Creation', () => {
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

  test('should load event creation form with all required fields', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-form-load' });

    // Navigate to create event page
    await eventPage.gotoCreateEvent();
    
    // Verify form elements are visible
    const elements = [
      { locator: eventPage.titleInput, description: 'Title input' },
      { locator: eventPage.leadTeacherSelect, description: 'Lead teacher select' },
      { locator: eventPage.venueInput, description: 'Venue input' },
      { locator: eventPage.descriptionEditor, description: 'Description editor' },
      { locator: eventPage.startDateInput, description: 'Start date input' },
      { locator: eventPage.endDateInput, description: 'End date input' },
      { locator: eventPage.submitButton, description: 'Submit button' }
    ];

    for (const element of elements) {
      await expect(element.locator).toBeVisible({
        timeout: testConfig.timeouts.assertion
      });
      console.log(`✅ Found: ${element.description}`);
    }

    // Verify form is editable
    await expect(eventPage.titleInput).toBeEditable();
    await expect(eventPage.venueInput).toBeEditable();
    await expect(eventPage.submitButton).toBeEnabled();
  });

  test('should successfully create a new event', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-creation-success' });

    // Get initial event count
    await eventPage.gotoEventsList();
    const initialCount = await eventPage.getEventCount();
    console.log(`Initial events: ${initialCount}`);

    // Navigate to create event page
    await eventPage.gotoCreateEvent();

    // Prepare event data
    const eventTitle = eventPage.generateEventTitle('Test Event Success');
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(19, 0, 0, 0);
    
    const endTime = new Date(tomorrow);
    endTime.setHours(21, 0, 0, 0);

    const eventData = {
      title: eventTitle,
      venue: 'Test Venue Location',
      description: 'Test event description with all required information.',
      startDate: tomorrow,
      endDate: endTime
    };

    // Fill and submit the form
    await eventPage.createEvent(eventData);

    // Verify submission was successful
    const isSuccessful = await eventPage.isSubmissionSuccessful();
    expect(isSuccessful).toBeTruthy();
    console.log('✅ Navigated away from form!');

    // Verify event was created
    await eventPage.gotoEventsList();
    await page.waitForTimeout(2000); // Wait for list to update
    
    const finalCount = await eventPage.getEventCount();
    console.log(`Final events: ${finalCount}`);
    console.log(`Events created: ${finalCount - initialCount}`);
    
    expect(finalCount).toBeGreaterThan(initialCount);
    
    // Check if our event appears in the list
    const eventFound = await eventPage.eventExists(eventTitle);
    expect(eventFound).toBeTruthy();
    console.log('✅ New event found in list!');
  });

  test('should show validation errors for empty form submission', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-form-validation' });

    await eventPage.gotoCreateEvent();
    
    // Submit empty form
    await eventPage.submitEmptyForm();
    
    // Check for validation messages
    const validationErrors = await eventPage.getValidationErrors();
    console.log(`Found ${validationErrors.length} validation messages`);
    expect(validationErrors.length).toBeGreaterThan(0);
    
    // Verify we're still on the form page
    const currentUrl = page.url();
    expect(currentUrl).toContain('/new-standardized');
  });

  test('should validate required fields individually', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-field-validation' });

    await eventPage.gotoCreateEvent();
    
    // Fill partial form (missing title)
    await eventPage.fillEventForm({
      title: '',
      venue: 'Test Venue',
      description: 'Test description'
    });
    
    await eventPage.submitEmptyForm();
    
    // Check title validation
    const hasTitleError = await eventPage.hasFieldValidationError('title');
    expect(hasTitleError).toBeTruthy();
    console.log('✅ Title validation shown');
    
    // Fill title but clear venue
    await BlazorHelpers.fillAndWait(page, eventPage.titleInput, 'Test Event');
    await eventPage.venueInput.clear();
    
    await eventPage.submitEmptyForm();
    
    // Check venue validation
    const hasVenueError = await eventPage.hasFieldValidationError('venue');
    expect(hasVenueError).toBeTruthy();
    console.log('✅ Venue validation shown');
  });

  test('should handle rich text editor for description', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-rich-text-editor' });

    await eventPage.gotoCreateEvent();
    
    const testDescription = 'This is a **bold** test description with *italic* text.';
    
    // Fill description using rich text editor
    await eventPage.fillEventForm({
      title: 'Rich Text Test Event',
      venue: 'Test Venue',
      description: testDescription
    });
    
    // Verify the description was set
    const descriptionContent = await page.evaluate(() => {
      const rteContent = document.querySelector('.e-rte-content');
      return rteContent ? rteContent.innerHTML : '';
    });
    
    expect(descriptionContent).toContain(testDescription);
    console.log('✅ Rich text editor content set successfully');
  });

  test('should persist form data during validation', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-form-persistence' });

    await eventPage.gotoCreateEvent();
    
    const testTitle = 'Persistent Event Title';
    const testVenue = 'Persistent Venue';
    
    // Fill form partially
    await BlazorHelpers.fillAndWait(page, eventPage.titleInput, testTitle);
    await BlazorHelpers.fillAndWait(page, eventPage.venueInput, testVenue);
    
    // Submit to trigger validation (missing description)
    await eventPage.submitEmptyForm();
    
    // Verify values persisted
    await expect(eventPage.titleInput).toHaveValue(testTitle);
    await expect(eventPage.venueInput).toHaveValue(testVenue);
    console.log('✅ Form values persisted after validation');
  });

  test('should create event with full details including dates', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-creation-full-details' });

    await eventPage.gotoCreateEvent();
    
    const eventTitle = eventPage.generateEventTitle('Full Detail Event');
    const nextWeek = new Date();
    nextWeek.setDate(nextWeek.getDate() + 7);
    nextWeek.setHours(18, 30, 0, 0);
    
    const endTime = new Date(nextWeek);
    endTime.setHours(20, 30, 0, 0);

    await eventPage.createEvent({
      title: eventTitle,
      venue: 'Community Center Room A',
      description: 'A comprehensive workshop covering advanced rope techniques. This event includes hands-on practice and Q&A session.',
      startDate: nextWeek,
      endDate: endTime
    });

    // Verify creation and navigate to event
    expect(await eventPage.isSubmissionSuccessful()).toBeTruthy();
    
    await eventPage.gotoEventsList();
    await eventPage.clickEvent(eventTitle);
    
    // Verify event details
    const details = await eventPage.getEventDetails();
    expect(details.title).toContain(eventTitle);
    expect(details.venue).toContain('Community Center Room A');
  });
});

test.describe('Event Creation - Visual Regression', () => {
  test('should match visual snapshot of create event form', async ({ page }) => {
    const loginPage = new LoginPage(page);
    const eventPage = new EventPage(page);
    
    // Login and navigate to form
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await eventPage.gotoCreateEvent();
    
    // Wait for form to be fully rendered
    await BlazorHelpers.waitForBlazorReady(page);
    
    // Take screenshot for visual comparison
    await expect(page).toHaveScreenshot('event-create-form.png', {
      fullPage: true,
      animations: 'disabled'
    });
  });
});

test.describe('Event Creation - Error Handling', () => {
  test('should handle network errors gracefully', async ({ page }) => {
    const loginPage = new LoginPage(page);
    const eventPage = new EventPage(page);
    
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await eventPage.gotoCreateEvent();
    
    // Simulate network failure
    await page.route('**/api/events', route => route.abort());
    
    // Try to create event
    await eventPage.fillEventForm({
      title: 'Network Error Test',
      venue: 'Test Venue',
      description: 'Testing network error handling'
    });
    
    await eventPage.submitButton.click();
    
    // Should show error message or stay on form
    await page.waitForTimeout(2000);
    const currentUrl = page.url();
    expect(currentUrl).toContain('/new-standardized');
  });
});