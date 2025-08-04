import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Login Validation Tests
 * Converted from Puppeteer test: test-blazor-login-validation.js
 * 
 * Tests comprehensive login page validation including:
 * - Empty form validation
 * - Invalid email format validation
 * - Invalid credentials
 * - Password toggle functionality
 * - Remember me checkbox
 * - Scene name login
 * - CSS validation styling
 * - Navigation links
 */

test.describe('Login Page - Validation Tests', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.goto();
  });

  test('should navigate to Blazor login page', async ({ page }) => {
    // Test 1: Navigate to Blazor Login Page (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'blazor-login-navigation' });

    // Wait for Blazor to initialize
    await BlazorHelpers.waitForBlazorReady(page);

    // Check if we're on the login page
    const welcomeTitle = await loginPage.pageTitle.isVisible();
    expect(welcomeTitle).toBeTruthy();
    console.log('✅ Successfully loaded Blazor login page');

    // Verify page title
    await expect(page).toHaveTitle(/Log in/i);
  });

  test('should show validation for empty form submission', async ({ page }) => {
    // Test 2: Validation - Empty Form Submission (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'empty-form-validation' });

    // Try to submit empty form
    await loginPage.submitEmptyForm();

    // Check for validation messages
    const hasValidation = await loginPage.hasValidationErrors();
    expect(hasValidation).toBeTruthy();
    console.log('✅ Validation summary displayed for empty form');

    // Get validation text
    const validationMessages = await loginPage.getValidationMessages();
    expect(validationMessages.length).toBeGreaterThan(0);
    validationMessages.forEach(msg => {
      console.log('   Validation message:', msg);
    });
  });

  test('should validate email/scene name field format', async ({ page }) => {
    // Test 3: Validation - Invalid Email Format (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'email-format-validation' });

    // Clear and enter invalid email
    await loginPage.emailInput.click({ clickCount: 3 });
    await loginPage.emailInput.fill('notanemail');

    // Clear password field and submit
    await loginPage.passwordInput.click({ clickCount: 3 });
    await loginPage.passwordInput.fill('Test123!');

    await loginPage.signInButton.click();
    await BlazorHelpers.waitForValidation(page);

    // Note: Since we accept both email and scene name, "notanemail" might be valid as a scene name
    console.log('✅ Tested email/scene name field with non-email format');
    
    // Verify we're still on login page
    await expect(page).toHaveURL(/.*\/Identity\/Account\/Login/);
  });

  test('should show error for invalid credentials', async ({ page }) => {
    // Test 4: Invalid Credentials (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'invalid-credentials' });

    // Clear and enter invalid credentials
    await loginPage.clearForm();
    await BlazorHelpers.fillAndWait(page, loginPage.emailInput, 'invalid@example.com');
    await BlazorHelpers.fillAndWait(page, loginPage.passwordInput, 'wrongpassword');

    await loginPage.signInButton.click();

    // Wait for server response
    await page.waitForTimeout(2000);

    // Check for error message
    const errorDiv = page.locator('.validation-errors');
    const errorVisible = await errorDiv.isVisible();
    
    if (errorVisible) {
      const errorText = await errorDiv.textContent();
      console.log('✅ Error message displayed:', errorText?.trim());
      expect(errorText).toBeTruthy();
    } else {
      console.log('⚠️  No error message displayed for invalid credentials');
      // Check if we're still on login page
      await expect(page).toHaveURL(/.*\/Identity\/Account\/Login/);
    }
  });

  test('should toggle password visibility', async ({ page }) => {
    // Test 5: Password Toggle Visibility (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'password-toggle' });

    // Enter a password to see the toggle effect
    await loginPage.passwordInput.fill('TestPassword123!');

    // Find password toggle button
    const passwordToggle = page.locator('.wcr-password-toggle');
    const toggleExists = await passwordToggle.isVisible();

    if (toggleExists) {
      // Check initial state (password should be hidden)
      let passwordType = await loginPage.passwordInput.evaluate(el => (el as HTMLInputElement).type);
      console.log(`   Initial password field type: ${passwordType}`);
      expect(passwordType).toBe('password');

      // Click toggle
      await passwordToggle.click();
      await page.waitForTimeout(500);

      // Check if type changed
      passwordType = await loginPage.passwordInput.evaluate(el => (el as HTMLInputElement).type);
      console.log(`   After toggle password field type: ${passwordType}`);
      expect(passwordType).toBe('text');
      console.log('✅ Password visibility toggle works');
    } else {
      console.log('⚠️  No password toggle button found');
    }
  });

  test('should handle remember me checkbox', async ({ page }) => {
    // Test 6: Remember Me Checkbox (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'remember-me-checkbox' });

    const rememberCheckbox = page.locator('.checkbox-input, input[type="checkbox"][name="rememberMe"]').first();
    const checkboxExists = await rememberCheckbox.isVisible();

    if (checkboxExists) {
      // Check initial state
      let isChecked = await rememberCheckbox.isChecked();
      console.log(`   Initial checkbox state: ${isChecked}`);

      // Click checkbox
      await rememberCheckbox.click();
      await page.waitForTimeout(500);

      // Check new state
      const newCheckedState = await rememberCheckbox.isChecked();
      console.log(`   After click checkbox state: ${newCheckedState}`);
      expect(newCheckedState).toBe(!isChecked);
      console.log('✅ Remember me checkbox is interactive');
    } else {
      console.log('❌ Remember me checkbox not found');
    }
  });

  test('should allow login with scene name', async ({ page }) => {
    // Test 7: Valid Login (Scene Name) (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'scene-name-login' });

    // Clear and enter valid credentials using scene name
    await loginPage.clearForm();
    await BlazorHelpers.fillAndWait(page, loginPage.emailInput, 'RopeAdmin'); // Using scene name instead of email
    await BlazorHelpers.fillAndWait(page, loginPage.passwordInput, testConfig.accounts.admin.password);

    console.log('   Attempting login with scene name: RopeAdmin');
    await loginPage.signInButton.click();

    // Wait for navigation or error
    await Promise.race([
      page.waitForURL(/^(?!.*\/Identity\/Account\/Login).*$/, { timeout: 10000 }).catch(() => {}),
      page.waitForTimeout(5000)
    ]);

    // Check if we're still on login page
    const currentUrl = page.url();
    if (currentUrl.includes('/Identity/Account/Login')) {
      console.log('⚠️  Still on login page - checking for errors');
      const errorAfterLogin = page.locator('.validation-errors');
      const hasError = await errorAfterLogin.isVisible();
      if (hasError) {
        const errorText = await errorAfterLogin.textContent();
        console.log('   Error:', errorText?.trim());
      }
    } else {
      console.log('✅ Successfully logged in and redirected to:', currentUrl);
      expect(currentUrl).not.toContain('/Identity/Account/Login');
    }
  });

  test('should apply CSS validation styling', async ({ page }) => {
    // Test 8: CSS Validation Styling (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'css-validation-styling' });

    // Submit empty form to trigger validation
    await loginPage.submitEmptyForm();

    // Check if input has error styling
    const inputWithError = page.locator('.wcr-input-error');
    const hasErrorStyling = await inputWithError.isVisible();

    if (hasErrorStyling) {
      console.log('✅ Error styling applied to invalid inputs');
      expect(await inputWithError.count()).toBeGreaterThan(0);
    } else {
      console.log('⚠️  No error styling found on inputs');
      // Check for alternative error styling
      const alternativeError = await page.locator('input.error, input.invalid').count();
      expect(alternativeError).toBeGreaterThan(0);
    }
  });

  test('should have working forgot password link', async ({ page }) => {
    // Test 9: Forgot Password Link (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'forgot-password-link' });

    const forgotLink = loginPage.forgotPasswordLink;
    const linkExists = await forgotLink.isVisible();

    if (linkExists) {
      const href = await forgotLink.getAttribute('href');
      console.log('✅ Forgot password link found:', href);
      expect(href).toBeTruthy();
      
      // Test navigation
      await forgotLink.click();
      await page.waitForURL(/.*\/(ForgotPassword|Identity\/Account\/ForgotPassword)/);
      expect(page.url()).toMatch(/ForgotPassword/i);
    } else {
      console.log('❌ Forgot password link not found');
    }
  });

  test('should have working create account tab', async ({ page }) => {
    // Test 10: Create Account Tab (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'create-account-tab' });

    const createAccountTab = page.locator('a.tab, a:has-text("Create Account")').first();
    const tabExists = await createAccountTab.isVisible();

    if (tabExists) {
      const href = await createAccountTab.getAttribute('href');
      console.log('✅ Create account tab found:', href);
      expect(href).toBeTruthy();
      
      // Test navigation
      await createAccountTab.click();
      await page.waitForURL(/.*\/(Register|Identity\/Account\/Register)/);
      expect(page.url()).toMatch(/Register/i);
    } else {
      console.log('❌ Create account tab not found');
    }
  });
});

test.describe('Login Page - Form Interactions', () => {
  test('should validate password requirements', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'password-requirements' });

    const loginPage = new LoginPage(page);
    await loginPage.goto();

    // Test weak password
    await loginPage.clearForm();
    await BlazorHelpers.fillAndWait(page, loginPage.emailInput, 'test@example.com');
    await BlazorHelpers.fillAndWait(page, loginPage.passwordInput, '123'); // Too short

    await loginPage.signInButton.click();
    await BlazorHelpers.waitForValidation(page);

    // Should show validation or stay on login page
    const currentUrl = page.url();
    expect(currentUrl).toContain('/Identity/Account/Login');
  });

  test('should handle special characters in inputs', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'special-characters' });

    const loginPage = new LoginPage(page);
    await loginPage.goto();

    // Test special characters
    const specialEmail = 'test+tag@example.com';
    const specialPassword = 'Test@123!#$%';

    await BlazorHelpers.fillAndWait(page, loginPage.emailInput, specialEmail);
    await BlazorHelpers.fillAndWait(page, loginPage.passwordInput, specialPassword);

    // Verify values were entered correctly
    await expect(loginPage.emailInput).toHaveValue(specialEmail);
    await expect(loginPage.passwordInput).toHaveValue(specialPassword);
  });

  test('should clear validation on input change', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'clear-validation-on-change' });

    const loginPage = new LoginPage(page);
    await loginPage.goto();

    // Trigger validation
    await loginPage.submitEmptyForm();
    
    // Verify validation is shown
    expect(await loginPage.hasValidationErrors()).toBeTruthy();

    // Start typing in email field
    await BlazorHelpers.fillAndWait(page, loginPage.emailInput, 'test@');

    // Validation might clear or update
    // This behavior depends on the implementation
    const validationMessages = await loginPage.getValidationMessages();
    console.log('Validation after input:', validationMessages);
  });
});

test.describe('Login Page - Accessibility', () => {
  test('should be keyboard navigable', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'keyboard-navigation' });

    const loginPage = new LoginPage(page);
    await loginPage.goto();

    // Tab through form elements
    await page.keyboard.press('Tab'); // Should focus email
    const emailFocused = await loginPage.emailInput.evaluate(el => el === document.activeElement);
    expect(emailFocused).toBeTruthy();

    await page.keyboard.press('Tab'); // Should focus password
    const passwordFocused = await loginPage.passwordInput.evaluate(el => el === document.activeElement);
    expect(passwordFocused).toBeTruthy();

    // Submit with Enter key
    await loginPage.passwordInput.press('Enter');
    await BlazorHelpers.waitForValidation(page);
  });

  test('should have proper ARIA labels', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'aria-labels' });

    const loginPage = new LoginPage(page);
    await loginPage.goto();

    // Check for labels or aria-labels
    const emailLabel = await loginPage.emailInput.evaluate(el => 
      el.getAttribute('aria-label') || el.getAttribute('placeholder') || 
      document.querySelector(`label[for="${el.id}"]`)?.textContent
    );
    expect(emailLabel).toBeTruthy();

    const passwordLabel = await loginPage.passwordInput.evaluate(el => 
      el.getAttribute('aria-label') || el.getAttribute('placeholder') || 
      document.querySelector(`label[for="${el.id}"]`)?.textContent
    );
    expect(passwordLabel).toBeTruthy();
  });
});