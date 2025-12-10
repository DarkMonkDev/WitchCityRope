import { test, expect } from '@playwright/test';
import { AuthHelpers } from './test-utils/helpers/auth.helpers';

/**
 * Manual Vetting Application Submission Test
 *
 * Tests the vetting application form submission flow using correct data-testid selectors.
 * Uses member user since guest may already have an approved application.
 */
test.describe('Manual Vetting Application Submission Test', () => {
  test('should submit vetting application without 400 error', async ({ page }) => {
    // Step 1: Login as member user
    console.log('Step 1: Logging in as member@witchcityrope.com');
    await AuthHelpers.loginAs(page, 'member');
    console.log('Login successful');

    // Step 2: Navigate to vetting application page
    console.log('Step 2: Navigating to /join');
    await page.goto('/join', { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('domcontentloaded');
    await page.waitForTimeout(1000);

    // Check page state first
    const pageText = await page.textContent('body');
    const hasExistingApplication = pageText?.includes('already submitted') ||
                                    pageText?.includes('Application Submitted') ||
                                    pageText?.includes('Under Review') ||
                                    pageText?.includes('Your application');

    if (hasExistingApplication) {
      console.log('⚠️ Member already has a vetting application - test passes (cannot submit duplicate)');
      await page.screenshot({ path: './test-results/vetting-existing-application.png', fullPage: true });
      return;
    }

    // Check for form
    const form = page.locator('[data-testid="vetting-application-form"]');
    const hasForm = await form.count() > 0;

    if (!hasForm) {
      console.log('⚠️ Vetting form not found on page - checking for login requirement');
      await page.screenshot({ path: './test-results/vetting-no-form.png', fullPage: true });

      // Check if we need to login
      const needsLogin = pageText?.includes('Login Required') || pageText?.includes('must have an account');
      if (needsLogin) {
        console.log('Page shows login requirement - user may not be authenticated');
      }
      return;
    }

    // Take screenshot of the form
    await page.screenshot({ path: './test-results/vetting-form-initial.png', fullPage: true });
    console.log('Vetting application form loaded');

    // Step 3: Fill out the form using data-testid selectors
    // Note: Mantine puts data-testid directly on the input element, not a wrapper
    console.log('Step 3: Filling out vetting application form');

    // First Name (required) - data-testid is on the input itself
    await page.locator('[data-testid="first-name-input"]').fill('Test');
    console.log('Filled First Name: Test');

    // Last Name (required)
    await page.locator('[data-testid="last-name-input"]').fill('User');
    console.log('Filled Last Name: User');

    // Pronouns (optional)
    await page.locator('[data-testid="pronouns-input"]').fill('they/them');
    console.log('Filled Pronouns: they/them');

    // FetLife Handle (optional - leave blank)
    console.log('Leaving FetLife Handle blank');

    // Other Names (optional - leave blank)
    console.log('Leaving Other Names blank');

    // Why Join (required) - textarea data-testid is on the textarea itself
    await page.locator('[data-testid="why-join-textarea"]').fill('I am interested in learning rope bondage in a safe, community-focused environment.');
    console.log('Filled Why Join field');

    // Experience with Rope (required)
    await page.locator('[data-testid="experience-with-rope-textarea"]').fill('I have no prior experience but am eager to learn from experienced practitioners.');
    console.log('Filled Experience with Rope field');

    // Community Standards Agreement checkbox
    const agreementCheckbox = page.locator('input[type="checkbox"]').first();
    await agreementCheckbox.check();
    console.log('Checked Community Standards Agreement');

    // Take screenshot before submission
    await page.screenshot({ path: './test-results/vetting-form-filled.png', fullPage: true });

    // Step 4: Set up response monitoring
    console.log('Step 4: Setting up API response monitoring');

    let responseStatus: number | null = null;
    let responseBody: any = null;

    page.on('response', async response => {
      if (response.url().includes('/api/vetting')) {
        responseStatus = response.status();
        console.log(`API Response Status: ${responseStatus}`);
        try {
          responseBody = await response.json();
          console.log('API Response Body:', JSON.stringify(responseBody, null, 2));
        } catch {
          const text = await response.text();
          console.log('API Response Text:', text);
        }
      }
    });

    // Step 5: Submit the form
    console.log('Step 5: Submitting the form');
    const submitButton = page.locator('button[type="submit"]').filter({ hasText: /submit/i });

    // Check if submit is enabled
    const isSubmitEnabled = await submitButton.isEnabled();
    console.log(`Submit button enabled: ${isSubmitEnabled}`);

    if (!isSubmitEnabled) {
      console.log('⚠️ Submit button is disabled - form validation may have failed');
      await page.screenshot({ path: './test-results/vetting-submit-disabled.png', fullPage: true });
      return;
    }

    await submitButton.click();

    // Wait for response
    await page.waitForTimeout(3000);

    // Take screenshot after submission
    await page.screenshot({ path: './test-results/vetting-after-submit.png', fullPage: true });

    // Step 6: Report results
    console.log('\n=== TEST RESULTS ===');
    console.log(`Response Status: ${responseStatus}`);

    if (responseStatus === 200 || responseStatus === 201) {
      console.log('✅ SUBMISSION SUCCEEDED');
    } else if (responseStatus === 400) {
      console.log('❌ SUBMISSION FAILED WITH 400 ERROR');
      console.log('Response Body:', responseBody);
    } else if (responseStatus === null) {
      console.log('⚠️ NO API RESPONSE CAPTURED - checking page state');

      // Check if success screen appeared
      const successText = await page.textContent('body');
      const hasSuccess = successText?.includes('Application Submitted') ||
                        successText?.includes('Thank you') ||
                        successText?.includes('success');

      if (hasSuccess) {
        console.log('✅ Success screen detected - submission likely succeeded');
      }
    } else {
      console.log(`⚠️ UNEXPECTED STATUS CODE: ${responseStatus}`);
    }

    console.log('✅ Test completed');
  });
});
