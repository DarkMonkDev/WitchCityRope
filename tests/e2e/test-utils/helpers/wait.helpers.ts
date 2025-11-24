import { Page, expect, Response } from '@playwright/test';

// Timeout configurations for consistent test execution
// ABSOLUTE MAXIMUM: 90 seconds (enforced in playwright.config.ts)
// WHY 90 SECONDS: No test should need more than 60 seconds realistically,
// but we use 90 seconds as a safety buffer. Tests consistently hitting these
// limits indicate problems (slow backend, infinite loops, etc.) that should be fixed.
export const TIMEOUTS = {
  SHORT: 5000,        // 5 seconds - Quick UI interactions
  MEDIUM: 10000,      // 10 seconds - Standard waits
  LONG: 30000,        // 30 seconds - Complex operations (DO NOT INCREASE)
  NAVIGATION: 30000,  // 30 seconds - Page navigation
  API_RESPONSE: 10000,// 10 seconds - API calls (should be fast)
  AUTHENTICATION: 15000, // 15 seconds - Auth flows
  FORM_SUBMISSION: 20000, // 20 seconds - Form processing
  PAGE_LOAD: 30000,   // 30 seconds - Full page load
  ABSOLUTE_MAX: 90000 // 90 seconds - NEVER EXCEED THIS
};

/**
 * Wait strategy helpers for Playwright tests
 * Handles React-specific loading states and API interactions
 */
export class WaitHelpers {
  /**
   * Wait for page to fully load with React hydration
   */
  static async waitForPageLoad(page: Page, expectedUrl?: string, timeout: number = TIMEOUTS.PAGE_LOAD) {
    // Wait for network to be idle
    await page.waitForLoadState('networkidle', { timeout });
    
    // If specific URL expected, wait for it
    if (expectedUrl) {
      await page.waitForURL(expectedUrl, { timeout });
    }
    
    // Wait for React to hydrate (check for React root)
    await page.waitForSelector('#root', { state: 'attached', timeout });
    
    // Additional wait for any loading spinners to disappear
    const loadingSelectors = [
      '[data-testid="loading-spinner"]',
      '[data-testid="page-loader"]', 
      '.loading',
      '.spinner'
    ];
    
    for (const selector of loadingSelectors) {
      const loadingElement = page.locator(selector);
      if (await loadingElement.count() > 0) {
        await expect(loadingElement).not.toBeVisible({ timeout: 5000 });
      }
    }
  }

  /**
   * Wait for specific API response
   */
  static async waitForApiResponse(page: Page, apiPath: string, options: {
    method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
    status?: number;
    timeout?: number;
  } = {}): Promise<Response> {
    const { method, status, timeout = TIMEOUTS.API_RESPONSE } = options;
    
    return page.waitForResponse(response => {
      const urlMatch = response.url().includes(apiPath);
      const methodMatch = !method || response.request().method() === method;
      const statusMatch = !status || response.status() === status;
      
      return urlMatch && methodMatch && statusMatch;
    }, { timeout });
  }

  /**
   * Wait for successful API response (2xx status)
   */
  static async waitForSuccessfulApiCall(page: Page, apiPath: string, method: string = 'GET'): Promise<Response> {
    return page.waitForResponse(response => 
      response.url().includes(apiPath) && 
      response.request().method() === method &&
      response.status() >= 200 && response.status() < 300
    );
  }

  /**
   * Wait for API error response
   */
  static async waitForApiError(page: Page, apiPath: string, expectedStatus?: number): Promise<Response> {
    return page.waitForResponse(response => {
      const urlMatch = response.url().includes(apiPath);
      const isError = response.status() >= 400;
      const statusMatch = !expectedStatus || response.status() === expectedStatus;
      
      return urlMatch && isError && statusMatch;
    });
  }

  /**
   * Wait for React component to render with specific data-testid
   */
  static async waitForComponent(page: Page, testId: string, options: {
    state?: 'attached' | 'visible' | 'hidden';
    timeout?: number;
  } = {}) {
    const { state = 'visible', timeout = TIMEOUTS.MEDIUM } = options;
    const selector = `[data-testid="${testId}"]`;
    
    await page.waitForSelector(selector, { state, timeout });
    return page.locator(selector);
  }

  /**
   * Wait for navigation to complete (useful for SPA routing)
   * Enhanced with network idle wait for authentication flows
   */
  static async waitForNavigation(page: Page, expectedUrl: string, timeout: number = TIMEOUTS.NAVIGATION) {
    // Wait for URL to change
    await page.waitForURL(expectedUrl, { 
      timeout,
      waitUntil: 'networkidle' 
    });
    
    // Wait for page to be interactive
    await page.waitForLoadState('domcontentloaded');
    
    // Wait for network to be idle (important for API calls after navigation)
    await page.waitForLoadState('networkidle', { timeout: TIMEOUTS.MEDIUM });
    
    // Wait for React routing to complete
    await page.waitForTimeout(200);
  }

  /**
   * Wait for form submission to complete
   */
  static async waitForFormSubmission(page: Page, submitButtonTestId: string = 'submit-button') {
    const submitButton = page.locator(`[data-testid="${submitButtonTestId}"]`);
    
    // Wait for button to show loading state
    await page.waitForTimeout(200);
    
    // Wait for loading state to complete (button re-enabled)
    await expect(submitButton).not.toHaveAttribute('disabled', { timeout: TIMEOUTS.FORM_SUBMISSION });
    
    // Verify loading text has cleared
    const buttonText = await submitButton.textContent();
    expect(buttonText?.toLowerCase()).not.toMatch(/loading|submitting|processing/);
    
    // Wait for network idle to ensure API call completed
    await page.waitForLoadState('networkidle', { timeout: TIMEOUTS.MEDIUM });
  }

  /**
   * Wait for element count to stabilize (useful for dynamic lists)
   */
  static async waitForElementCount(
    page: Page, 
    selector: string, 
    expectedCount: number, 
    timeout: number = 5000
  ) {
    let attempts = 0;
    const maxAttempts = timeout / 100;
    
    while (attempts < maxAttempts) {
      const count = await page.locator(selector).count();
      if (count === expectedCount) {
        return true;
      }
      await page.waitForTimeout(100);
      attempts++;
    }
    
    throw new Error(`Expected ${expectedCount} elements matching "${selector}", but found ${await page.locator(selector).count()}`);
  }

  /**
   * Wait for text content to appear in element
   */
  static async waitForTextContent(
    page: Page, 
    selector: string, 
    expectedText: string | RegExp,
    timeout: number = 5000
  ) {
    const element = page.locator(selector);
    
    if (typeof expectedText === 'string') {
      await expect(element).toContainText(expectedText, { timeout });
    } else {
      await expect(element).toContainText(expectedText, { timeout });
    }
    
    return element;
  }

  /**
   * Wait for element attribute to have specific value
   */
  static async waitForAttribute(
    page: Page,
    selector: string,
    attribute: string,
    expectedValue: string | RegExp,
    timeout: number = 5000
  ) {
    const element = page.locator(selector);
    await expect(element).toHaveAttribute(attribute, expectedValue, { timeout });
    return element;
  }

  /**
   * Wait for multiple conditions to be met
   */
  static async waitForConditions(page: Page, conditions: Array<() => Promise<void>>, timeout: number = 10000) {
    const startTime = Date.now();
    
    while (Date.now() - startTime < timeout) {
      try {
        await Promise.all(conditions.map(condition => condition()));
        return; // All conditions met
      } catch (error) {
        // Continue waiting
        await page.waitForTimeout(100);
      }
    }
    
    throw new Error(`Timeout: Not all conditions were met within ${timeout}ms`);
  }

  /**
   * Wait for React state updates to complete (useful after user interactions)
   */
  static async waitForStateUpdate(page: Page, delay: number = 100) {
    // Give React time to process state updates
    await page.waitForTimeout(delay);
    
    // Wait for any pending network requests to complete
    await page.waitForLoadState('networkidle', { timeout: 2000 });
  }

  /**
   * Wait for modal or dialog to appear/disappear
   */
  static async waitForModal(page: Page, modalTestId: string, shouldBeVisible: boolean = true, timeout: number = 5000) {
    const modal = page.locator(`[data-testid="${modalTestId}"]`);
    
    if (shouldBeVisible) {
      await expect(modal).toBeVisible({ timeout });
      // Also check for modal backdrop
      const backdrop = page.locator('.mantine-Modal-backdrop, .modal-backdrop');
      if (await backdrop.count() > 0) {
        await expect(backdrop).toBeVisible({ timeout: 1000 });
      }
    } else {
      await expect(modal).not.toBeVisible({ timeout });
    }
    
    return modal;
  }

  /**
   * Wait for toast notification
   */
  static async waitForNotification(
    page: Page, 
    type: 'success' | 'error' | 'warning' | 'info' = 'success',
    message?: string,
    timeout: number = 5000
  ) {
    const notificationSelectors = [
      `[data-testid="notification-${type}"]`,
      `.notification-${type}`,
      `.mantine-Notification`,
      '.toast',
      '[role="alert"]'
    ];
    
    let notification;
    for (const selector of notificationSelectors) {
      notification = page.locator(selector);
      if (await notification.count() > 0) {
        await expect(notification).toBeVisible({ timeout });
        break;
      }
    }
    
    if (!notification) {
      throw new Error(`No notification found of type: ${type}`);
    }
    
    if (message) {
      await expect(notification).toContainText(message, { timeout: 1000 });
    }
    
    return notification;
  }

  /**
   * Wait for data loading to complete (useful for API-driven components)
   */
  static async waitForDataLoad(page: Page, componentTestId: string, timeout: number = TIMEOUTS.LONG) {
    const component = page.locator(`[data-testid="${componentTestId}"]`);
    
    // First, wait for component to exist
    await expect(component).toBeVisible({ timeout: 5000 });
    
    // Wait for any loading states to clear
    const loadingStates = [
      component.locator('[data-testid*="loading"]'),
      component.locator('.loading'),
      component.locator('.spinner'),
      component.locator('[aria-label="Loading"]')
    ];
    
    for (const loadingState of loadingStates) {
      if (await loadingState.count() > 0) {
        await expect(loadingState).not.toBeVisible({ timeout });
      }
    }
    
    // Wait for actual content to appear
    await page.waitForTimeout(100);
    
    return component;
  }

  /**
   * Wait for all images to load
   */
  static async waitForImages(page: Page, timeout: number = 10000) {
    await page.waitForLoadState('networkidle', { timeout });
    
    // Check that all images have loaded
    const images = page.locator('img');
    const imageCount = await images.count();
    
    for (let i = 0; i < imageCount; i++) {
      const image = images.nth(i);
      await expect(image).toHaveAttribute('src');
      
      // Wait for image to load
      await page.waitForFunction(
        (img) => img.complete && img.naturalHeight !== 0,
        await image.elementHandle(),
        { timeout: 2000 }
      ).catch(() => {
        // Continue if individual image fails to load
        console.warn(`Image ${i} failed to load within timeout`);
      });
    }
  }
}