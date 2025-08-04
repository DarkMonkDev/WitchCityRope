import { test, expect } from '@playwright/test';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { AuthHelpers } from '../helpers/auth.helpers';

test.describe('Field-Specific Validation Tests', () => {
  test.describe('Email Field Validation', () => {
    test('email format validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/register`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const emailInput = await page.locator('input[type="email"], input[name*="Email" i]').first();
      
      // Test various invalid email formats
      const invalidEmails = [
        { value: 'notanemail', error: 'valid email' },
        { value: 'missing@domain', error: 'valid email' },
        { value: '@example.com', error: 'valid email' },
        { value: 'user@', error: 'valid email' },
        { value: 'user name@example.com', error: 'valid email' },
        { value: 'user@domain@example.com', error: 'valid email' }
      ];
      
      for (const testCase of invalidEmails) {
        await emailInput.clear();
        await emailInput.fill(testCase.value);
        await page.click('body'); // Blur
        await page.waitForTimeout(500);
        
        const error = await page.locator('.wcr-validation-message, .text-danger').filter({ hasText: /email/i }).first();
        expect(await error.isVisible()).toBeTruthy();
        console.log(`  ✅ Invalid email "${testCase.value}" shows error`);
      }
      
      // Test valid email
      await emailInput.clear();
      await emailInput.fill('valid.email@example.com');
      await page.click('body');
      await page.waitForTimeout(500);
      
      const errors = await page.locator('.wcr-validation-message').filter({ hasText: /email/i }).all();
      expect(errors.length).toBe(0);
      console.log('  ✅ Valid email accepted');
    });

    test('email uniqueness validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/register`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      // Use a known existing email
      const emailInput = await page.locator('input[type="email"]').first();
      await emailInput.fill('admin@witchcityrope.com');
      
      // Fill other required fields to trigger server validation
      await page.fill('input[name*="SceneName" i]', 'TestUser123');
      await page.fill('input[type="password"]', 'Test123!');
      await page.fill('input[name*="ConfirmPassword" i]', 'Test123!');
      
      // Check required checkboxes
      const checkboxes = await page.locator('input[type="checkbox"][required]').all();
      for (const checkbox of checkboxes) {
        await checkbox.check();
      }
      
      // Submit form
      await page.click('button[type="submit"]');
      await page.waitForTimeout(2000);
      
      // Check for duplicate email error
      const duplicateError = await page.locator('.validation-errors, .alert-danger').filter({ hasText: /already|exists|taken/i }).first();
      expect(await duplicateError.isVisible()).toBeTruthy();
      console.log('  ✅ Email uniqueness validation working');
    });
  });

  test.describe('Password Field Validation', () => {
    test('password strength requirements', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/register`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const passwordInput = await page.locator('input[type="password"]').first();
      
      // Test weak passwords
      const weakPasswords = [
        { value: 'short', strength: 'weak' },
        { value: 'alllowercase', strength: 'weak' },
        { value: 'ALLUPPERCASE', strength: 'weak' },
        { value: '12345678', strength: 'weak' },
        { value: 'NoSpecial123', strength: 'medium' }
      ];
      
      for (const testCase of weakPasswords) {
        await passwordInput.clear();
        await passwordInput.fill(testCase.value);
        await page.waitForTimeout(500);
        
        // Check password strength indicator
        const strengthIndicator = await page.locator('.wcr-password-strength, .password-strength').first();
        if (await strengthIndicator.isVisible()) {
          const strengthText = await strengthIndicator.textContent();
          console.log(`  📊 Password "${testCase.value}" strength: ${strengthText}`);
        }
        
        // Check requirements list
        const requirements = await page.locator('.wcr-password-requirements li, .password-requirement').all();
        if (requirements.length > 0) {
          console.log(`  📋 ${requirements.length} requirements shown`);
        }
      }
      
      // Test strong password
      await passwordInput.clear();
      await passwordInput.fill('Strong!Pass123');
      await page.waitForTimeout(500);
      
      const strongIndicator = await page.locator('.wcr-password-strength').first();
      if (await strongIndicator.isVisible()) {
        const strengthText = await strongIndicator.textContent();
        expect(strengthText?.toLowerCase()).toContain('strong');
        console.log('  ✅ Strong password recognized');
      }
    });

    test('password confirmation matching', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/register`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const passwordInput = await page.locator('input[type="password"]').first();
      const confirmInput = await page.locator('input[name*="ConfirmPassword" i]');
      
      // Set different passwords
      await passwordInput.fill('Test123!');
      await confirmInput.fill('Different123!');
      await page.click('body');
      await page.waitForTimeout(500);
      
      const mismatchError = await page.locator('.wcr-validation-message, .text-danger').filter({ hasText: /match|confirm/i }).first();
      expect(await mismatchError.isVisible()).toBeTruthy();
      console.log('  ✅ Password mismatch detected');
      
      // Set matching passwords
      await confirmInput.clear();
      await confirmInput.fill('Test123!');
      await page.click('body');
      await page.waitForTimeout(500);
      
      const errors = await page.locator('.wcr-validation-message').filter({ hasText: /match|confirm/i }).all();
      expect(errors.length).toBe(0);
      console.log('  ✅ Matching passwords accepted');
    });

    test('password visibility toggle', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/login`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const passwordInput = await page.locator('input[name*="Password" i]').first();
      const toggleButton = await page.locator('.wcr-password-toggle, button[aria-label*="password" i]').first();
      
      if (await toggleButton.isVisible()) {
        // Check initial state (password hidden)
        let inputType = await passwordInput.getAttribute('type');
        expect(inputType).toBe('password');
        console.log('  ✅ Password initially hidden');
        
        // Toggle to show password
        await toggleButton.click();
        await page.waitForTimeout(300);
        
        inputType = await passwordInput.getAttribute('type');
        expect(inputType).toBe('text');
        console.log('  ✅ Password shown after toggle');
        
        // Toggle back to hide
        await toggleButton.click();
        await page.waitForTimeout(300);
        
        inputType = await passwordInput.getAttribute('type');
        expect(inputType).toBe('password');
        console.log('  ✅ Password hidden again');
      }
    });
  });

  test.describe('Phone Number Validation', () => {
    test.beforeEach(async ({ page }) => {
      await AuthHelpers.login(page);
    });

    test('phone number format validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/manage-profile`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const phoneInput = await page.locator('input[type="tel"], input[placeholder*="phone" i]').first();
      
      if (await phoneInput.isVisible()) {
        // Test invalid phone formats
        const invalidPhones = [
          'not-a-phone',
          '123',
          '123-456',
          'abcd-efg-hijk'
        ];
        
        for (const phone of invalidPhones) {
          await phoneInput.clear();
          await phoneInput.fill(phone);
          await page.click('body');
          await page.waitForTimeout(500);
          
          const error = await page.locator('.wcr-validation-message, .text-danger').filter({ hasText: /phone|format/i }).first();
          if (await error.isVisible()) {
            console.log(`  ✅ Invalid phone "${phone}" shows error`);
          }
        }
        
        // Test valid phone formats
        const validPhones = [
          '(555) 123-4567',
          '555-123-4567',
          '5551234567',
          '+1 555 123 4567'
        ];
        
        for (const phone of validPhones) {
          await phoneInput.clear();
          await phoneInput.fill(phone);
          await page.click('body');
          await page.waitForTimeout(500);
          
          const errors = await page.locator('.wcr-validation-message').filter({ hasText: /phone/i }).all();
          if (errors.length === 0) {
            console.log(`  ✅ Valid phone "${phone}" accepted`);
          }
        }
      } else {
        console.log('  ⚠️ Phone input not found on this page');
      }
    });
  });

  test.describe('Date/DateTime Validation', () => {
    test.beforeEach(async ({ page }) => {
      await AuthHelpers.login(page);
    });

    test('event date validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/admin/events/new-standardized`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      // Find date inputs
      const startDateInput = await page.locator('input[type="datetime-local"][name*="Start" i]').first();
      const endDateInput = await page.locator('input[type="datetime-local"][name*="End" i]').first();
      
      if (await startDateInput.isVisible() && await endDateInput.isVisible()) {
        // Test past date validation
        const pastDate = new Date();
        pastDate.setDate(pastDate.getDate() - 1);
        const pastDateStr = pastDate.toISOString().slice(0, 16);
        
        await startDateInput.fill(pastDateStr);
        await page.click('body');
        await page.waitForTimeout(500);
        
        const pastDateError = await page.locator('.wcr-validation-message, .text-danger').filter({ hasText: /past|future/i }).first();
        if (await pastDateError.isVisible()) {
          console.log('  ✅ Past date validation working');
        }
        
        // Test end before start validation
        const futureStart = new Date();
        futureStart.setDate(futureStart.getDate() + 7);
        const futureEnd = new Date();
        futureEnd.setDate(futureEnd.getDate() + 6);
        
        await startDateInput.fill(futureStart.toISOString().slice(0, 16));
        await endDateInput.fill(futureEnd.toISOString().slice(0, 16));
        await page.click('body');
        await page.waitForTimeout(500);
        
        const dateOrderError = await page.locator('.wcr-validation-message, .text-danger').filter({ hasText: /before|after|end/i }).first();
        if (await dateOrderError.isVisible()) {
          console.log('  ✅ Date order validation working');
        }
        
        // Test valid dates
        await endDateInput.fill(new Date(futureStart.getTime() + 2 * 60 * 60 * 1000).toISOString().slice(0, 16));
        await page.click('body');
        await page.waitForTimeout(500);
        
        const dateErrors = await page.locator('.wcr-validation-message').filter({ hasText: /date|time/i }).all();
        expect(dateErrors.length).toBe(0);
        console.log('  ✅ Valid date range accepted');
      }
    });
  });

  test.describe('Number/Range Validation', () => {
    test('number range validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const ageInput = await page.locator('input[type="number"][name*="Age" i], [data-testid="age-input"] input').first();
      
      if (await ageInput.isVisible()) {
        // Test below minimum
        await ageInput.fill('17');
        await page.click('body');
        await page.waitForTimeout(500);
        
        let error = await page.locator('.wcr-validation-message').filter({ hasText: /18|age|minimum/i }).first();
        expect(await error.isVisible()).toBeTruthy();
        console.log('  ✅ Below minimum age shows error');
        
        // Test above maximum
        await ageInput.clear();
        await ageInput.fill('121');
        await page.click('body');
        await page.waitForTimeout(500);
        
        error = await page.locator('.wcr-validation-message').filter({ hasText: /120|age|maximum/i }).first();
        expect(await error.isVisible()).toBeTruthy();
        console.log('  ✅ Above maximum age shows error');
        
        // Test valid age
        await ageInput.clear();
        await ageInput.fill('25');
        await page.click('body');
        await page.waitForTimeout(500);
        
        const errors = await page.locator('.wcr-validation-message').all();
        expect(errors.length).toBe(0);
        console.log('  ✅ Valid age accepted');
      }
    });

    test('ticket quantity validation', async ({ page }) => {
      // Navigate to an event with tickets
      await page.goto(`${testConfig.baseUrl}/events`, {
        waitUntil: 'networkidle'
      });
      
      // Click on first ticketed event
      const eventCard = await page.locator('.event-card').filter({ hasText: /ticket/i }).first();
      if (await eventCard.isVisible()) {
        await eventCard.click();
        await BlazorHelpers.waitForBlazorReady(page);
        
        const quantityInput = await page.locator('input[type="number"][name*="Quantity" i]').first();
        if (await quantityInput.isVisible()) {
          // Test zero quantity
          await quantityInput.fill('0');
          await page.click('body');
          await page.waitForTimeout(500);
          
          let error = await page.locator('.wcr-validation-message, .text-danger').filter({ hasText: /quantity|minimum|least/i }).first();
          if (await error.isVisible()) {
            console.log('  ✅ Zero quantity shows error');
          }
          
          // Test exceeding maximum
          await quantityInput.clear();
          await quantityInput.fill('100');
          await page.click('body');
          await page.waitForTimeout(500);
          
          error = await page.locator('.wcr-validation-message, .text-danger').filter({ hasText: /maximum|exceed|available/i }).first();
          if (await error.isVisible()) {
            console.log('  ✅ Exceeding maximum shows error');
          }
        }
      }
    });
  });

  test.describe('Text Length Validation', () => {
    test('scene name length validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/register`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const sceneNameInput = await page.locator('input[name*="SceneName" i]').first();
      
      // Test too short
      await sceneNameInput.fill('A');
      await page.click('body');
      await page.waitForTimeout(500);
      
      let error = await page.locator('.wcr-validation-message').filter({ hasText: /characters|length|minimum/i }).first();
      expect(await error.isVisible()).toBeTruthy();
      console.log('  ✅ Too short scene name shows error');
      
      // Test too long
      await sceneNameInput.clear();
      await sceneNameInput.fill('A'.repeat(51));
      await page.click('body');
      await page.waitForTimeout(500);
      
      error = await page.locator('.wcr-validation-message').filter({ hasText: /characters|length|maximum/i }).first();
      expect(await error.isVisible()).toBeTruthy();
      console.log('  ✅ Too long scene name shows error');
      
      // Test valid length
      await sceneNameInput.clear();
      await sceneNameInput.fill('ValidSceneName');
      await page.click('body');
      await page.waitForTimeout(500);
      
      const errors = await page.locator('.wcr-validation-message').filter({ hasText: sceneNameInput.toString() }).all();
      expect(errors.length).toBe(0);
      console.log('  ✅ Valid scene name length accepted');
    });

    test('message/description length validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/contact`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const messageTextarea = await page.locator('textarea[name*="Message" i]').first();
      
      if (await messageTextarea.isVisible()) {
        // Test too short message
        await messageTextarea.fill('Short');
        await page.click('body');
        await page.waitForTimeout(500);
        
        const error = await page.locator('.wcr-validation-message, .text-danger').filter({ hasText: /characters|length|minimum/i }).first();
        if (await error.isVisible()) {
          console.log('  ✅ Too short message shows error');
        }
        
        // Test valid message
        await messageTextarea.clear();
        await messageTextarea.fill('This is a valid message with sufficient length to meet the minimum requirements.');
        await page.click('body');
        await page.waitForTimeout(500);
        
        const errors = await page.locator('.wcr-validation-message').filter({ hasText: /message/i }).all();
        expect(errors.length).toBe(0);
        console.log('  ✅ Valid message length accepted');
      }
    });
  });
});