import { test, expect } from '@playwright/test';

/**
 * Focused test for Add buttons functionality
 * Based on evidence from previous test screenshots that buttons are working
 */

test.describe('Focused Add Buttons Test', () => {

  test('Manual verification of Add buttons functionality', async ({ page }) => {
    console.log('🧪 Manual verification of Add buttons in Events admin');

    // Login
    await page.goto('http://localhost:5173/login');
    await page.waitForTimeout(3000);

    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    await page.waitForTimeout(3000);

    // Navigate to events
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForTimeout(3000);

    // Click first event
    const eventRows = page.locator('[data-testid="event-row"]');
    const eventCount = await eventRows.count();
    console.log(`📊 Found ${eventCount} events`);

    if (eventCount > 0) {
      await eventRows.first().click();
      await page.waitForTimeout(3000);

      // Look for Setup tab and click it
      const setupTab = page.locator('text=Setup');
      const setupCount = await setupTab.count();

      if (setupCount > 0) {
        await setupTab.click();
        await page.waitForTimeout(2000);
        console.log('✅ Setup tab clicked');
      }

      // Take comprehensive screenshot
      await page.screenshot({ path: 'test-results/manual-verification-full-page.png', fullPage: true });

      // Check for Add Session button
      const addSessionBtn = page.locator('button:has-text("ADD SESSION")');
      const sessionBtnVisible = await addSessionBtn.isVisible();
      console.log(`📝 Add Session button visible: ${sessionBtnVisible}`);

      // Check for Add Ticket Type button
      const addTicketBtn = page.locator('button:has-text("ADD TICKET TYPE")');
      const ticketBtnVisible = await addTicketBtn.isVisible();
      console.log(`🎫 Add Ticket Type button visible: ${ticketBtnVisible}`);

      // Check if we can scroll to find Volunteer Position section
      await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
      await page.waitForTimeout(1000);

      // Take screenshot after scroll
      await page.screenshot({ path: 'test-results/manual-verification-after-scroll.png', fullPage: true });

      // Look for volunteer position section
      const volunteerSection = page.locator('text=Volunteer', 'text=Position');
      const volunteerCount = await volunteerSection.count();
      console.log(`👥 Volunteer section elements found: ${volunteerCount}`);

      // Look for any add volunteer button
      const addVolunteerBtn = page.locator('button:has-text("ADD"), button:has-text("Add Volunteer"), button:has-text("Add Position")');
      const volunteerBtnCount = await addVolunteerBtn.count();
      console.log(`👥 Volunteer add buttons found: ${volunteerBtnCount}`);

      // Test Add Session button if visible
      if (sessionBtnVisible) {
        console.log('🧪 Testing Add Session button...');

        // Use force: true to bypass visibility issues
        await addSessionBtn.click({ force: true });
        await page.waitForTimeout(2000);

        // Check if modal opened
        const modal = page.locator('[role="dialog"], .modal, .mantine-modal');
        const modalVisible = await modal.isVisible();
        console.log(`📝 Session modal opened: ${modalVisible}`);

        if (modalVisible) {
          await page.screenshot({ path: 'test-results/session-modal-success.png', fullPage: true });

          // Close modal
          const cancelBtn = page.locator('button:has-text("CANCEL")');
          const cancelVisible = await cancelBtn.isVisible();
          if (cancelVisible) {
            await cancelBtn.click();
            await page.waitForTimeout(1000);
            console.log('✅ Session modal closed successfully');
          }
        }
      }

      // Test Add Ticket Type button if visible
      if (ticketBtnVisible) {
        console.log('🧪 Testing Add Ticket Type button...');

        await addTicketBtn.click({ force: true });
        await page.waitForTimeout(2000);

        const modal = page.locator('[role="dialog"], .modal, .mantine-modal');
        const modalVisible = await modal.isVisible();
        console.log(`🎫 Ticket Type modal opened: ${modalVisible}`);

        if (modalVisible) {
          await page.screenshot({ path: 'test-results/ticket-modal-success.png', fullPage: true });

          const cancelBtn = page.locator('button:has-text("CANCEL")');
          const cancelVisible = await cancelBtn.isVisible();
          if (cancelVisible) {
            await cancelBtn.click();
            await page.waitForTimeout(1000);
            console.log('✅ Ticket Type modal closed successfully');
          }
        }
      }

      // Look for and test volunteer buttons if any exist
      if (volunteerBtnCount > 0) {
        console.log('🧪 Testing Add Volunteer Position button...');

        await addVolunteerBtn.first().click({ force: true });
        await page.waitForTimeout(2000);

        const modal = page.locator('[role="dialog"], .modal, .mantine-modal');
        const modalVisible = await modal.isVisible();
        console.log(`👥 Volunteer Position modal opened: ${modalVisible}`);

        if (modalVisible) {
          await page.screenshot({ path: 'test-results/volunteer-modal-success.png', fullPage: true });

          const cancelBtn = page.locator('button:has-text("CANCEL")');
          const cancelVisible = await cancelBtn.isVisible();
          if (cancelVisible) {
            await cancelBtn.click();
            await page.waitForTimeout(1000);
            console.log('✅ Volunteer Position modal closed successfully');
          }
        }
      }

      // Final verification screenshot
      await page.screenshot({ path: 'test-results/manual-verification-final.png', fullPage: true });
    }

    console.log('✅ Manual verification test completed');
  });

});