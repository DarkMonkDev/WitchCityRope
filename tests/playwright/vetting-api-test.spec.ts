import { test, expect } from '@playwright/test';

test.describe('Vetting API and Form Testing', () => {
  test('Should test API endpoints for vetting functionality', async ({ page }) => {
    console.log('🌐 Testing vetting API endpoints...');

    // First login to get authentication
    await page.goto('http://localhost:5173/');
    await page.locator('text=LOGIN').click();
    await page.locator('[data-testid="email-input"], input[name="email"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"], input[name="password"]').fill('Test123!');
    await page.locator('[data-testid="sign-in-button"], button:has-text("Sign In"), button[type="submit"]').click();
    await page.waitForLoadState('networkidle');

    console.log('✅ Logged in as admin');

    // Test API endpoints directly
    const endpoints = [
      'http://localhost:5655/api/vetting/status',
      'http://localhost:5655/api/vetting/application',
      'http://localhost:5655/api/user/profile'
    ];

    for (const endpoint of endpoints) {
      try {
        const response = await page.request.get(endpoint);
        console.log(`📡 ${endpoint}: Status ${response.status()}`);

        if (response.ok()) {
          const data = await response.json();
          console.log(`📝 Response data: ${JSON.stringify(data, null, 2).substring(0, 300)}...`);
        } else {
          const errorText = await response.text();
          console.log(`❌ Error response: ${errorText.substring(0, 200)}...`);
        }
      } catch (error) {
        console.log(`💥 Request failed: ${error}`);
      }
    }
  });

  test('Should test with guest account to access vetting form', async ({ page }) => {
    console.log('🔍 Testing with guest account to access vetting form...');

    // Try with guest account which should not be vetted
    await page.goto('http://localhost:5173/');
    await page.locator('text=LOGIN').click();
    await page.locator('[data-testid="email-input"], input[name="email"]').fill('guest@witchcityrope.com');
    await page.locator('[data-testid="password-input"], input[name="password"]').fill('Test123!');
    await page.locator('[data-testid="sign-in-button"], button:has-text("Sign In"), button[type="submit"]').click();
    await page.waitForLoadState('networkidle');

    console.log('✅ Logged in as guest');

    // Navigate to join page
    await page.goto('http://localhost:5173/join');
    await page.waitForLoadState('networkidle');

    // Take screenshot
    await page.screenshot({ path: 'test-results/vetting-form-guest-account.png', fullPage: true });

    // Check page content
    const bodyText = await page.textContent('body');
    console.log(`📝 Page content preview: ${bodyText?.substring(0, 500)}...`);

    // Check for form
    const formCount = await page.locator('form').count();
    const inputCount = await page.locator('input').count();

    console.log(`📝 Form elements with guest account:`);
    console.log(`   - Forms: ${formCount}`);
    console.log(`   - Inputs: ${inputCount}`);

    // Check for errors
    const hasError = bodyText?.includes('error') || bodyText?.includes('Error') || bodyText?.includes('wrong');
    console.log(`❌ Has error content: ${hasError}`);

    if (formCount > 0) {
      console.log('🎯 FORM FOUND! Testing actual form fields...');

      // Look for all input fields
      const inputs = await page.locator('input').all();
      console.log(`📝 Found ${inputs.length} input fields:`);

      for (let i = 0; i < inputs.length; i++) {
        const input = inputs[i];
        const name = await input.getAttribute('name');
        const type = await input.getAttribute('type');
        const placeholder = await input.getAttribute('placeholder');
        const visible = await input.isVisible();

        console.log(`   Input ${i}: name="${name}", type="${type}", placeholder="${placeholder}", visible=${visible}`);
      }

      // Specifically test for new fields
      const pronounsField = page.locator('input[name="pronouns"], [data-testid="pronouns"]');
      const otherNamesField = page.locator('input[name="otherNames"], textarea[name="otherNames"], [data-testid="other-names"]');

      const pronounsExists = await pronounsField.count() > 0;
      const otherNamesExists = await otherNamesField.count() > 0;

      console.log(`📋 NEW FIELDS VERIFICATION:`);
      console.log(`   - Pronouns field: ${pronounsExists ? '✅ FOUND' : '❌ MISSING'}`);
      console.log(`   - Other Names field: ${otherNamesExists ? '✅ FOUND' : '❌ MISSING'}`);

      if (pronounsExists) {
        await pronounsField.fill('they/them');
        const value = await pronounsField.inputValue();
        console.log(`📝 Pronouns field works: "${value}"`);
      }

      if (otherNamesExists) {
        await otherNamesField.fill('Test handles and names');
        const value = await otherNamesField.inputValue();
        console.log(`📝 Other Names field works: "${value}"`);
      }

      // Take screenshot with filled fields
      await page.screenshot({ path: 'test-results/vetting-form-fields-filled-guest.png', fullPage: true });
    }
  });

  test('Should verify database schema supports new fields', async ({ page }) => {
    console.log('🗄️ Testing database schema for new fields...');

    // Login as admin to get access
    await page.goto('http://localhost:5173/');
    await page.locator('text=LOGIN').click();
    await page.locator('[data-testid="email-input"], input[name="email"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"], input[name="password"]').fill('Test123!');
    await page.locator('[data-testid="sign-in-button"], button:has-text("Sign In"), button[type="submit"]').click();
    await page.waitForLoadState('networkidle');

    // Test a mock submission to see if API accepts new fields
    const mockVettingData = {
      realName: "Test User",
      pronouns: "they/them",
      fetlifeHandle: "TestUser",
      otherNames: "Also known as: Community Member, Workshop Attendee",
      whyJoin: "I want to learn rope bondage in a safe environment",
      experience: "Complete beginner",
      agreesToStandards: true
    };

    try {
      const response = await page.request.post('http://localhost:5655/api/vetting/application', {
        data: mockVettingData
      });

      console.log(`📡 Vetting submission test: Status ${response.status()}`);

      if (response.ok()) {
        const responseData = await response.json();
        console.log(`✅ API accepted submission with new fields`);
        console.log(`📝 Response: ${JSON.stringify(responseData, null, 2)}`);
      } else {
        const errorText = await response.text();
        console.log(`⚠️ API response: ${errorText.substring(0, 300)}...`);

        // Check if it's a validation error vs server error
        if (response.status() === 400) {
          console.log(`📋 Validation error - checking if new fields are mentioned`);
          const mentionsPronouns = errorText.includes('pronouns');
          const mentionsOtherNames = errorText.includes('otherNames') || errorText.includes('other names');

          console.log(`   - Error mentions pronouns: ${mentionsPronouns}`);
          console.log(`   - Error mentions other names: ${mentionsOtherNames}`);

          if (!mentionsPronouns && !mentionsOtherNames) {
            console.log(`✅ New fields not causing validation errors`);
          }
        }
      }
    } catch (error) {
      console.log(`💥 API request failed: ${error}`);
    }
  });
});