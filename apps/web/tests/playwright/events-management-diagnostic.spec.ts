import { test, expect } from '@playwright/test';

/**
 * Diagnostic test for Events Management System demo pages
 * This test will examine what's actually present on the pages
 * to help debug the main E2E test issues.
 */

test.describe('Events Management System Diagnostics', () => {
  
  test('Diagnose Events Management API Demo page', async ({ page }) => {
    console.log('=== DIAGNOSTIC: Events Management API Demo ===');
    
    // Navigate to the demo page
    await page.goto('/admin/events-management-api-demo');
    
    // Wait for page to load
    await page.waitForTimeout(3000);
    
    // Take screenshot
    await page.screenshot({ path: 'test-results/diagnostic-api-demo.png', fullPage: true });
    
    // Get page title
    const title = await page.title();
    console.log('Page title:', title);
    
    // Get all text content
    const pageText = await page.textContent('body');
    console.log('Page text (first 500 chars):', pageText?.slice(0, 500));
    
    // Count different element types
    const headings = await page.locator('h1, h2, h3, h4, h5, h6').count();
    const tabs = await page.locator('[role="tab"]').count();
    const buttons = await page.locator('button').count();
    const divs = await page.locator('div').count();
    
    console.log('Element counts:', { headings, tabs, buttons, divs });
    
    // List all headings
    console.log('=== HEADINGS ===');
    for (let i = 0; i < headings; i++) {
      const heading = await page.locator('h1, h2, h3, h4, h5, h6').nth(i).textContent();
      console.log(`Heading ${i}:`, heading);
    }
    
    // List all tabs
    console.log('=== TABS ===');
    for (let i = 0; i < tabs; i++) {
      const tab = await page.locator('[role="tab"]').nth(i).textContent();
      console.log(`Tab ${i}:`, tab);
    }
    
    // List all buttons
    console.log('=== BUTTONS ===');
    for (let i = 0; i < Math.min(buttons, 10); i++) { // Limit to 10 buttons
      const button = await page.locator('button').nth(i).textContent();
      console.log(`Button ${i}:`, button);
    }
    
    // Check for specific expected text
    const expectedTexts = [
      'Events Management API Integration Demo',
      'Current API (Working)',
      'Future Events Management API',
      'Rope Basics Workshop (Fallback)',
      'Advanced Shibari Techniques (Fallback)',
      'Community Social Gathering (Fallback)'
    ];
    
    console.log('=== EXPECTED TEXT CHECKS ===');
    for (const text of expectedTexts) {
      const exists = await page.locator(`text=${text}`).count() > 0;
      console.log(`"${text}": ${exists ? 'FOUND' : 'NOT FOUND'}`);
    }
    
    // Check console errors
    let hasErrors = false;
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.log('Console Error found:', msg.text());
        hasErrors = true;
      }
    });
    
    // Verify the page loaded at all
    expect(title).toBeTruthy();
  });
  
  test('Diagnose Event Session Matrix Demo page', async ({ page }) => {
    console.log('=== DIAGNOSTIC: Event Session Matrix Demo ===');
    
    // Navigate to the demo page
    await page.goto('/admin/event-session-matrix-demo');
    
    // Wait for page to load
    await page.waitForTimeout(3000);
    
    // Take screenshot
    await page.screenshot({ path: 'test-results/diagnostic-matrix-demo.png', fullPage: true });
    
    // Get page title
    const title = await page.title();
    console.log('Page title:', title);
    
    // Get all text content
    const pageText = await page.textContent('body');
    console.log('Page text (first 500 chars):', pageText?.slice(0, 500));
    
    // Count different element types
    const headings = await page.locator('h1, h2, h3, h4, h5, h6').count();
    const tabs = await page.locator('[role="tab"], .tab, [data-testid*="tab"]').count();
    const forms = await page.locator('form').count();
    const inputs = await page.locator('input').count();
    const textareas = await page.locator('textarea').count();
    const buttons = await page.locator('button').count();
    const iframes = await page.locator('iframe').count();
    
    console.log('Element counts:', { headings, tabs, forms, inputs, textareas, buttons, iframes });
    
    // List all headings
    console.log('=== HEADINGS ===');
    for (let i = 0; i < headings; i++) {
      const heading = await page.locator('h1, h2, h3, h4, h5, h6').nth(i).textContent();
      console.log(`Heading ${i}:`, heading);
    }
    
    // Check for specific expected content
    const expectedTexts = [
      'Basic Info',
      'Tickets',
      'Emails',
      'Volunteers',
      'Save Draft',
      'Cancel',
      'Event Session Matrix',
      'TinyMCE'
    ];
    
    console.log('=== EXPECTED TEXT CHECKS ===');
    for (const text of expectedTexts) {
      const exists = await page.locator(`text=${text}`).count() > 0;
      console.log(`"${text}": ${exists ? 'FOUND' : 'NOT FOUND'}`);
    }
    
    // Look for TinyMCE elements
    const tinyElements = await page.locator('[class*="tiny"], [id*="tiny"]').count();
    console.log('TinyMCE elements found:', tinyElements);
    
    // Verify the page loaded at all
    expect(title).toBeTruthy();
  });
});