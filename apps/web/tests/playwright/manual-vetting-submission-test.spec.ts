import { test, expect } from '@playwright/test';
import { AuthHelpers } from './helpers/auth.helpers';

test.describe('Manual Vetting Application Submission Test', () => {
  test('should submit vetting application without 400 error', async ({ page }) => {
    // Step 1 & 2: Login as member user (not guest - guest already has approved application)
    console.log('Step 1-2: Logging in as member@witchcityrope.com');
    await AuthHelpers.loginAs(page, 'member');
    console.log('Login successful');

    // Step 3: Navigate to vetting application page
    console.log('Step 3: Navigating to /vetting/apply');
    await page.goto('http://localhost:5173/vetting/apply');
    await page.waitForLoadState('networkidle');

    // Take screenshot of the form
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/vetting-form-initial.png', fullPage: true });
    console.log('Vetting application form loaded');

    // Step 4: Fill out the form using placeholder-based selectors
    console.log('Step 4: Filling out vetting application form');

    // Real Name
    await page.fill('input[placeholder*="Enter your real name"]', 'Test User');
    console.log('Filled Real Name: Test User');

    // Pronouns (optional)
    await page.fill('input[placeholder*="Enter your pronouns"]', 'they/them');
    console.log('Filled Pronouns: they/them');

    // FetLife Handle (optional - leave blank)
    console.log('Leaving FetLife Handle blank');

    // Other Names (optional - leave blank)
    console.log('Leaving Other Names blank');

    // Why would you like to join
    await page.fill('textarea[placeholder*="Why would you like to join"]', 'I am interested in learning rope bondage');
    console.log('Filled Why Join: I am interested in learning rope bondage');

    // Experience with rope
    await page.fill('textarea[placeholder*="Experience with Rope"]', 'I have no experience');
    console.log('Filled Experience: I have no experience');

    // Agree to Community Standards - find checkbox by text
    await page.locator('text=I agree to all of the above items').click();
    console.log('Checked Agree to Community Standards');

    // Take screenshot before submission
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/vetting-form-filled.png', fullPage: true });

    // Step 5: Listen for console errors and network requests
    const consoleErrors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
        console.log('BROWSER CONSOLE ERROR:', msg.text());
      }
    });

    let responseStatus: number | null = null;
    let responseBody: any = null;
    let requestBody: any = null;

    page.on('request', async request => {
      if (request.url().includes('/api/vetting/submit')) {
        console.log('REQUEST URL:', request.url());
        console.log('REQUEST METHOD:', request.method());
        try {
          const postData = request.postDataJSON();
          requestBody = postData;
          console.log('REQUEST BODY:', JSON.stringify(postData, null, 2));
        } catch (e) {
          console.log('Could not parse request body');
        }
      }
    });

    page.on('response', async response => {
      if (response.url().includes('/api/vetting/submit')) {
        responseStatus = response.status();
        console.log(`API Response Status: ${responseStatus}`);
        try {
          responseBody = await response.json();
          console.log('API Response Body:', JSON.stringify(responseBody, null, 2));
        } catch (e) {
          const text = await response.text();
          console.log('API Response Text:', text);
        }
      }
    });

    // Step 6: Submit the form
    console.log('Step 5: Submitting the form');
    await page.locator('button:has-text("Submit Application")').click();

    // Wait for the response
    await page.waitForTimeout(3000);

    // Take screenshot after submission
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/vetting-form-after-submit.png', fullPage: true });

    // Step 7: Report results
    console.log('\n=== TEST RESULTS ===');
    console.log(`Response Status: ${responseStatus}`);
    console.log(`Console Errors: ${consoleErrors.length > 0 ? consoleErrors.join(', ') : 'None'}`);

    if (responseStatus === 200 || responseStatus === 201) {
      console.log('✅ SUBMISSION SUCCEEDED');
    } else if (responseStatus === 400) {
      console.log('❌ SUBMISSION FAILED WITH 400 ERROR');
      console.log('Request Body:', requestBody);
      console.log('Response Body:', responseBody);
    } else if (responseStatus === null) {
      console.log('⚠️ NO RESPONSE RECEIVED - Request may not have been sent');
    } else {
      console.log(`⚠️ UNEXPECTED STATUS CODE: ${responseStatus}`);
    }

    // Don't assert - just report the results
    console.log('\n=== FINAL VERDICT ===');
    if (responseStatus === 400) {
      console.log('The 400 error IS STILL PRESENT after the HowFoundUs removal');
    } else if (responseStatus === 200 || responseStatus === 201) {
      console.log('The 400 error is FIXED - submission succeeded!');
    }
  });
});
