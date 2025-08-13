import { Page, Locator } from '@playwright/test';
import { testConfig } from './test.config';

/**
 * Blazor Server specific helper functions
 * Handles SignalR connections, component rendering, and Blazor-specific timing
 */
export class BlazorHelpers {
  /**
   * Wait for Blazor Server to be fully initialized
   * Checks for SignalR connection and Blazor object availability
   */
  static async waitForBlazorReady(page: Page): Promise<void> {
    try {
      // First check if this is a Blazor page at all
      const isBlazorPage = await page.evaluate(() => {
        return document.querySelector('script[src*="blazor.server.js"]') !== null ||
               document.querySelector('script[src*="blazor.webassembly.js"]') !== null;
      });

      if (!isBlazorPage) {
        console.log('Not a Blazor page, skipping Blazor initialization wait');
        return;
      }

      // First try using the E2E helper if available
      const hasE2EHelper = await page.evaluate(() => typeof (window as any).waitForBlazorE2E === 'function');
      
      if (hasE2EHelper) {
        console.log('Using Blazor E2E helper for initialization check');
        try {
          const blazorState = await page.evaluate((timeout) => (window as any).waitForBlazorE2E(timeout), testConfig.timeouts.blazorE2EHelper);
          console.log('Blazor state from E2E helper:', blazorState);
          const blazorReady = blazorState.status === 'ready';
          if (blazorReady) {
            console.log('✅ Blazor successfully initialized via E2E helper');
            return;
          }
        } catch (error) {
          console.warn('E2E helper timeout, falling back to other methods:', error);
        }
      }
      
      // Fallback: Use custom event or polling with configurable timeouts
      const blazorReady = await Promise.race([
        // Wait for custom initialization event
        page.waitForEvent('blazor:initialized', { timeout: testConfig.timeouts.blazorReady }).then(() => true),
        
        // Fallback: Wait for Blazor object and internal state with retry logic
        this.waitForBlazorWithRetries(page, testConfig.timeouts.blazorReady)
      ]);

      if (!blazorReady) {
        console.warn('Blazor initialization timeout - attempting to continue');
        
        // Try to manually check state one more time
        const blazorState = await page.evaluate(() => {
          const blazor = (window as any).Blazor;
          return {
            exists: !!blazor,
            hasInternal: blazor && !!blazor._internal,
            hasNavigationManager: blazor && blazor._internal && !!blazor._internal.navigationManager,
            isReady: (window as any).isBlazorReady ? (window as any).isBlazorReady() : false
          };
        });
        
        console.log('Final Blazor state check:', blazorState);
      } else {
        console.log('✅ Blazor successfully initialized');
      }

      // Additional small wait to ensure all components are rendered
      await page.waitForTimeout(100);
    } catch (error) {
      console.warn('Error waiting for Blazor initialization:', error);
      // Don't throw - allow tests to continue
    }
  }

  /**
   * Wait for Blazor readiness with retry logic
   * Internal method with multiple fallback strategies
   */
  private static async waitForBlazorWithRetries(page: Page, timeout: number): Promise<boolean> {
    const retryInterval = 500;
    const maxRetries = Math.floor(timeout / retryInterval);
    let retries = 0;
    
    while (retries < maxRetries) {
      try {
        const isReady = await page.waitForFunction(
          () => {
            const blazor = (window as any).Blazor;
            if (!blazor) return false;
            
            // Check multiple indicators of Blazor readiness
            const hasInternal = !!blazor._internal;
            const hasNavigationManager = hasInternal && !!blazor._internal.navigationManager;
            const hasCircuitManager = hasInternal && !!blazor._internal.circuitManager;
            const isStarted = blazor._started || hasNavigationManager;
            
            // Also check the global helper function if available
            const isReady = (window as any).isBlazorReady ? (window as any).isBlazorReady() : false;
            
            // Check E2E state if available
            const e2eState = (window as any).__blazorE2EState;
            const e2eInitialized = e2eState && e2eState.initialized;
            
            return isStarted || isReady || e2eInitialized || (hasInternal && hasNavigationManager);
          },
          { timeout: retryInterval }
        );
        
        if (isReady) {
          return true;
        }
      } catch (error) {
        retries++;
        console.log(`Blazor readiness check attempt ${retries}/${maxRetries}...`);
        
        // Add a small delay between retries
        await new Promise(resolve => setTimeout(resolve, 100));
      }
    }
    
    return false;
  }

  /**
   * Wait for a specific Blazor component to be rendered
   * Uses data-testid for reliable element selection
   */
  static async waitForComponent(page: Page, testId: string, options?: { timeout?: number }): Promise<Locator> {
    const selector = `[data-testid="${testId}"]`;
    const element = page.locator(selector);
    
    // Wait for element to be visible
    await element.waitFor({ 
      state: 'visible', 
      timeout: options?.timeout || 5000 
    });
    
    // Small additional wait for any animations or final renders
    await page.waitForTimeout(100);
    
    return element;
  }

  /**
   * Wait for Blazor Server navigation to complete
   * Monitors URL changes and component updates
   */
  static async waitForNavigation(page: Page, expectedUrl?: string | RegExp): Promise<void> {
    if (expectedUrl) {
      await page.waitForURL(expectedUrl, { timeout: 10000 });
    }
    
    // Wait for any pending SignalR updates
    await page.waitForLoadState('networkidle');
    
    // Wait for Blazor to update components after navigation
    await this.waitForBlazorReady(page);
  }

  /**
   * Handle authentication state for reuse across tests
   * Stores cookies and local storage for authenticated sessions
   */
  static async saveAuthenticationState(page: Page, path: string): Promise<void> {
    await page.context().storageState({ path });
  }

  /**
   * Wait for form validation to complete
   * Handles both Blazor Server and ASP.NET Core Identity validation
   */
  static async waitForValidation(page: Page): Promise<void> {
    // Wait for validation messages to appear or disappear
    await page.waitForTimeout(500);
    
    // Wait for any validation summary updates (both Blazor and Identity patterns)
    const validationSelectors = [
      '.wcr-validation-summary',           // Custom styled validation summary
      '.validation-summary-errors',        // ASP.NET Core Identity validation summary
      '.field-validation-error',           // ASP.NET Core Identity field errors
      '.validation-message'                // Blazor validation messages
    ];
    
    try {
      // Wait for any validation element to appear
      await page.waitForSelector(validationSelectors.join(', '), { 
        state: 'visible', 
        timeout: 1000 
      });
    } catch {
      // Validation messages may not appear if form is valid
    }
  }

  /**
   * Check if Blazor circuit is connected
   * Useful for debugging connection issues
   */
  static async isCircuitConnected(page: Page): Promise<boolean> {
    return await page.evaluate(() => {
      const blazor = (window as any).Blazor;
      if (!blazor || !blazor._internal) return false;
      
      // Check if SignalR connection is active
      const connection = blazor._internal.connection;
      return connection && connection.state === 'Connected';
    });
  }

  /**
   * Reconnect Blazor circuit if disconnected
   * Useful for handling transient connection issues
   */
  static async ensureCircuitConnected(page: Page): Promise<void> {
    const isConnected = await this.isCircuitConnected(page);
    
    if (!isConnected) {
      console.log('Blazor circuit disconnected, attempting to reconnect...');
      await page.reload();
      await this.waitForBlazorReady(page);
    }
  }

  /**
   * Wait for specific text to appear in a component
   * Useful for dynamic content that loads after render
   */
  static async waitForText(page: Page, text: string, options?: { timeout?: number }): Promise<void> {
    await page.waitForFunction(
      (searchText) => {
        const elements = Array.from(document.querySelectorAll('*'));
        return elements.some(el => el.textContent?.includes(searchText));
      },
      text,
      { timeout: options?.timeout || 5000 }
    );
  }

  /**
   * Click a button and wait for Blazor to process the action
   * Handles both form submissions and regular button clicks
   */
  static async clickAndWait(page: Page, selector: string | Locator): Promise<void> {
    const element = typeof selector === 'string' ? page.locator(selector) : selector;
    
    // Click the element
    await element.click();
    
    // Wait for any immediate JavaScript execution
    await page.waitForTimeout(100);
    
    // Wait for network activity to settle (form submissions, API calls)
    await page.waitForLoadState('networkidle');
    
    // Ensure Blazor has processed the update
    await this.waitForBlazorReady(page);
  }

  /**
   * Fill a form field and wait for Blazor to process the change
   * Handles Blazor's two-way binding updates
   */
  static async fillAndWait(page: Page, selector: string | Locator, value: string): Promise<void> {
    const element = typeof selector === 'string' ? page.locator(selector) : selector;
    
    // Clear and fill the field
    await element.clear();
    await element.fill(value);
    
    // Trigger change event for Blazor binding
    await element.press('Tab');
    
    // Wait for Blazor to process the update
    await page.waitForTimeout(100);
  }

  /**
   * Debug helper to log Blazor circuit state
   */
  static async logCircuitState(page: Page): Promise<void> {
    const state = await page.evaluate(() => {
      const blazor = (window as any).Blazor;
      if (!blazor || !blazor._internal) return 'Blazor not initialized';
      
      const connection = blazor._internal.connection;
      if (!connection) return 'No SignalR connection';
      
      return {
        connectionState: connection.state,
        connectionId: connection.connectionId,
        baseUri: blazor._internal.navigationManager?.baseUri,
        locationUrl: window.location.href
      };
    });
    
    console.log('Blazor Circuit State:', state);
  }

  /**
   * Check if a URL is an Identity page (Razor Page, not Blazor)
   */
  static isIdentityPage(url: string): boolean {
    // Check if URL contains any of the Identity page patterns
    return testConfig.identityPages.some(pattern => url.includes(pattern)) ||
           url.includes('/Identity/') || 
           url.includes('/Account/');
  }

  /**
   * Smart wait that checks if the current page is Blazor or Identity
   */
  static async waitForPageReady(page: Page): Promise<void> {
    const currentUrl = page.url();
    
    if (this.isIdentityPage(currentUrl)) {
      // For Identity pages, just wait for network to settle
      await page.waitForLoadState('networkidle');
    } else {
      // For Blazor pages, wait for Blazor to be ready
      await this.waitForBlazorReady(page);
    }
  }
}