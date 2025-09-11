import { test, expect } from '@playwright/test';

test('Debug TinyMCE loading issues', async ({ page }) => {
  // Listen for console errors
  const consoleErrors: string[] = [];
  const networkErrors: string[] = [];

  page.on('console', msg => {
    if (msg.type() === 'error') {
      consoleErrors.push(msg.text());
      console.log('Console Error:', msg.text());
    }
  });

  page.on('response', response => {
    if (!response.ok() && response.url().includes('tiny')) {
      networkErrors.push(`${response.status()} ${response.url()}`);
      console.log('Network Error:', response.status(), response.url());
    }
  });

  console.log('Navigating to TinyMCE test page...');
  
  try {
    // Navigate to the test page (using correct port)
    await page.goto('http://localhost:5174/test-tinymce');
    
    console.log('Page loaded, checking for TinyMCE elements...');
    
    // Wait for page to load
    await page.waitForTimeout(3000);
    
    // Take screenshot of initial state
    await page.screenshot({ path: 'test-results/tinymce-debug-initial.png', fullPage: true });
    
    // Check if TinyMCE iframe is present
    const iframes = await page.locator('iframe').count();
    console.log(`Found ${iframes} iframes on page`);
    
    // Look for TinyMCE specific elements
    const tinyElements = await page.locator('[class*="tiny"], [id*="tiny"], [data-tiny]').count();
    console.log(`Found ${tinyElements} TinyMCE-related elements`);
    
    // Check for error messages in the page
    const errorMessages = await page.locator('text="API key"').count();
    console.log(`Found ${errorMessages} API key related messages`);
    
    // Wait a bit more for TinyMCE to fully initialize
    await page.waitForTimeout(5000);
    
    // Take final screenshot
    await page.screenshot({ path: 'test-results/tinymce-debug-final.png', fullPage: true });
    
    // Check if the editor is functional
    const textareas = await page.locator('textarea').count();
    console.log(`Found ${textareas} textareas`);
    
    // Print all errors found
    console.log('=== Console Errors ===');
    consoleErrors.forEach(error => console.log(error));
    
    console.log('=== Network Errors ===');
    networkErrors.forEach(error => console.log(error));
    
    // Get page content for analysis
    const pageContent = await page.content();
    console.log('=== Page Title ===');
    console.log(await page.title());
    
    // Log any script tags loading TinyMCE
    const scripts = await page.locator('script[src*="tiny"]').count();
    console.log(`Found ${scripts} TinyMCE script tags`);
    
  } catch (error) {
    console.error('Error during test:', error);
    await page.screenshot({ path: 'test-results/tinymce-debug-error.png', fullPage: true });
    throw error;
  }
});