import { test, expect } from '@playwright/test';

test.describe('Debug App Loading', () => {
  test('debug console errors and app loading', async ({ page }) => {
    console.log('🔧 Starting debug session...');

    // Capture console messages
    const consoleMessages = [];
    page.on('console', msg => {
      consoleMessages.push(`${msg.type()}: ${msg.text()}`);
      console.log(`🖥️ Console ${msg.type()}: ${msg.text()}`);
    });

    // Capture page errors
    page.on('pageerror', error => {
      console.log(`❌ Page Error: ${error.message}`);
    });

    // Navigate and wait for the page to fully load
    console.log('📍 Navigating to homepage...');
    await page.goto('http://localhost:5173');
    
    // Wait longer for React to fully initialize
    await page.waitForTimeout(5000);
    
    // Check if React root element exists
    const rootElement = await page.locator('#root').count();
    console.log(`🔍 React root element found: ${rootElement > 0}`);
    
    // Check for any content in the root
    const rootContent = await page.locator('#root').textContent();
    console.log(`📝 Root content preview: ${rootContent?.slice(0, 200) || 'EMPTY'}`);
    
    // Check for specific React component indicators
    const reactDevtoolsFlag = await page.evaluate(() => {
      return window.React !== undefined || window.__REACT_DEVTOOLS_GLOBAL_HOOK__ !== undefined;
    });
    console.log(`⚛️ React detected: ${reactDevtoolsFlag}`);
    
    // Check for Mantine components (should have mantine classes)
    const mantineElements = await page.locator('[class*="mantine"]').count();
    console.log(`🎨 Mantine elements found: ${mantineElements}`);
    
    // Check for WitchCityRope specific content
    const wcrElements = await page.locator('text=WITCH CITY ROPE, text=Welcome, text=Events').count();
    console.log(`🔮 WitchCityRope content found: ${wcrElements}`);
    
    // Check the page source to see what's actually rendered
    const pageContent = await page.content();
    const hasMaintsx = pageContent.includes('/src/main.tsx');
    const hasReactRefresh = pageContent.includes('react-refresh');
    console.log(`📄 Main.tsx script loaded: ${hasMaintsx}`);
    console.log(`🔄 React refresh active: ${hasReactRefresh}`);
    
    // Take a screenshot of current state
    await page.screenshot({ path: 'test-results/debug-app-state.png', fullPage: true });
    console.log('📸 Debug screenshot captured');
    
    // Try to manually check if the app is working by looking for common elements
    await page.waitForTimeout(2000);
    
    // Final assessment
    console.log(`📊 Debug Summary:`);
    console.log(`  - Console messages: ${consoleMessages.length}`);
    console.log(`  - React root exists: ${rootElement > 0}`);
    console.log(`  - Content in root: ${rootContent ? 'YES' : 'NO'}`);
    console.log(`  - Mantine UI elements: ${mantineElements}`);
    console.log(`  - App-specific content: ${wcrElements}`);
    
    // Log all console messages for analysis
    consoleMessages.forEach((msg, i) => {
      if (i < 20) { // Limit to first 20 messages
        console.log(`  ${i + 1}. ${msg}`);
      }
    });
  });

  test('check specific routes directly', async ({ page }) => {
    console.log('🧭 Testing specific routes...');
    
    const routes = ['/', '/login', '/events'];
    
    for (const route of routes) {
      console.log(`🔍 Testing route: ${route}`);
      await page.goto(`http://localhost:5173${route}`);
      await page.waitForTimeout(3000);
      
      const title = await page.title();
      const content = await page.textContent('body');
      
      console.log(`  📝 Route ${route}:`);
      console.log(`    Title: ${title}`);
      console.log(`    Content length: ${content?.length || 0} chars`);
      console.log(`    Has 'WITCH CITY': ${content?.includes('WITCH CITY') || false}`);
      console.log(`    Has 'Login': ${content?.includes('Login') || false}`);
      console.log(`    Has 'Events': ${content?.includes('Events') || false}`);
      
      await page.screenshot({ path: `test-results/debug-route-${route.replace('/', 'home')}.png`, fullPage: true });
    }
  });
});