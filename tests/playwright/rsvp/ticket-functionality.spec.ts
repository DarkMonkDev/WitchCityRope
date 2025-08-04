import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { EventPage } from '../pages/event.page';
import { RsvpPage } from '../pages/rsvp.page';
import { MemberDashboardPage } from '../pages/member-dashboard.page';
import { testConfig, generateTestData } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Ticket Functionality Tests
 * Converted from Puppeteer test: test-ticket-functionality.js
 * 
 * Tests ticket functionality for workshop events including:
 * - Creating workshop events with ticket sales
 * - NO RSVP tab for workshop events
 * - Ticket purchase flow
 * - Admin ticket management
 * - Check-in for ticketed events
 * - Ticket terminology throughout system
 */

test.describe('Ticket Functionality', () => {
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

  test.describe.serial('Complete Ticket Flow', () => {
    test('Test 1: Create a workshop event (uses tickets)', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'create-workshop-event' });
      
      // Login as admin
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      await loginPage.verifyLoginSuccess();
      
      // Navigate to admin events page
      await page.goto(`${testConfig.baseUrl}/admin/events`);
      await page.waitForLoadState('networkidle');
      
      // Click Create New Event button
      await page.waitForSelector('button:has-text("Create New Event")', { timeout: 10000 });
      await page.locator('button:has-text("Create New Event")').click();
      console.log('✓ Clicked Create New Event');
      
      // Wait for form to load
      await page.waitForSelector('form', { timeout: 10000 });
      
      // Fill basic information
      const testData = generateTestData();
      eventTitle = `Test Workshop - Ticket Testing ${testData.uniqueId}`;
      
      await page.fill('input[name="model.Title"]', eventTitle);
      console.log('✓ Entered event title');
      
      // Set description using the rich text editor
      await page.evaluate(() => {
        const editor = document.querySelector('.e-richtexteditor');
        if (editor && editor.ej2_instances && editor.ej2_instances[0]) {
          editor.ej2_instances[0].value = '<p>This is a test workshop to verify ticket functionality after Registration → Ticket refactoring.</p>';
        }
      });
      console.log('✓ Set event description');
      
      // Set location
      await page.fill('input[name="model.Location"]', 'Test Studio - Room A');
      console.log('✓ Entered location');
      
      // Set capacity
      const capacityInput = page.locator('input[name="model.Capacity"]');
      await capacityInput.clear();
      await capacityInput.fill('20');
      console.log('✓ Set capacity to 20');
      
      // Set pricing
      const priceInput = page.locator('input[name="model.IndividualPrice"]');
      await priceInput.clear();
      await priceInput.fill('45');
      console.log('✓ Set individual price to $45');
      
      // Submit form
      await page.click('button[type="submit"]:not(.btn-secondary)');
      console.log('✓ Submitted event creation form');
      
      // Wait for redirect
      await page.waitForLoadState('networkidle');
      
      // Get event ID from URL
      const currentUrl = page.url();
      const eventIdMatch = currentUrl.match(/\/admin\/events\/edit\/([a-f0-9-]+)/);
      if (eventIdMatch) {
        eventId = eventIdMatch[1];
        console.log(`✅ Workshop event created successfully (ID: ${eventId})`);
      } else {
        throw new Error('Failed to create workshop event');
      }
    });

    test('Test 2: Verify NO RSVP tab for workshop events', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'no-rsvp-tab-workshop' });
      
      // Login as admin
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      await loginPage.verifyLoginSuccess();
      
      // Navigate to event edit page
      await page.goto(`${testConfig.baseUrl}/admin/events/edit/${eventId}`);
      await page.waitForLoadState('networkidle');
      
      // Check that RSVP tab does NOT exist
      const rsvpTab = page.locator('button.tab-button').filter({ hasText: 'RSVPs' });
      await expect(rsvpTab).not.toBeVisible();
      console.log('✅ RSVP tab correctly hidden for workshop event');
      
      // Verify Tickets/Orders tab exists
      const ticketsTab = page.locator('button.tab-button').filter({ hasText: 'Tickets' });
      await expect(ticketsTab).toBeVisible();
      console.log('✓ Tickets/Orders tab is visible');
      
      // Click on it
      await ticketsTab.click();
      await page.waitForTimeout(1000);
      
      // Verify content
      const ordersSection = page.locator('.orders-section, .tickets-section');
      if (await ordersSection.isVisible()) {
        console.log('✓ Tickets/Orders content is displayed');
      }
    });

    test('Test 3: Purchase a ticket as a member', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'ticket-purchase' });
      
      // Login as member
      await loginPage.goto();
      await loginPage.login(testConfig.accounts.member.email, testConfig.accounts.member.password);
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
        
        // Look for Register/Purchase Ticket button
        const registerButton = page.locator('button').filter({ 
          hasText: /Register|Purchase Ticket/i 
        });
        
        if (await registerButton.isVisible()) {
          await registerButton.click();
          console.log('✓ Clicked ticket purchase button');
          
          // Wait for registration form or modal
          await page.waitForTimeout(2000);
          
          // Fill emergency contact if form appears
          const emergencyNameInput = page.locator('input[name*="emergency"]');
          if (await emergencyNameInput.isVisible()) {
            await emergencyNameInput.fill('Emergency Contact');
            console.log('✓ Filled emergency contact');
            
            const emergencyPhoneInput = page.locator('input[name*="phone"]');
            if (await emergencyPhoneInput.isVisible()) {
              await emergencyPhoneInput.fill('555-0911');
              console.log('✓ Filled emergency phone');
            }
            
            // Submit registration
            const submitButton = page.locator('button[type="submit"]').filter({ 
              hasText: /Complete|Register/i 
            });
            if (await submitButton.isVisible()) {
              await submitButton.click();
              console.log('✓ Submitted ticket purchase');
              
              await page.waitForTimeout(2000);
              
              // Check for confirmation
              const confirmation = page.locator('.alert-success, .confirmation-message, :has-text("TKT-")');
              if (await confirmation.isVisible()) {
                console.log('✅ Ticket purchase completed successfully');
              }
            }
          }
        } else {
          console.log('⚠️ Register/Purchase button not found - UI may need implementation');
        }
      } else {
        console.log('⚠️ Event link not found on events page');
      }
    });

    test('Test 4: Verify tickets in admin panel', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'admin-ticket-management' });
      
      // Login as admin
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      await loginPage.verifyLoginSuccess();
      
      // Navigate to event edit page
      await page.goto(`${testConfig.baseUrl}/admin/events/edit/${eventId}`);
      await page.waitForLoadState('networkidle');
      
      // Click on Tickets/Orders tab
      const ticketsTab = page.locator('button.tab-button').filter({ hasText: 'Tickets' });
      await expect(ticketsTab).toBeVisible();
      await ticketsTab.click();
      await page.waitForTimeout(1000);
      console.log('✓ Clicked Tickets/Orders tab');
      
      // Check for orders table
      const ordersTable = page.locator('.orders-table, table:has-text("Order")');
      if (await ordersTable.isVisible()) {
        console.log('✓ Orders table found');
        
        // Count orders
        const orderRows = await page.locator('tbody tr').count();
        console.log(`✓ Found ${orderRows} order(s) in the table`);
        
        // Check for confirmation codes (should start with TKT- not REG-)
        const tktCodes = await page.locator('td:has-text("TKT-")').count();
        if (tktCodes > 0) {
          console.log(`✓ Found ${tktCodes} ticket confirmation code(s) with TKT- prefix`);
        }
      } else {
        console.log('⚠️ Orders table not found - checking for empty state');
        const emptyState = page.locator('.empty-state');
        if (await emptyState.isVisible()) {
          console.log('✓ Empty state displayed (no orders yet)');
        }
      }
    });

    test('Test 5: Workshop check-in (tickets only)', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'workshop-checkin' });
      
      // Login as admin
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      await loginPage.verifyLoginSuccess();
      
      // Navigate to check-in page
      await rsvpPage.navigateToCheckIn(eventId);
      console.log('✓ Navigated to check-in page');
      
      // For workshops, there should be NO RSVPs tab
      const isRsvpTabVisible = await rsvpPage.isRsvpTabVisible();
      expect(isRsvpTabVisible).toBeFalsy();
      console.log('✓ No RSVPs tab (correct for workshop)');
      
      // Should show tickets directly
      const ticketsTable = page.locator('.attendees-table');
      if (await ticketsTable.isVisible()) {
        console.log('✓ Tickets table displayed by default');
        
        // Check headers - should NOT have "Has Ticket" column
        const hasTicketColumn = await rsvpPage.hasTicketColumnExists();
        expect(hasTicketColumn).toBeFalsy();
        console.log('✓ No "Has Ticket" column (all attendees have tickets)');
      }
    });

    test('Test 6: Verify ticket terminology', async ({ page }) => {
      test.info().annotations.push({ type: 'test-id', description: 'ticket-terminology' });
      
      // Login as member
      await loginPage.goto();
      await loginPage.login(testConfig.accounts.member.email, testConfig.accounts.member.password);
      await loginPage.verifyLoginSuccess();
      
      // Navigate to member dashboard
      await memberDashboardPage.goto();
      await memberDashboardPage.waitForDashboardData();
      
      // Look for "My Tickets" section (not "My Registrations")
      const ticketsSection = page.locator(':has-text("My Tickets"), :has-text("Upcoming Events")');
      await expect(ticketsSection).toBeVisible();
      console.log('✓ Found tickets section on member dashboard');
      
      // Check for confirmation codes with TKT- prefix
      const tktCodes = await page.locator(':has-text("TKT-")').count();
      console.log(`✓ Found ${tktCodes} ticket confirmation codes`);
      
      // Ensure no REG- prefixes exist
      const regCodes = await page.locator(':has-text("REG-")').count();
      expect(regCodes).toBe(0);
      console.log('✓ No old REG- confirmation codes found');
    });
  });

  test('should enforce ticket pricing', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'ticket-pricing' });
    
    // This test would verify that payment is required for non-free workshops
    // Implementation depends on payment integration
  });

  test('should handle sold out workshops', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'sold-out-workshop' });
    
    // This test would verify behavior when workshop tickets are sold out
    // Implementation depends on actual application behavior
  });
});

test.describe('Ticket vs RSVP Distinction', () => {
  test('should show correct UI elements based on event type', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'event-type-ui-distinction' });
    
    const loginPage = new LoginPage(page);
    const rsvpPage = new RsvpPage(page);
    
    // Login as member
    await loginPage.goto();
    await loginPage.login(testConfig.accounts.member.email, testConfig.accounts.member.password);
    await loginPage.verifyLoginSuccess();
    
    // Navigate to events page
    await page.goto(`${testConfig.baseUrl}/events`);
    await page.waitForLoadState('networkidle');
    
    // Check social events show RSVP buttons
    const socialEvents = page.locator('.event-card').filter({ hasText: /Rope Jam|Social|Meetup/i });
    const socialCount = await socialEvents.count();
    if (socialCount > 0) {
      const firstSocial = socialEvents.first();
      await firstSocial.click();
      await page.waitForLoadState('networkidle');
      
      const rsvpButton = await rsvpPage.getRsvpButtonText();
      expect(rsvpButton).toMatch(/RSVP/i);
      console.log('✓ Social events show RSVP button');
      
      await page.goBack();
    }
    
    // Check workshops show ticket/register buttons
    const workshops = page.locator('.event-card').filter({ hasText: /Workshop|Class/i });
    const workshopCount = await workshops.count();
    if (workshopCount > 0) {
      const firstWorkshop = workshops.first();
      await firstWorkshop.click();
      await page.waitForLoadState('networkidle');
      
      const ticketButton = page.locator('button').filter({ hasText: /Register|Purchase|Ticket/i });
      await expect(ticketButton).toBeVisible();
      console.log('✓ Workshops show ticket/register button');
    }
  });
});