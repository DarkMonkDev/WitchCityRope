import { Page, ConsoleMessage } from '@playwright/test';

/**
 * Console Error Filtering Helpers
 *
 * Provides utilities to filter out expected console errors and warnings
 * to reduce noise in E2E test output.
 */

export interface ConsoleErrorOptions {
  /**
   * Whether to filter out 401/Unauthorized errors (default: true)
   * These occur when unauthenticated users visit public routes
   */
  filter401Errors?: boolean;

  /**
   * Additional error text patterns to filter out
   */
  customFilters?: string[];

  /**
   * Whether to log filtered messages to console (default: true)
   */
  logFilteredMessages?: boolean;
}

export class ConsoleHelpers {
  private errors: string[] = [];
  private warnings: string[] = [];
  private filteredCount = 0;

  /**
   * Set up console error listeners with filtering
   *
   * @example
   * ```typescript
   * const consoleHelper = new ConsoleHelpers();
   * consoleHelper.setupConsoleListeners(page);
   *
   * // Later in test:
   * const errors = consoleHelper.getErrors();
   * expect(errors.length).toBe(0);
   * ```
   */
  setupConsoleListeners(page: Page, options: ConsoleErrorOptions = {}) {
    const {
      filter401Errors = true,
      customFilters = [],
      logFilteredMessages = true,
    } = options;

    // Listen for console messages
    page.on('console', (msg: ConsoleMessage) => {
      const errorText = msg.text();

      if (msg.type() === 'error') {
        if (this.shouldFilterError(errorText, filter401Errors, customFilters)) {
          this.filteredCount++;
          if (logFilteredMessages) {
            console.log('ℹ️  Filtered expected error:', errorText.substring(0, 100));
          }
          return;
        }

        this.errors.push(errorText);
        console.log('❌ Console Error:', errorText);
      } else if (msg.type() === 'warn' || msg.type() === 'warning') {
        // Filter out React DOM nesting warnings if they're already fixed
        if (errorText.includes('validateDOMNesting')) {
          if (logFilteredMessages) {
            console.log('ℹ️  Filtered DOM nesting warning (may be resolved)');
          }
          return;
        }

        this.warnings.push(errorText);
        console.log('⚠️  Console Warning:', errorText);
      }
    });

    // Listen for page errors (JavaScript exceptions)
    page.on('pageerror', (error: Error) => {
      this.errors.push(error.message);
      console.log('❌ Page Error:', error.message);
    });
  }

  /**
   * Determine if an error should be filtered out
   */
  private shouldFilterError(
    errorText: string,
    filter401: boolean,
    customFilters: string[]
  ): boolean {
    // Filter 401/Unauthorized errors (expected on public pages)
    if (filter401) {
      const is401Error = errorText.includes('401') || errorText.includes('Unauthorized');
      const isFailedResource = errorText.includes('Failed to load resource');
      const isNetworkError = errorText.includes('net::ERR');

      if ((is401Error && isFailedResource) || (is401Error && isNetworkError) || is401Error) {
        return true;
      }
    }

    // Filter custom patterns
    for (const filter of customFilters) {
      if (errorText.includes(filter)) {
        return true;
      }
    }

    // Filter common development noise
    const developmentNoisePatterns = [
      'Download the React DevTools',
      'React DevTools',
      'Webpack HMR',
      'Hot Module Replacement',
    ];

    for (const pattern of developmentNoisePatterns) {
      if (errorText.includes(pattern)) {
        return true;
      }
    }

    return false;
  }

  /**
   * Get all collected errors
   */
  getErrors(): string[] {
    return [...this.errors];
  }

  /**
   * Get all collected warnings
   */
  getWarnings(): string[] {
    return [...this.warnings];
  }

  /**
   * Get count of filtered messages
   */
  getFilteredCount(): number {
    return this.filteredCount;
  }

  /**
   * Print a summary of console activity
   */
  printSummary() {
    console.log('\n=== CONSOLE SUMMARY ===');
    console.log(`Errors: ${this.errors.length}`);
    console.log(`Warnings: ${this.warnings.length}`);
    console.log(`Filtered: ${this.filteredCount}`);

    if (this.errors.length > 0) {
      console.log('\n❌ ERRORS:');
      this.errors.forEach((error, i) => console.log(`  ${i + 1}. ${error.substring(0, 150)}`));
    }

    if (this.warnings.length > 0) {
      console.log('\n⚠️  WARNINGS:');
      this.warnings.forEach((warn, i) => console.log(`  ${i + 1}. ${warn.substring(0, 150)}`));
    }

    if (this.errors.length === 0 && this.warnings.length === 0) {
      console.log('✅ No console errors or warnings');
    }
  }

  /**
   * Reset all collected messages
   */
  reset() {
    this.errors = [];
    this.warnings = [];
    this.filteredCount = 0;
  }
}

/**
 * Quick setup for common use case: filter 401s, collect errors
 *
 * @example
 * ```typescript
 * test('my test', async ({ page }) => {
 *   const { getErrors, printSummary } = setupConsoleErrorFiltering(page);
 *
 *   // ... test code ...
 *
 *   printSummary();
 *   expect(getErrors().length).toBe(0);
 * });
 * ```
 */
export function setupConsoleErrorFiltering(page: Page, options: ConsoleErrorOptions = {}) {
  const helper = new ConsoleHelpers();
  helper.setupConsoleListeners(page, options);

  return {
    getErrors: () => helper.getErrors(),
    getWarnings: () => helper.getWarnings(),
    getFilteredCount: () => helper.getFilteredCount(),
    printSummary: () => helper.printSummary(),
    reset: () => helper.reset(),
  };
}
