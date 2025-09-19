import { test, expect } from '@playwright/test';

/**
 * Specific test for Volunteers tab and Add Volunteer Position functionality
 */

test.describe('Volunteers Tab Test', () => {

  test('Check Volunteers tab for Add Volunteer Position button', async ({ page }) => {
    console.log('🧪 Testing Volunteers tab specifically');

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

      // Look for Volunteers tab using more specific selector
      const volunteersTab = page.locator('[data-testid="tab-volunteers"]');
      const volunteerTabExists = await volunteersTab.count() > 0;
      console.log(`📋 Volunteers tab exists: ${volunteerTabExists}`);

      if (volunteerTabExists) {
        await volunteersTab.click();
        await page.waitForTimeout(2000);

        await page.screenshot({ path: 'test-results/volunteers-tab-content.png', fullPage: true });

        // Look for any volunteer-related content
        const pageContent = await page.textContent('body');
        console.log('📋 Page content includes:');
        console.log(`   - "volunteer": ${pageContent?.toLowerCase().includes('volunteer')}`);
        console.log(`   - "position": ${pageContent?.toLowerCase().includes('position')}`);

        // Look for all buttons in the page
        const allButtons = page.locator('button');
        const buttonCount = await allButtons.count();
        console.log(`🔘 Total buttons on Volunteers tab: ${buttonCount}`);

        // Check each button text
        const buttonTexts: string[] = [];
        for (let i = 0; i < buttonCount; i++) {
          const buttonText = await allButtons.nth(i).textContent();
          if (buttonText) {
            buttonTexts.push(buttonText.trim());
          }
        }

        console.log('🔘 Button texts found:');
        buttonTexts.forEach((text, index) => {
          console.log(`   ${index + 1}. "${text}"`);
        });

        // Look for volunteer position related buttons
        const volunteerAddButtons = buttonTexts.filter(text =>
          text.toLowerCase().includes('add') &&
          (text.toLowerCase().includes('volunteer') || text.toLowerCase().includes('position'))
        );

        console.log(`➕ Volunteer add buttons found: ${volunteerAddButtons.length}`);
        volunteerAddButtons.forEach(text => console.log(`   - "${text}"`));

        // Check if there are any tables or lists for volunteer positions
        const tables = page.locator('table');
        const tableCount = await tables.count();
        console.log(`📋 Tables found: ${tableCount}`);

        const lists = page.locator('ul, ol');
        const listCount = await lists.count();
        console.log(`📋 Lists found: ${listCount}`);

        // If no specific volunteer add button, check if it might be in a different tab
        if (volunteerAddButtons.length === 0) {
          console.log('⚠️ No Add Volunteer Position button found in Volunteers tab');
          console.log('📋 Checking other tabs for volunteer functionality...');

          // Check other tabs
          const allTabs = page.locator('[role="tab"], .tab, [data-testid*="tab"]');
          const tabCount = await allTabs.count();
          console.log(`🏷️ Total tabs found: ${tabCount}`);

          for (let i = 0; i < tabCount; i++) {
            const tabText = await allTabs.nth(i).textContent();
            console.log(`   Tab ${i + 1}: "${tabText}"`);
          }
        }

      } else {
        console.log('❌ Volunteers tab not found with data-testid="tab-volunteers"');

        // Try alternative selectors for volunteers tab
        const altVolunteersTab = page.locator('text=Volunteers').first();
        const altTabExists = await altVolunteersTab.count() > 0;
        console.log(`📋 Alternative Volunteers tab found: ${altTabExists}`);

        if (altTabExists) {
          console.log('🔄 Trying alternative Volunteers tab...');
          await altVolunteersTab.click();
          await page.waitForTimeout(2000);

          await page.screenshot({ path: 'test-results/volunteers-tab-alternative.png', fullPage: true });
        }
      }

      // Final comprehensive screenshot
      await page.screenshot({ path: 'test-results/volunteers-investigation-final.png', fullPage: true });
    }

    console.log('✅ Volunteers tab investigation completed');
  });

});