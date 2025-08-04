import { test, expect } from '@playwright/test';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { PerformanceHelpers } from '../helpers/performance.helpers';

// Performance metrics collector
interface PerformanceMetrics {
  name: string;
  loadTime: number;
  domContentLoaded: number;
  firstPaint: number;
  interactive: number;
}

class PerformanceCollector {
  private metrics: PerformanceMetrics[] = [];

  add(name: string, metrics: Omit<PerformanceMetrics, 'name'>) {
    this.metrics.push({ name, ...metrics });
  }

  getAverages() {
    const grouped: Record<string, PerformanceMetrics[]> = {};
    
    this.metrics.forEach(m => {
      if (!grouped[m.name]) {
        grouped[m.name] = [];
      }
      grouped[m.name].push(m);
    });

    const averages: Record<string, any> = {};
    Object.keys(grouped).forEach(key => {
      const items = grouped[key];
      averages[key] = {
        avgLoadTime: items.reduce((sum, item) => sum + item.loadTime, 0) / items.length,
        avgDomContentLoaded: items.reduce((sum, item) => sum + item.domContentLoaded, 0) / items.length,
        avgFirstPaint: items.reduce((sum, item) => sum + item.firstPaint, 0) / items.length,
        avgInteractive: items.reduce((sum, item) => sum + item.interactive, 0) / items.length,
        samples: items.length
      };
    });

    return averages;
  }

  printSummary() {
    const averages = this.getAverages();
    console.log('\n📊 Performance Summary:');
    console.log('═══════════════════════════════════════════════════════════════');
    
    Object.keys(averages).forEach(key => {
      const avg = averages[key];
      console.log(`\n${key}:`);
      console.log(`  Load Time: ${avg.avgLoadTime.toFixed(2)}ms`);
      console.log(`  DOM Content Loaded: ${avg.avgDomContentLoaded.toFixed(2)}ms`);
      console.log(`  First Paint: ${avg.avgFirstPaint.toFixed(2)}ms`);
      console.log(`  Time to Interactive: ${avg.avgInteractive.toFixed(2)}ms`);
      console.log(`  Samples: ${avg.samples}`);
    });
  }
}

test.describe('Validation Performance Tests', () => {
  const collector = new PerformanceCollector();

  test.afterAll(() => {
    collector.printSummary();
  });

  test('measure page load performance', async ({ page }) => {
    console.log('\n⏱️ Measuring Page Load Performance...');
    
    const pagesToTest = [
      { name: 'Login Page', url: '/login' },
      { name: 'Register Page', url: '/register' },
      { name: 'Validation Test Page', url: '/test/validation' },
      { name: 'Event Edit Form', url: '/admin/events/new-standardized' }
    ];

    for (const pageInfo of pagesToTest) {
      console.log(`\n📍 Testing ${pageInfo.name}...`);
      
      // Measure 3 times for average
      for (let i = 0; i < 3; i++) {
        const startTime = Date.now();
        
        await page.goto(`${testConfig.baseUrl}${pageInfo.url}`, {
          waitUntil: 'networkidle'
        });

        await BlazorHelpers.waitForBlazorReady(page);
        const loadTime = Date.now() - startTime;

        // Get performance metrics
        const perfData = await page.evaluate(() => {
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

        collector.add(pageInfo.name, {
          loadTime,
          domContentLoaded: perfData.domContentLoaded,
          firstPaint: perfData.firstPaint,
          interactive: perfData.domInteractive
        });

        console.log(`  Run ${i + 1}: ${loadTime}ms`);
      }
    }
  });

  test('measure form interaction performance', async ({ page }) => {
    console.log('\n⚡ Measuring Form Interaction Performance...');
    
    await page.goto(`${testConfig.baseUrl}/test/validation`, {
      waitUntil: 'networkidle'
    });
    await BlazorHelpers.waitForBlazorReady(page);

    const measurements = {
      inputLatency: [] as number[],
      validationLatency: [] as number[],
      submitLatency: 0
    };

    // Measure input latency
    const inputs = await page.locator('input:not([type="hidden"]):not([readonly])').all();
    
    for (let i = 0; i < Math.min(inputs.length, 3); i++) {
      const input = inputs[i];
      
      const startType = Date.now();
      await input.click();
      await input.fill('test value');
      const typeLatency = Date.now() - startType;
      measurements.inputLatency.push(typeLatency);
      
      // Measure validation trigger
      const startValidation = Date.now();
      await page.click('body'); // Blur to trigger validation
      await page.waitForTimeout(100);
      const validationLatency = Date.now() - startValidation;
      measurements.validationLatency.push(validationLatency);
    }

    // Measure form submission
    const submitButton = await page.locator('button[type="submit"]').first();
    if (await submitButton.isVisible()) {
      const startSubmit = Date.now();
      await submitButton.click();
      await page.waitForTimeout(500);
      measurements.submitLatency = Date.now() - startSubmit;
    }

    const avgInputLatency = measurements.inputLatency.length > 0 
      ? measurements.inputLatency.reduce((a, b) => a + b, 0) / measurements.inputLatency.length 
      : 0;
    
    const avgValidationLatency = measurements.validationLatency.length > 0
      ? measurements.validationLatency.reduce((a, b) => a + b, 0) / measurements.validationLatency.length
      : 0;

    console.log(`  ✓ Avg input latency: ${avgInputLatency.toFixed(2)}ms`);
    console.log(`  ✓ Avg validation latency: ${avgValidationLatency.toFixed(2)}ms`);
    console.log(`  ✓ Submit latency: ${measurements.submitLatency}ms`);

    expect(avgInputLatency).toBeLessThan(200);
    expect(avgValidationLatency).toBeLessThan(300);
  });

  test('measure memory usage', async ({ page, context }) => {
    console.log('\n💾 Testing Memory Usage...');
    
    // Enable Chrome DevTools Protocol
    const client = await context.newCDPSession(page);
    await client.send('Performance.enable');
    
    // Test pages with many validation components
    const testPages = [
      { url: '/test/validation', name: 'Validation Test Page' },
      { url: '/admin/events/new-standardized', name: 'Event Edit Form' },
      { url: '/register', name: 'Register Form' }
    ];
    
    for (const testPage of testPages) {
      await page.goto(`${testConfig.baseUrl}${testPage.url}`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      await page.waitForTimeout(2000);
      
      // Get memory metrics before interaction
      const metricsBefore = await client.send('Performance.getMetrics');
      const heapBefore = metricsBefore.metrics.find(m => m.name === 'JSHeapUsedSize')?.value || 0;
      
      console.log(`\n${testPage.name} Memory Metrics:`);
      const relevantMetrics = metricsBefore.metrics.filter(m => 
        ['JSHeapUsedSize', 'JSHeapTotalSize', 'Nodes', 'LayoutCount'].includes(m.name)
      );
      
      relevantMetrics.forEach(metric => {
        const value = metric.name.includes('Size') 
          ? `${(metric.value / 1024 / 1024).toFixed(2)} MB`
          : metric.value;
        console.log(`  ${metric.name}: ${value}`);
      });
      
      // Interact with form to see memory changes
      const inputs = await page.locator('input:not([type="hidden"]):not([readonly])').all();
      for (let i = 0; i < Math.min(inputs.length, 5); i++) {
        await inputs[i].click();
        await inputs[i].fill(`Test value ${i}`);
      }
      
      // Re-measure after interaction
      const metricsAfter = await client.send('Performance.getMetrics');
      const heapAfter = metricsAfter.metrics.find(m => m.name === 'JSHeapUsedSize')?.value || 0;
      
      const heapDiff = heapAfter - heapBefore;
      console.log(`  Heap increase after interaction: ${(heapDiff / 1024).toFixed(2)} KB`);
      
      // Memory increase should be reasonable
      expect(heapDiff).toBeLessThan(5 * 1024 * 1024); // Less than 5MB increase
    }
  });

  test('compare original vs standardized forms', async ({ page }) => {
    console.log('\n🔄 Comparing Original vs Standardized Forms...');
    
    // Test pairs to compare (if original versions still exist)
    const comparisons = [
      {
        name: 'Login Form',
        original: '/Identity/Account/Login',
        standardized: '/login'
      },
      {
        name: 'Register Form',
        original: '/Identity/Account/Register', 
        standardized: '/register'
      }
    ];

    for (const comparison of comparisons) {
      console.log(`\n📊 Comparing ${comparison.name}...`);
      
      // Test original (if it exists)
      try {
        const originalMetrics = await measurePagePerformance(page, comparison.original);
        collector.add(`${comparison.name} - Original`, originalMetrics);
      } catch (e) {
        console.log(`  Original version not found or accessible`);
      }
      
      // Test standardized
      const standardizedMetrics = await measurePagePerformance(page, comparison.standardized);
      collector.add(`${comparison.name} - Standardized`, standardizedMetrics);
    }
  });
});

async function measurePagePerformance(page: any, url: string) {
  const startTime = Date.now();
  
  await page.goto(`${testConfig.baseUrl}${url}`, {
    waitUntil: 'networkidle'
  });

  await BlazorHelpers.waitForBlazorReady(page);
  const loadTime = Date.now() - startTime;
  
  // Get performance metrics
  const perfData = await page.evaluate(() => {
    const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
    const paint = performance.getEntriesByType('paint');
    
    return {
      domContentLoaded: navigation.domContentLoadedEventEnd - navigation.domContentLoadedEventStart,
      firstPaint: paint.find(p => p.name === 'first-paint')?.startTime || 0,
      domInteractive: navigation.domInteractive - navigation.fetchStart
    };
  });

  return {
    loadTime,
    domContentLoaded: perfData.domContentLoaded,
    firstPaint: perfData.firstPaint,
    interactive: perfData.domInteractive
  };
}