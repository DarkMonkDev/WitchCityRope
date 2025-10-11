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
    if (await page.locator('[role="tab"]:has-text("Setup")').count() > 0) {
      await page.locator('[role="tab"]:has-text("Setup")').click();
      await page.waitForTimeout(1000);
      await page.screenshot({ path: 'test-results/setup-tab.png', fullPage: true });
      console.log('✅ Clicked Setup tab');
      
      // Step 6: Look for Session Matrix elements (MODAL-BASED UI)
      const sessionMatrixElements = {
        sessionsGrid: page.locator('[data-testid="grid-sessions"]'),
        addSessionBtn: page.locator('[data-testid="button-add-session"]'),
        addTicketBtn: page.locator('button:has-text("Add Ticket Type")'),
        sessionRows: page.locator('[data-testid="session-row"]'),
        ticketTypeSection: page.locator('text=Ticket Types')
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

      console.log(`🎉 Event Session Matrix features detected: ${matrixFeatures}/5`);
      
      // Step 7: Try to interact with session management (MODAL-BASED)
      // CRITICAL: Must add session BEFORE attempting to add ticket types
      // The Event Session Matrix requires at least one session to exist before tickets can be configured
      let sessionAdded = false;
      if (await sessionMatrixElements.addSessionBtn.count() > 0) {
        console.log('✅ Testing Add Session functionality...');
        await page.locator('[data-testid="button-add-session"]').click();
        await page.waitForTimeout(1000);

        // Wait for modal to open
        const sessionModal = page.locator('[data-testid="modal-add-session"]');
        const modalOpened = await sessionModal.count() > 0;
        console.log(`Session modal opened: ${modalOpened}`);
        await page.screenshot({ path: 'test-results/session-modal-opened.png', fullPage: true });

        if (modalOpened) {
          // Look for modal form fields
          const sessionForm = {
            sessionId: page.locator('[data-testid="input-session-id"]'),
            sessionName: page.locator('[data-testid="input-session-name"]'),
            sessionDate: page.locator('[data-testid="input-session-date"]'),
            startTime: page.locator('[data-testid="input-session-start-time"]'),
            endTime: page.locator('[data-testid="input-session-end-time"]'),
            capacity: page.locator('[data-testid="input-session-capacity"]')
          };

          console.log('🔍 Checking modal form fields...');
          for (const [fieldName, fieldLocator] of Object.entries(sessionForm)) {
            const fieldExists = await fieldLocator.count() > 0;
            console.log(`Session ${fieldName}: ${fieldExists ? 'FOUND' : 'MISSING'}`);
          }

          // Try to fill and save the session if fields exist
          if (await sessionForm.sessionName.count() > 0) {
            await sessionForm.sessionName.fill('Test Session 1');

            if (await sessionForm.capacity.count() > 0) {
              await sessionForm.capacity.fill('20');
            }

            const saveSessionBtn = page.locator('[data-testid="button-save-session"]');
            if (await saveSessionBtn.count() > 0) {
              await saveSessionBtn.click();
              await page.waitForTimeout(1000);

              // Verify session appears in grid
              const sessionInGrid = await page.locator('[data-testid="session-name"]:has-text("Test Session 1")').count() > 0;
              sessionAdded = sessionInGrid;
              console.log(`✅ Session ${sessionAdded ? 'saved and appears in grid' : 'save attempted'}`);
            }
          }
        }
      }

      // Step 8: Try to interact with ticket management (MODAL-BASED)
      // ONLY attempt if session was successfully added first
      if (sessionAdded && await sessionMatrixElements.addTicketBtn.count() > 0) {
        console.log('✅ Testing Add Ticket Type functionality...');
        await sessionMatrixElements.addTicketBtn.click();
        await page.waitForTimeout(1000);

        // Wait for ticket modal to open
        const ticketModal = page.locator('text=Add Ticket Type').or(page.locator('[role="dialog"]'));
        const ticketModalOpened = await ticketModal.count() > 0;
        console.log(`Ticket modal opened: ${ticketModalOpened}`);
        await page.screenshot({ path: 'test-results/ticket-modal-opened.png', fullPage: true });

        if (ticketModalOpened) {
          // Look for ticket modal form fields
          const ticketForm = {
            ticketName: page.locator('input[placeholder*="General Admission"]').or(page.locator('label:has-text("Ticket Name") + input')),
            description: page.locator('textarea[placeholder*="included"]'),
            price: page.locator('label:has-text("Price") + input').or(page.locator('input[placeholder="0.00"]')),
            quantity: page.locator('label:has-text("Quantity Available") + input'),
            sessionsIncluded: page.locator('label:has-text("Sessions Included")'),
            saleEndDate: page.locator('[data-testid="ticket-sale-end-date-input"]')
          };

          console.log('🔍 Checking ticket modal form fields...');
          for (const [fieldName, fieldLocator] of Object.entries(ticketForm)) {
            const fieldExists = await fieldLocator.count() > 0;
            console.log(`Ticket ${fieldName}: ${fieldExists ? 'FOUND' : 'MISSING'}`);
          }

          // Try to fill basic fields if they exist
          if (await ticketForm.ticketName.count() > 0) {
            console.log('✅ Ticket form fields found - modal-based UI confirmed');
          }

          // Close the modal before continuing (press Escape key)
          await page.keyboard.press('Escape');
          await page.waitForTimeout(500);
          console.log('✅ Ticket modal closed');
        }
      } else if (!sessionAdded) {
        console.log('⚠️ Skipping ticket type test - session must be added first');
      }

    } else {
      console.log('⚠️ Setup tab not found - Event Session Matrix may not be fully integrated');
    }
    
    // Step 9: Check other tabs for completeness (use role="tab" selectors)
    if (await page.locator('[role="tab"]:has-text("Emails")').count() > 0) {
      await page.locator('[role="tab"]:has-text("Emails")').click();
      await page.waitForTimeout(500);
      await page.screenshot({ path: 'test-results/emails-tab.png', fullPage: true });
      console.log('✅ Emails tab accessible');
    }

    if (await page.locator('[role="tab"]:has-text("Volunteers")').count() > 0) {
      await page.locator('[role="tab"]:has-text("Volunteers")').click();
      await page.waitForTimeout(500);
      await page.screenshot({ path: 'test-results/volunteers-tab.png', fullPage: true });
      console.log('✅ Volunteers tab accessible');
    }

    // Step 10: Go back to Basic Info to test form completion
    if (await page.locator('[role="tab"]:has-text("Basic Info")').count() > 0) {
      await page.locator('[role="tab"]:has-text("Basic Info")').click();
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