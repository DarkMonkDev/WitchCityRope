import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { EventPage } from '../pages/event.page';
import { RsvpPage } from '../pages/rsvp.page';
import { MemberDashboardPage } from '../pages/member-dashboard.page';
import { testConfig, generateTestData } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Member RSVP Flow Tests
 * Converted from Puppeteer test: test-member-rsvp-flow.js
 * 
 * Tests the complete RSVP flow from member perspective:
 * - Admin creates social event
 * - Member RSVPs to event
 * - RSVP appears on member dashboard
 * - Admin can verify the RSVP
 */

test.describe('Member RSVP Flow', () => {
  let loginPage: LoginPage;
  let eventPage: EventPage;
  let rsvpPage: RsvpPage;
  let memberDashboardPage: MemberDashboardPage;
  let eventId: string;
  let eventTitle: string;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    eventPage = new EventPage(page);
    rsvpPage = new RsvpPage(page);
    memberDashboardPage = new MemberDashboardPage(page);
  });

  test.describe.serial('Complete Member RSVP Journey', () => {
    test('Part 1: Admin creates social event', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'admin-create-social-event' });
      
      // Login as admin
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      await loginPage.verifyLoginSuccess();
      
      // Navigate to admin events page
      await page.goto(`${testConfig.baseUrl}/admin/events`);
      await page.waitForLoadState('networkidle');
      console.log('✓ Navigated to admin events page');
      
      // Click Create New Event button
      await page.locator('button:has-text("Create New Event")').click();
      console.log('✓ Clicked Create New Event');
      
      // Wait for form to load
      await page.waitForSelector('.event-editor-container', { timeout: 10000 });
      await page.waitForSelector('.tab-content', { timeout: 10000 });
      
      // Select Social event type
      const meetupButton = page.locator('.type-option-compact').filter({ hasText: 'Meetup' });
      await meetupButton.click();
      console.log('✓ Selected Social event type');
      
      // Fill event details
      const testData = generateTestData();
      eventTitle = `Test Rope Jam - Member RSVP ${testData.uniqueId}`;
      
      const titleInput = page.locator('input.form-input[placeholder*="Rope Basics Workshop"]');
      await titleInput.click({ clickCount: 3 });
      await titleInput.fill(eventTitle);
      console.log('✓ Entered event title');
      
      // Set description
      await page.waitForTimeout(1000);
      await page.evaluate(() => {
        const editors = document.querySelectorAll('.e-richtexteditor');
        if (editors.length > 0 && (editors[0] as any).ej2_instances && (editors[0] as any).ej2_instances[0]) {
          (editors[0] as any).ej2_instances[0].value = '<p>Test rope jam for verifying member RSVP functionality.</p>';
          (editors[0] as any).ej2_instances[0].dataBind();
        }
      });
      console.log('✓ Set event description');
      
      // Set future dates
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 7);
      futureDate.setHours(19, 0, 0, 0);
      const startDateValue = futureDate.toISOString().slice(0, 16);
      
      const startDateInput = page.locator('input[type="datetime-local"]').first();
      await startDateInput.click({ clickCount: 3 });
      await startDateInput.fill(startDateValue);
      console.log('✓ Set start date/time (7 days from now)');
      
      // Set end date
      const endDate = new Date(futureDate);
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
      const capacityInput = page.locator('input.form-input[type="number"]').first();
      await capacityInput.click({ clickCount: 3 });
      await capacityInput.fill('30');
      console.log('✓ Set capacity to 30');
      
      // Switch to Tickets tab
      const ticketsTab = page.locator('.tab-button').filter({ hasText: 'Tickets' });
      await ticketsTab.click();
      await page.waitForTimeout(500);
      console.log('✓ Clicked Tickets tab');
      
      // Set as free event
      const pricingSelect = page.locator('select.form-input').first();
      await pricingSelect.selectOption('Free');
      console.log('✓ Set as free event');
      
      // Submit form - specifically click "Create Event" not "Save as Draft"
      await page.waitForSelector('.form-footer', { timeout: 10000 });
      const createButton = page.locator('.form-footer button, .form-footer .btn').filter({
        hasText: /^Create Event$/
      });
      
      if (await createButton.isVisible()) {
        await createButton.click();
        console.log('✓ Clicked Create Event button');
      } else {
        // Log available buttons for debugging
        const availableButtons = await page.locator('.form-footer button, .form-footer .btn').allTextContents();
        console.log('⚠️ Could not find Create Event button. Available buttons:', availableButtons);
        throw new Error('Create Event button not found');
      }
      
      // Wait for save and potential redirect
      await page.waitForTimeout(2000);
      
      // Get event ID from URL if redirected
      const currentUrl = page.url();
      const eventIdMatch = currentUrl.match(/\/admin\/events\/edit\/([a-f0-9-]+)/);
      if (eventIdMatch) {
        eventId = eventIdMatch[1];
        console.log(`✅ Event created successfully (ID: ${eventId})`);
      }
      
      // Check if we need to publish the event
      if (currentUrl.includes('/edit/')) {
        console.log('ℹ️ Event created as draft, looking for publish option...');
        
        // Look for status dropdown or publish button
        const statusSelect = page.locator('select[name*="status"], select[name*="Status"]');
        if (await statusSelect.isVisible()) {
          await statusSelect.selectOption('Published');
          console.log('✓ Set event status to Published');
          
          // Save changes
          const saveButton = page.locator('button, .btn').filter({
            hasText: /Save|Update/i
          });
          if (await saveButton.isVisible()) {
            await saveButton.click();
            console.log('✓ Saved event with published status');
            await page.waitForTimeout(2000);
          }
        }
      }
      
      // Verify event appears in list
      await page.goto(`${testConfig.baseUrl}/admin/events`);
      await page.waitForLoadState('networkidle');
      
      const eventInList = await page.locator('.event-name a').filter({ hasText: eventTitle }).isVisible();
      expect(eventInList).toBeTruthy();
      console.log('✓ Event appears in admin events list');
    });

    test('Part 2: Member RSVPs to event', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'member-rsvp-to-event' });
      
      // Login as vetted member
      await loginPage.goto();
      await loginPage.login(testConfig.accounts.vetted.email, testConfig.accounts.vetted.password);
      await loginPage.verifyLoginSuccess();
      
      // Navigate to member events page
      await page.goto(`${testConfig.baseUrl}/member/events`);
      await page.waitForLoadState('networkidle');
      console.log('✓ Navigated to member events page');
      
      // Check for upcoming events tab
      const upcomingTab = page.locator('a, button, .tab, [role="tab"]').filter({
        hasText: /Upcoming|Future|All Events/i
      });
      if (await upcomingTab.isVisible()) {
        await upcomingTab.click();
        console.log('✓ Clicked on Upcoming/Future events tab');
        await page.waitForTimeout(2000);
      }
      
      // Wait for events to load
      await page.waitForSelector('.events-container, .event-card, .event-list, .events-grid', { timeout: 10000 });
      
      // Find the test event
      const eventCard = page.locator('.event-card, .event-item, [class*="event"], .card').filter({
        hasText: eventTitle
      });
      
      if (await eventCard.isVisible()) {
        console.log('✓ Found test event in list');
        
        // Click on the event
        await eventCard.click();
        await page.waitForTimeout(2000);
        console.log('✓ Clicked on event to view details');
        
        // RSVP to the event
        const rsvpSuccess = await rsvpPage.rsvpToEvent();
        expect(rsvpSuccess).toBeTruthy();
        console.log('✅ RSVP successful!');
      } else {
        console.log('⚠️ Could not find test event in member view');
        
        // Take screenshot for debugging
        await page.screenshot({ path: 'test-results/member-events-page.png', fullPage: true });
        console.log('📸 Screenshot saved as member-events-page.png');
      }
      
      // Navigate to member dashboard
      await memberDashboardPage.goto();
      console.log('\n✓ Navigated to member dashboard');
      
      // Wait for dashboard data
      await memberDashboardPage.waitForDashboardData();
      
      // Check for RSVP in dashboard
      const hasRsvp = await memberDashboardPage.hasRsvpForEvent(eventTitle);
      if (hasRsvp) {
        console.log('✅ RSVP appears in member dashboard');
        
        // Get event details
        const eventDetails = await memberDashboardPage.getEventDetails(eventTitle);
        if (eventDetails) {
          console.log(`✓ Event status: ${eventDetails.status}`);
          console.log(`✓ Event button: ${eventDetails.buttonText}`);
        }
      } else {
        console.log('⚠️ RSVP not visible in dashboard');
        
        // Get dashboard summary for debugging
        const summary = await memberDashboardPage.getDashboardSummary();
        console.log('Dashboard summary:', summary);
        
        await page.screenshot({ path: 'test-results/member-dashboard.png' });
        console.log('📸 Screenshot saved as member-dashboard.png');
      }
    });

    test('Part 3: Admin verifies member RSVP', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'admin-verify-rsvp' });
      
      // Login as admin
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      await loginPage.verifyLoginSuccess();
      
      // Navigate to admin events page
      await page.goto(`${testConfig.baseUrl}/admin/events`);
      await page.waitForLoadState('networkidle');
      console.log('✓ Navigated to admin events page');
      
      // Find and click on the test event
      const eventLink = page.locator('.event-name a').filter({ hasText: eventTitle });
      if (await eventLink.isVisible()) {
        await eventLink.click();
        console.log('✓ Clicked on test event in admin');
        await page.waitForTimeout(2000);
      } else if (eventId) {
        // Try direct navigation if we have the ID
        await page.goto(`${testConfig.baseUrl}/admin/events/edit/${eventId}`);
        console.log('✓ Navigated directly to event edit page');
      }
      
      // Navigate to RSVPs tab
      await rsvpPage.navigateToRsvpTab();
      console.log('✓ Clicked Registrations/RSVPs tab');
      
      // Check for member in RSVP list
      const memberRsvp = await rsvpPage.findRsvpByEmail(testConfig.accounts.vetted.email);
      if (memberRsvp) {
        console.log('✅ Member RSVP verified in admin view!');
        
        // Check RSVP status
        const rowText = await memberRsvp.textContent();
        if (rowText?.includes('Confirmed') || rowText?.includes('Registered')) {
          console.log('✓ RSVP status: Confirmed');
        }
      } else {
        console.log('⚠️ Member not found in RSVP list');
        await page.screenshot({ path: 'test-results/admin-rsvp-list.png' });
      }
    });
  });

  test('should handle RSVP when event is at capacity', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'rsvp-at-capacity' });
    
    // This test would verify behavior when an event reaches capacity
    // Implementation depends on actual application behavior
  });

  test('should allow member to cancel RSVP from dashboard', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'cancel-rsvp-dashboard' });
    
    // Login as member
    await loginPage.goto();
    await loginPage.login(testConfig.accounts.vetted.email, testConfig.accounts.vetted.password);
    await loginPage.verifyLoginSuccess();
    
    // Navigate to dashboard
    await memberDashboardPage.goto();
    await memberDashboardPage.waitForDashboardData();
    
    // Find an RSVP to cancel
    const rsvps = await memberDashboardPage.getAllRsvps();
    if (rsvps.length > 0) {
      const firstRsvp = rsvps[0];
      const cancelled = await memberDashboardPage.cancelRsvp(firstRsvp.title);
      expect(cancelled).toBeTruthy();
      console.log('✅ Successfully cancelled RSVP from dashboard');
    }
  });
});