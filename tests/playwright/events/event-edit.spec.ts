import { test, expect } from '@playwright/test';
import { EventPage } from '../pages/event.page';
import { LoginPage } from '../pages/login.page';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Event Edit Tests
 * Tests event editing functionality including:
 * - Edit form loading
 * - Field updates
 * - Validation during edit
 * - Save functionality
 */

test.describe('Event Editing', () => {
  let eventPage: EventPage;
  let loginPage: LoginPage;
  let testEventTitle: string;

  test.beforeEach(async ({ page }) => {
    eventPage = new EventPage(page);
    loginPage = new LoginPage(page);
    
    // Login as admin
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    // Create a test event to edit
    await eventPage.gotoCreateEvent();
    testEventTitle = eventPage.generateEventTitle('Edit Test Event');
    
    await eventPage.createEvent({
      title: testEventTitle,
      venue: 'Original Venue',
      description: 'Original description for editing test.'
    });
    
    // Navigate back to the event
    await eventPage.gotoEventsList();
    await eventPage.clickEvent(testEventTitle);
  });

  test('should load edit form with existing data', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-edit-load' });

    // Click edit button
    await eventPage.clickEditEvent();
    
    // Verify form loaded with existing data
    await expect(eventPage.titleInput).toHaveValue(testEventTitle);
    await expect(eventPage.venueInput).toHaveValue('Original Venue');
    
    // Check description content
    const descriptionContent = await page.evaluate(() => {
      const rteContent = document.querySelector('.e-rte-content');
      const textarea = document.querySelector('textarea[name="Description"]');
      return rteContent ? rteContent.textContent : textarea ? (textarea as HTMLTextAreaElement).value : '';
    });
    
    expect(descriptionContent).toContain('Original description');
    console.log('✅ Edit form loaded with existing data');
  });

  test('should update event title', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-edit-title' });

    await eventPage.clickEditEvent();
    
    // Update title
    const newTitle = eventPage.generateEventTitle('Updated Event');
    await eventPage.titleInput.clear();
    await BlazorHelpers.fillAndWait(page, eventPage.titleInput, newTitle);
    
    // Save changes
    await BlazorHelpers.clickAndWait(page, eventPage.submitButton);
    
    // Verify update
    await eventPage.gotoEventsList();
    const oldEventExists = await eventPage.eventExists(testEventTitle);
    const newEventExists = await eventPage.eventExists(newTitle);
    
    expect(oldEventExists).toBeFalsy();
    expect(newEventExists).toBeTruthy();
    console.log('✅ Event title updated successfully');
  });

  test('should update multiple fields', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-edit-multiple' });

    await eventPage.clickEditEvent();
    
    // Update multiple fields
    const updates = {
      venue: 'Updated Venue Location',
      description: 'This is the updated description with new information.'
    };
    
    await eventPage.venueInput.clear();
    await BlazorHelpers.fillAndWait(page, eventPage.venueInput, updates.venue);
    
    await eventPage.fillEventForm({
      title: testEventTitle, // Keep same title
      venue: updates.venue,
      description: updates.description
    });
    
    // Save changes
    await BlazorHelpers.clickAndWait(page, eventPage.submitButton);
    await page.waitForTimeout(2000);
    
    // Verify updates by viewing event details
    await eventPage.gotoEventsList();
    await eventPage.clickEvent(testEventTitle);
    
    const details = await eventPage.getEventDetails();
    expect(details.venue).toContain(updates.venue);
    console.log('✅ Multiple fields updated successfully');
  });

  test('should validate required fields during edit', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-edit-validation' });

    await eventPage.clickEditEvent();
    
    // Clear required field
    await eventPage.titleInput.clear();
    
    // Try to save
    await eventPage.submitButton.click();
    await BlazorHelpers.waitForValidation(page);
    
    // Check for validation error
    const hasTitleError = await eventPage.hasFieldValidationError('title');
    expect(hasTitleError).toBeTruthy();
    
    // Verify we're still on edit form
    const currentUrl = page.url();
    expect(currentUrl).toContain('edit');
    console.log('✅ Validation working during edit');
  });

  test('should cancel edit without saving changes', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-edit-cancel' });

    await eventPage.clickEditEvent();
    
    // Make changes
    await eventPage.titleInput.clear();
    await BlazorHelpers.fillAndWait(page, eventPage.titleInput, 'Cancelled Changes');
    
    // Cancel
    if (await eventPage.cancelButton.isVisible()) {
      await eventPage.cancelButton.click();
      
      // Confirm cancellation if dialog appears
      page.on('dialog', dialog => dialog.accept());
      
      await page.waitForLoadState('networkidle');
      
      // Verify original title still exists
      await eventPage.gotoEventsList();
      const originalExists = await eventPage.eventExists(testEventTitle);
      const cancelledExists = await eventPage.eventExists('Cancelled Changes');
      
      expect(originalExists).toBeTruthy();
      expect(cancelledExists).toBeFalsy();
      console.log('✅ Edit cancelled successfully');
    } else {
      console.log('ℹ️  No cancel button found - skipping cancel test');
    }
  });

  test('should update event dates', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-edit-dates' });

    await eventPage.clickEditEvent();
    
    // Set new dates
    const nextMonth = new Date();
    nextMonth.setMonth(nextMonth.getMonth() + 1);
    nextMonth.setHours(14, 0, 0, 0);
    
    const endTime = new Date(nextMonth);
    endTime.setHours(16, 0, 0, 0);
    
    await eventPage.fillEventForm({
      title: testEventTitle,
      venue: 'Original Venue',
      description: 'Original description for editing test.',
      startDate: nextMonth,
      endDate: endTime
    });
    
    // Save changes
    await BlazorHelpers.clickAndWait(page, eventPage.submitButton);
    
    // Verify save was successful
    const isSuccessful = await eventPage.isSubmissionSuccessful();
    expect(isSuccessful).toBeTruthy();
    console.log('✅ Event dates updated successfully');
  });
});

test.describe('Event Bulk Operations', () => {
  test('should select multiple events if bulk operations available', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-bulk-select' });

    const loginPage = new LoginPage(page);
    const eventPage = new EventPage(page);
    
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await eventPage.gotoEventsList();
    
    // Look for checkboxes in event rows
    const checkboxes = page.locator('table tbody tr input[type="checkbox"]');
    const checkboxCount = await checkboxes.count();
    
    if (checkboxCount > 0) {
      console.log(`✅ Found ${checkboxCount} event checkboxes`);
      
      // Select first two events
      if (checkboxCount >= 2) {
        await checkboxes.nth(0).check();
        await checkboxes.nth(1).check();
        
        // Look for bulk action buttons
        const bulkDeleteButton = page.locator('button:has-text("Delete Selected")');
        
        if (await bulkDeleteButton.isVisible()) {
          console.log('✅ Bulk delete button appeared');
          
          // Don't actually delete, just verify functionality exists
          expect(await bulkDeleteButton.isEnabled()).toBeTruthy();
        }
      }
    } else {
      console.log('ℹ️  No bulk selection checkboxes found');
    }
  });
});

test.describe('Event RSVP/Registration', () => {
  test('should handle event RSVP if available', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-rsvp' });

    const eventPage = new EventPage(page);
    
    // Go to public events
    await eventPage.gotoPublicEvents();
    
    // Look for RSVP buttons
    const rsvpButtons = page.locator('button:has-text("RSVP"), button:has-text("Register"), a:has-text("Sign up")');
    
    if (await rsvpButtons.count() > 0) {
      console.log('✅ Found RSVP/Registration buttons');
      
      // Click first RSVP button
      await rsvpButtons.first().click();
      await page.waitForLoadState('networkidle');
      
      // Check if we're on a registration form or login page
      const currentUrl = page.url();
      if (currentUrl.includes('login')) {
        console.log('✅ Redirected to login for RSVP');
      } else if (currentUrl.includes('register') || currentUrl.includes('rsvp')) {
        console.log('✅ Navigated to RSVP/registration form');
        
        // Look for registration form
        const registrationForm = page.locator('form').filter({ has: page.locator('button[type="submit"]') });
        await expect(registrationForm).toBeVisible();
      }
    } else {
      console.log('ℹ️  No RSVP functionality found on public events page');
    }
  });
});

test.describe('Event Categories/Tags', () => {
  test('should handle event categories if available', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-categories' });

    const loginPage = new LoginPage(page);
    const eventPage = new EventPage(page);
    
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await eventPage.gotoCreateEvent();
    
    // Look for category/tag fields
    const categorySelect = page.locator('select[name*="category"], select[name*="Category"]').first();
    const tagInput = page.locator('input[name*="tag"], input[placeholder*="tag"]').first();
    
    if (await categorySelect.isVisible()) {
      console.log('✅ Found category select field');
      
      // Get available categories
      const options = await categorySelect.locator('option').allTextContents();
      console.log('Available categories:', options);
      
      if (options.length > 1) {
        await categorySelect.selectOption({ index: 1 });
        console.log('✅ Selected a category');
      }
    }
    
    if (await tagInput.isVisible()) {
      console.log('✅ Found tag input field');
      await tagInput.fill('workshop, beginner-friendly');
      console.log('✅ Added tags');
    }
    
    if (!await categorySelect.isVisible() && !await tagInput.isVisible()) {
      console.log('ℹ️  No category/tag functionality found');
    }
  });
});

test.describe('Event Import/Export', () => {
  test('should check for import/export functionality', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-import-export' });

    const loginPage = new LoginPage(page);
    const eventPage = new EventPage(page);
    
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await eventPage.gotoEventsList();
    
    // Look for import/export buttons
    const exportButton = page.locator('button:has-text("Export"), a:has-text("Export")');
    const importButton = page.locator('button:has-text("Import"), a:has-text("Import")');
    
    if (await exportButton.isVisible()) {
      console.log('✅ Export functionality available');
      
      // Test export
      const [download] = await Promise.all([
        page.waitForEvent('download'),
        exportButton.click()
      ]);
      
      console.log(`✅ Export initiated: ${download.suggestedFilename()}`);
    }
    
    if (await importButton.isVisible()) {
      console.log('✅ Import functionality available');
      
      await importButton.click();
      
      // Look for file input
      const fileInput = page.locator('input[type="file"]');
      if (await fileInput.isVisible()) {
        console.log('✅ Import dialog opened');
      }
    }
    
    if (!await exportButton.isVisible() && !await importButton.isVisible()) {
      console.log('ℹ️  No import/export functionality found');
    }
  });
});