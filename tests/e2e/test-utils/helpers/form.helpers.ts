import { Page, expect } from '@playwright/test';
import { WaitHelpers, TIMEOUTS } from './wait.helpers';

/**
 * Form interaction helpers with enhanced timeout configurations
 * Specifically designed for Mantine UI components
 */
export class FormHelpers {
  /**
   * Fill form data with proper wait strategies
   */
  static async fillFormData(page: Page, formData: Record<string, string>, formSelector?: string) {
    const baseSelector = formSelector ? `${formSelector} ` : '';
    
    for (const [field, value] of Object.entries(formData)) {
      const inputSelector = `${baseSelector}[data-testid="${field}-input"], ${baseSelector}[name="${field}"], ${baseSelector}#${field}`;
      const input = page.locator(inputSelector);
      
      // Wait for input to be visible and enabled
      await expect(input).toBeVisible({ timeout: TIMEOUTS.MEDIUM });
      await expect(input).not.toHaveAttribute('disabled', { timeout: TIMEOUTS.SHORT });
      
      // Clear and fill input
      await input.clear();
      await input.fill(value);
      
      // Verify value was set
      await expect(input).toHaveValue(value, { timeout: TIMEOUTS.SHORT });
      
      // Allow time for React state updates
      await page.waitForTimeout(100);
    }
  }

  /**
   * Submit form and wait for response
   */
  static async submitForm(page: Page, submitButtonSelector: string = '[data-testid="submit-button"]', expectedUrl?: string) {
    const submitButton = page.locator(submitButtonSelector);
    
    // Wait for button to be ready
    await expect(submitButton).toBeVisible({ timeout: TIMEOUTS.MEDIUM });
    await expect(submitButton).not.toHaveAttribute('disabled', { timeout: TIMEOUTS.SHORT });
    
    // Click submit button
    await submitButton.click();
    
    // Wait for form submission to complete
    await WaitHelpers.waitForFormSubmission(page, 'submit-button');
    
    // If expected URL provided, wait for navigation
    if (expectedUrl) {
      await WaitHelpers.waitForNavigation(page, expectedUrl);
    }
  }

  /**
   * Wait for form validation error
   */
  static async waitForFormError(page: Page, errorTestId: string, expectedMessage?: string) {
    const errorElement = page.locator(`[data-testid="${errorTestId}"]`);
    
    await expect(errorElement).toBeVisible({ timeout: TIMEOUTS.MEDIUM });
    
    if (expectedMessage) {
      await expect(errorElement).toContainText(expectedMessage, { timeout: TIMEOUTS.SHORT });
    }
    
    return errorElement;
  }

  /**
   * Wait for form validation success
   */
  static async waitForFormSuccess(page: Page, successTestId: string = 'success-message', expectedMessage?: string) {
    const successElement = page.locator(`[data-testid="${successTestId}"]`);
    
    await expect(successElement).toBeVisible({ timeout: TIMEOUTS.MEDIUM });
    
    if (expectedMessage) {
      await expect(successElement).toContainText(expectedMessage, { timeout: TIMEOUTS.SHORT });
    }
    
    return successElement;
  }

  /**
   * Toggle checkbox or switch component
   */
  static async toggleCheckbox(page: Page, checkboxTestId: string, shouldCheck?: boolean) {
    const checkbox = page.locator(`[data-testid="${checkboxTestId}"]`);
    
    await expect(checkbox).toBeVisible({ timeout: TIMEOUTS.MEDIUM });
    
    // Get current state if shouldCheck not specified
    if (shouldCheck === undefined) {
      await checkbox.click();
    } else {
      const isChecked = await checkbox.isChecked();
      if (isChecked !== shouldCheck) {
        await checkbox.click();
      }
    }
    
    // Allow time for state update
    await page.waitForTimeout(200);
  }

  /**
   * Select option from dropdown
   */
  static async selectDropdownOption(page: Page, dropdownTestId: string, optionValue: string | number) {
    const dropdown = page.locator(`[data-testid="${dropdownTestId}"]`);
    
    await expect(dropdown).toBeVisible({ timeout: TIMEOUTS.MEDIUM });
    
    // Click to open dropdown
    await dropdown.click();
    
    // Wait for dropdown options to appear
    await page.waitForTimeout(300);
    
    // Select option by value or text
    const option = page.locator(`[data-testid="${dropdownTestId}-option-${optionValue}"], [value="${optionValue}"]`);
    if (await option.count() === 0) {
      // Fallback: select by text content
      await page.locator(`text="${optionValue}"`).click();
    } else {
      await option.click();
    }
    
    // Allow time for selection to register
    await page.waitForTimeout(200);
  }

  /**
   * Fill and submit login form specifically
   */
  static async fillAndSubmitLoginForm(page: Page, email: string, password: string) {
    // Wait for form to be ready
    await expect(page.locator('[data-testid="email-input"]')).toBeVisible({ timeout: TIMEOUTS.MEDIUM });
    await expect(page.locator('[data-testid="password-input"]')).toBeVisible({ timeout: TIMEOUTS.MEDIUM });
    await expect(page.locator('[data-testid="login-button"]')).toBeVisible({ timeout: TIMEOUTS.MEDIUM });
    
    // Fill form
    await page.locator('[data-testid="email-input"]').fill(email);
    await page.locator('[data-testid="password-input"]').fill(password);
    
    // Verify form is ready for submission
    await expect(page.locator('[data-testid="login-button"]')).not.toHaveAttribute('disabled');
    
    // Set up API monitoring before submit
    const loginResponsePromise = WaitHelpers.waitForApiResponse(page, '/api/auth/login', {
      method: 'POST',
      timeout: TIMEOUTS.AUTHENTICATION
    });
    
    // Submit form
    await page.locator('[data-testid="login-button"]').click();
    
    // Wait for API response
    return loginResponsePromise;
  }

  /**
   * Clear all form inputs
   */
  static async clearForm(page: Page, formSelector?: string) {
    const baseSelector = formSelector ? `${formSelector} ` : '';
    const inputs = page.locator(`${baseSelector}input[type="text"], ${baseSelector}input[type="email"], ${baseSelector}input[type="password"], ${baseSelector}textarea`);
    
    const inputCount = await inputs.count();
    
    for (let i = 0; i < inputCount; i++) {
      const input = inputs.nth(i);
      if (await input.isVisible()) {
        await input.clear();
      }
    }
    
    // Allow time for React state updates
    await page.waitForTimeout(200);
  }

  /**
   * Verify form validation state
   */
  static async verifyFormValidation(page: Page, fieldTestId: string, shouldBeValid: boolean) {
    const field = page.locator(`[data-testid="${fieldTestId}"]`);
    
    if (shouldBeValid) {
      // Check that no error message is visible
      const errorMessage = page.locator(`[data-testid="${fieldTestId}-error"]`);
      if (await errorMessage.count() > 0) {
        await expect(errorMessage).not.toBeVisible({ timeout: TIMEOUTS.SHORT });
      }
      
      // Check for success indicators
      const successIndicator = page.locator(`[data-testid="${fieldTestId}-success"]`);
      if (await successIndicator.count() > 0) {
        await expect(successIndicator).toBeVisible({ timeout: TIMEOUTS.SHORT });
      }
    } else {
      // Check that error message is visible
      const errorMessage = page.locator(`[data-testid="${fieldTestId}-error"]`);
      await expect(errorMessage).toBeVisible({ timeout: TIMEOUTS.MEDIUM });
    }
  }

  /**
   * Wait for form loading state to complete
   */
  static async waitForFormReady(page: Page, formSelector?: string) {
    const baseSelector = formSelector ? `${formSelector} ` : '';
    
    // Wait for form to be visible
    const form = page.locator(`${baseSelector}form, ${baseSelector}[data-testid*="form"]`);
    if (await form.count() > 0) {
      await expect(form).toBeVisible({ timeout: TIMEOUTS.MEDIUM });
    }
    
    // Wait for any loading spinners in form to disappear
    const loadingElements = page.locator(`${baseSelector}[data-testid*="loading"], ${baseSelector}.loading`);
    if (await loadingElements.count() > 0) {
      await expect(loadingElements).not.toBeVisible({ timeout: TIMEOUTS.MEDIUM });
    }
    
    // Allow time for React hydration
    await page.waitForTimeout(300);
  }
}