import { Page, Locator } from '@playwright/test';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { testConfig } from '../helpers/test.config';

/**
 * Page Object Model for the Register page
 * Encapsulates all registration page interactions and validations
 */
export class RegisterPage {
  readonly page: Page;
  
  // Page elements
  readonly emailInput: Locator;
  readonly sceneNameInput: Locator;
  readonly passwordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly ageConfirmationCheckbox: Locator;
  readonly acceptTermsCheckbox: Locator;
  readonly submitButton: Locator;
  readonly signInLink: Locator;
  readonly validationSummary: Locator;
  readonly pageTitle: Locator;
  readonly passwordToggle: Locator;
  readonly confirmPasswordToggle: Locator;
  
  // Validation message locators
  readonly emailValidation: Locator;
  readonly sceneNameValidation: Locator;
  readonly passwordValidation: Locator;
  readonly confirmPasswordValidation: Locator;
  readonly ageConfirmationValidation: Locator;
  readonly acceptTermsValidation: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Initialize locators for Identity page
    this.emailInput = page.locator('input[name="Input.Email"]');
    this.sceneNameInput = page.locator('input[name="Input.SceneName"]');
    this.passwordInput = page.locator('input[name="Input.Password"]');
    this.confirmPasswordInput = page.locator('input[name="Input.ConfirmPassword"]');
    this.ageConfirmationCheckbox = page.locator('input[name="Input.AgeConfirmation"]');
    this.acceptTermsCheckbox = page.locator('input[name="Input.AcceptTerms"]');
    this.submitButton = page.locator('button#registerSubmit[type="submit"]');
    this.signInLink = page.locator('a:has-text("Sign in"), a.sign-in-link').first();
    this.validationSummary = page.locator('.validation-summary-errors, .text-danger[role="alert"]');
    this.pageTitle = page.locator('h1:has-text("Register"), h2:has-text("Create a new account")').first();
    this.passwordToggle = page.locator('.wcr-password-container .wcr-password-toggle').first();
    this.confirmPasswordToggle = page.locator('.wcr-password-container .wcr-password-toggle').nth(1);
    
    // Validation messages - Identity uses span.text-danger
    this.emailValidation = page.locator('span[data-valmsg-for="Input.Email"]');
    this.sceneNameValidation = page.locator('span[data-valmsg-for="Input.SceneName"]');
    this.passwordValidation = page.locator('span[data-valmsg-for="Input.Password"]');
    this.confirmPasswordValidation = page.locator('span[data-valmsg-for="Input.ConfirmPassword"]');
    this.ageConfirmationValidation = page.locator('span[data-valmsg-for="Input.AgeConfirmation"]');
    this.acceptTermsValidation = page.locator('span[data-valmsg-for="Input.AcceptTerms"]');
  }

  /**
   * Navigate to the register page
   */
  async goto(): Promise<void> {
    // Use relative URL - Playwright will prepend the baseURL from config
    await this.page.goto('/Identity/Account/Register');
    // No need to wait for Blazor - Identity pages are Razor Pages, not Blazor components
    
    // Wait for register form to be visible
    await this.emailInput.waitFor({ state: 'visible', timeout: testConfig.timeouts.navigation });
  }

  /**
   * Fill registration form with provided data
   */
  async fillForm(data: {
    email: string;
    sceneName: string;
    password: string;
    confirmPassword: string;
    acceptAge?: boolean;
    acceptTerms?: boolean;
  }): Promise<void> {
    // Fill text inputs
    await this.emailInput.fill(data.email);
    await this.sceneNameInput.fill(data.sceneName);
    await this.passwordInput.fill(data.password);
    await this.confirmPasswordInput.fill(data.confirmPassword);
    
    // Handle checkboxes
    if (data.acceptAge) {
      await this.ageConfirmationCheckbox.check();
    }
    if (data.acceptTerms) {
      await this.acceptTermsCheckbox.check();
    }
  }

  /**
   * Submit the registration form
   */
  async submit(): Promise<void> {
    await this.submitButton.click();
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Complete registration with all valid data
   */
  async registerNewUser(email?: string, sceneName?: string): Promise<void> {
    const timestamp = Date.now();
    const userData = {
      email: email || `test${timestamp}@example.com`,
      sceneName: sceneName || `TestUser${timestamp}`,
      password: 'Test123!',
      confirmPassword: 'Test123!',
      acceptAge: true,
      acceptTerms: true
    };
    
    await this.fillForm(userData);
    await this.submit();
  }

  /**
   * Test empty form submission to trigger validation
   */
  async submitEmptyForm(): Promise<void> {
    await this.submitButton.click();
    // Wait for server-side validation response
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Get all validation messages
   */
  async getValidationMessages(): Promise<string[]> {
    const messages: string[] = [];
    
    // Get validation summary messages
    const summaryMessages = await this.validationSummary.locator('ul li').allTextContents();
    messages.push(...summaryMessages);
    
    // Get field validation messages (Identity uses span.text-danger)
    const fieldMessages = await this.page.locator('span.text-danger').allTextContents();
    messages.push(...fieldMessages);
    
    return messages.filter(msg => msg.trim().length > 0);
  }

  /**
   * Check if validation summary is displayed
   */
  async hasValidationErrors(): Promise<boolean> {
    try {
      // Check for validation summary errors first
      const summaryVisible = await this.validationSummary.isVisible();
      if (summaryVisible) return true;
      
      // Check for field validation errors
      const fieldErrors = await this.page.locator('span.text-danger').count();
      return fieldErrors > 0;
    } catch {
      return false;
    }
  }

  /**
   * Toggle password visibility
   */
  async togglePasswordVisibility(): Promise<void> {
    await this.passwordToggle.click();
    await this.page.waitForTimeout(100);
  }

  /**
   * Get password input type (password or text)
   */
  async getPasswordInputType(): Promise<string> {
    return await this.passwordInput.evaluate(el => (el as HTMLInputElement).type);
  }

  /**
   * Navigate to sign in page
   */
  async clickSignIn(): Promise<void> {
    await this.signInLink.click();
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Check if register page is displayed
   */
  async isDisplayed(): Promise<boolean> {
    try {
      await this.pageTitle.waitFor({ state: 'visible', timeout: 5000 });
      await this.emailInput.waitFor({ state: 'visible', timeout: 5000 });
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Verify successful registration by checking navigation
   */
  async verifyRegistrationSuccess(expectedUrl?: string | RegExp): Promise<void> {
    // Default to email confirmation or success page
    const targetUrl = expectedUrl || new RegExp('(/email-confirmation|/register-success|/login)');
    
    await this.page.waitForURL(targetUrl, { timeout: 10000 });
    
    // Verify we're no longer on register page
    const currentUrl = this.page.url();
    if (currentUrl.includes('/register') && !currentUrl.includes('success')) {
      throw new Error('Registration failed - still on register page');
    }
  }

  /**
   * Clear form inputs
   */
  async clearForm(): Promise<void> {
    await this.emailInput.clear();
    await this.sceneNameInput.clear();
    await this.passwordInput.clear();
    await this.confirmPasswordInput.clear();
    
    if (await this.ageConfirmationCheckbox.isChecked()) {
      await this.ageConfirmationCheckbox.uncheck();
    }
    if (await this.acceptTermsCheckbox.isChecked()) {
      await this.acceptTermsCheckbox.uncheck();
    }
  }

  /**
   * Check if all form elements exist
   */
  async verifyFormElements(): Promise<{ [key: string]: boolean }> {
    const elements = {
      'Email input': this.emailInput,
      'Scene name input': this.sceneNameInput,
      'Password input': this.passwordInput,
      'Confirm password input': this.confirmPasswordInput,
      'Age checkbox': this.ageConfirmationCheckbox,
      'Terms checkbox': this.acceptTermsCheckbox,
      'Submit button': this.submitButton,
      'Sign in link': this.signInLink
    };
    
    const results: { [key: string]: boolean } = {};
    
    for (const [name, element] of Object.entries(elements)) {
      results[name] = await element.isVisible();
    }
    
    return results;
  }
}