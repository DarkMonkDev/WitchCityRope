import { Page } from '@playwright/test';

export class PerformanceHelpers {
  /**
   * Measure page load performance metrics
   */
  static async measurePageLoad(page: Page): Promise<{
    loadTime: number;
    domContentLoaded: number;
    firstPaint: number;
    firstContentfulPaint: number;
    domInteractive: number;
  }> {
    const metrics = await page.evaluate(() => {
      const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
      const paint = performance.getEntriesByType('paint');
      
      return {
        domContentLoaded: navigation.domContentLoadedEventEnd - navigation.domContentLoadedEventStart,
        loadComplete: navigation.loadEventEnd - navigation.loadEventStart,
        firstPaint: paint.find(p => p.name === 'first-paint')?.startTime || 0,
        firstContentfulPaint: paint.find(p => p.name === 'first-contentful-paint')?.startTime || 0,
        domInteractive: navigation.domInteractive - navigation.fetchStart
      };
    });

    return {
      loadTime: metrics.loadComplete,
      domContentLoaded: metrics.domContentLoaded,
      firstPaint: metrics.firstPaint,
      firstContentfulPaint: metrics.firstContentfulPaint,
      domInteractive: metrics.domInteractive
    };
  }

  /**
   * Measure form interaction latency
   */
  static async measureFormInteraction(page: Page, selector: string): Promise<{
    inputLatency: number;
    validationLatency: number;
  }> {
    const input = await page.locator(selector).first();
    
    // Measure input latency
    const startInput = Date.now();
    await input.click();
    await input.fill('test value');
    const inputLatency = Date.now() - startInput;
    
    // Measure validation latency
    const startValidation = Date.now();
    await page.click('body'); // Blur to trigger validation
    await page.waitForTimeout(100);
    const validationLatency = Date.now() - startValidation;
    
    return { inputLatency, validationLatency };
  }

  /**
   * Get memory usage metrics
   */
  static async getMemoryMetrics(page: Page): Promise<{
    jsHeapUsedSize: number;
    jsHeapTotalSize: number;
    domNodes: number;
  }> {
    const metrics = await page.evaluate(() => {
      return {
        jsHeapUsedSize: (performance as any).memory?.usedJSHeapSize || 0,
        jsHeapTotalSize: (performance as any).memory?.totalJSHeapSize || 0,
        domNodes: document.getElementsByTagName('*').length
      };
    });

    return metrics;
  }

  /**
   * Measure Blazor circuit latency
   */
  static async measureBlazorLatency(page: Page): Promise<number> {
    return await page.evaluate(() => {
      return new Promise<number>((resolve) => {
        const start = Date.now();
        // Trigger a Blazor interaction if available
        const blazorElement = document.querySelector('[blazor\\:elementreference]');
        if (blazorElement) {
          blazorElement.dispatchEvent(new Event('click'));
        }
        
        // Wait for Blazor to process
        setTimeout(() => {
          resolve(Date.now() - start);
        }, 100);
      });
    });
  }

  /**
   * Profile validation performance
   */
  static async profileValidation(page: Page): Promise<{
    validationCount: number;
    averageValidationTime: number;
    slowestValidation: number;
  }> {
    const timings: number[] = [];
    const inputs = await page.locator('input:not([type="hidden"]):not([readonly])').all();
    
    for (const input of inputs.slice(0, 5)) { // Test first 5 inputs
      const start = Date.now();
      await input.click();
      await input.fill('test');
      await page.click('body');
      await page.waitForTimeout(200);
      timings.push(Date.now() - start);
    }
    
    return {
      validationCount: timings.length,
      averageValidationTime: timings.reduce((a, b) => a + b, 0) / timings.length,
      slowestValidation: Math.max(...timings)
    };
  }
}