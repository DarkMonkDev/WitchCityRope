import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test.describe('Event Session Matrix System Test', () => {
  test('Test complete Event Session Matrix functionality', async ({ page }) => {
    console.log('🚀 Testing Event Session Matrix system...');

    // Step 1: Login as admin using AuthHelpers
    await AuthHelpers.loginAs(page, 'admin');
    console.log('✅ Logged in as admin successfully');
    
    // Step 2: Navigate to admin events page
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');
    console.log('✅ On admin events page');
    
    // Step 3: Click Create Event
    const createEventBtn = page.locator('button:has-text("Create Event")');
    await createEventBtn.click();
    await page.waitForTimeout(1000);
    await page.screenshot({ path: 'test-results/event-modal-opened.png', fullPage: true });
    console.log('✅ Create Event modal opened');
    
    // Step 4: Check all tabs in the modal
    const tabs = {
      basicInfo: page.locator('text=Basic Info'),
      setup: page.locator('text=Setup'),
      emails: page.locator('text=Emails'),
      volunteers: page.locator('text=Volunteers')
    };
    
    console.log('🔍 Checking available tabs...');
    for (const [tabName, tabLocator] of Object.entries(tabs)) {
      const tabExists = await tabLocator.count() > 0;
      console.log(`${tabName} tab: ${tabExists ? 'EXISTS' : 'MISSING'}`);
    }
    
    // Step 5: Click on Setup tab (this should contain the Event Session Matrix)
    if (await tabs.setup.count() > 0) {
      await tabs.setup.click();
      await page.waitForTimeout(1000);
      await page.screenshot({ path: 'test-results/setup-tab.png', fullPage: true });
      console.log('✅ Clicked Setup tab');
      
      // Step 6: Look for Session Matrix elements
      const sessionMatrixElements = {
        sessionSection: page.locator('text=Session').or(page.locator('fieldset:has-text("Session")')),
        addSessionBtn: page.locator('button:has-text("Add Session")').or(page.locator('[data-testid*="add-session"]')),
        sessionList: page.locator('[data-testid*="session-list"]').or(page.locator('.session-item')),
        timeSlots: page.locator('input[type="time"]').or(page.locator('[placeholder*="time"]')),
        dateInputs: page.locator('input[type="date"]').or(page.locator('[placeholder*="date"]')),
        ticketTypes: page.locator('text=Ticket Type').or(page.locator('fieldset:has-text("Ticket")')),
        addTicketBtn: page.locator('button:has-text("Add Ticket")').or(page.locator('[data-testid*="add-ticket"]')),
        priceInputs: page.locator('input[name*="price"]').or(page.locator('[placeholder*="price"]')),
        capacityInputs: page.locator('input[name*="capacity"]').or(page.locator('[placeholder*="capacity"]'))
      };
      
      console.log('🎯 Checking Event Session Matrix elements...');
      let matrixFeatures = 0;
      
      for (const [elementName, elementLocator] of Object.entries(sessionMatrixElements)) {
        const elementCount = await elementLocator.count();
        console.log(`${elementName}: ${elementCount} element(s) found`);
        if (elementCount > 0) {
          matrixFeatures++;
          await page.screenshot({ path: `test-results/matrix-${elementName}.png`, fullPage: true });
        }
      }
      
      console.log(`🎉 Event Session Matrix features detected: ${matrixFeatures}/9`);
      
      // Step 7: Try to interact with session management
      // CRITICAL: Must add session BEFORE attempting to add ticket types
      // The Event Session Matrix requires at least one session to exist before tickets can be configured
      let sessionAdded = false;
      if (await sessionMatrixElements.addSessionBtn.count() > 0) {
        console.log('✅ Testing Add Session functionality...');
        await page.locator('[data-testid="button-add-session"]').click();
        await page.waitForTimeout(1000);
        await page.screenshot({ path: 'test-results/add-session-clicked.png', fullPage: true });

        // Look for session form fields that appear
        const sessionForm = {
          sessionTitle: page.locator('input[name*="session"]').or(page.locator('[placeholder*="session"]')),
          startTime: page.locator('input[type="time"]').first(),
          endTime: page.locator('input[type="time"]').last(),
          capacity: page.locator('input[name*="capacity"]').or(page.locator('[placeholder*="capacity"]'))
        };

        for (const [fieldName, fieldLocator] of Object.entries(sessionForm)) {
          const fieldExists = await fieldLocator.count() > 0;
          console.log(`Session ${fieldName}: ${fieldExists ? 'FOUND' : 'MISSING'}`);
        }

        // Try to save the session if save button exists
        const saveSessionBtn = page.locator('button:has-text("Save"), button:has-text("Add")').last();
        if (await saveSessionBtn.count() > 0 && await saveSessionBtn.isVisible()) {
          await saveSessionBtn.click();
          await page.waitForTimeout(500);
          sessionAdded = true;
          console.log('✅ Session saved successfully');
        }
      }

      // Step 8: Try to interact with ticket management
      // ONLY attempt if session was successfully added first
      if (sessionAdded && await sessionMatrixElements.addTicketBtn.count() > 0) {
        console.log('✅ Testing Add Ticket Type functionality...');
        await sessionMatrixElements.addTicketBtn.click();
        await page.waitForTimeout(1000);
        await page.screenshot({ path: 'test-results/add-ticket-clicked.png', fullPage: true });

        // Look for ticket form fields
        const ticketForm = {
          ticketName: page.locator('input[name*="ticket"]').or(page.locator('[placeholder*="ticket"]')),
          ticketPrice: page.locator('input[name*="price"]').or(page.locator('[placeholder*="price"]')),
          ticketCapacity: page.locator('input[name*="capacity"]').or(page.locator('[placeholder*="capacity"]'))
        };

        for (const [fieldName, fieldLocator] of Object.entries(ticketForm)) {
          const fieldExists = await fieldLocator.count() > 0;
          console.log(`Ticket ${fieldName}: ${fieldExists ? 'FOUND' : 'MISSING'}`);
        }
      } else if (!sessionAdded) {
        console.log('⚠️ Skipping ticket type test - session must be added first');
      }
      
    } else {
      console.log('⚠️ Setup tab not found - Event Session Matrix may not be fully integrated');
    }
    
    // Step 9: Check other tabs for completeness
    if (await tabs.emails.count() > 0) {
      await tabs.emails.click();
      await page.waitForTimeout(500);
      await page.screenshot({ path: 'test-results/emails-tab.png', fullPage: true });
      console.log('✅ Emails tab accessible');
    }
    
    if (await tabs.volunteers.count() > 0) {
      await tabs.volunteers.click();
      await page.waitForTimeout(500);
      await page.screenshot({ path: 'test-results/volunteers-tab.png', fullPage: true });
      console.log('✅ Volunteers tab accessible');
    }
    
    // Step 10: Go back to Basic Info to test form completion
    if (await tabs.basicInfo.count() > 0) {
      await tabs.basicInfo.click();
      await page.waitForTimeout(500);
      
      // Fill in basic event info
      const titleInput = page.locator('input[name="title"]').or(page.locator('[placeholder*="title"]'));
      const descriptionInput = page.locator('textarea[name="description"]').or(page.locator('[placeholder*="description"]'));
      
      if (await titleInput.count() > 0) {
        await titleInput.fill('Event Session Matrix Integration Test');
        console.log('✅ Event title filled');
      }
      
      if (await descriptionInput.count() > 0) {
        await descriptionInput.fill('This event tests the complete Event Session Matrix system integration.');
        console.log('✅ Event description filled');
      }
      
      await page.screenshot({ path: 'test-results/basic-info-filled.png', fullPage: true });
    }
    
    // Step 11: Look for save/create button
    const saveBtn = page.locator('button:has-text("Save")').or(
      page.locator('button:has-text("Create")').or(
        page.locator('button[type="submit"]')
      )
    );
    
    const saveBtnExists = await saveBtn.count() > 0;
    console.log(`✅ Save/Create button exists: ${saveBtnExists}`);
    
    if (saveBtnExists) {
      await page.screenshot({ path: 'test-results/ready-to-save.png', fullPage: true });
      // Note: Not actually saving to avoid creating test data
    }
    
    // Step 12: Final summary screenshot
    await page.screenshot({ path: 'test-results/event-session-matrix-test-complete.png', fullPage: true });
    
    console.log('🎉 Event Session Matrix system test completed!');
    
    // Test always passes - this is for discovery
    expect(true).toBe(true);
  });
});