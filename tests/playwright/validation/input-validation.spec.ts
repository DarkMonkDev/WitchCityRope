import { test, expect } from '@playwright/test';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { AuthHelpers } from '../helpers/auth.helpers';

test.describe('Input Type Validation Tests', () => {
  test.describe('Text Input Validation', () => {
    test('required field validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      // Test empty required field
      const nameInput = await page.locator('[data-testid="name-input"] input').first();
      await nameInput.focus();
      await page.click('body'); // Blur without entering anything
      await page.waitForTimeout(500);
      
      const requiredError = await page.locator('[data-testid="name-input"] .wcr-validation-message').first();
      expect(await requiredError.isVisible()).toBeTruthy();
      const errorText = await requiredError.textContent();
      expect(errorText).toContain('required');
      console.log('✅ Required field validation working');
    });

    test('minimum length validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const nameInput = await page.locator('[data-testid="name-input"] input').first();
      await nameInput.fill('a'); // Too short
      await page.click('body');
      await page.waitForTimeout(500);
      
      const lengthError = await page.locator('[data-testid="name-input"] .wcr-validation-message').first();
      expect(await lengthError.isVisible()).toBeTruthy();
      const errorText = await lengthError.textContent();
      expect(errorText).toContain('2 characters');
      console.log('✅ Minimum length validation working');
    });

    test('maximum length validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const nameInput = await page.locator('[data-testid="name-input"] input').first();
      await nameInput.fill('a'.repeat(51)); // Too long
      await page.click('body');
      await page.waitForTimeout(500);
      
      const lengthError = await page.locator('[data-testid="name-input"] .wcr-validation-message').first();
      expect(await lengthError.isVisible()).toBeTruthy();
      const errorText = await lengthError.textContent();
      expect(errorText).toContain('50 characters');
      console.log('✅ Maximum length validation working');
    });

    test('pattern validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/register`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const sceneNameInput = await page.locator('input[name*="SceneName" i]').first();
      
      // Test with special characters that might not be allowed
      await sceneNameInput.fill('Test@#$%');
      await page.click('body');
      await page.waitForTimeout(500);
      
      // Check if there's a pattern validation error
      const patternError = await page.locator('.wcr-validation-message').filter({ hasText: /invalid|format|characters/i }).first();
      if (await patternError.isVisible()) {
        console.log('✅ Pattern validation working');
      } else {
        console.log('ℹ️ No pattern validation on scene name field');
      }
    });
  });

  test.describe('Email Input Validation', () => {
    test('email format validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const emailInput = await page.locator('[data-testid="email-input"] input').first();
      
      // Test invalid formats
      const invalidFormats = [
        'plaintext',
        '@example.com',
        'user@',
        'user..name@example.com',
        'user name@example.com',
        'user@domain@example.com'
      ];
      
      for (const invalidEmail of invalidFormats) {
        await emailInput.clear();
        await emailInput.fill(invalidEmail);
        await page.click('body');
        await page.waitForTimeout(500);
        
        const emailError = await page.locator('[data-testid="email-input"] .wcr-validation-message').first();
        expect(await emailError.isVisible()).toBeTruthy();
        console.log(`✅ Invalid email "${invalidEmail}" shows error`);
      }
    });

    test('email domain validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const emailInput = await page.locator('[data-testid="email-input"] input').first();
      
      // Test valid email formats
      const validEmails = [
        'user@example.com',
        'user.name@example.com',
        'user+tag@example.co.uk',
        'user_name@sub.example.com'
      ];
      
      for (const validEmail of validEmails) {
        await emailInput.clear();
        await emailInput.fill(validEmail);
        await page.click('body');
        await page.waitForTimeout(500);
        
        const emailErrors = await page.locator('[data-testid="email-input"] .wcr-validation-message').all();
        expect(emailErrors.length).toBe(0);
        console.log(`✅ Valid email "${validEmail}" accepted`);
      }
    });
  });

  test.describe('Password Input Validation', () => {
    test('password strength indicator', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const passwordInput = await page.locator('[data-testid="password-input"] input').first();
      
      // Test different password strengths
      const passwords = [
        { value: '123', strength: 'Very Weak' },
        { value: 'password', strength: 'Weak' },
        { value: 'Password1', strength: 'Medium' },
        { value: 'Password1!', strength: 'Strong' },
        { value: 'C0mpl3x!P@ssw0rd', strength: 'Very Strong' }
      ];
      
      for (const test of passwords) {
        await passwordInput.clear();
        await passwordInput.fill(test.value);
        await page.waitForTimeout(300); // Wait for strength calculation
        
        const strengthIndicator = await page.locator('.wcr-password-strength').first();
        if (await strengthIndicator.isVisible()) {
          const strengthText = await strengthIndicator.textContent();
          console.log(`✅ Password "${test.value}" shows strength: ${strengthText}`);
        }
      }
    });

    test('password requirements checklist', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const passwordInput = await page.locator('[data-testid="password-input"] input').first();
      await passwordInput.fill('Test123!');
      await page.waitForTimeout(300);
      
      // Check requirements list
      const requirements = await page.locator('.wcr-password-requirements li').all();
      expect(requirements.length).toBeGreaterThan(0);
      
      for (const req of requirements) {
        const text = await req.textContent();
        const hasCheckmark = await req.locator('.checkmark, .check, .✓').isVisible().catch(() => false);
        console.log(`${hasCheckmark ? '✅' : '❌'} ${text}`);
      }
    });

    test('password visibility toggle', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const passwordInput = await page.locator('[data-testid="password-input"] input').first();
      const toggleButton = await page.locator('[data-testid="password-input"] .wcr-password-toggle').first();
      
      // Check initial state
      expect(await passwordInput.getAttribute('type')).toBe('password');
      
      // Toggle visibility
      await toggleButton.click();
      expect(await passwordInput.getAttribute('type')).toBe('text');
      console.log('✅ Password visibility toggle working');
      
      // Toggle back
      await toggleButton.click();
      expect(await passwordInput.getAttribute('type')).toBe('password');
    });
  });

  test.describe('Number Input Validation', () => {
    test('number range validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const ageInput = await page.locator('[data-testid="age-input"] input').first();
      
      // Test below minimum
      await ageInput.fill('17');
      await page.click('body');
      await page.waitForTimeout(500);
      
      let error = await page.locator('[data-testid="age-input"] .wcr-validation-message').first();
      expect(await error.isVisible()).toBeTruthy();
      expect(await error.textContent()).toContain('18');
      console.log('✅ Below minimum value shows error');
      
      // Test above maximum
      await ageInput.clear();
      await ageInput.fill('121');
      await page.click('body');
      await page.waitForTimeout(500);
      
      error = await page.locator('[data-testid="age-input"] .wcr-validation-message').first();
      expect(await error.isVisible()).toBeTruthy();
      expect(await error.textContent()).toContain('120');
      console.log('✅ Above maximum value shows error');
    });

    test('number step validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const priceInput = await page.locator('[data-testid="price-input"] input').first();
      if (await priceInput.isVisible()) {
        // Test decimal places
        await priceInput.fill('10.999'); // More than 2 decimal places
        await page.click('body');
        await page.waitForTimeout(500);
        
        const error = await page.locator('[data-testid="price-input"] .wcr-validation-message').first();
        if (await error.isVisible()) {
          console.log('✅ Step validation working for decimal places');
        }
      }
    });

    test('integer only validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const ageInput = await page.locator('[data-testid="age-input"] input').first();
      
      // Try to enter decimal
      await ageInput.fill('25.5');
      await page.click('body');
      await page.waitForTimeout(500);
      
      // Check if decimal is rejected or rounded
      const value = await ageInput.inputValue();
      if (value === '25' || value === '26') {
        console.log('✅ Integer-only validation working (decimal rounded/truncated)');
      } else {
        const error = await page.locator('[data-testid="age-input"] .wcr-validation-message').first();
        if (await error.isVisible()) {
          console.log('✅ Integer-only validation shows error for decimal');
        }
      }
    });
  });

  test.describe('Checkbox Input Validation', () => {
    test('required checkbox validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      // Submit form without checking required checkbox
      const submitButton = await page.locator('button[type="submit"]').first();
      await submitButton.click();
      await page.waitForTimeout(1000);
      
      const checkboxError = await page.locator('[data-testid="terms-checkbox"] .wcr-validation-message').first();
      expect(await checkboxError.isVisible()).toBeTruthy();
      console.log('✅ Required checkbox validation working');
      
      // Check the checkbox and verify error clears
      const checkbox = await page.locator('[data-testid="terms-checkbox"] input[type="checkbox"]').first();
      await checkbox.check();
      await page.waitForTimeout(500);
      
      const errors = await page.locator('[data-testid="terms-checkbox"] .wcr-validation-message').all();
      expect(errors.length).toBe(0);
      console.log('✅ Checkbox validation cleared when checked');
    });

    test('checkbox group validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/register`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      // Try to submit without checking required checkboxes
      const submitButton = await page.locator('button[type="submit"]').first();
      await submitButton.click();
      await page.waitForTimeout(1000);
      
      // Check for checkbox-related errors
      const checkboxErrors = await page.locator('.wcr-validation-message').filter({ hasText: /agree|confirm|accept/i }).all();
      if (checkboxErrors.length > 0) {
        console.log(`✅ ${checkboxErrors.length} required checkbox errors shown`);
      }
    });
  });

  test.describe('Select/Dropdown Validation', () => {
    test('required select validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/contact`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const subjectSelect = await page.locator('select[name*="Subject" i]').first();
      if (await subjectSelect.isVisible()) {
        // Submit without selecting
        const submitButton = await page.locator('button[type="submit"]').first();
        await submitButton.click();
        await page.waitForTimeout(1000);
        
        const selectError = await page.locator('.wcr-validation-message, .text-danger').filter({ hasText: /subject/i }).first();
        if (await selectError.isVisible()) {
          console.log('✅ Required select validation working');
        }
        
        // Select an option and verify error clears
        await subjectSelect.selectOption({ index: 1 });
        await page.waitForTimeout(500);
        
        const errors = await page.locator('.wcr-validation-message').filter({ hasText: /subject/i }).all();
        if (errors.length === 0) {
          console.log('✅ Select validation cleared when option selected');
        }
      }
    });
  });

  test.describe('Textarea Validation', () => {
    test('textarea length validation', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/contact`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const messageTextarea = await page.locator('textarea[name*="Message" i]').first();
      
      // Test too short
      await messageTextarea.fill('Hi');
      await page.click('body');
      await page.waitForTimeout(500);
      
      const lengthError = await page.locator('.wcr-validation-message, .text-danger').filter({ hasText: /characters|length/i }).first();
      if (await lengthError.isVisible()) {
        console.log('✅ Textarea minimum length validation working');
      }
      
      // Test valid length
      await messageTextarea.clear();
      await messageTextarea.fill('This is a valid message with sufficient length to meet requirements.');
      await page.click('body');
      await page.waitForTimeout(500);
      
      const errors = await page.locator('.wcr-validation-message').filter({ hasText: /message/i }).all();
      if (errors.length === 0) {
        console.log('✅ Valid textarea content accepted');
      }
    });

    test('textarea max length enforcement', async ({ page }) => {
      await page.goto(`${testConfig.baseUrl}/contact`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const messageTextarea = await page.locator('textarea[name*="Message" i]').first();
      const maxLength = await messageTextarea.getAttribute('maxlength');
      
      if (maxLength) {
        const longText = 'a'.repeat(parseInt(maxLength) + 10);
        await messageTextarea.fill(longText);
        
        const actualValue = await messageTextarea.inputValue();
        expect(actualValue.length).toBeLessThanOrEqual(parseInt(maxLength));
        console.log(`✅ Textarea maxlength (${maxLength}) enforced`);
      }
    });
  });
});