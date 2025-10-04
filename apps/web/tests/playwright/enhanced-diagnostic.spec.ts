import { test, expect } from '@playwright/test';

test.describe('Enhanced Diagnostic - Based on Lessons Learned', () => {
  test('systematic React app rendering verification', async ({ page }) => {
    console.log('=== ENHANCED DIAGNOSTIC TEST ===');
    console.log('Based on test-executor lessons learned patterns');
    
    // Step 1: Infrastructure vs Application Layer Distinction
    console.log('\n1. TESTING INFRASTRUCTURE LAYER');
    
    // Check API accessibility first
    const apiResponse = await fetch('http://localhost:5656/api/events');
    const apiStatus = apiResponse.status;
    const apiEvents = apiStatus === 200 ? await apiResponse.text() : 'Failed';
    
    console.log('API Status:', apiStatus);
    console.log('API Events Response Length:', apiEvents.length);
    console.log('API Working:', apiStatus === 200 ? '✅ YES' : '❌ NO');
    
    // Step 2: React Rendering Verification Pattern
    console.log('\n2. TESTING APPLICATION LAYER - React Rendering');
    
    await page.goto('http://localhost:5173', { 
      waitUntil: 'networkidle',
      timeout: 10000 
    });
    
    // Critical checks from lessons learned
    const title = await page.title();
    const rootElement = await page.$('#root');
    const rootContent = await page.evaluate(() => {
      const root = document.getElementById('root');
      return root ? root.innerHTML : null;
    });
    
    const reactGlobal = await page.evaluate(() => {
      return typeof window.React !== 'undefined';
    });
    
    const viteReactPlugin = await page.evaluate(() => {
      return !!window.__vite_plugin_react_preamble_installed__;
    });
    
    console.log('Page title:', title);
    console.log('Root element exists:', !!rootElement);
    console.log('Root element content length:', rootContent ? rootContent.length : 0);
    console.log('React globally available:', reactGlobal);
    console.log('Vite React plugin loaded:', viteReactPlugin);
    
    // Critical Discovery Pattern from Lessons
    const htmlContent = await page.content();
    const hasViteClient = htmlContent.includes('vite/client');
    const hasMainTsx = htmlContent.includes('/src/main.tsx');
    
    console.log('Head includes Vite client:', hasViteClient);
    console.log('Head includes main.tsx script:', hasMainTsx);
    console.log('Full HTML length:', htmlContent.length);
    
    // Step 3: Events Page Specific Test
    console.log('\n3. TESTING EVENTS PAGE RENDERING');
    
    await page.goto('http://localhost:5173/events', {
      waitUntil: 'networkidle',
      timeout: 10000
    });
    
    const eventsPageContent = await page.textContent('body');
    const eventsContentLength = eventsPageContent ? eventsPageContent.length : 0;
    
    // Check for actual DOM elements
    const formCount = await page.$$eval('form', forms => forms.length);
    const inputCount = await page.$$eval('input', inputs => inputs.length);
    const buttonCount = await page.$$eval('button', buttons => buttons.length);
    
    console.log('Events page content length:', eventsContentLength);
    console.log('Forms found:', formCount);
    console.log('Inputs found:', inputCount);
    console.log('Buttons found:', buttonCount);
    
    // Step 4: Diagnostic Summary
    console.log('\n4. DIAGNOSTIC SUMMARY');
    console.log('Infrastructure healthy:', apiStatus === 200 ? '✅' : '❌');
    console.log('HTML delivery working:', title.includes('Witch') ? '✅' : '❌');
    console.log('React mounting working:', (rootContent && rootContent.length > 0) ? '✅' : '❌');
    console.log('Events page functional:', eventsContentLength > 50 ? '✅' : '❌');
    
    // Take screenshots for evidence
    await page.screenshot({ path: 'enhanced-diagnostic-home.png' });
    await page.goto('http://localhost:5173/events');
    await page.screenshot({ path: 'enhanced-diagnostic-events.png' });
    
    // Final determination based on lessons learned
    const reactWorking = rootContent && rootContent.length > 0;
    const infrastructureWorking = apiStatus === 200;
    
    console.log('\n5. FINAL DIAGNOSIS');
    if (infrastructureWorking && !reactWorking) {
      console.log('🚨 CRITICAL: Infrastructure healthy but React app not rendering');
      console.log('🔍 Root Cause: main.tsx script loading/execution issue');
      console.log('📋 Recommended Fix: React developer needed');
    } else if (!infrastructureWorking) {
      console.log('🚨 CRITICAL: Infrastructure issues blocking app');
      console.log('🔍 Root Cause: API service problems');
      console.log('📋 Recommended Fix: DevOps/Backend developer needed');
    } else if (reactWorking && infrastructureWorking) {
      console.log('✅ SUCCESS: Both infrastructure and React app working');
    }
  });
});
