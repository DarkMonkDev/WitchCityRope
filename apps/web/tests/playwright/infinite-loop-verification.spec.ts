import { test, expect } from '@playwright/test';

test.describe('Infinite Loop Verification', () => {
  test('should load page without infinite loop errors', async ({ page }) => {
    // Set up console error tracking
    const consoleErrors: string[] = [];
    const consoleWarnings: string[] = [];
    
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
      if (msg.type() === 'warning') {
        consoleWarnings.push(msg.text());
      }
    });

    // Track page errors
    const pageErrors: string[] = [];
    page.on('pageerror', error => {
      pageErrors.push(error.message);
    });

    console.log('🧪 Starting infinite loop verification test');
    
    try {
      // Navigate to the home page
      console.log('📍 Navigating to http://localhost:5173');
      await page.goto('http://localhost:5173', { 
        waitUntil: 'domcontentloaded',
        timeout: 10000 
      });

      // Wait for the page to load and stabilize
      console.log('⏳ Waiting for page to stabilize...');
      await page.waitForTimeout(3000);

      // Check for specific infinite loop indicators
      const infiniteLoopErrors = consoleErrors.filter(error => 
        error.includes('Maximum update depth exceeded') ||
        error.includes('Too many re-renders') ||
        error.includes('Maximum call stack size exceeded') ||
        error.includes('RangeError: Maximum call stack') ||
        error.toLowerCase().includes('infinite')
      );

      // Take a screenshot for debugging
      await page.screenshot({ 
        path: '/home/chad/repos/witchcityrope-react/apps/web/test-results/infinite-loop-check.png',
        fullPage: true 
      });

      console.log('📊 Test Results:');
      console.log(`   Console Errors: ${consoleErrors.length}`);
      console.log(`   Console Warnings: ${consoleWarnings.length}`);
      console.log(`   Page Errors: ${pageErrors.length}`);
      console.log(`   Infinite Loop Errors: ${infiniteLoopErrors.length}`);

      if (consoleErrors.length > 0) {
        console.log('❌ Console Errors Found:');
        consoleErrors.forEach((error, i) => {
          console.log(`   ${i + 1}. ${error}`);
        });
      }

      if (pageErrors.length > 0) {
        console.log('❌ Page Errors Found:');
        pageErrors.forEach((error, i) => {
          console.log(`   ${i + 1}. ${error}`);
        });
      }

      // Check if page loaded successfully
      const title = await page.title();
      console.log(`📄 Page Title: "${title}"`);

      // Verify no infinite loop errors
      expect(infiniteLoopErrors.length, 
        `Found infinite loop errors: ${infiniteLoopErrors.join(', ')}`
      ).toBe(0);

      // Verify page loaded (has a title)
      expect(title).toBeTruthy();
      expect(title).not.toBe('');

      console.log('✅ SUCCESS: No infinite loop detected, page loaded successfully');

    } catch (error) {
      console.log('❌ TEST FAILED:', error);
      throw error;
    }
  });

  test('should handle navigation without infinite loops', async ({ page }) => {
    const consoleErrors: string[] = [];
    
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    console.log('🧪 Testing navigation stability');

    // Navigate to home page
    await page.goto('http://localhost:5173');
    await page.waitForTimeout(1000);

    // Try clicking any navigation elements if they exist
    try {
      const navElements = await page.$$('[data-testid*="nav"], a[href], button');
      if (navElements.length > 0) {
        console.log(`📍 Found ${navElements.length} clickable elements`);
        
        // Click first navigation element
        await navElements[0].click();
        await page.waitForTimeout(1000);
        
        console.log('✅ Navigation click successful');
      }
    } catch (error) {
      console.log('ℹ️ No interactive elements found or click failed:', error);
    }

    // Check for infinite loop errors during navigation
    const infiniteLoopErrors = consoleErrors.filter(error => 
      error.includes('Maximum update depth exceeded') ||
      error.includes('Too many re-renders')
    );

    expect(infiniteLoopErrors.length).toBe(0);
    console.log('✅ SUCCESS: Navigation completed without infinite loops');
  });
});