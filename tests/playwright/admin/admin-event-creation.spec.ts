import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { AdminEventsPage } from '../pages/admin-events.page';
import { testConfig, generateTestData } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Admin Event Creation Tests
 * Converted from Puppeteer test: test-admin-event-creation.js
 * 
 * Tests admin event creation functionality:
 * - Creating events through UI
 * - Form validation
 * - Event visibility after creation
 * - Different event types and settings
 */

test.describe('Admin Event Creation', () => {
  let loginPage: LoginPage;
  let adminEvents: AdminEventsPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    adminEvents = new AdminEventsPage(page);
    
    // Login as admin before each test
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
  });

  test('should create a new event through admin UI', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-create-event-ui' });

    const testData = generateTestData();
    const eventData = {
      title: `${testData.eventName}`,
      description: 'Test event created via Playwright admin UI',
      startDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      endDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000 + 2 * 60 * 60 * 1000).toISOString().split('T')[0],
      location: 'Test Location',
      maxAttendees: 20,
      ticketPrice: 0,
      eventType: 'Social',
      status: 'Published',
      showInPublicList: true
    };

    // Navigate to admin events page
    console.log('1️⃣ Navigating to admin events page...');
    await adminEvents.goto();
    await adminEvents.screenshot('1-events-page');
    
    // Check if we're on the admin page
    const adminUrl = page.url();
    expect(adminUrl).toContain('admin/events');
    console.log('   ✅ On admin events page');
    
    // Look for create event functionality
    console.log('2️⃣ Looking for event creation UI...');
    
    const canCreate = await adminEvents.canCreateEvents();
    expect(canCreate).toBeTruthy();
    console.log('   ✅ Create event button found');
    
    // Open create event form
    console.log('3️⃣ Opening create event form...');
    await adminEvents.openCreateEventModal();
    await adminEvents.screenshot('2-create-form');
    
    // Check form fields
    console.log('4️⃣ Checking form fields...');
    await expect(adminEvents.titleInput).toBeVisible();
    await expect(adminEvents.descriptionInput).toBeVisible();
    await expect(adminEvents.startDateInput).toBeVisible();
    await expect(adminEvents.endDateInput).toBeVisible();
    await expect(adminEvents.locationInput).toBeVisible();
    console.log('   ✅ All required form fields present');
    
    // Fill and submit event form
    console.log('5️⃣ Creating event...');
    await adminEvents.createEvent(eventData);
    
    console.log(`   Event created: "${eventData.title}"`);
    
    // Verify event appears in the list
    console.log('6️⃣ Verifying event in list...');
    await adminEvents.searchEvents(eventData.title);
    
    const eventExists = await adminEvents.eventExists(eventData.title);
    expect(eventExists).toBeTruthy();
    console.log('   ✅ Event found in events list');
    
    // Take final screenshot
    await adminEvents.screenshot('3-event-created');
  });

  test('should validate required fields when creating event', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-event-validation' });

    // Navigate to admin events
    await adminEvents.goto();
    
    // Open create event form
    await adminEvents.openCreateEventModal();
    
    // Try to submit empty form
    console.log('🔍 Testing form validation...');
    await BlazorHelpers.clickAndWait(page, adminEvents.saveEventButton);
    
    // Check for validation messages
    await BlazorHelpers.waitForValidation(page);
    
    const validationMessages = await page.locator('.wcr-validation-message, .validation-message').allTextContents();
    console.log(`   Found ${validationMessages.length} validation messages`);
    expect(validationMessages.length).toBeGreaterThan(0);
    
    // Verify form wasn't submitted (still on create form)
    await expect(adminEvents.titleInput).toBeVisible();
    console.log('   ✅ Form validation working correctly');
  });

  test('should create different types of events', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-event-types' });

    const eventTypes = ['Social', 'Workshop', 'Performance', 'Vetting'];
    
    for (const eventType of eventTypes) {
      const testData = generateTestData();
      const eventData = {
        title: `${eventType} Event - ${testData.uniqueId}`,
        description: `Test ${eventType} event`,
        startDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
        endDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000 + 2 * 60 * 60 * 1000).toISOString().split('T')[0],
        location: 'Test Venue',
        maxAttendees: 30,
        ticketPrice: eventType === 'Workshop' ? 25 : 0,
        eventType: eventType,
        status: 'Published',
        showInPublicList: eventType !== 'Vetting'
      };
      
      console.log(`\n📅 Creating ${eventType} event...`);
      
      // Navigate to events page
      await adminEvents.goto();
      
      // Create event
      await adminEvents.createEvent(eventData);
      
      // Verify event created
      await adminEvents.searchEvents(eventData.title);
      const exists = await adminEvents.eventExists(eventData.title);
      expect(exists).toBeTruthy();
      
      console.log(`   ✅ ${eventType} event created successfully`);
    }
  });

  test('should create draft event and publish later', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-draft-event' });

    const testData = generateTestData();
    const eventData = {
      title: `Draft Event - ${testData.uniqueId}`,
      description: 'Test draft event',
      startDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      endDate: new Date(Date.now() + 14 * 24 * 60 * 60 * 1000 + 2 * 60 * 60 * 1000).toISOString().split('T')[0],
      location: 'TBD',
      maxAttendees: 50,
      ticketPrice: 0,
      eventType: 'Social',
      status: 'Draft',
      showInPublicList: false
    };
    
    // Navigate to admin events
    await adminEvents.goto();
    
    // Create draft event
    console.log('📝 Creating draft event...');
    await adminEvents.createEvent(eventData);
    
    // Filter by draft status
    await adminEvents.filterByStatus('Draft');
    
    // Verify draft event exists
    const draftExists = await adminEvents.eventExists(eventData.title);
    expect(draftExists).toBeTruthy();
    console.log('   ✅ Draft event created');
    
    // Edit event to publish
    console.log('📢 Publishing draft event...');
    await adminEvents.editEvent(eventData.title);
    
    // Change status to published
    await adminEvents.eventStatusSelect.selectOption('Published');
    await adminEvents.showInPublicListCheckbox.check();
    await BlazorHelpers.clickAndWait(page, adminEvents.saveEventButton);
    
    // Filter by published status
    await adminEvents.filterByStatus('Published');
    
    // Verify event is now published
    const publishedExists = await adminEvents.eventExists(eventData.title);
    expect(publishedExists).toBeTruthy();
    console.log('   ✅ Event successfully published');
  });

  test('should handle event with all optional fields', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-event-all-fields' });

    const testData = generateTestData();
    const eventData = {
      title: `Complete Event - ${testData.uniqueId}`,
      description: `This is a comprehensive test event with all fields filled.
      
      It includes:
      - Multiple paragraphs
      - Rich text formatting
      - Special characters & symbols
      - Numbers: 123, 456.78
      
      This tests the full capability of the event creation form.`,
      startDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000 + 4 * 60 * 60 * 1000).toISOString().split('T')[0],
      location: '123 Test Street, Test City, TC 12345',
      maxAttendees: 100,
      ticketPrice: 49.99,
      eventType: 'Workshop',
      status: 'Published',
      showInPublicList: true
    };
    
    // Navigate to admin events
    await adminEvents.goto();
    
    // Create event with all fields
    console.log('🎯 Creating event with all fields...');
    await adminEvents.createEvent(eventData);
    
    // Search for the event
    await adminEvents.searchEvents(eventData.title);
    
    // View event details
    await adminEvents.viewEventDetails(eventData.title);
    
    // Verify we're on the event details page
    await expect(page.locator('h1, .event-title').filter({ hasText: eventData.title })).toBeVisible();
    console.log('   ✅ Event created with all fields successfully');
    
    // Take screenshot of the detailed event
    await page.screenshot({ 
      path: 'test-results/screenshots/admin-complete-event.png',
      fullPage: true 
    });
  });

  test('should display correct event information in list', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-event-list-display' });

    const testData = generateTestData();
    const eventData = {
      title: `Display Test - ${testData.uniqueId}`,
      description: 'Event for testing list display',
      startDate: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      endDate: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000 + 2 * 60 * 60 * 1000).toISOString().split('T')[0],
      location: 'Display Test Venue',
      maxAttendees: 25,
      ticketPrice: 15,
      eventType: 'Performance',
      status: 'Published'
    };
    
    // Create event
    await adminEvents.goto();
    await adminEvents.createEvent(eventData);
    
    // Get events from table
    console.log('📊 Checking event display in table...');
    const events = await adminEvents.getEvents();
    
    const createdEvent = events.find(e => e.title.includes(eventData.title));
    expect(createdEvent).toBeDefined();
    
    if (createdEvent) {
      console.log('   Event in table:', createdEvent);
      expect(createdEvent.location).toContain(eventData.location);
      expect(createdEvent.status).toContain(eventData.status);
    }
  });
});

test.describe('Admin Event Creation - Error Handling', () => {
  test('should handle duplicate event titles gracefully', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-duplicate-event' });

    const loginPage = new LoginPage(page);
    const adminEvents = new AdminEventsPage(page);
    
    // Login
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    const duplicateTitle = `Duplicate Event - ${Date.now()}`;
    const eventData = {
      title: duplicateTitle,
      description: 'First event',
      startDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      endDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000 + 2 * 60 * 60 * 1000).toISOString().split('T')[0],
      location: 'Test Location',
      eventType: 'Social'
    };
    
    // Create first event
    await adminEvents.goto();
    await adminEvents.createEvent(eventData);
    
    // Try to create duplicate
    console.log('⚠️ Attempting to create duplicate event...');
    await adminEvents.openCreateEventModal();
    
    // Fill same data
    await BlazorHelpers.fillAndWait(page, adminEvents.titleInput, eventData.title);
    await BlazorHelpers.fillAndWait(page, adminEvents.descriptionInput, 'Duplicate attempt');
    await BlazorHelpers.fillAndWait(page, adminEvents.startDateInput, eventData.startDate);
    await BlazorHelpers.fillAndWait(page, adminEvents.endDateInput, eventData.endDate);
    await BlazorHelpers.fillAndWait(page, adminEvents.locationInput, eventData.location);
    
    // Try to save
    await BlazorHelpers.clickAndWait(page, adminEvents.saveEventButton);
    
    // Should show error or validation message
    const errorMessages = await page.locator('.alert-danger, .error-message, .validation-message').allTextContents();
    console.log('   Error messages:', errorMessages);
    
    // Form should still be visible (not submitted)
    const formStillVisible = await adminEvents.titleInput.isVisible();
    expect(formStillVisible).toBeTruthy();
    console.log('   ✅ Duplicate event creation properly handled');
  });
});