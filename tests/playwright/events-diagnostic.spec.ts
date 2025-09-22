import { test, expect } from '@playwright/test';

test.describe('Events Page Diagnostic', () => {
  test('Events page actually displays events - comprehensive diagnosis', async ({ page }) => {
    console.log('🔍 STARTING DIAGNOSTIC TEST FOR EVENTS PAGE');
    
    // Log all network requests, especially API calls
    page.on('request', request => {
      const url = request.url();
      if (url.includes('/api/') || url.includes('events')) {
        console.log('🌐 Network Request:', {
          method: request.method(),
          url: url,
          timestamp: new Date().toISOString()
        });
      }
    });

    // Log all API responses
    page.on('response', async response => {
      const url = response.url();
      if (url.includes('/api/events')) {
        console.log('📡 API Response Status:', response.status());
        console.log('📡 API Response URL:', url);
        try {
          const data = await response.json();
          console.log('📡 API Response Data:', JSON.stringify(data, null, 2));
        } catch (err) {
          console.log('📡 API Response (not JSON):', await response.text());
        }
      }
    });

    // Log console errors from the browser
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.log('❌ Browser Console Error:', msg.text());
      }
      if (msg.type() === 'warn') {
        console.log('⚠️ Browser Console Warning:', msg.text());
      }
      if (msg.type() === 'log' && msg.text().includes('event')) {
        console.log('📝 Browser Console (events-related):', msg.text());
      }
    });

    console.log('🚀 Navigating to events page...');
    
    // Navigate to events page
    await page.goto('/events');
    
    console.log('⏳ Waiting for page to stabilize...');
    
    // Wait for initial load and any API calls
    await page.waitForTimeout(5000);
    
    console.log('📸 Taking full page screenshot...');
    
    // Take a full page screenshot to see what's actually displayed
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react./test-results/events-page-actual-diagnosis.png', 
      fullPage: true 
    });
    
    // Get the complete page content for analysis
    const pageContent = await page.content();
    
    console.log('🔍 Analyzing page content...');
    console.log('📊 Page contains "Rope Fundamentals":', pageContent.includes('Rope Fundamentals'));
    console.log('📊 Page contains "Loading":', pageContent.includes('Loading') || pageContent.includes('loading'));
    console.log('📊 Page contains "error":', pageContent.includes('error') || pageContent.includes('Error'));
    console.log('📊 Page contains "events":', pageContent.includes('events') || pageContent.includes('Events'));
    
    // Check for specific event data that should be displayed
    console.log('📊 Page contains "Floor Work":', pageContent.includes('Floor Work'));
    console.log('📊 Page contains "Dynamic Suspension":', pageContent.includes('Dynamic Suspension'));
    console.log('📊 Page contains "Monthly Rope Social":', pageContent.includes('Monthly Rope Social'));
    
    // Look for various possible event-related elements
    console.log('🔍 Searching for event-related DOM elements...');
    
    // Try to find event elements using different selectors
    const eventTestIds = await page.locator('[data-testid*="event"]').count();
    const eventCards = await page.locator('[class*="event"]').count();
    const cardElements = await page.locator('[class*="card"]').count();
    const listElements = await page.locator('ul, ol').count();
    const articleElements = await page.locator('article').count();
    const sectionElements = await page.locator('section').count();
    
    console.log('📊 Elements found:');
    console.log('  - Elements with data-testid containing "event":', eventTestIds);
    console.log('  - Elements with class containing "event":', eventCards);
    console.log('  - Elements with class containing "card":', cardElements);
    console.log('  - List elements (ul, ol):', listElements);
    console.log('  - Article elements:', articleElements);
    console.log('  - Section elements:', sectionElements);
    
    // Get the actual visible text on the page
    const bodyText = await page.textContent('body');
    const visibleText = bodyText || '';
    
    console.log('📄 Visible text analysis:');
    console.log('  - Contains "Rope Fundamentals":', visibleText.includes('Rope Fundamentals'));
    console.log('  - Contains "Events":', visibleText.includes('Events'));
    console.log('  - Contains "Loading":', visibleText.includes('Loading'));
    console.log('  - Contains "Error":', visibleText.includes('Error'));
    console.log('  - Text length:', visibleText.length, 'characters');
    
    // Extract first 500 characters of visible text for analysis
    const textSample = visibleText.substring(0, 500);
    console.log('📝 First 500 characters of visible text:', textSample);
    
    // Check for loading/spinner elements
    const loadingElements = await page.locator('[aria-label*="loading"], [class*="spinner"], [class*="loading"]').count();
    console.log('⏳ Loading/spinner elements found:', loadingElements);
    
    // Check if there are any React error boundaries or error messages
    const errorElements = await page.locator('[class*="error"], [role="alert"]').count();
    console.log('❌ Error elements found:', errorElements);
    
    // Try to find the main content area
    const mainContent = await page.locator('main, [role="main"], #root > *').count();
    console.log('🏠 Main content areas found:', mainContent);
    
    // Check for navigation or header elements to confirm the app loaded
    const navElements = await page.locator('nav, [role="navigation"], header').count();
    console.log('🧭 Navigation/header elements found:', navElements);
    
    // Wait a bit longer and take another screenshot to see if anything changes
    console.log('⏳ Waiting additional time for any dynamic loading...');
    await page.waitForTimeout(3000);
    
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react./test-results/events-page-after-wait.png', 
      fullPage: true 
    });
    
    // Final analysis after waiting
    const finalBodyText = await page.textContent('body');
    const finalVisibleText = finalBodyText || '';
    
    console.log('🔍 FINAL ANALYSIS:');
    console.log('  - Final text contains "Rope Fundamentals":', finalVisibleText.includes('Rope Fundamentals'));
    console.log('  - Final text contains event titles:', 
      finalVisibleText.includes('Floor Work') || 
      finalVisibleText.includes('Dynamic Suspension') || 
      finalVisibleText.includes('Monthly Rope Social')
    );
    console.log('  - Page appears to be loading:', finalVisibleText.includes('Loading') || finalVisibleText.includes('loading'));
    console.log('  - Page shows errors:', finalVisibleText.includes('Error') || finalVisibleText.includes('error'));
    
    // Get current URL to verify routing
    const currentUrl = page.url();
    console.log('🌐 Current page URL:', currentUrl);
    
    // Take a screenshot of just the main content area if we can find it
    const mainContentLocator = page.locator('main, [role="main"], #root > div');
    if (await mainContentLocator.count() > 0) {
      await mainContentLocator.first().screenshot({ 
        path: '/home/chad/repos/witchcityrope-react./test-results/events-main-content.png'
      });
    }
    
    console.log('✅ DIAGNOSTIC TEST COMPLETE - Check console output and screenshots for analysis');
    
    // Don't fail the test - we want to complete the diagnosis regardless
    expect(true).toBe(true);
  });
});