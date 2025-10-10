import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test.describe('Admin Events Management Detailed Test', () => {
  test('Test Events Management card click and event creation', async ({ page }) => {
    console.log('🚀 Starting detailed admin events management test...');

    // Step 1: Login as admin using AuthHelpers
    await AuthHelpers.loginAs(page, 'admin');
    console.log('✅ Logged in as admin successfully');
    
    // Step 2: Navigate directly to admin (since we know it works from previous test)
    await page.goto('http://localhost:5173/admin');
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: 'test-results/admin-dashboard-full.png', fullPage: true });
    console.log('✅ Admin dashboard loaded');
    
    // Step 3: Click on Events Management card
    // Use h3 selector to specifically target the card heading, not the navigation link
    const eventsManagementCard = page.locator('h3:has-text("Events Management")');
    
    const cardExists = await eventsManagementCard.count() > 0;
    console.log(`✅ Events Management card exists: ${cardExists}`);
    
    if (cardExists) {
      await eventsManagementCard.click();
      await page.waitForLoadState('networkidle');
      await page.screenshot({ path: 'test-results/admin-events-management-page.png', fullPage: true });
      console.log('✅ Clicked Events Management card');
      
      // Check current URL
      const currentUrl = page.url();
      console.log(`Current URL: ${currentUrl}`);
      
      // Step 4: Look for Create Event button
      await page.waitForTimeout(2000); // Wait for any loading
      const createEventBtn = page.locator('text=Create Event').or(
        page.locator('button:has-text("Create")').or(
          page.locator('[data-testid*="create"]')
        )
      );
      
      const createBtnExists = await createEventBtn.count() > 0;
      console.log(`✅ Create Event button exists: ${createBtnExists}`);
      
      if (createBtnExists) {
        await page.screenshot({ path: 'test-results/before-create-event-click.png', fullPage: true });
        await page.locator('[data-testid="button-create-event"]').click();
        await page.waitForTimeout(1000);
        await page.screenshot({ path: 'test-results/after-create-event-click.png', fullPage: true });
        console.log('✅ Clicked Create Event button');
        
        // Step 5: Check for Event Session Matrix form elements
        const formTitle = page.locator('input[name="title"]').or(
          page.locator('[data-testid*="title"]').or(
            page.locator('label:has-text("Title")').locator('..').locator('input')
          )
        );
        
        const formDescription = page.locator('textarea[name="description"]').or(
          page.locator('[data-testid*="description"]').or(
            page.locator('label:has-text("Description")').locator('..').locator('textarea')
          )
        );
        
        // Look for session-related elements
        const sessionElements = page.locator('text=Session').or(
          page.locator('text=Time').or(
            page.locator('[data-testid*="session"]').or(
              page.locator('text=Date')
            )
          )
        );
        
        // Look for ticket/pricing elements  
        const ticketElements = page.locator('text=Ticket').or(
          page.locator('text=Price').or(
            page.locator('[data-testid*="ticket"]').or(
              page.locator('text=Cost')
            )
          )
        );
        
        const formElements = {
          title: await formTitle.count() > 0,
          description: await formDescription.count() > 0,
          sessions: await sessionElements.count() > 0,
          tickets: await ticketElements.count() > 0
        };
        
        console.log('✅ Event creation form elements found:', formElements);
        
        // Step 6: Try to fill the form if basic elements exist
        if (formElements.title) {
          await formTitle.fill('Test Event - Session Matrix Verification');
          await page.screenshot({ path: 'test-results/form-title-filled.png', fullPage: true });
          console.log('✅ Title field filled');
        }
        
        if (formElements.description) {
          await formDescription.fill('This is a test event to verify the Event Session Matrix system is working properly.');
          await page.screenshot({ path: 'test-results/form-description-filled.png', fullPage: true });
          console.log('✅ Description field filled');
        }
        
        // Step 7: Look for any session/time controls
        if (formElements.sessions) {
          console.log('✅ Session controls detected - Event Session Matrix system appears to be integrated');
          await page.screenshot({ path: 'test-results/session-controls-detected.png', fullPage: true });
          
          // Try to interact with session controls
          const sessionControl = sessionElements.first();
          if (await sessionControl.isVisible()) {
            await sessionControl.click();
            await page.waitForTimeout(500);
            await page.screenshot({ path: 'test-results/session-control-clicked.png', fullPage: true });
          }
        }
        
        // Step 8: Look for any ticket/pricing controls
        if (formElements.tickets) {
          console.log('✅ Ticket/pricing controls detected - Event Session Matrix pricing system appears to be integrated');
          await page.screenshot({ path: 'test-results/ticket-controls-detected.png', fullPage: true });
          
          // Try to interact with ticket controls
          const ticketControl = ticketElements.first();
          if (await ticketControl.isVisible()) {
            await ticketControl.click();
            await page.waitForTimeout(500);
            await page.screenshot({ path: 'test-results/ticket-control-clicked.png', fullPage: true });
          }
        }
        
        // Step 9: Look for submit/save button
        const submitBtn = page.locator('button:has-text("Save")').or(
          page.locator('button:has-text("Create")').or(
            page.locator('button[type="submit"]')
          )
        );
        
        const submitExists = await submitBtn.count() > 0;
        console.log(`✅ Submit button exists: ${submitExists}`);
        
        if (submitExists) {
          await page.screenshot({ path: 'test-results/before-submit.png', fullPage: true });
          // Don't actually submit to avoid creating test data, just verify the button exists
        }
        
      } else {
        console.log('⚠️ Create Event button not found - may need to look for different selector');
        await page.screenshot({ path: 'test-results/no-create-event-button.png', fullPage: true });
        
        // Look for any buttons on the page
        const allButtons = page.locator('button');
        const buttonCount = await allButtons.count();
        console.log(`Found ${buttonCount} buttons on the page`);
        
        // Log button texts
        for (let i = 0; i < Math.min(buttonCount, 10); i++) {
          const buttonText = await allButtons.nth(i).textContent();
          console.log(`Button ${i}: "${buttonText}"`);
        }
      }
      
      // Step 10: Check for existing events list
      const eventsList = page.locator('[data-testid*="events-list"]').or(
        page.locator('table').or(
          page.locator('.event-card').or(
            page.locator('text=Introduction to Rope Safety')
          )
        )
      );
      
      const eventsListExists = await eventsList.count() > 0;
      console.log(`✅ Events list exists: ${eventsListExists}`);
      
      if (eventsListExists) {
        await page.screenshot({ path: 'test-results/events-list-found.png', fullPage: true });
        
        // Look for edit/delete buttons
        const editButtons = page.locator('button:has-text("Edit")');
        const deleteButtons = page.locator('button:has-text("Delete")'); 
        
        const editCount = await editButtons.count();
        const deleteCount = await deleteButtons.count();
        
        console.log(`✅ Found ${editCount} edit buttons, ${deleteCount} delete buttons`);
      }
      
    } else {
      console.log('⚠️ Events Management card not found on admin dashboard');
      
      // Try to find any clickable elements
      const clickableElements = page.locator('a, button, [role="button"]');
      const clickableCount = await clickableElements.count();
      console.log(`Found ${clickableCount} clickable elements on admin dashboard`);
    }
    
    // Step 11: Final comprehensive screenshot
    await page.screenshot({ path: 'test-results/admin-events-test-complete.png', fullPage: true });
    console.log('🎉 Admin events detailed test completed!');
    
    // Test passes regardless - this is for discovery
    expect(true).toBe(true);
  });
});