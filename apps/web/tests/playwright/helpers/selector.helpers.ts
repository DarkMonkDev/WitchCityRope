import { Page, Locator } from '@playwright/test';

/**
 * Selector Helpers for E2E Tests
 *
 * PURPOSE: Prevent test hangs by providing clear error messages
 * when elements are not found, instead of silent 30-second timeouts.
 *
 * CRITICAL: This addresses the primary E2E reliability issue - tests
 * hanging when selectors don't match actual page elements.
 */

export interface WaitForElementOptions {
  timeout?: number;
  errorMessage?: string;
  state?: 'attached' | 'detached' | 'visible' | 'hidden';
}

export class SelectorHelpers {
  /**
   * Wait for element with clear, actionable error message if not found
   *
   * ❌ BAD - Hangs for 30 seconds with generic error:
   * ```typescript
   * await page.locator('input[name="firstName"]').fill(value);
   * // Error: locator.fill: Timeout 30000ms exceeded
   * ```
   *
   * ✅ GOOD - Fails fast with clear error:
   * ```typescript
   * const input = await SelectorHelpers.waitForElementWithError(
   *   page,
   *   '[data-testid="first-name-input"]',
   *   {
   *     timeout: 5000,
   *     errorMessage: 'firstName input not found - check ProfileSettingsPage has data-testid="first-name-input"'
   *   }
   * );
   * await input.fill(value);
   * // Error: firstName input not found - check ProfileSettingsPage has data-testid="first-name-input"
   * ```
   */
  static async waitForElementWithError(
    page: Page,
    selector: string,
    options: WaitForElementOptions = {}
  ): Promise<Locator> {
    const {
      timeout = 5000,
      errorMessage,
      state = 'visible'
    } = options;

    const element = page.locator(selector);

    try {
      await element.waitFor({ state, timeout });
      return element;
    } catch (error) {
      const currentUrl = page.url();
      const pageTitle = await page.title().catch(() => 'Unknown');

      const defaultMessage =
        `❌ Element not found: ${selector}\n` +
        `\n` +
        `Current page state:\n` +
        `- URL: ${currentUrl}\n` +
        `- Title: ${pageTitle}\n` +
        `- Waiting for: ${state}\n` +
        `- Timeout: ${timeout}ms\n` +
        `\n` +
        `This likely means:\n` +
        `1. The element was removed/renamed in the component\n` +
        `2. The selector is incorrect or outdated\n` +
        `3. The page didn't load correctly\n` +
        `4. The element is behind authentication or permission checks\n` +
        `\n` +
        `To fix:\n` +
        `1. Check the component source for the actual selector\n` +
        `2. Verify data-testid attributes are present\n` +
        `3. Ensure user has correct permissions\n` +
        `4. Check browser console for JavaScript errors\n`;

      throw new Error(errorMessage || defaultMessage);
    }
  }

  /**
   * Wait for multiple elements with a single clear error if any fail
   *
   * Use when form/page requires multiple elements to be present:
   * ```typescript
   * const [email, password, submit] = await SelectorHelpers.waitForElements(
   *   page,
   *   [
   *     '[data-testid="email-input"]',
   *     '[data-testid="password-input"]',
   *     '[data-testid="submit-button"]'
   *   ],
   *   {
   *     timeout: 5000,
   *     errorMessage: 'Login form elements missing - check LoginPage component'
   *   }
   * );
   * ```
   */
  static async waitForElements(
    page: Page,
    selectors: string[],
    options: WaitForElementOptions = {}
  ): Promise<Locator[]> {
    const { timeout = 5000, errorMessage } = options;

    const elements: Locator[] = [];
    const missingSelectors: string[] = [];

    for (const selector of selectors) {
      try {
        const element = await this.waitForElementWithError(page, selector, {
          ...options,
          errorMessage: undefined // We'll provide custom error below
        });
        elements.push(element);
      } catch {
        missingSelectors.push(selector);
      }
    }

    if (missingSelectors.length > 0) {
      const defaultMessage =
        `❌ ${missingSelectors.length}/${selectors.length} required elements not found:\n` +
        missingSelectors.map(s => `  - ${s}`).join('\n') +
        `\n\nPage: ${page.url()}\n` +
        `Timeout: ${timeout}ms`;

      throw new Error(errorMessage || defaultMessage);
    }

    return elements;
  }

  /**
   * Wait for element and log when found
   *
   * Useful for debugging timing issues:
   * ```typescript
   * const startTime = Date.now();
   * const element = await SelectorHelpers.waitAndLog(
   *   page,
   *   '[data-testid="event-card"]',
   *   'event cards'
   * );
   * // Console: [12:34:56.789] ✅ Found event cards after 234ms
   * ```
   */
  static async waitAndLog(
    page: Page,
    selector: string,
    elementName: string,
    options: WaitForElementOptions = {}
  ): Promise<Locator> {
    const startTime = Date.now();
    const timestamp = new Date().toISOString();

    console.log(`[${timestamp}] ⏳ Waiting for ${elementName} (${selector})...`);

    try {
      const element = await this.waitForElementWithError(page, selector, options);
      const elapsed = Date.now() - startTime;
      const foundTimestamp = new Date().toISOString();

      console.log(`[${foundTimestamp}] ✅ Found ${elementName} after ${elapsed}ms`);

      return element;
    } catch (error) {
      const elapsed = Date.now() - startTime;
      const failedTimestamp = new Date().toISOString();

      console.log(`[${failedTimestamp}] ❌ ${elementName} not found after ${elapsed}ms`);
      throw error;
    }
  }

  /**
   * Wait for element with retry logic
   *
   * Some elements appear after API calls complete.
   * Instead of increasing timeout, retry with backoff:
   * ```typescript
   * const element = await SelectorHelpers.waitWithRetry(
   *   page,
   *   '[data-testid="user-profile"]',
   *   {
   *     maxAttempts: 3,
   *     retryDelay: 1000,
   *     errorMessage: 'Profile didn't load - API may have failed'
   *   }
   * );
   * ```
   */
  static async waitWithRetry(
    page: Page,
    selector: string,
    options: WaitForElementOptions & {
      maxAttempts?: number;
      retryDelay?: number;
    } = {}
  ): Promise<Locator> {
    const {
      maxAttempts = 3,
      retryDelay = 1000,
      timeout = 5000,
      errorMessage
    } = options;

    let lastError: Error | null = null;

    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        console.log(`🔄 Attempt ${attempt}/${maxAttempts} to find: ${selector}`);

        const element = await this.waitForElementWithError(page, selector, {
          timeout,
          state: options.state
        });

        console.log(`✅ Found ${selector} on attempt ${attempt}`);
        return element;
      } catch (error) {
        lastError = error as Error;
        console.log(`❌ Attempt ${attempt} failed, retrying in ${retryDelay}ms...`);

        if (attempt < maxAttempts) {
          await page.waitForTimeout(retryDelay);
        }
      }
    }

    const finalMessage = errorMessage ||
      `Element not found after ${maxAttempts} attempts:\n${selector}\n\nLast error:\n${lastError?.message}`;

    throw new Error(finalMessage);
  }

  /**
   * Check if element exists without throwing error
   *
   * Use for conditional logic:
   * ```typescript
   * const hasLogoutButton = await SelectorHelpers.exists(
   *   page,
   *   '[data-testid="logout-button"]'
   * );
   *
   * if (hasLogoutButton) {
   *   // User is logged in
   * } else {
   *   // User is not logged in
   * }
   * ```
   */
  static async exists(
    page: Page,
    selector: string,
    options: { timeout?: number; state?: 'attached' | 'visible' } = {}
  ): Promise<boolean> {
    const { timeout = 1000, state = 'attached' } = options;

    try {
      await page.locator(selector).waitFor({ state, timeout });
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Wait for element count to match expected
   *
   * Useful for lists that load dynamically:
   * ```typescript
   * await SelectorHelpers.waitForCount(
   *   page,
   *   '[data-testid="event-card"]',
   *   6,
   *   {
   *     errorMessage: 'Expected 6 events but different count found'
   *   }
   * );
   * ```
   */
  static async waitForCount(
    page: Page,
    selector: string,
    expectedCount: number,
    options: { timeout?: number; errorMessage?: string } = {}
  ): Promise<void> {
    const { timeout = 5000, errorMessage } = options;
    const startTime = Date.now();

    while (Date.now() - startTime < timeout) {
      const actualCount = await page.locator(selector).count();

      if (actualCount === expectedCount) {
        console.log(`✅ Found expected count of ${expectedCount} for ${selector}`);
        return;
      }

      await page.waitForTimeout(100);
    }

    const finalCount = await page.locator(selector).count();
    const defaultMessage =
      `Expected ${expectedCount} elements but found ${finalCount}:\n` +
      `Selector: ${selector}\n` +
      `Timeout: ${timeout}ms\n` +
      `Page: ${page.url()}`;

    throw new Error(errorMessage || defaultMessage);
  }

  /**
   * Wait for element to contain specific text
   *
   * Useful for waiting for dynamic content:
   * ```typescript
   * await SelectorHelpers.waitForText(
   *   page,
   *   '[data-testid="welcome-message"]',
   *   'Welcome back, Admin!',
   *   {
   *     errorMessage: 'Welcome message didn't load with user name'
   *   }
   * );
   * ```
   */
  static async waitForText(
    page: Page,
    selector: string,
    expectedText: string | RegExp,
    options: { timeout?: number; errorMessage?: string } = {}
  ): Promise<Locator> {
    const { timeout = 5000, errorMessage } = options;
    const element = page.locator(selector);

    try {
      if (typeof expectedText === 'string') {
        await element.waitFor({ state: 'visible', timeout: timeout / 2 });
        await page.waitForFunction(
          ({ sel, text }) => {
            const el = document.querySelector(sel);
            return el?.textContent?.includes(text) || false;
          },
          { selector, text: expectedText },
          { timeout: timeout / 2 }
        );
      } else {
        await element.waitFor({ state: 'visible', timeout: timeout / 2 });
        await page.waitForFunction(
          ({ sel, pattern }) => {
            const el = document.querySelector(sel);
            return new RegExp(pattern).test(el?.textContent || '');
          },
          { selector, pattern: expectedText.source },
          { timeout: timeout / 2 }
        );
      }

      const actualText = await element.textContent();
      console.log(`✅ Element ${selector} contains expected text: "${actualText}"`);

      return element;
    } catch (error) {
      const actualText = await element.textContent().catch(() => 'Element not found');
      const defaultMessage =
        `Element doesn't contain expected text:\n` +
        `Selector: ${selector}\n` +
        `Expected: ${expectedText}\n` +
        `Actual: ${actualText}\n` +
        `Page: ${page.url()}`;

      throw new Error(errorMessage || defaultMessage);
    }
  }
}
