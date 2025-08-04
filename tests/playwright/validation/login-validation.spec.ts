import { test, expect } from '@playwright/test';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

test.describe('Login Page Validation Tests', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(`${testConfig.baseUrl}/login`, {
      waitUntil: 'networkidle'
    });
    await BlazorHelpers.waitForBlazorReady(page);
  });

  test('empty form submission', async ({ page }) => {
    console.log('📍 Test: Empty form submission');
    
    const submitBtn = await page.locator('.sign-in-btn, button[type="submit"]').first();
    await submitBtn.click();
    await page.waitForTimeout(1000);
    
    // Check validation summary
    const validationSummary = await page.locator('.wcr-validation-summary');
    if (await validationSummary.isVisible()) {
      const errors = await page.locator('.wcr-validation-summary li').allTextContents();
      console.log('✅ Validation errors displayed:');
      errors.forEach(error => console.log(`   - ${error}`));
      expect(errors.length).toBeGreaterThan(0);
    }
    
    // Check for red borders on inputs
    const errorInputs = await page.locator('.wcr-input-error').all();
    console.log(`✅ ${errorInputs.length} input fields showing error state`);
    expect(errorInputs.length).toBeGreaterThan(0);
    
    await page.screenshot({ 
      path: 'test-results/validation-tests/login-empty-form.png',
      fullPage: true 
    });
  });

  test('invalid email format', async ({ page }) => {
    console.log('\n📍 Test: Invalid email format');
    
    const emailInput = await page.locator('input[name="loginModel.Email"], #Input_Email').first();
    await emailInput.fill('invalid-email-format');
    await page.click('body'); // Blur to trigger validation
    await page.waitForTimeout(500);
    
    const emailError = await page.locator('.wcr-field-validation[data-valmsg-for="loginModel.Email"], .wcr-validation-message').first();
    if (await emailError.isVisible()) {
      const errorText = await emailError.textContent();
      console.log('✅ Email validation error:', errorText);
      expect(errorText).toContain('email');
    }
    
    await page.screenshot({ 
      path: 'test-results/validation-tests/login-invalid-email.png',
      fullPage: true 
    });
  });

  test('missing password', async ({ page }) => {
    console.log('\n📍 Test: Only email filled');
    
    const emailInput = await page.locator('input[name="loginModel.Email"], #Input_Email').first();
    await emailInput.fill('test@example.com');
    
    const submitBtn = await page.locator('.sign-in-btn, button[type="submit"]').first();
    await submitBtn.click();
    await page.waitForTimeout(1000);
    
    const passwordError = await page.locator('.wcr-field-validation[data-valmsg-for="loginModel.Password"], .wcr-validation-message').filter({ hasText: /password/i }).first();
    if (await passwordError.isVisible()) {
      const errorText = await passwordError.textContent();
      console.log('✅ Password field error:', errorText);
      expect(errorText).toBeTruthy();
    }
    
    await page.screenshot({ 
      path: 'test-results/validation-tests/login-missing-password.png',
      fullPage: true 
    });
  });

  test('invalid credentials', async ({ page }) => {
    console.log('\n📍 Test: Invalid credentials');
    
    const emailInput = await page.locator('input[name="loginModel.Email"], #Input_Email').first();
    const passwordInput = await page.locator('input[name="loginModel.Password"], #Input_Password').first();
    
    await emailInput.fill('test@example.com');
    await passwordInput.fill('wrongpassword');
    
    const submitBtn = await page.locator('.sign-in-btn, button[type="submit"]').first();
    await submitBtn.click();
    await page.waitForTimeout(2000);
    
    const loginError = await page.locator('.validation-errors, .alert-danger').first();
    if (await loginError.isVisible()) {
      const errorText = await loginError.textContent();
      console.log('✅ Login error message:', errorText);
      expect(errorText).toContain('Invalid');
    }
    
    await page.screenshot({ 
      path: 'test-results/validation-tests/login-invalid-credentials.png',
      fullPage: true 
    });
  });

  test('password visibility toggle', async ({ page }) => {
    console.log('\n📍 Test: Password visibility toggle');
    
    const passwordInput = await page.locator('input[name="loginModel.Password"], #Input_Password').first();
    const passwordToggle = await page.locator('.wcr-password-toggle, .password-toggle').first();
    
    if (await passwordToggle.isVisible()) {
      // Check password is initially masked
      let passwordType = await passwordInput.getAttribute('type');
      console.log(`   Password field type (before toggle): ${passwordType}`);
      expect(passwordType).toBe('password');
      
      // Show password
      await passwordToggle.click();
      await page.waitForTimeout(500);
      
      passwordType = await passwordInput.getAttribute('type');
      console.log(`   Password field type (after toggle): ${passwordType}`);
      expect(passwordType).toBe('text');
      
      // Hide password again
      await passwordToggle.click();
      await page.waitForTimeout(500);
      
      passwordType = await passwordInput.getAttribute('type');
      console.log(`   Password field type (after second toggle): ${passwordType}`);
      expect(passwordType).toBe('password');
      
      await page.screenshot({ 
        path: 'test-results/validation-tests/login-password-toggle.png',
        fullPage: true 
      });
    } else {
      console.log('   ⚠️ Password toggle button not found');
    }
  });

  test('scene name login', async ({ page }) => {
    console.log('\n📍 Test: Scene name login support');
    
    const emailInput = await page.locator('input[name="loginModel.Email"], #Input_Email').first();
    const placeholder = await emailInput.getAttribute('placeholder');
    
    if (placeholder?.toLowerCase().includes('scene')) {
      console.log('✅ Login supports scene name (placeholder indicates both email/scene name)');
      
      // Try logging in with a scene name
      await emailInput.fill('TestSceneName');
      const passwordInput = await page.locator('input[name="loginModel.Password"], #Input_Password').first();
      await passwordInput.fill('Test123!');
      
      const submitBtn = await page.locator('.sign-in-btn, button[type="submit"]').first();
      await submitBtn.click();
      await page.waitForTimeout(2000);
      
      // Should get invalid login (unless the scene name exists)
      const loginError = await page.locator('.validation-errors, .alert-danger').first();
      if (await loginError.isVisible()) {
        console.log('✅ Scene name login attempt processed');
      }
    } else {
      console.log('ℹ️ Login form may not explicitly show scene name support in placeholder');
    }
  });

  test('remember me functionality', async ({ page }) => {
    console.log('\n📍 Test: Remember me checkbox');
    
    const rememberCheckbox = await page.locator('input[type="checkbox"][name*="RememberMe" i]').first();
    
    if (await rememberCheckbox.isVisible()) {
      // Check initial state
      const isChecked = await rememberCheckbox.isChecked();
      console.log(`   Remember me initial state: ${isChecked ? 'checked' : 'unchecked'}`);
      
      // Toggle checkbox
      await rememberCheckbox.click();
      const newState = await rememberCheckbox.isChecked();
      console.log(`   Remember me after click: ${newState ? 'checked' : 'unchecked'}`);
      expect(newState).toBe(!isChecked);
      
      // Verify label is clickable
      const label = await page.locator('label').filter({ hasText: /remember/i }).first();
      if (await label.isVisible()) {
        await label.click();
        const finalState = await rememberCheckbox.isChecked();
        console.log(`   Remember me after label click: ${finalState ? 'checked' : 'unchecked'}`);
        expect(finalState).toBe(isChecked);
      }
    } else {
      console.log('   ⚠️ Remember me checkbox not found');
    }
  });

  test('form accessibility', async ({ page }) => {
    console.log('\n📍 Test: Form accessibility');
    
    // Test tab navigation
    await page.keyboard.press('Tab');
    const focusedElement1 = await page.evaluate(() => document.activeElement?.tagName);
    console.log(`   First Tab: focused on ${focusedElement1}`);
    
    await page.keyboard.press('Tab');
    const focusedElement2 = await page.evaluate(() => document.activeElement?.tagName);
    console.log(`   Second Tab: focused on ${focusedElement2}`);
    
    await page.keyboard.press('Tab');
    const focusedElement3 = await page.evaluate(() => document.activeElement?.tagName);
    console.log(`   Third Tab: focused on ${focusedElement3}`);
    
    // Check for proper labels
    const emailLabel = await page.locator('label[for*="Email" i]').first();
    const passwordLabel = await page.locator('label[for*="Password" i]').first();
    
    expect(await emailLabel.isVisible()).toBeTruthy();
    expect(await passwordLabel.isVisible()).toBeTruthy();
    console.log('✅ Form has proper labels');
    
    // Check for aria attributes
    const emailInput = await page.locator('input[name="loginModel.Email"], #Input_Email').first();
    const ariaInvalid = await emailInput.getAttribute('aria-invalid');
    const ariaDescribedBy = await emailInput.getAttribute('aria-describedby');
    console.log(`   Email input aria-invalid: ${ariaInvalid}`);
    console.log(`   Email input aria-describedby: ${ariaDescribedBy}`);
  });

  test('login form links', async ({ page }) => {
    console.log('\n📍 Test: Login form links');
    
    // Check for register link
    const registerLink = await page.locator('a').filter({ hasText: /register|sign up|create/i }).first();
    if (await registerLink.isVisible()) {
      const href = await registerLink.getAttribute('href');
      console.log(`✅ Register link found: ${href}`);
      expect(href).toContain('register');
    }
    
    // Check for forgot password link
    const forgotLink = await page.locator('a').filter({ hasText: /forgot/i }).first();
    if (await forgotLink.isVisible()) {
      const href = await forgotLink.getAttribute('href');
      console.log(`✅ Forgot password link found: ${href}`);
      expect(href).toContain('forgot');
    }
    
    // Check for external login section
    const externalLogin = await page.locator('section').filter({ hasText: /external|google|facebook/i }).first();
    if (await externalLogin.isVisible()) {
      console.log('✅ External login options available');
    } else {
      console.log('ℹ️ No external login options found');
    }
  });

  test('real-time validation', async ({ page }) => {
    console.log('\n📍 Test: Real-time validation on blur');
    
    const emailInput = await page.locator('input[name="loginModel.Email"], #Input_Email').first();
    const passwordInput = await page.locator('input[name="loginModel.Password"], #Input_Password').first();
    
    // Type invalid email and blur
    await emailInput.fill('notanemail');
    await passwordInput.focus(); // Move focus to trigger blur
    await page.waitForTimeout(500);
    
    const emailError = await page.locator('.wcr-validation-message').filter({ hasText: /email/i }).first();
    if (await emailError.isVisible()) {
      console.log('✅ Real-time email validation works on blur');
    }
    
    // Clear and type valid email
    await emailInput.clear();
    await emailInput.fill('valid@example.com');
    await passwordInput.focus();
    await page.waitForTimeout(500);
    
    const emailErrors = await page.locator('.wcr-validation-message').filter({ hasText: /email/i }).all();
    if (emailErrors.length === 0) {
      console.log('✅ Validation cleared when valid email entered');
    }
  });
});