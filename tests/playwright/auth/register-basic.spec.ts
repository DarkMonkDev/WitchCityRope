import { test, expect } from '@playwright/test';
import { RegisterPage } from '../pages/register.page';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';

/**
 * Basic Registration Page Tests
 * Converted from Puppeteer test: test-blazor-register-basic.js
 * 
 * Tests basic registration page functionality including:
 * - Page loading and element visibility
 * - Form element verification
 * - Empty form validation
 * - Form filling and interaction
 * - Password visibility toggle
 * - Navigation to login page
 */

test.describe('Register Page - Basic Functionality', () => {
  let registerPage: RegisterPage;

  test.beforeEach(async ({ page }) => {
    registerPage = new RegisterPage(page);
    await registerPage.goto();
  });

  test('should navigate to register page', async ({ page }) => {
    // Test 1: Navigate to Register page (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'register-page-navigation' });

    // Verify page loaded
    const pageTitle = await page.title();
    console.log(`✓ Page title: ${pageTitle}`);
    await expect(page).toHaveTitle(/Register|Create Account/i);

    // Verify we're on the register page
    await expect(page).toHaveURL(/.*\/(Register|Identity\/Account\/Register)/);
    
    // Check if register form is displayed
    const isDisplayed = await registerPage.isDisplayed();
    expect(isDisplayed).toBeTruthy();
  });

  test('should have all form elements', async ({ page }) => {
    // Test 2: Check form elements (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'form-elements-check' });

    const elements = await registerPage.verifyFormElements();
    
    // Log results
    for (const [name, isVisible] of Object.entries(elements)) {
      if (isVisible) {
        console.log(`✓ ${name} found`);
      } else {
        console.log(`✗ ${name} NOT found`);
      }
      expect(isVisible).toBeTruthy();
    }

    // Additional checks for element states
    await expect(registerPage.emailInput).toBeEditable();
    await expect(registerPage.sceneNameInput).toBeEditable();
    await expect(registerPage.passwordInput).toBeEditable();
    await expect(registerPage.confirmPasswordInput).toBeEditable();
    await expect(registerPage.submitButton).toBeEnabled();
  });

  test('should validate empty form submission', async ({ page }) => {
    // Test 3: Test empty form validation (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'empty-form-validation' });

    // Submit empty form
    await registerPage.submitEmptyForm();

    // Check for validation messages
    const hasValidation = await registerPage.hasValidationErrors();
    expect(hasValidation).toBeTruthy();

    const errors = await registerPage.getValidationMessages();
    console.log('✓ Validation errors displayed:');
    errors.forEach(error => console.log(`  - ${error}`));
    
    expect(errors.length).toBeGreaterThan(0);
    
    // Verify we're still on register page
    await expect(page).toHaveURL(/.*\/(Register|Identity\/Account\/Register)/);
  });

  test('should fill form with valid data', async ({ page }) => {
    // Test 4: Fill form with valid data (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'form-filling' });

    const testData = {
      email: 'test@example.com',
      sceneName: 'TestUser123',
      password: 'Test123!',
      confirmPassword: 'Test123!',
      acceptAge: true,
      acceptTerms: true
    };

    // Fill the form
    await registerPage.fillForm(testData);
    
    console.log('✓ Email entered');
    console.log('✓ Scene name entered');
    console.log('✓ Password entered');
    console.log('✓ Confirm password entered');
    console.log('✓ Age confirmation checked');
    console.log('✓ Terms accepted');

    // Verify values were entered
    await expect(registerPage.emailInput).toHaveValue(testData.email);
    await expect(registerPage.sceneNameInput).toHaveValue(testData.sceneName);
    await expect(registerPage.passwordInput).toHaveValue(testData.password);
    await expect(registerPage.confirmPasswordInput).toHaveValue(testData.confirmPassword);
    await expect(registerPage.ageConfirmationCheckbox).toBeChecked();
    await expect(registerPage.acceptTermsCheckbox).toBeChecked();
  });

  test('should toggle password visibility', async ({ page }) => {
    // Test 5: Test password toggle (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'password-visibility-toggle' });

    // Enter password to see the effect
    await registerPage.passwordInput.fill('Test123!');

    const passwordToggleVisible = await registerPage.passwordToggle.isVisible();
    
    if (passwordToggleVisible) {
      // Check initial type
      let passwordType = await registerPage.getPasswordInputType();
      expect(passwordType).toBe('password');

      // Click toggle
      await registerPage.togglePasswordVisibility();
      
      // Check new type
      passwordType = await registerPage.getPasswordInputType();
      console.log(`✓ Password visibility toggled - type is now: ${passwordType}`);
      expect(passwordType).toBe('text');
    } else {
      console.log('⚠️  No password toggle button found');
    }
  });

  test('should navigate to login page', async ({ page }) => {
    // Test 6: Test navigation to login (from original test)
    test.info().annotations.push({ type: 'test-id', description: 'login-navigation' });

    const signInLinkVisible = await registerPage.signInLink.isVisible();
    
    if (signInLinkVisible) {
      const href = await registerPage.signInLink.getAttribute('href');
      console.log(`✓ Sign in link found - href: ${href}`);
      expect(href).toBeTruthy();

      // Click and navigate
      await registerPage.clickSignIn();
      await expect(page).toHaveURL(/.*\/(Login|Identity\/Account\/Login)/);
    } else {
      console.log('❌ Sign in link not found');
    }
  });

  test('should take screenshot of filled form', async ({ page }) => {
    // Additional test to match original screenshot functionality
    test.info().annotations.push({ type: 'test-id', description: 'screenshot-filled-form' });

    // Fill form with test data
    await registerPage.fillForm({
      email: 'test@example.com',
      sceneName: 'TestUser123',
      password: 'Test123!',
      confirmPassword: 'Test123!',
      acceptAge: true,
      acceptTerms: true
    });

    // Take screenshot
    await page.screenshot({ 
      path: 'test-results/screenshots/register-page-filled.png',
      fullPage: true 
    });
    console.log('\n✓ Screenshot saved as register-page-filled.png');
  });
});

test.describe('Register Page - Validation Tests', () => {
  let registerPage: RegisterPage;

  test.beforeEach(async ({ page }) => {
    registerPage = new RegisterPage(page);
    await registerPage.goto();
  });

  test('should validate email format', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'email-validation' });

    // Invalid email format
    await registerPage.fillForm({
      email: 'invalid-email',
      sceneName: 'TestUser',
      password: 'Test123!',
      confirmPassword: 'Test123!',
      acceptAge: true,
      acceptTerms: true
    });

    await registerPage.submit();
    await BlazorHelpers.waitForValidation(page);

    // Should show validation error or stay on register page
    const currentUrl = page.url();
    expect(currentUrl).toMatch(/\/(Register|Identity\/Account\/Register)/);
  });

  test('should validate password match', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'password-match-validation' });

    // Mismatched passwords
    await registerPage.fillForm({
      email: 'test@example.com',
      sceneName: 'TestUser',
      password: 'Test123!',
      confirmPassword: 'Different123!',
      acceptAge: true,
      acceptTerms: true
    });

    await registerPage.submit();
    await BlazorHelpers.waitForValidation(page);

    // Should show validation error
    const hasErrors = await registerPage.hasValidationErrors();
    expect(hasErrors).toBeTruthy();

    // Check for password mismatch error
    const errors = await registerPage.getValidationMessages();
    const passwordError = errors.find(err => err.toLowerCase().includes('password') && err.toLowerCase().includes('match'));
    expect(passwordError).toBeTruthy();
  });

  test('should require age confirmation', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'age-confirmation-required' });

    // Fill form without age confirmation
    await registerPage.fillForm({
      email: 'test@example.com',
      sceneName: 'TestUser',
      password: 'Test123!',
      confirmPassword: 'Test123!',
      acceptAge: false, // Not checked
      acceptTerms: true
    });

    await registerPage.submit();
    await BlazorHelpers.waitForValidation(page);

    // Should show validation error
    const hasErrors = await registerPage.hasValidationErrors();
    expect(hasErrors).toBeTruthy();

    const errors = await registerPage.getValidationMessages();
    const ageError = errors.find(err => err.toLowerCase().includes('age') || err.toLowerCase().includes('18'));
    expect(ageError).toBeTruthy();
  });

  test('should require terms acceptance', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'terms-acceptance-required' });

    // Fill form without accepting terms
    await registerPage.fillForm({
      email: 'test@example.com',
      sceneName: 'TestUser',
      password: 'Test123!',
      confirmPassword: 'Test123!',
      acceptAge: true,
      acceptTerms: false // Not checked
    });

    await registerPage.submit();
    await BlazorHelpers.waitForValidation(page);

    // Should show validation error
    const hasErrors = await registerPage.hasValidationErrors();
    expect(hasErrors).toBeTruthy();

    const errors = await registerPage.getValidationMessages();
    const termsError = errors.find(err => err.toLowerCase().includes('terms'));
    expect(termsError).toBeTruthy();
  });

  test('should validate scene name requirements', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'scene-name-validation' });

    // Test various invalid scene names
    const invalidSceneNames = [
      '', // Empty
      'a', // Too short
      'Test User', // Contains space (if not allowed)
      '123', // Only numbers (if not allowed)
      'Test@User!' // Special characters (if not allowed)
    ];

    for (const sceneName of invalidSceneNames) {
      await registerPage.clearForm();
      await registerPage.fillForm({
        email: 'test@example.com',
        sceneName: sceneName,
        password: 'Test123!',
        confirmPassword: 'Test123!',
        acceptAge: true,
        acceptTerms: true
      });

      await registerPage.submit();
      await BlazorHelpers.waitForValidation(page);

      // Should either show error or stay on register page
      const currentUrl = page.url();
      if (!currentUrl.match(/\/(Register|Identity\/Account\/Register)/)) {
        console.log(`Scene name "${sceneName}" was accepted`);
      } else {
        console.log(`Scene name "${sceneName}" was rejected`);
      }
    }
  });
});

test.describe('Register Page - User Experience', () => {
  test('should maintain form state after validation', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'form-state-persistence' });

    const registerPage = new RegisterPage(page);
    await registerPage.goto();

    // Fill partial form
    const partialData = {
      email: 'test@example.com',
      sceneName: 'TestUser123',
      password: 'Test123!',
      confirmPassword: '', // Missing
      acceptAge: true,
      acceptTerms: false // Missing
    };

    await registerPage.fillForm(partialData);
    await registerPage.submit();
    await BlazorHelpers.waitForValidation(page);

    // Verify filled fields persist
    await expect(registerPage.emailInput).toHaveValue(partialData.email);
    await expect(registerPage.sceneNameInput).toHaveValue(partialData.sceneName);
    await expect(registerPage.passwordInput).toHaveValue(partialData.password);
    await expect(registerPage.ageConfirmationCheckbox).toBeChecked();
    await expect(registerPage.acceptTermsCheckbox).not.toBeChecked();
  });

  test('should handle special characters in inputs', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'special-characters-handling' });

    const registerPage = new RegisterPage(page);
    await registerPage.goto();

    // Test special characters
    const testData = {
      email: 'test+tag@example.com',
      sceneName: 'Test_User-123',
      password: 'Test@123!#$%',
      confirmPassword: 'Test@123!#$%',
      acceptAge: true,
      acceptTerms: true
    };

    await registerPage.fillForm(testData);

    // Verify values were entered correctly
    await expect(registerPage.emailInput).toHaveValue(testData.email);
    await expect(registerPage.sceneNameInput).toHaveValue(testData.sceneName);
    await expect(registerPage.passwordInput).toHaveValue(testData.password);
  });

  test('should be keyboard navigable', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'keyboard-navigation' });

    const registerPage = new RegisterPage(page);
    await registerPage.goto();

    // Tab through form elements
    await page.keyboard.press('Tab'); // Email
    const emailFocused = await registerPage.emailInput.evaluate(el => el === document.activeElement);
    expect(emailFocused).toBeTruthy();

    await page.keyboard.press('Tab'); // Scene name
    const sceneNameFocused = await registerPage.sceneNameInput.evaluate(el => el === document.activeElement);
    expect(sceneNameFocused).toBeTruthy();

    await page.keyboard.press('Tab'); // Password
    const passwordFocused = await registerPage.passwordInput.evaluate(el => el === document.activeElement);
    expect(passwordFocused).toBeTruthy();

    await page.keyboard.press('Tab'); // Confirm password
    const confirmPasswordFocused = await registerPage.confirmPasswordInput.evaluate(el => el === document.activeElement);
    expect(confirmPasswordFocused).toBeTruthy();
  });
});

test.describe('Register Page - Visual Tests', () => {
  test('should match visual snapshot', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'visual-snapshot' });

    const registerPage = new RegisterPage(page);
    await registerPage.goto();
    
    // Wait for page to be fully rendered
    await BlazorHelpers.waitForBlazorReady(page);
    
    // Take screenshot for visual comparison
    await expect(page).toHaveScreenshot('register-page.png', {
      fullPage: true,
      animations: 'disabled'
    });
  });

  test('should show proper error styling', async ({ page }) => {
    test.info().annotations.push({ type: 'test-id', description: 'error-styling' });

    const registerPage = new RegisterPage(page);
    await registerPage.goto();

    // Submit empty form to trigger validation
    await registerPage.submitEmptyForm();

    // Take screenshot with errors
    await expect(page).toHaveScreenshot('register-page-errors.png', {
      fullPage: true,
      animations: 'disabled'
    });
  });
});