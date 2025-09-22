import { test, expect } from '@playwright/test';

test.describe('Vetting Form - Authenticated Testing', () => {
  test('Should access vetting form after login and test new fields', async ({ page }) => {
    console.log('🔍 Testing vetting form with authentication...');

    // First navigate to home page and login
    await page.goto('http://localhost:5173/');
    await page.waitForLoadState('networkidle');

    // Click LOGIN button
    await page.locator('[data-testid="login-button"]').click();

    // Fill login form with a user that hasn't been vetted yet
    await page.locator('[data-testid="email-input"]').fill('member@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');

    // Submit login
    await page.locator('[data-testid="sign-in-button"]').click();

    // Wait for navigation
    await page.waitForLoadState('networkidle');
    console.log('✅ Successfully logged in');

    // Now navigate to join route
    await page.goto('http://localhost:5173/join');
    await page.waitForLoadState('networkidle');

    // Take screenshot to see the actual form
    await page.screenshot({ path: 'test-results/vetting-form-authenticated.png', fullPage: true });

    // Check if we can see the actual form fields now
    console.log('📋 Checking for form fields...');

    // Look for the form
    const form = page.locator('form');
    const formCount = await form.count();
    console.log(`📝 Number of forms found: ${formCount}`);

    if (formCount > 0) {
      // Test specific field selectors
      const realNameField = page.locator('input[name="realName"], [data-testid="real-name"]');
      const pronounsField = page.locator('input[name="pronouns"], [data-testid="pronouns"]');
      const fetlifeField = page.locator('input[name="fetlifeHandle"], [data-testid="fetlife-handle"]');
      const otherNamesField = page.locator('input[name="otherNames"], textarea[name="otherNames"], [data-testid="other-names"]');

      // Check if fields are visible
      const realNameVisible = await realNameField.isVisible();
      const pronounsVisible = await pronounsField.isVisible();
      const fetlifeVisible = await fetlifeField.isVisible();
      const otherNamesVisible = await otherNamesField.isVisible();

      console.log(`📝 Field visibility:`);
      console.log(`   - Real Name: ${realNameVisible}`);
      console.log(`   - Pronouns: ${pronounsVisible}`);
      console.log(`   - FetLife Handle: ${fetlifeVisible}`);
      console.log(`   - Other Names: ${otherNamesVisible}`);

      if (pronounsVisible && otherNamesVisible) {
        console.log('✅ NEW FIELDS FOUND AND VISIBLE!');

        // Test filling the new fields
        await pronounsField.fill('they/them');
        await otherNamesField.fill('Also known as: Community Member, Workshop Attendee');

        // Verify the values were set
        const pronounsValue = await pronounsField.inputValue();
        const otherNamesValue = await otherNamesField.inputValue();

        console.log(`📝 Pronouns field value: "${pronounsValue}"`);
        console.log(`📝 Other Names field value: "${otherNamesValue}"`);

        // Test field length limits
        const longPronouns = 'a'.repeat(60); // Over 50 character limit
        const longOtherNames = 'b'.repeat(600); // Over 500 character limit

        await pronounsField.fill(longPronouns);
        await otherNamesField.fill(longOtherNames);

        const truncatedPronouns = await pronounsField.inputValue();
        const truncatedOtherNames = await otherNamesField.inputValue();

        console.log(`📏 Pronouns length test: ${truncatedPronouns.length} chars (should be <= 50)`);
        console.log(`📏 Other Names length test: ${truncatedOtherNames.length} chars (should be <= 500)`);

        // Take screenshot with filled fields
        await page.screenshot({ path: 'test-results/vetting-form-filled-fields.png', fullPage: true });

        console.log('✅ Successfully tested new fields functionality');
      } else {
        console.log('❌ New fields not found or not visible');
      }
    } else {
      // Check if we're still seeing the login required message
      const bodyText = await page.textContent('body');
      const hasLoginRequired = bodyText?.includes('Login Required') || false;
      const hasAlreadyVetted = bodyText?.includes('already') && bodyText?.includes('vetted') || false;

      console.log(`📋 Page analysis:`);
      console.log(`   - Shows login required: ${hasLoginRequired}`);
      console.log(`   - Shows already vetted: ${hasAlreadyVetted}`);
      console.log(`   - Page content preview: ${bodyText?.substring(0, 300)}...`);
    }

    // Check current URL
    const currentUrl = page.url();
    console.log(`📍 Final URL: ${currentUrl}`);
  });

  test('Should test API communication with new fields', async ({ page }) => {
    console.log('🌐 Testing API communication with new fields...');

    // Monitor API requests
    const apiRequests: any[] = [];
    page.on('request', request => {
      if (request.url().includes('/api/') && request.method() === 'POST') {
        apiRequests.push({
          url: request.url(),
          method: request.method(),
          headers: Object.fromEntries(request.headers()),
          postData: request.postDataJSON()
        });
      }
    });

    // Login first
    await page.goto('http://localhost:5173/');
    await page.locator('[data-testid="login-button"]').click();
    await page.locator('[data-testid="email-input"]').fill('member@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    await page.locator('[data-testid="sign-in-button"]').click();
    await page.waitForLoadState('networkidle');

    // Navigate to join page
    await page.goto('http://localhost:5173/join');
    await page.waitForLoadState('networkidle');

    // If form is available, try to submit with new fields
    const formCount = await page.locator('form').count();
    if (formCount > 0) {
      console.log('📝 Form found, testing submission...');

      // Fill form including new fields
      const pronounsField = page.locator('input[name="pronouns"], [data-testid="pronouns"]');
      const otherNamesField = page.locator('input[name="otherNames"], textarea[name="otherNames"], [data-testid="other-names"]');

      if (await pronounsField.isVisible()) {
        await pronounsField.fill('she/her');
      }
      if (await otherNamesField.isVisible()) {
        await otherNamesField.fill('Also known as: API Test User, Integration Tester');
      }

      // Try to submit
      const submitButton = page.locator('button[type="submit"], input[type="submit"], [data-testid*="submit"]');
      if (await submitButton.isVisible()) {
        await submitButton.click();
        await page.waitForTimeout(3000);
      }
    }

    // Analyze captured API requests
    console.log(`📡 Captured ${apiRequests.length} API POST requests`);

    for (const request of apiRequests) {
      console.log(`📤 ${request.method} ${request.url}`);

      if (request.postData) {
        console.log(`📝 Request payload: ${JSON.stringify(request.postData, null, 2)}`);

        // Check specifically for new fields
        const hasPronouns = 'pronouns' in request.postData;
        const hasOtherNames = 'otherNames' in request.postData;

        console.log(`📋 New fields in API request:`);
        console.log(`   - Pronouns field: ${hasPronouns ? '✅ Present' : '❌ Missing'}`);
        console.log(`   - Other Names field: ${hasOtherNames ? '✅ Present' : '❌ Missing'}`);

        if (hasPronouns) {
          console.log(`   - Pronouns value: "${request.postData.pronouns}"`);
        }
        if (hasOtherNames) {
          console.log(`   - Other Names value: "${request.postData.otherNames}"`);
        }
      }
    }

    if (apiRequests.length === 0) {
      console.log('⚠️ No API POST requests captured - form may not have been submitted');
    }
  });
});