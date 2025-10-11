import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test.describe('Debug Save Button Regression - Field Clearing Investigation', () => {
  test('should NOT clear fields when Save button is clicked', async ({ page }) => {
    // Login as admin
    console.log('🔐 Logging in as admin...');
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to event that we KNOW has data
    const eventId = '64535b73-74c3-4b95-a1a9-2b2db70c3ba0';
    console.log(`📍 Navigating to event: ${eventId}`);
    await page.goto(`http://localhost:5173/admin/events/${eventId}`);

    // Wait for form to load
    console.log('⏳ Waiting for form to load...');
    await page.waitForSelector('[data-testid="event-form"]', { timeout: 10000 });

    // ==== CAPTURE INITIAL STATE ====
    console.log('\n=== INITIAL FIELD STATE (Before Manual Entry) ===');

    const titleBefore = await page.locator('input[placeholder="Enter event title"]').first().inputValue();
    const shortDescBefore = await page.locator('input[placeholder*="Brief description"]').first().inputValue();
    const venueBefore = await page.locator('input[placeholder*="Select venue"]').first().inputValue();

    console.log(`Title BEFORE: "${titleBefore}"`);
    console.log(`ShortDescription BEFORE: "${shortDescBefore}"`);
    console.log(`Venue BEFORE: "${venueBefore}"`);

    // ==== MANUALLY FILL SHORT DESCRIPTION ====
    console.log('\n=== MANUALLY FILLING SHORT DESCRIPTION ===');

    const shortDescInput = page.locator('input[placeholder*="Brief description"]').first();
    const testValue = 'This is a test short description for debugging';

    await shortDescInput.click();
    await shortDescInput.clear();
    await shortDescInput.fill(testValue);

    console.log(`✅ Filled shortDescription with: "${testValue}"`);

    // Verify it was filled
    const shortDescAfterFill = await shortDescInput.inputValue();
    console.log(`✅ Verified shortDescription value: "${shortDescAfterFill}"`);
    expect(shortDescAfterFill).toBe(testValue);

    // ==== TAKE SCREENSHOT BEFORE SAVE ====
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope/apps/web/test-results/before-save-button.png',
      fullPage: true
    });
    console.log('📸 Screenshot saved: before-save-button.png');

    // ==== CLICK SAVE BUTTON ====
    console.log('\n=== CLICKING SAVE BUTTON ===');

    // Find the save button
    const saveButton = page.locator('button:has-text("SAVE"), button:has-text("Save")').first();
    const saveButtonExists = await saveButton.count() > 0;

    if (!saveButtonExists) {
      console.log('❌ Save button not found! Available buttons:');
      const allButtons = await page.evaluate(() => {
        return Array.from(document.querySelectorAll('button')).map(btn => ({
          text: btn.textContent?.trim(),
          visible: btn.offsetParent !== null
        }));
      });
      console.log(JSON.stringify(allButtons, null, 2));
    } else {
      console.log('✅ Save button found, clicking...');

      // Monitor network requests during save
      const saveRequestPromise = page.waitForResponse(
        response => response.url().includes('/api/events') && response.request().method() === 'PUT',
        { timeout: 5000 }
      ).catch(() => null);

      await saveButton.click();
      console.log('✅ Save button clicked');

      // Wait a bit for any async operations
      await page.waitForTimeout(1000);

      const saveResponse = await saveRequestPromise;
      if (saveResponse) {
        console.log(`📡 Save API called: ${saveResponse.status()}`);
        const responseBody = await saveResponse.json().catch(() => null);
        if (responseBody) {
          console.log('📡 Save response:', JSON.stringify(responseBody, null, 2));
        }
      } else {
        console.log('⚠️  No save API request detected');
      }

      // ==== CAPTURE STATE AFTER SAVE ====
      console.log('\n=== FIELD STATE AFTER SAVE ===');

      const titleAfter = await page.locator('input[placeholder="Enter event title"]').first().inputValue();
      const shortDescAfter = await shortDescInput.inputValue();
      const venueAfter = await page.locator('input[placeholder*="Select venue"]').first().inputValue();

      console.log(`Title AFTER: "${titleAfter}"`);
      console.log(`ShortDescription AFTER: "${shortDescAfter}"`);
      console.log(`Venue AFTER: "${venueAfter}"`);

      // ==== TAKE SCREENSHOT AFTER SAVE ====
      await page.screenshot({
        path: '/home/chad/repos/witchcityrope/apps/web/test-results/after-save-button.png',
        fullPage: true
      });
      console.log('📸 Screenshot saved: after-save-button.png');

      // ==== REGRESSION CHECK ====
      console.log('\n=== REGRESSION ANALYSIS ===');

      const titleCleared = titleBefore !== '' && titleAfter === '';
      const shortDescCleared = shortDescAfterFill !== '' && shortDescAfter === '';
      const venueCleared = venueBefore !== '' && venueAfter === '';

      console.log(`🔍 Title cleared? ${titleCleared} (was: "${titleBefore}", now: "${titleAfter}")`);
      console.log(`🔍 ShortDescription cleared? ${shortDescCleared} (was: "${shortDescAfterFill}", now: "${shortDescAfter}")`);
      console.log(`🔍 Venue cleared? ${venueCleared} (was: "${venueBefore}", now: "${venueAfter}")`);

      if (shortDescCleared) {
        console.log('🚨 REGRESSION CONFIRMED: ShortDescription was cleared by Save button!');
        console.log('🚨 This is the bug reported by the user!');
      } else {
        console.log('✅ No regression: ShortDescription retained value after Save');
      }

      // ==== CHECK FORM VALIDATION STATE ====
      console.log('\n=== FORM STATE INVESTIGATION ===');

      const formState = await page.evaluate(() => {
        return {
          hasValidationErrors: !!document.querySelector('[data-testid*="error"], .error-message, .mantine-InputWrapper-error'),
          hasSuccessMessage: !!document.querySelector('[data-testid*="success"], .success-message'),
          formDisabled: !!document.querySelector('form[disabled]')
        };
      });

      console.log('Form State:', JSON.stringify(formState, null, 2));

      // This test documents the regression - we expect it might fail if regression exists
      if (shortDescCleared) {
        console.log('\n❌ TEST RESULT: REGRESSION DETECTED');
        console.log('   ShortDescription value was cleared by Save button');
        console.log('   Expected: Value should persist');
        console.log('   Actual: Field became empty');
      } else {
        console.log('\n✅ TEST RESULT: NO REGRESSION');
        console.log('   ShortDescription value persisted after Save');
      }
    }
  });

  test('should preserve manually entered data when navigating away and back', async ({ page }) => {
    // Login as admin
    console.log('🔐 Logging in as admin...');
    await AuthHelpers.loginAs(page, 'admin');

    // Navigate to event
    const eventId = '64535b73-74c3-4b95-a1a9-2b2db70c3ba0';
    console.log(`📍 Navigating to event: ${eventId}`);
    await page.goto(`http://localhost:5173/admin/events/${eventId}`);

    // Wait for form to load
    await page.waitForSelector('[data-testid="event-form"]', { timeout: 10000 });

    // Fill shortDescription with test data
    const testValue = 'Navigation test - this should persist after save';
    const shortDescInput = page.locator('input[placeholder*="Brief description"]').first();

    await shortDescInput.click();
    await shortDescInput.clear();
    await shortDescInput.fill(testValue);

    console.log(`✅ Filled shortDescription: "${testValue}"`);

    // Save the form
    const saveButton = page.locator('button:has-text("SAVE"), button:has-text("Save")').first();
    await saveButton.click();
    console.log('✅ Clicked save button');

    // Wait for save to complete
    await page.waitForTimeout(2000);

    // Navigate away
    console.log('🔄 Navigating away to dashboard...');
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');

    // Navigate back to the event
    console.log('🔄 Navigating back to event...');
    await page.goto(`http://localhost:5173/admin/events/${eventId}`);
    await page.waitForSelector('[data-testid="event-form"]', { timeout: 10000 });

    // Check if shortDescription was persisted
    const shortDescAfterReturn = await page.locator('input[placeholder*="Brief description"]').first().inputValue();

    console.log('\n=== PERSISTENCE CHECK ===');
    console.log(`Value before navigation: "${testValue}"`);
    console.log(`Value after return: "${shortDescAfterReturn}"`);

    if (shortDescAfterReturn === testValue) {
      console.log('✅ Data persisted correctly after save and navigation');
    } else if (shortDescAfterReturn === '') {
      console.log('❌ Data was NOT saved - field is empty after return');
      console.log('🚨 This confirms the Save button does NOT persist data');
    } else {
      console.log(`⚠️  Unexpected value: "${shortDescAfterReturn}"`);
    }
  });
});
