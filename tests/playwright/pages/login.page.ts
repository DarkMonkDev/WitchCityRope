import { Page, Locator } from '@playwright/test';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { testConfig } from '../helpers/test.config';

/**
 * Page Object Model for the Login page
 * Encapsulates all login page interactions and validations
 */
export class LoginPage {
  readonly page: Page;
  
  // Page elements
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly signInButton: Locator;
  readonly forgotPasswordLink: Locator;
  readonly registerLink: Locator;
  readonly rememberMeCheckbox: Locator;
  readonly validationSummary: Locator;
  readonly pageTitle: Locator;
  
  // Validation message locators
  readonly emailValidation: Locator;
  readonly passwordValidation: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Initialize locators for Identity page
    this.emailInput = page.locator('input[name="Input.EmailOrUsername"]');
    this.passwordInput = page.locator('input[name="Input.Password"]');
    this.signInButton = page.locator('button[type="submit"]');
    this.forgotPasswordLink = page.locator('a.auth-footer-link:has-text("Forgot your password?"), a[href*="password-reset"], a#forgot-password').first();
    // The login page has both a tab button and a link in the footer
    this.registerLink = page.locator('button.wcr-auth-tab:has-text("Create Account"), a:has-text("Sign up")').first();
    // For ASP.NET Core checkboxes, use the checkbox type to avoid the hidden input
    this.rememberMeCheckbox = page.locator('input[type="checkbox"][name="Input.RememberMe"]');
    // Identity pages use ASP.NET Core validation classes
    this.validationSummary = page.locator('.validation-summary-errors, .wcr-validation-summary');
    this.pageTitle = page.locator('.wcr-auth-title, h1:has-text("Welcome")').first();
    
    // Identity validation messages use field-validation-error class when errors are present
    // The validation span is always present but gets the field-validation-error class when there's an error
    this.emailValidation = page.locator('span[data-valmsg-for="Input.EmailOrUsername"]');
    this.passwordValidation = page.locator('span[data-valmsg-for="Input.Password"]');
  }

  /**
   * Navigate to the login page
   */
  async goto(): Promise<void> {
    // Use relative URL - Playwright will prepend the baseURL from config
    await this.page.goto(testConfig.urls.login);
    // No need to wait for Blazor - Identity pages are Razor Pages, not Blazor components
    
    // Wait for login form to be visible
    await this.emailInput.waitFor({ state: 'visible', timeout: testConfig.timeouts.navigation });
  }

  /**
   * Perform login with given credentials
   * Using EXACT approach from working simple-login-test.js
   */
  async login(email: string, password: string, rememberMe: boolean = false): Promise<void> {
    // Fill in credentials - using direct fill like working test
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    
    // Check remember me if requested
    if (rememberMe) {
      await this.rememberMeCheckbox.check();
    }
    
    // Submit the form - using direct click like working test
    const [response] = await Promise.all([
      this.page.waitForLoadState('networkidle'),
      this.signInButton.click()
    ]);
    
    // Wait for processing
    await this.page.waitForTimeout(2000);
  }

  /**
   * Quick login using test account
   */
  async loginAsAdmin(): Promise<void> {
    await this.login(testConfig.accounts.admin.email, testConfig.accounts.admin.password);
  }

  async loginAsMember(): Promise<void> {
    await this.login(testConfig.accounts.member.email, testConfig.accounts.member.password);
  }

  /**
   * Verify successful login by checking navigation
   * Updated to handle Identity redirect patterns
   */
  async verifyLoginSuccess(expectedUrl?: string | RegExp): Promise<void> {
    // Wait for any redirect to complete
    await this.page.waitForTimeout(3000);
    
    const currentUrl = this.page.url();
    console.log('🔗 Current URL after login:', currentUrl);
    
    // Check for successful redirect patterns
    const isSuccessful = currentUrl.includes('ReturnUrl=') || 
                        currentUrl.includes('/dashboard') || 
                        currentUrl.includes('/admin') ||
                        currentUrl.includes('/member') ||
                        (!currentUrl.includes('/Identity/Account/Login') && !currentUrl.includes('/login'));
    
    if (!isSuccessful) {
      throw new Error(`Login failed - still on login page: ${currentUrl}`);
    }
    
    console.log('✅ Login verification successful');
  }

  /**
   * Test empty form submission to trigger validation
   */
  async submitEmptyForm(): Promise<void> {
    await this.signInButton.click();
    // Wait for server-side validation response
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Check if validation messages are displayed
   */
  async hasValidationErrors(): Promise<boolean> {
    try {
      // Check for validation summary errors first (ModelOnly errors)
      const summaryErrors = await this.validationSummary.isVisible();
      if (summaryErrors) return true;
      
      // Check for field validation errors (ASP.NET Core Identity uses field-validation-error class)
      const fieldErrors = this.page.locator('.field-validation-error, .wcr-validation-message');
      const count = await fieldErrors.count();
      return count > 0;
    } catch {
      return false;
    }
  }

  /**
   * Get all validation messages
   */
  async getValidationMessages(): Promise<string[]> {
    const messages: string[] = [];
    
    // Get validation summary messages
    const summaryMessages = await this.validationSummary.locator('ul li').allTextContents();
    messages.push(...summaryMessages);
    
    // Get field validation messages (ASP.NET Core Identity)
    const fieldMessages = await this.page.locator('.field-validation-error, .wcr-validation-message').allTextContents();
    messages.push(...fieldMessages);
    
    return messages.filter(msg => msg.trim().length > 0);
  }

  /**
   * Check if specific field has validation error
   */
  async hasFieldValidationError(field: 'email' | 'password'): Promise<boolean> {
    const validationLocator = field === 'email' ? this.emailValidation : this.passwordValidation;
    // Check if the validation span has the error class and contains text
    const hasErrorClass = await validationLocator.evaluate(el => 
      el.classList.contains('field-validation-error')
    );
    const hasText = await validationLocator.textContent().then(text => text?.trim().length > 0);
    return hasErrorClass && hasText;
  }

  /**
   * Navigate to forgot password page
   */
  async clickForgotPassword(): Promise<void> {
    await this.forgotPasswordLink.click();
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Navigate to registration page
   */
  async clickRegister(): Promise<void> {
    await this.registerLink.click();
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Check if login page is displayed
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
   * Take a screenshot for debugging
   */
  async screenshot(name: string): Promise<void> {
    await this.page.screenshot({ 
      path: `test-results/screenshots/login-${name}.png`,
      fullPage: true 
    });
  }

  /**
   * Get page title text
   */
  async getPageTitle(): Promise<string> {
    return await this.pageTitle.textContent() || '';
  }

  /**
   * Clear form inputs
   */
  async clearForm(): Promise<void> {
    await this.emailInput.clear();
    await this.passwordInput.clear();
    if (await this.rememberMeCheckbox.isChecked()) {
      await this.rememberMeCheckbox.uncheck();
    }
  }
}