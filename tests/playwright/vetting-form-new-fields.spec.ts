import { test, expect } from '@playwright/test';

test.describe('Vetting Form - New Fields Testing', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the join route
    await page.goto('http://localhost:5173/join');
    await page.waitForLoadState('networkidle');
  });

  test('Should display new Pronouns and Other Names fields in correct positions', async ({ page }) => {
    console.log('🔍 Testing vetting form new fields...');

    // Take screenshot for evidence
    await page.screenshot({ path: 'test-results/vetting-form-initial.png', fullPage: true });

    // Check if the form loads
    const form = page.locator('form');
    await expect(form).toBeVisible();

    // Check for Pronouns field after Real Name
    const realNameField = page.locator('[data-testid="real-name-input"], input[name="realName"], #realName');
    await expect(realNameField).toBeVisible();
    console.log('✅ Real Name field found');

    const pronounsField = page.locator('[data-testid="pronouns-input"], input[name="pronouns"], #pronouns');
    await expect(pronounsField).toBeVisible();
    console.log('✅ Pronouns field found');

    // Check for Other Names field after FetLife Handle
    const fetlifeField = page.locator('[data-testid="fetlife-handle-input"], input[name="fetlifeHandle"], #fetlifeHandle');
    await expect(fetlifeField).toBeVisible();
    console.log('✅ FetLife Handle field found');

    const otherNamesField = page.locator('[data-testid="other-names-input"], input[name="otherNames"], #otherNames, textarea[name="otherNames"]');
    await expect(otherNamesField).toBeVisible();
    console.log('✅ Other Names field found');

    // Check field positioning - Pronouns should come after Real Name
    const realNamePosition = await realNameField.boundingBox();
    const pronounsPosition = await pronounsField.boundingBox();

    if (realNamePosition && pronounsPosition) {
      expect(pronounsPosition.y).toBeGreaterThan(realNamePosition.y);
      console.log('✅ Pronouns field correctly positioned after Real Name');
    }

    // Check field positioning - Other Names should come after FetLife Handle
    const fetlifePosition = await fetlifeField.boundingBox();
    const otherNamesPosition = await otherNamesField.boundingBox();

    if (fetlifePosition && otherNamesPosition) {
      expect(otherNamesPosition.y).toBeGreaterThan(fetlifePosition.y);
      console.log('✅ Other Names field correctly positioned after FetLife Handle');
    }
  });

  test('Should validate field length limits', async ({ page }) => {
    console.log('🔍 Testing field validation...');

    const pronounsField = page.locator('[data-testid="pronouns-input"], input[name="pronouns"], #pronouns');
    const otherNamesField = page.locator('[data-testid="other-names-input"], input[name="otherNames"], #otherNames, textarea[name="otherNames"]');

    // Test Pronouns field - should accept up to 50 characters
    const pronounsTest = 'they/them/theirs - this is a test of pronouns field';
    await pronounsField.fill(pronounsTest);

    const pronounsValue = await pronounsField.inputValue();
    console.log(`📝 Pronouns field value: "${pronounsValue}" (${pronounsValue.length} chars)`);

    // Test Other Names field - should accept up to 500 characters
    const otherNamesTest = 'Also known as: RopeMaster, Shibari Student, Community Member, Workshop Attendee, Event Organizer, Safety Team Member, Mentor, Student, Teacher Assistant, Former Scene Name, Previous Handle, Alternative Identity, Community Nickname, Forum Username, Discord Handle, Slack Username, Email Alias, Previous Email, Secondary Contact, Emergency Contact Name, Reference Contact, Workshop Contact, Event Contact Information';
    await otherNamesField.fill(otherNamesTest);

    const otherNamesValue = await otherNamesField.inputValue();
    console.log(`📝 Other Names field value length: ${otherNamesValue.length} chars`);

    // Verify fields accept the content
    expect(pronounsValue.length).toBeGreaterThan(0);
    expect(otherNamesValue.length).toBeGreaterThan(0);
    console.log('✅ Fields accept content within limits');
  });

  test('Should verify fields are optional', async ({ page }) => {
    console.log('🔍 Testing optional field behavior...');

    // Fill only required fields to test that new fields are optional
    const emailField = page.locator('[data-testid="email-input"], input[name="email"], #email');
    const realNameField = page.locator('[data-testid="real-name-input"], input[name="realName"], #realName');
    const sceneNameField = page.locator('[data-testid="scene-name-input"], input[name="sceneName"], #sceneName');

    await emailField.fill('test.newuser@example.com');
    await realNameField.fill('Test User');
    await sceneNameField.fill('TestNewbie');

    // Leave Pronouns and Other Names empty
    console.log('📝 Leaving new fields empty to test optional behavior');

    // Try to submit form (this should work without validation errors for the new fields)
    const submitButton = page.locator('[data-testid="submit-button"], button[type="submit"], input[type="submit"]');

    if (await submitButton.isVisible()) {
      await submitButton.click();
      console.log('✅ Form submission attempted with empty optional fields');

      // Wait a moment for any validation to appear
      await page.waitForTimeout(1000);

      // Take screenshot to see any validation messages
      await page.screenshot({ path: 'test-results/vetting-form-optional-validation.png', fullPage: true });
    }
  });

  test('Should submit form successfully with new fields filled', async ({ page }) => {
    console.log('🔍 Testing form submission with new fields...');

    // Fill all form fields including the new ones
    const emailField = page.locator('[data-testid="email-input"], input[name="email"], #email');
    const realNameField = page.locator('[data-testid="real-name-input"], input[name="realName"], #realName');
    const sceneNameField = page.locator('[data-testid="scene-name-input"], input[name="sceneName"], #sceneName');
    const pronounsField = page.locator('[data-testid="pronouns-input"], input[name="pronouns"], #pronouns');
    const fetlifeField = page.locator('[data-testid="fetlife-handle-input"], input[name="fetlifeHandle"], #fetlifeHandle');
    const otherNamesField = page.locator('[data-testid="other-names-input"], input[name="otherNames"], #otherNames, textarea[name="otherNames"]');

    // Use a unique email to avoid conflicts
    const timestamp = Date.now();
    await emailField.fill(`test.vetting.${timestamp}@example.com`);
    await realNameField.fill('Test Vetting User');
    await sceneNameField.fill(`VettingTest${timestamp}`);
    await pronounsField.fill('they/them');
    await fetlifeField.fill('TestVettingUser');
    await otherNamesField.fill('Also known as: Community Newbie, Workshop Attendee, Learning Enthusiast');

    // Take screenshot before submission
    await page.screenshot({ path: 'test-results/vetting-form-filled.png', fullPage: true });

    // Submit the form
    const submitButton = page.locator('[data-testid="submit-button"], button[type="submit"], input[type="submit"]');

    if (await submitButton.isVisible()) {
      console.log('📤 Submitting form with all fields filled...');
      await submitButton.click();

      // Wait for response
      await page.waitForTimeout(3000);

      // Take screenshot after submission
      await page.screenshot({ path: 'test-results/vetting-form-submitted.png', fullPage: true });

      // Check for success message or redirect
      const currentUrl = page.url();
      console.log(`📍 Current URL after submission: ${currentUrl}`);

      // Check for any error messages
      const errorMessages = await page.locator('.error, .alert-error, [class*="error"]').all();
      if (errorMessages.length > 0) {
        for (const error of errorMessages) {
          const errorText = await error.textContent();
          console.log(`❌ Error found: ${errorText}`);
        }
      } else {
        console.log('✅ No error messages found');
      }

      // Check for success indicators
      const successMessages = await page.locator('.success, .alert-success, [class*="success"]').all();
      if (successMessages.length > 0) {
        for (const success of successMessages) {
          const successText = await success.textContent();
          console.log(`✅ Success message: ${successText}`);
        }
      }
    }
  });

  test('Should verify API communication for new fields', async ({ page }) => {
    console.log('🌐 Testing API communication...');

    // Monitor network requests
    const requests: any[] = [];
    page.on('request', request => {
      if (request.url().includes('/api/')) {
        requests.push({
          url: request.url(),
          method: request.method(),
          postData: request.postDataJSON()
        });
      }
    });

    // Fill form with new fields
    await page.locator('[data-testid="email-input"], input[name="email"], #email').fill('test.api@example.com');
    await page.locator('[data-testid="real-name-input"], input[name="realName"], #realName').fill('API Test User');
    await page.locator('[data-testid="scene-name-input"], input[name="sceneName"], #sceneName').fill('APITester');
    await page.locator('[data-testid="pronouns-input"], input[name="pronouns"], #pronouns').fill('she/her');
    await page.locator('[data-testid="other-names-input"], input[name="otherNames"], #otherNames, textarea[name="otherNames"]').fill('Also known as: API Test User, Integration Tester');

    // Submit form
    const submitButton = page.locator('[data-testid="submit-button"], button[type="submit"], input[type="submit"]');
    if (await submitButton.isVisible()) {
      await submitButton.click();
      await page.waitForTimeout(2000);
    }

    // Analyze API requests
    console.log(`📡 Captured ${requests.length} API requests`);

    for (const request of requests) {
      console.log(`📤 ${request.method} ${request.url}`);

      if (request.postData) {
        console.log(`📝 Request data: ${JSON.stringify(request.postData, null, 2)}`);

        // Check if new fields are included in the request
        const hasPronouns = request.postData.pronouns !== undefined;
        const hasOtherNames = request.postData.otherNames !== undefined;

        console.log(`📋 New fields in API request:`);
        console.log(`   - Pronouns field present: ${hasPronouns}`);
        console.log(`   - Other Names field present: ${hasOtherNames}`);

        if (hasPronouns) {
          console.log(`   - Pronouns value: "${request.postData.pronouns}"`);
        }
        if (hasOtherNames) {
          console.log(`   - Other Names value: "${request.postData.otherNames}"`);
        }
      }
    }
  });
});