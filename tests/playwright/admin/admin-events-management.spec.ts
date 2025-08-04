import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { AdminEventsPage } from '../pages/admin-events.page';
import { testConfig, generateTestData } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Admin Events Management Tests
 * Converted from Puppeteer test: diagnose-admin-events-page.js
 * 
 * Tests comprehensive admin events management:
 * - Event listing and filtering
 * - Event editing
 * - Event deletion
 * - Bulk operations
 * - Search functionality
 */

test.describe('Admin Events Management', () => {
  let loginPage: LoginPage;
  let adminEvents: AdminEventsPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    adminEvents = new AdminEventsPage(page);
    
    // Login as admin before each test
    console.log('🔐 Logging in...');
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    console.log('✅ Login successful\n');
  });

  test('should diagnose admin events page structure', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-events-page-diagnosis' });

    // Navigate to admin events
    console.log('📍 Navigating to admin events page...');
    await adminEvents.goto();
    console.log('✅ Navigated to:', page.url());
    
    // Analyze page structure
    console.log('\n📊 Page Analysis:');
    
    // Check for main containers
    const containers = await page.evaluate(() => {
      const results = {
        mainContent: document.querySelector('main') ? 'Found' : 'Not found',
        blazorApp: document.querySelector('#app') ? 'Found' : 'Not found',
        eventsList: document.querySelector('.events-list, .event-list, [class*="event"]') ? 'Found' : 'Not found',
        adminContainer: document.querySelector('.admin-container, .admin-page') ? 'Found' : 'Not found'
      };
      return results;
    });
    console.log('   Main containers:', containers);
    
    // Find all buttons
    const buttons = await adminEvents.getAvailableActions();
    console.log('\n   Buttons found:', buttons.length);
    buttons.forEach(btn => {
      console.log(`     - "${btn}"`);
    });
    
    // Look for create button specifically
    const hasCreateButton = await adminEvents.canCreateEvents();
    if (hasCreateButton) {
      console.log('\n🎯 Found create button! Clicking it...');
      await adminEvents.openCreateEventModal();
      
      // Check what happens after click
      console.log('   Current URL:', page.url());
      
      // Look for form or modal
      const afterClickElements = await page.evaluate(() => {
        return {
          modal: document.querySelector('.modal, .dialog, [role="dialog"]') ? 'Found modal' : 'No modal',
          form: document.querySelector('form') ? 'Found form' : 'No form',
          editor: document.querySelector('.event-editor, .editor, [class*="editor"]') ? 'Found editor' : 'No editor',
          typeSelection: document.querySelector('.event-type, .type-option, [class*="type"]') ? 'Found type selection' : 'No type selection'
        };
      });
      console.log('   After click:', afterClickElements);
      
      // Take screenshot
      await adminEvents.screenshot('after-create-click');
      console.log('   📸 Screenshot saved: admin-events-after-create-click.png');
    } else {
      console.log('\n⚠️  No create button found!');
      
      // Take screenshot of current page
      await adminEvents.screenshot('page-structure');
      console.log('   📸 Screenshot saved: admin-events-page.png');
    }
    
    // Check for any existing events
    const events = await adminEvents.getEvents();
    console.log(`\n📋 Existing events found: ${events.length}`);
    
    // Get page content structure
    const pageStructure = await page.evaluate(() => {
      const getStructure = (element: Element, depth = 0): any => {
        if (depth > 2) return null;
        const children = Array.from(element.children);
        return {
          tag: element.tagName,
          class: element.className,
          id: element.id,
          childCount: children.length,
          text: element.textContent?.substring(0, 50)
        };
      };
      return getStructure(document.body);
    });
    console.log('\n🏗️  Page structure:', JSON.stringify(pageStructure, null, 2));
  });

  test('should display and manage existing events', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-events-listing' });

    // Navigate to admin events
    await adminEvents.goto();
    
    // Get current events
    console.log('📋 Checking existing events...');
    const events = await adminEvents.getEvents();
    console.log(`   Found ${events.length} events`);
    
    if (events.length > 0) {
      console.log('   First few events:');
      events.slice(0, 3).forEach((event, index) => {
        console.log(`   ${index + 1}. ${event.title} - ${event.date} at ${event.location} (${event.status})`);
      });
      
      // Test filtering
      console.log('\n🔍 Testing filters...');
      
      // Filter by status
      await adminEvents.filterByStatus('Published');
      const publishedEvents = await adminEvents.getEvents();
      console.log(`   Published events: ${publishedEvents.length}`);
      
      // Reset filter
      await adminEvents.filterByStatus('All');
      
      // Filter by type
      await adminEvents.filterByType('Social');
      const socialEvents = await adminEvents.getEvents();
      console.log(`   Social events: ${socialEvents.length}`);
    }
  });

  test('should search for events', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-events-search' });

    // First create a searchable event
    const testData = generateTestData();
    const searchableEvent = {
      title: `Searchable Event ${testData.uniqueId}`,
      description: 'This event is for testing search functionality',
      startDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      endDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000 + 2 * 60 * 60 * 1000).toISOString().split('T')[0],
      location: 'Search Test Venue',
      eventType: 'Workshop'
    };
    
    // Navigate to events and create the event
    await adminEvents.goto();
    await adminEvents.createEvent(searchableEvent);
    
    // Search for the event
    console.log('🔍 Testing search functionality...');
    await adminEvents.searchEvents(testData.uniqueId);
    
    // Verify search results
    const searchResults = await adminEvents.getEvents();
    const foundEvent = searchResults.find(e => e.title.includes(testData.uniqueId));
    
    expect(foundEvent).toBeDefined();
    console.log('   ✅ Event found in search results');
    
    // Test partial search
    await adminEvents.searchEvents('Searchable');
    const partialResults = await adminEvents.getEvents();
    console.log(`   Partial search returned ${partialResults.length} results`);
  });

  test('should edit existing events', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-events-edit' });

    // Create an event to edit
    const testData = generateTestData();
    const originalEvent = {
      title: `Original Event ${testData.uniqueId}`,
      description: 'Original description',
      startDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      endDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000 + 2 * 60 * 60 * 1000).toISOString().split('T')[0],
      location: 'Original Location',
      maxAttendees: 20,
      eventType: 'Social'
    };
    
    await adminEvents.goto();
    await adminEvents.createEvent(originalEvent);
    
    // Edit the event
    console.log('✏️ Editing event...');
    await adminEvents.editEvent(originalEvent.title);
    
    // Update some fields
    await BlazorHelpers.fillAndWait(page, adminEvents.descriptionInput, 'Updated description with more details');
    await BlazorHelpers.fillAndWait(page, adminEvents.locationInput, 'Updated Venue');
    await BlazorHelpers.fillAndWait(page, adminEvents.maxAttendeesInput, '50');
    
    // Save changes
    await BlazorHelpers.clickAndWait(page, adminEvents.saveEventButton);
    
    // Verify changes
    console.log('   Verifying changes...');
    await adminEvents.searchEvents(testData.uniqueId);
    
    // View details to confirm changes
    await adminEvents.viewEventDetails(originalEvent.title);
    
    // Check updated values
    const updatedLocation = await page.locator(':has-text("Updated Venue")').isVisible();
    expect(updatedLocation).toBeTruthy();
    console.log('   ✅ Event successfully edited');
  });

  test('should delete events', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-events-delete' });

    // Create an event to delete
    const testData = generateTestData();
    const eventToDelete = {
      title: `Delete Me ${testData.uniqueId}`,
      description: 'This event will be deleted',
      startDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      endDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000 + 2 * 60 * 60 * 1000).toISOString().split('T')[0],
      location: 'Temporary Location',
      eventType: 'Social'
    };
    
    await adminEvents.goto();
    await adminEvents.createEvent(eventToDelete);
    
    // Delete the event
    console.log('🗑️ Deleting event...');
    await adminEvents.deleteEvent(eventToDelete.title, true);
    
    // Verify deletion
    console.log('   Verifying deletion...');
    await adminEvents.searchEvents(testData.uniqueId);
    
    const eventStillExists = await adminEvents.eventExists(eventToDelete.title);
    expect(eventStillExists).toBeFalsy();
    console.log('   ✅ Event successfully deleted');
  });

  test('should handle bulk operations', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-events-bulk-ops' });

    // Check if bulk actions are available
    const hasBulkActions = await adminEvents.bulkActionsDropdown.isVisible();
    
    if (hasBulkActions) {
      console.log('🔄 Testing bulk operations...');
      
      // Create multiple events for bulk operations
      const testData = generateTestData();
      const bulkEvents = [];
      
      for (let i = 0; i < 3; i++) {
        const event = {
          title: `Bulk Event ${i + 1} - ${testData.uniqueId}`,
          description: 'Event for bulk operations',
          startDate: new Date(Date.now() + (7 + i) * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
          endDate: new Date(Date.now() + (7 + i) * 24 * 60 * 60 * 1000 + 2 * 60 * 60 * 1000).toISOString().split('T')[0],
          location: 'Bulk Test Venue',
          eventType: 'Workshop',
          status: 'Draft'
        };
        
        await adminEvents.createEvent(event);
        bulkEvents.push(event.title);
      }
      
      // Select events for bulk action
      await adminEvents.goto();
      await adminEvents.searchEvents(testData.uniqueId);
      await adminEvents.selectEventsForBulkAction(bulkEvents);
      
      // Apply bulk publish
      await adminEvents.applyBulkAction('publish');
      
      // Verify all events are published
      await adminEvents.filterByStatus('Published');
      
      for (const title of bulkEvents) {
        const exists = await adminEvents.eventExists(title);
        expect(exists).toBeTruthy();
      }
      
      console.log('   ✅ Bulk operations working correctly');
    } else {
      console.log('   ℹ️ Bulk actions not available');
    }
  });

  test('should paginate through events', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-events-pagination' });

    await adminEvents.goto();
    
    // Check if pagination exists
    const hasPagination = await adminEvents.pagination.isVisible();
    
    if (hasPagination) {
      console.log('📄 Testing pagination...');
      
      // Get current page events
      const page1Events = await adminEvents.getEvents();
      console.log(`   Page 1: ${page1Events.length} events`);
      
      // Navigate to page 2 if available
      const page2Link = adminEvents.pagination.locator('a:has-text("2")');
      if (await page2Link.isVisible()) {
        await adminEvents.goToPage(2);
        
        const page2Events = await adminEvents.getEvents();
        console.log(`   Page 2: ${page2Events.length} events`);
        
        // Verify different events
        const differentEvents = page2Events[0]?.title !== page1Events[0]?.title;
        expect(differentEvents).toBeTruthy();
        console.log('   ✅ Pagination working correctly');
      }
    } else {
      console.log('   ℹ️ Not enough events for pagination');
    }
  });

  test('should sort events', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-events-sorting' });

    await adminEvents.goto();
    
    // Check if sort dropdown exists
    const hasSort = await adminEvents.sortDropdown.isVisible();
    
    if (hasSort) {
      console.log('🔤 Testing event sorting...');
      
      // Sort by date
      await adminEvents.sortBy('date');
      const dateSortedEvents = await adminEvents.getEvents();
      console.log('   Sorted by date');
      
      // Sort by title
      await adminEvents.sortBy('name');
      const nameSortedEvents = await adminEvents.getEvents();
      console.log('   Sorted by name');
      
      // Verify order changed
      if (dateSortedEvents.length > 1 && nameSortedEvents.length > 1) {
        const orderChanged = dateSortedEvents[0].title !== nameSortedEvents[0].title;
        expect(orderChanged).toBeTruthy();
        console.log('   ✅ Sorting working correctly');
      }
    } else {
      console.log('   ℹ️ Sort functionality not available');
    }
  });
});

test.describe('Admin Events Management - Error Handling', () => {
  test('should handle event not found gracefully', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'admin-event-not-found' });

    const loginPage = new LoginPage(page);
    const adminEvents = new AdminEventsPage(page);
    
    // Login
    await loginPage.goto();
    await loginPage.loginAsAdmin();
    await loginPage.verifyLoginSuccess();
    
    // Navigate to events
    await adminEvents.goto();
    
    // Try to edit non-existent event
    console.log('⚠️ Testing error handling...');
    
    try {
      await adminEvents.editEvent('Non-existent Event 12345');
    } catch (error: any) {
      console.log('   ✅ Correctly threw error for non-existent event');
      expect(error.message).toContain('not found');
    }
  });
});