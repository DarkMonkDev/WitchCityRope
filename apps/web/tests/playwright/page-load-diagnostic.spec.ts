import { test, expect } from '@playwright/test';

test('can the fucking page even load', async ({ page }) => {
  console.log('🔍 Starting diagnostic test for http://localhost:5173');
  
  // Capture all console messages
  const consoleMessages: string[] = [];
  page.on('console', msg => {
    const message = `[${msg.type()}] ${msg.text()}`;
    console.log('Browser Console:', message);
    consoleMessages.push(message);
  });
  
  // Capture page errors
  const pageErrors: string[] = [];
  page.on('pageerror', err => {
    const error = `Page Error: ${err.message}`;
    console.log(error);
    pageErrors.push(error);
  });
  
  // Capture network failures
  const networkErrors: string[] = [];
  page.on('requestfailed', request => {
    const error = `Failed Request: ${request.method()} ${request.url()} - ${request.failure()?.errorText}`;
    console.log(error);
    networkErrors.push(error);
  });
  
  console.log('🌐 Attempting to navigate to http://localhost:5173...');
  
  try {
    // Navigate with extended timeout
    await page.goto('http://localhost:5173', { 
      timeout: 15000,
      waitUntil: 'networkidle' 
    });
    
    console.log('✅ Page navigation completed');
    
    // Take screenshot immediately
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/apps/web/what-the-fuck-is-happening.png',
      fullPage: true 
    });
    console.log('📸 Screenshot saved: what-the-fuck-is-happening.png');
    
    // Get basic page info
    const title = await page.title();
    console.log('📄 Page title:', title);
    
    const url = page.url();
    console.log('🔗 Current URL:', url);
    
    // Check if page has content
    const content = await page.content();
    console.log('📝 Page content length:', content.length);
    console.log('📝 Has substantial content:', content.length > 1000);
    
    // Look for React root element
    const reactRoot = await page.locator('#root').count();
    console.log('⚛️  React root element found:', reactRoot > 0);
    
    if (reactRoot > 0) {
      const rootContent = await page.locator('#root').innerHTML();
      console.log('⚛️  Root element content length:', rootContent.length);
      console.log('⚛️  Root element has content:', rootContent.length > 10);
    }
    
    // Check for common error patterns in the DOM
    const errorPatterns = [
      'text=/error/i',
      'text=/failed/i', 
      'text=/wrong/i',
      'text=/cannot/i',
      'text=/404/i',
      'text=/500/i'
    ];
    
    let errorCount = 0;
    for (const pattern of errorPatterns) {
      const count = await page.locator(pattern).count();
      if (count > 0) {
        console.log(`❌ Found ${count} elements matching error pattern: ${pattern}`);
        errorCount += count;
      }
    }
    
    console.log('❌ Total error indicators found:', errorCount);
    
    // Check for loading indicators
    const loadingIndicators = [
      'text=/loading/i',
      'text=/wait/i',
      '[data-testid*="loading"]',
      '.loading',
      '.spinner'
    ];
    
    let loadingCount = 0;
    for (const indicator of loadingIndicators) {
      const count = await page.locator(indicator).count();
      if (count > 0) {
        console.log(`⏳ Found ${count} loading indicators: ${indicator}`);
        loadingCount += count;
      }
    }
    
    console.log('⏳ Total loading indicators found:', loadingCount);
    
    // Check for interactive elements
    const buttons = await page.locator('button').count();
    const links = await page.locator('a').count();
    const inputs = await page.locator('input').count();
    
    console.log('🖱️  Interactive elements found:');
    console.log(`   Buttons: ${buttons}`);
    console.log(`   Links: ${links}`);
    console.log(`   Inputs: ${inputs}`);
    
    // Try to find navigation or main content
    const nav = await page.locator('nav').count();
    const main = await page.locator('main').count();
    const header = await page.locator('header').count();
    
    console.log('🏗️  Structure elements:');
    console.log(`   Navigation: ${nav}`);
    console.log(`   Main content: ${main}`);
    console.log(`   Header: ${header}`);
    
    // Wait a bit more to see if anything loads
    console.log('⏱️  Waiting 3 seconds for any delayed content...');
    await page.waitForTimeout(3000);
    
    // Take another screenshot after waiting
    await page.screenshot({ 
      path: '/home/chad/repos/witchcityrope-react/apps/web/after-wait-screenshot.png',
      fullPage: true 
    });
    console.log('📸 After-wait screenshot saved: after-wait-screenshot.png');
    
    // Final content check
    const finalContent = await page.content();
    console.log('📝 Final content length:', finalContent.length);
    console.log('📈 Content changed during wait:', finalContent.length !== content.length);
    
    // Log summary
    console.log('\n📊 DIAGNOSTIC SUMMARY:');
    console.log('======================');
    console.log(`✅ Page loaded successfully: ${title}`);
    console.log(`📏 Content length: ${finalContent.length} characters`);
    console.log(`🔥 Console messages: ${consoleMessages.length}`);
    console.log(`❌ Page errors: ${pageErrors.length}`);
    console.log(`🌐 Network errors: ${networkErrors.length}`);
    console.log(`🖱️  Interactive elements: ${buttons + links + inputs}`);
    console.log(`❌ Error indicators in DOM: ${errorCount}`);
    console.log(`⏳ Loading indicators: ${loadingCount}`);
    
    // Print first few console messages if any
    if (consoleMessages.length > 0) {
      console.log('\n🔍 CONSOLE MESSAGES:');
      consoleMessages.slice(0, 10).forEach(msg => console.log(`   ${msg}`));
      if (consoleMessages.length > 10) {
        console.log(`   ... and ${consoleMessages.length - 10} more messages`);
      }
    }
    
    // Print any errors
    if (pageErrors.length > 0) {
      console.log('\n💥 PAGE ERRORS:');
      pageErrors.forEach(error => console.log(`   ${error}`));
    }
    
    if (networkErrors.length > 0) {
      console.log('\n🌐 NETWORK ERRORS:');
      networkErrors.forEach(error => console.log(`   ${error}`));
    }
    
    // Basic functionality test - can we click on things?
    if (buttons > 0) {
      console.log('\n🖱️  Testing first button click...');
      try {
        await page.locator('button').first().click({ timeout: 2000 });
        console.log('✅ Button click succeeded');
      } catch (error) {
        console.log('❌ Button click failed:', error);
      }
    }
    
  } catch (error) {
    console.log('\n💥 CRITICAL FAILURE:');
    console.log('===================');
    console.log('❌ FAILED TO LOAD PAGE:', error);
    
    // Take error screenshot
    try {
      await page.screenshot({ 
        path: '/home/chad/repos/witchcityrope-react/apps/web/error-screenshot.png',
        fullPage: true 
      });
      console.log('📸 Error screenshot saved: error-screenshot.png');
    } catch (screenshotError) {
      console.log('❌ Could not take error screenshot:', screenshotError);
    }
    
    // Log the error details
    console.log('Error type:', typeof error);
    console.log('Error message:', error instanceof Error ? error.message : String(error));
    
    throw error;
  }
});

test('network connectivity check', async ({ page }) => {
  console.log('🌐 Testing network connectivity...');
  
  // Test if we can reach the server at all
  const response = await page.request.get('http://localhost:5173');
  console.log('HTTP Response Status:', response.status());
  console.log('HTTP Response Headers:', await response.allHeaders());
  
  const body = await response.text();
  console.log('Response body length:', body.length);
  console.log('Response body preview (first 500 chars):');
  console.log(body.substring(0, 500));
});