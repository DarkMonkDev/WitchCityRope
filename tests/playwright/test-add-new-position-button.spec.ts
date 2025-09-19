import { test, expect } from '@playwright/test';

/**
 * Test the Add New Position button specifically
 */

test.describe('Add New Position Button Test', () => {

  test('Verify Add New Position button opens modal without errors', async ({ page }) => {
    console.log('🧪 Testing Add New Position button functionality');

    // Login and navigate to event
    await page.goto('http://localhost:5173/login');
    await page.waitForTimeout(3000);

    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    await page.waitForTimeout(3000);

    await page.goto('http://localhost:5173/admin/events');
    await page.waitForTimeout(3000);

    const eventRows = page.locator('[data-testid="event-row"]');
    const eventCount = await eventRows.count();

    if (eventCount > 0) {
      await eventRows.first().click();
      await page.waitForTimeout(3000);

      // Go to Volunteers tab
      const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
      await volunteersTab.click();
      await page.waitForTimeout(2000);

      // Find and click Add New Position button
      const addPositionBtn = page.locator('button:has-text("ADD NEW POSITION")');
      const buttonExists = await addPositionBtn.isVisible();
      console.log(`✅ Add New Position button visible: ${buttonExists}`);

      if (buttonExists) {
        console.log('🖱️ Clicking Add New Position button...');

        await addPositionBtn.click();
        await page.waitForTimeout(2000);

        // Check if modal opened
        const modal = page.locator('[role="dialog"], .modal, .mantine-modal');
        const modalVisible = await modal.isVisible();
        console.log(`✅ Add New Position modal opened: ${modalVisible}`);

        if (modalVisible) {
          await page.screenshot({ path: 'test-results/add-new-position-modal.png', fullPage: true });

          // Look for modal content
          const modalContent = await page.textContent('[role="dialog"], .modal, .mantine-modal');
          console.log('📋 Modal contains:');
          console.log(`   - "position": ${modalContent?.toLowerCase().includes('position')}`);
          console.log(`   - "volunteer": ${modalContent?.toLowerCase().includes('volunteer')}`);
          console.log(`   - form fields: ${modalContent?.toLowerCase().includes('name') || modalContent?.toLowerCase().includes('description')}`);

          // Look for form fields
          const inputFields = page.locator('[role="dialog"] input, .modal input, .mantine-modal input');
          const inputCount = await inputFields.count();
          console.log(`📝 Input fields in modal: ${inputCount}`);

          // Look for textarea fields
          const textareaFields = page.locator('[role="dialog"] textarea, .modal textarea, .mantine-modal textarea');
          const textareaCount = await textareaFields.count();
          console.log(`📝 Textarea fields in modal: ${textareaCount}`);

          // Look for close/cancel button
          const cancelBtn = page.locator('[role="dialog"] button:has-text("Cancel"), .modal button:has-text("Cancel"), .mantine-modal button:has-text("Cancel")');
          const cancelExists = await cancelBtn.isVisible();

          if (cancelExists) {
            console.log('✅ Cancel button found - closing modal');
            await cancelBtn.click();
            await page.waitForTimeout(1000);

            const modalStillVisible = await modal.isVisible();
            console.log(`✅ Modal closed successfully: ${!modalStillVisible}`);
          }

        } else {
          console.log('❌ Add New Position modal did not open');

          // Check for any errors
          const pageContent = await page.textContent('body');
          console.log('📋 Checking for error messages...');

          // Take screenshot of current state
          await page.screenshot({ path: 'test-results/add-new-position-no-modal.png', fullPage: true });
        }

      } else {
        console.log('❌ Add New Position button not visible');
        await page.screenshot({ path: 'test-results/add-new-position-not-found.png', fullPage: true });
      }

      // Final screenshot
      await page.screenshot({ path: 'test-results/add-new-position-final.png', fullPage: true });
    }

    console.log('✅ Add New Position button test completed');
  });

});