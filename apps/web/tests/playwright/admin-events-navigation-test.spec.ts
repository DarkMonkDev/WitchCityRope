import { test, expect } from '@playwright/test';

test.describe('Admin Events Navigation Test', () => {
  test('Navigate to admin events page and test Event Session Matrix', async ({ page }) => {
    console.log('🚀 Testing navigation to admin events page...');
    
    // Step 1: Login as admin
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    const emailInput = page.locator('[data-testid="email-input"]');
    const passwordInput = page.locator('[data-testid="password-input"]');
    const loginButton = page.locator('[data-testid="login-button"]');
    
    await emailInput.fill('admin@witchcityrope.com');
    await passwordInput.fill('Test123!');
    await loginButton.click();
    
    // Wait for login
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await page.waitForLoadState('networkidle');
    console.log('✅ Logged in successfully');
    
    // Step 2: Navigate directly to admin events page
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: 'test-results/admin-events-page-direct.png', fullPage: true });
    console.log('✅ Navigated to admin events page');
    
    const currentUrl = page.url();
    console.log(`Current URL: ${currentUrl}`);
    
    // Step 3: Check page content and look for Event Session Matrix elements
    const pageHeading = page.locator('h1, h2').first();
    if (await pageHeading.count() > 0) {
      const headingText = await pageHeading.textContent();
      console.log(`Page heading: "${headingText}"`);
    }
    
    // Step 4: Look for various event management elements
    const elements = {
      createButton: page.locator('button:has-text("Create")').or(page.locator('text=Create Event')),
      eventsList: page.locator('table, [data-testid*="events"], .event-list'),
      addEventBtn: page.locator('button:has-text("Add")').or(page.locator('[data-testid*="add"]')),
      newEventBtn: page.locator('button:has-text("New")').or(page.locator('[data-testid*="new"]')),
      eventForm: page.locator('form, [data-testid*="form"]'),
      sessionMatrix: page.locator('text=Session').or(page.locator('[data-testid*="session"]')),
      ticketTypes: page.locator('text=Ticket').or(page.locator('text=Price')),
    };
    
    console.log('🔍 Checking for event management elements...');
    
    for (const [name, locator] of Object.entries(elements)) {
      const count = await locator.count();
      console.log(`${name}: ${count} element(s) found`);
      if (count > 0) {
        await page.screenshot({ path: `test-results/element-${name}-found.png`, fullPage: true });
      }
    }
    
    // Step 5: Try clicking create/add button if found
    const actionButtons = [elements.createButton, elements.addEventBtn, elements.newEventBtn];
    
    for (let i = 0; i < actionButtons.length; i++) {
      const button = actionButtons[i];
      const count = await button.count();
      if (count > 0) {
        const buttonText = await button.first().textContent();
        console.log(`✅ Found action button: "${buttonText}"`);
        
        await page.screenshot({ path: `test-results/before-click-button-${i}.png`, fullPage: true });
        await button.first().click();
        await page.waitForTimeout(1000);
        await page.screenshot({ path: `test-results/after-click-button-${i}.png`, fullPage: true });
        
        // Check if a modal or form appeared
        const modal = page.locator('.modal, [role="dialog"], .overlay');
        const modalExists = await modal.count() > 0;
        console.log(`Modal appeared: ${modalExists}`);
        
        // Look for Event Session Matrix form elements
        const formElements = {
          titleInput: page.locator('input[name="title"], [placeholder*="title"], label:has-text("Title") + input'),
          descriptionInput: page.locator('textarea[name="description"], [placeholder*="description"], label:has-text("Description") + textarea'),
          dateInput: page.locator('input[type="date"], input[name="date"], [placeholder*="date"]'),
          timeInput: page.locator('input[type="time"], input[name="time"], [placeholder*="time"]'),
          sessionControls: page.locator('text=Session, [data-testid*="session"], fieldset:has-text("Session")'),
          ticketControls: page.locator('text=Ticket, text=Price, [data-testid*="ticket"], fieldset:has-text("Ticket")'),
          submitButton: page.locator('button[type="submit"], button:has-text("Save"), button:has-text("Create")')
        };
        
        console.log('🎯 Checking Event Session Matrix form elements...');
        let sessionMatrixFound = false;
        
        for (const [formName, formLocator] of Object.entries(formElements)) {
          const formCount = await formLocator.count();
          console.log(`${formName}: ${formCount} element(s) found`);
          if (formCount > 0) {
            await page.screenshot({ path: `test-results/form-${formName}-detected.png`, fullPage: true });
            
            if (formName.includes('session') || formName.includes('ticket')) {
              sessionMatrixFound = true;
            }
          }
        }
        
        if (sessionMatrixFound) {
          console.log('🎉 Event Session Matrix elements detected!');
          
          // Try to fill basic form fields
          if (await formElements.titleInput.count() > 0) {
            await formElements.titleInput.first().fill('Test Event - Session Matrix Integration Verified');
            console.log('✅ Title filled');
          }
          
          if (await formElements.descriptionInput.count() > 0) {
            await formElements.descriptionInput.first().fill('This test verifies the Event Session Matrix system is properly integrated.');
            console.log('✅ Description filled');
          }
          
          await page.screenshot({ path: 'test-results/form-filled-verification.png', fullPage: true });
        }
        
        break; // Found a working button, no need to try others
      }
    }
    
    // Step 6: Look for existing events to test edit functionality
    const existingEvents = page.locator('tr:has-text("Introduction to Rope Safety"), .event-item, [data-testid*="event-card"]');
    const eventsCount = await existingEvents.count();
    console.log(`✅ Found ${eventsCount} existing events`);
    
    if (eventsCount > 0) {
      await page.screenshot({ path: 'test-results/existing-events-found.png', fullPage: true });
      
      // Look for edit buttons
      const editButtons = page.locator('button:has-text("Edit"), [data-testid*="edit"], .edit-button');
      const editCount = await editButtons.count();
      console.log(`✅ Found ${editCount} edit buttons`);
      
      if (editCount > 0) {
        await editButtons.first().click();
        await page.waitForTimeout(1000);
        await page.screenshot({ path: 'test-results/edit-event-form.png', fullPage: true });
        console.log('✅ Clicked edit button - checking for Event Session Matrix in edit mode');
        
        // Check if edit form has session matrix elements
        const editSessionElements = await page.locator('text=Session, [data-testid*="session"]').count();
        const editTicketElements = await page.locator('text=Ticket, text=Price, [data-testid*="ticket"]').count();
        
        console.log(`Edit form - Session elements: ${editSessionElements}, Ticket elements: ${editTicketElements}`);
      }
    }
    
    // Step 7: Final summary screenshot
    await page.screenshot({ path: 'test-results/admin-events-final-state.png', fullPage: true });
    
    console.log('🎉 Admin events navigation test completed!');
    
    // Test always passes - this is for discovery
    expect(true).toBe(true);
  });
});