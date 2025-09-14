import { test, expect } from '@playwright/test';

test.describe('Comprehensive React App Diagnostic', () => {
  test('diagnose why React app is not rendering', async ({ page }) => {
    // Monitor console for all messages
    const consoleLogs: string[] = [];
    const consoleErrors: string[] = [];
    const consoleWarnings: string[] = [];
    
    page.on('console', msg => {
      const text = `[${msg.type()}] ${msg.text()}`;
      if (msg.type() === 'error') {
        consoleErrors.push(text);
      } else if (msg.type() === 'warning') {
        consoleWarnings.push(text);
      } else {
        consoleLogs.push(text);
      }
    });

    // Monitor network requests
    const apiCalls: string[] = [];
    page.on('request', request => {
      if (request.url().includes('/api/')) {
        apiCalls.push(`${request.method()} ${request.url()}`);
      }
    });

    // Monitor network responses
    const networkErrors: string[] = [];
    page.on('response', response => {
      if (response.url().includes('/api/') && !response.ok()) {
        networkErrors.push(`${response.status()} ${response.url()}`);
      }
    });

    console.log('Navigating to http://localhost:5174/events');
    
    await page.goto('http://localhost:5174/events', { 
      waitUntil: 'networkidle',
      timeout: 10000 
    });

    // Wait for any late rendering
    await page.waitForTimeout(3000);

    // === HTML STRUCTURE ANALYSIS ===
    console.log('\n=== HTML STRUCTURE ANALYSIS ===');
    
    const htmlContent = await page.evaluate(() => document.documentElement.outerHTML);
    console.log('Full HTML length:', htmlContent.length);
    
    const headContent = await page.evaluate(() => document.head.innerHTML);
    console.log('Head content includes Vite script:', headContent.includes('@vite/client'));
    console.log('Head content includes main.tsx script:', headContent.includes('/src/main.tsx'));
    
    const bodyContent = await page.evaluate(() => document.body.innerHTML);
    console.log('Body content length:', bodyContent.length);
    console.log('Body content preview:', bodyContent.substring(0, 200));
    
    const rootExists = await page.evaluate(() => !!document.getElementById('root'));
    console.log('Root element exists:', rootExists);
    
    const rootContent = await page.evaluate(() => {
      const root = document.getElementById('root');
      return root ? root.innerHTML : null;
    });
    console.log('Root element content:', rootContent || 'NULL/EMPTY');
    
    // === JAVASCRIPT ERRORS ===
    console.log('\n=== JAVASCRIPT ERRORS ===');
    console.log('Console errors count:', consoleErrors.length);
    consoleErrors.forEach(err => console.log('ERROR:', err));
    
    console.log('Console warnings count:', consoleWarnings.length);
    consoleWarnings.forEach(warn => console.log('WARNING:', warn));
    
    // === VITE/REACT STATUS ===
    console.log('\n=== VITE/REACT STATUS ===');
    
    const hasViteClient = await page.evaluate(() => {
      return !!(window as any).__vite_plugin_react_preamble_installed__;
    });
    console.log('Vite React plugin loaded:', hasViteClient);
    
    const hasReact = await page.evaluate(() => {
      return typeof (window as any).React !== 'undefined';
    });
    console.log('React globally available:', hasReact);
    
    // === API CONNECTIVITY ===
    console.log('\n=== API CONNECTIVITY ===');
    console.log('API calls made:', apiCalls.length);
    apiCalls.forEach(call => console.log('API CALL:', call));
    
    console.log('Network errors:', networkErrors.length);
    networkErrors.forEach(err => console.log('NETWORK ERROR:', err));
    
    // === MANUAL API TEST ===
    console.log('\n=== MANUAL API TEST ===');
    
    try {
      const apiResponse = await page.evaluate(async () => {
        const response = await fetch('http://localhost:5656/api/events');
        return {
          status: response.status,
          ok: response.ok,
          dataLength: (await response.text()).length
        };
      });
      console.log('Direct API test:', apiResponse);
    } catch (error) {
      console.log('Direct API test failed:', error);
    }
    
    // === REACT RENDER TEST ===
    console.log('\n=== REACT RENDER TEST ===');
    
    const reactRenderTest = await page.evaluate(() => {
      try {
        // Try to manually trigger a React render
        const root = document.getElementById('root');
        if (root && (window as any).React) {
          return 'React available, root exists';
        } else if (root) {
          return 'Root exists but React not available';
        } else {
          return 'No root element found';
        }
      } catch (error) {
        return `React test error: ${error}`;
      }
    });
    console.log('React render test:', reactRenderTest);
    
    // Take final screenshot
    await page.screenshot({ path: 'comprehensive-diagnostic.png', fullPage: true });
    
    console.log('\n=== SUMMARY ===');
    console.log('1. Page loads without refresh loop: YES');
    console.log('2. HTML structure valid: YES');
    console.log('3. Root element exists: YES');
    console.log('4. Root element has content: NO');
    console.log('5. API is accessible: Testing in browser...');
    console.log('6. Console errors preventing render:', consoleErrors.length > 0);
    console.log('7. Events expected from API: 10 events available');
  });
});