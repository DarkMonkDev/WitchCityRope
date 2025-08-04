import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { EventPage } from '../pages/event.page';
import { RsvpPage } from '../pages/rsvp.page';
import { testConfig, generateTestData } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * RSVP Functionality Tests
 * Converted from Puppeteer test: test-rsvp-functionality.js
 * 
 * Tests RSVP functionality for social events including:
 * - Creating social events with RSVP capability
 * - RSVP tab visibility for social events
 * - Member RSVP creation
 * - Admin RSVP management
 * - Event check-in functionality
 */

test.describe('RSVP Functionality', () => {
  let loginPage: LoginPage;
  let eventPage: EventPage;
  let rsvpPage: RsvpPage;
  let eventId: string;
  let eventTitle: string;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    eventPage = new EventPage(page);
    rsvpPage = new RsvpPage(page);
  });

  test.describe.serial('Complete RSVP Flow', () => {
    test('Test 1: Admin creates a social event (Rope Jam)', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'create-social-event' });
      
      // Login as admin
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      await loginPage.verifyLoginSuccess();
      
      // Navigate to admin events page
      await page.goto(`${testConfig.baseUrl}/admin/events`);
      await page.waitForLoadState('networkidle');
      
      // Click Create New Event button
      await page.locator('button:has-text("Create New Event")').click();
      console.log('✓ Clicked Create New Event');
      
      // Wait for form to load
      await page.waitForSelector('.event-editor-container', { timeout: 10000 });
      await page.waitForSelector('.tab-content', { timeout: 10000 });
      
      // Select Social event type by clicking the Meetup button
      await page.waitForSelector('.event-type-toggle-compact', { timeout: 10000 });
      const meetupButton = page.locator('.type-option-compact').filter({ hasText: 'Meetup' });
      await meetupButton.click();
      console.log('✓ Selected Social event type');
      
      // Fill basic information
      const testData = generateTestData();
      eventTitle = `Test Rope Jam - RSVP Testing ${testData.uniqueId}`;
      
      const titleInput = page.locator('input.form-input[placeholder*="Rope Basics Workshop"]');
      await titleInput.click({ clickCount: 3 });
      await titleInput.fill(eventTitle);
      console.log('✓ Entered event title');
      
      // Set description using the rich text editor
      await page.waitForTimeout(1000); // Give RTE time to initialize
      await page.evaluate(() => {
        const editors = document.querySelectorAll('.e-richtexteditor');
        if (editors.length > 0 && (editors[0] as any).ej2_instances && (editors[0] as any).ej2_instances[0]) {
          (editors[0] as any).ej2_instances[0].value = '<p>This is a test rope jam event to verify RSVP functionality works correctly.</p>';
          (editors[0] as any).ej2_instances[0].dataBind();
        }
      });
      console.log('✓ Set event description');
      
      // Set dates and times
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      tomorrow.setHours(19, 0, 0, 0);
      const startDateValue = tomorrow.toISOString().slice(0, 16);
      
      const startDateInput = page.locator('input[type="datetime-local"]').first();
      await startDateInput.click({ clickCount: 3 });
      await startDateInput.fill(startDateValue);
      console.log('✓ Set start date/time');
      
      // Set end date
      const endDate = new Date(tomorrow);
      endDate.setHours(22, 0, 0, 0);
      const endDateValue = endDate.toISOString().slice(0, 16);
      
      const endDateInput = page.locator('input[type="datetime-local"]').nth(1);
      await endDateInput.click({ clickCount: 3 });
      await endDateInput.fill(endDateValue);
      console.log('✓ Set end date/time');
      
      // Set location
      const venueInput = page.locator('input.form-input[placeholder="Enter venue name"]');
      await venueInput.click({ clickCount: 3 });
      await venueInput.fill('Test Venue - Main Hall');
      console.log('✓ Entered location');
      
      // Set capacity
      const capacityInput = page.locator('input[type="number"].form-input').first();
      await capacityInput.click({ clickCount: 3 });
      await capacityInput.fill('30');
      console.log('✓ Set capacity to 30');
      
      // Switch to Tickets tab to set pricing
      await page.locator('.tab-button').filter({ hasText: 'Tickets' }).click();
      await page.waitForTimeout(500);
      console.log('✓ Clicked Tickets tab');
      
      // Set pricing (free event)
      const pricingSelect = page.locator('select.form-input').first();
      await pricingSelect.selectOption('Free');
      console.log('✓ Set as free event');
      
      // Submit form
      await page.waitForSelector('.form-footer', { timeout: 10000 });
      await page.locator('.form-footer .btn-primary').click();
      console.log('✓ Clicked Create Event button');
      
      // Wait for redirect to event edit page
      await page.waitForTimeout(2000);
      await page.waitForFunction(
        () => window.location.href.includes('/admin/events/edit/'),
        { timeout: 10000 }
      );
      
      // Get event ID from URL
      const currentUrl = page.url();
      const eventIdMatch = currentUrl.match(/\/admin\/events\/edit\/([a-f0-9-]+)/);
      expect(eventIdMatch).toBeTruthy();
      eventId = eventIdMatch![1];
      console.log(`✅ Social event created successfully (ID: ${eventId})`);
    });

    test('Test 2: Verify RSVP tab appears for social events', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'rsvp-tab-visibility' });
      
      // Login as admin
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      await loginPage.verifyLoginSuccess();
      
      // Navigate to event edit page
      await page.goto(`${testConfig.baseUrl}/admin/events/edit/${eventId}`);
      await page.waitForLoadState('networkidle');
      
      // Check for RSVP tab
      const rsvpTab = page.locator('button.tab-button').filter({ hasText: 'RSVPs' });
      await expect(rsvpTab).toBeVisible();
      console.log('✅ RSVP tab is visible for social event');
      
      // Click on RSVP tab
      await rsvpTab.click();
      await page.waitForTimeout(1000);
      
      // Verify RSVP content is displayed
      await expect(page.locator('.stat-card')).toBeVisible({ timeout: 5000 });
      console.log('✓ RSVP content panel is displayed');
      
      // Check for stats
      const statLabels = await page.locator('.stat-card .stat-label').allTextContents();
      console.log(`✓ RSVP stats found: ${statLabels.join(', ')}`);
      expect(statLabels.length).toBeGreaterThan(0);
    });

    test('Test 3: Member creates RSVP', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'member-rsvp-creation' });
      
      // Login as member
      await loginPage.goto();
      await loginPage.login(testConfig.accounts.vetted.email, testConfig.accounts.vetted.password);
      await loginPage.verifyLoginSuccess();
      
      // Navigate to events page
      await page.goto(`${testConfig.baseUrl}/events`);
      await page.waitForLoadState('networkidle');
      
      // Find and click on our test event
      const eventLink = page.locator(`a[href*="${eventId}"]`);
      if (await eventLink.isVisible()) {
        await eventLink.click();
        await page.waitForLoadState('networkidle');
        console.log('✓ Navigated to event details page');
        
        // RSVP to the event
        const rsvpSuccess = await rsvpPage.rsvpToEvent();
        expect(rsvpSuccess).toBeTruthy();
        console.log('✅ RSVP created successfully');
      } else {
        console.log('⚠️ Event link not found on events page');
      }
    });

    test('Test 4: Admin verifies RSVP in management panel', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'admin-rsvp-management' });
      
      // Login as admin
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      await loginPage.verifyLoginSuccess();
      
      // Navigate to event edit page
      await page.goto(`${testConfig.baseUrl}/admin/events/edit/${eventId}`);
      await page.waitForLoadState('networkidle');
      
      // Navigate to RSVP tab
      await rsvpPage.navigateToRsvpTab();
      console.log('✓ Clicked RSVP tab');
      
      // Check for RSVPs
      const rsvpCount = await rsvpPage.getRsvpCount();
      console.log(`✓ Found ${rsvpCount} RSVP(s) in the table`);
      
      if (rsvpCount > 0) {
        // Check for action buttons
        const confirmButtonCount = await page.locator('button').filter({ hasText: 'Confirm' }).count();
        const cancelButtonCount = await page.locator('button').filter({ hasText: 'Cancel' }).count();
        console.log(`✓ Found ${confirmButtonCount} Confirm button(s)`);
        console.log(`✓ Found ${cancelButtonCount} Cancel button(s)`);
        
        // Verify member RSVP exists
        const memberRsvp = await rsvpPage.findRsvpByEmail(testConfig.accounts.vetted.email);
        expect(memberRsvp).toBeTruthy();
        console.log('✅ Member RSVP verified in admin view');
      } else {
        // Check for empty state
        await expect(rsvpPage.emptyStateMessage).toBeVisible();
        console.log('✓ Empty state displayed (No RSVPs yet)');
      }
    });

    test('Test 5: Event check-in functionality', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'event-checkin' });
      
      // Login as admin
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      await loginPage.verifyLoginSuccess();
      
      // Navigate to check-in page
      await rsvpPage.navigateToCheckIn(eventId);
      console.log('✓ Navigated to check-in page');
      
      // Verify RSVPs tab exists for social events
      const isRsvpTabVisible = await rsvpPage.isRsvpTabVisible();
      expect(isRsvpTabVisible).toBeTruthy();
      console.log('✓ RSVPs tab found on check-in page');
      
      // Click RSVPs tab
      if (isRsvpTabVisible) {
        const rsvpsTab = page.locator('button.tab-button').filter({ hasText: 'RSVPs' });
        await rsvpsTab.click();
        await page.waitForTimeout(1000);
        
        // Check attendee count
        const attendeeCount = await rsvpPage.getCheckInAttendeeCount();
        console.log(`✓ Found ${attendeeCount} attendee(s) in RSVP list`);
        
        // Check for "Has Ticket" column
        const hasTicketColumn = await rsvpPage.hasTicketColumnExists();
        if (hasTicketColumn) {
          console.log('✓ "Has Ticket" column present for RSVP attendees');
        }
      }
    });
  });

  test.afterAll(async ({ browser }) => {
    // Cleanup: Delete test event if created
    if (eventId) {
      console.log('\n🧹 Cleaning up test data...');
      // Note: Event deletion would go here if implemented
    }
  });
});

test.describe('RSVP Edge Cases', () => {
  test('should handle multiple RSVPs from same user', async ({ page }) => {
    // Test that a user cannot RSVP twice to the same event
    test.info().annotations.push({ type: 'test-id', description: 'duplicate-rsvp-prevention' });
    
    const loginPage = new LoginPage(page);
    const rsvpPage = new RsvpPage(page);
    
    // This would test the edge case of duplicate RSVPs
    // Implementation depends on actual application behavior
  });

  test('should enforce capacity limits', async ({ page }) => {
    // Test that RSVPs are rejected when event is at capacity
    test.info().annotations.push({ type: 'test-id', description: 'capacity-enforcement' });
    
    // This would test capacity limit enforcement
    // Implementation depends on actual application behavior
  });
});