import { test, expect } from '@playwright/test';

test.describe('Final React App Diagnosis', () => {
  test('comprehensive TypeScript error and rendering check', async ({ page }) => {
    console.log('=== FINAL COMPREHENSIVE DIAGNOSIS ===');
    
    // Capture ALL console messages including errors
    const allLogs: string[] = [];
    const errorLogs: string[] = [];
    const warnLogs: string[] = [];
    
    page.on('console', msg => {
      const text = msg.text();
      const type = msg.type();
      
      allLogs.push(`[${type}] ${text}`);
      
      if (type === 'error') {
        errorLogs.push(text);
      } else if (type === 'warning') {
        warnLogs.push(text);
      }
    });
    
    // Capture any page errors
    page.on('pageerror', error => {
      errorLogs.push(`PAGE ERROR: ${error.message}`);
    });
    
    // Navigate to the page
    await page.goto('http://localhost:5173', { 
      waitUntil: 'networkidle',
      timeout: 15000 
    });
    
    // Wait to capture all initialization logs
    await page.waitForTimeout(3000);
    
    // Test if React initialization console log appears
    const hasReactInitLog = allLogs.some(log => log.includes('Starting React app initialization'));
    console.log('React initialization log found:', hasReactInitLog);
    
    // Check what actually happens after script loads
    const rootElement = await page.evaluate(() => {
      const root = document.getElementById('root');
      return {
        exists: !!root,
        innerHTML: root?.innerHTML || '',
        childNodes: root?.childNodes?.length || 0
      };
    });
    
    console.log('Root element analysis:', JSON.stringify(rootElement, null, 2));
    
    // Try to detect any TypeScript compilation errors
    const hasTypeScriptError = errorLogs.some(log => 
      log.includes('TS') || 
      log.includes('Cannot resolve') || 
      log.includes('Module not found') ||
      log.includes('Unexpected token') ||
      log.includes('SyntaxError')
    );
    
    console.log('TypeScript/Module errors detected:', hasTypeScriptError);
    
    // Print all logs for analysis
    console.log('\n=== ALL CONSOLE LOGS ===');
    allLogs.forEach((log, i) => console.log(`${i + 1}: ${log}`));
    
    console.log('\n=== ERROR LOGS ===');
    errorLogs.forEach((log, i) => console.log(`${i + 1}: ${log}`));
    
    console.log('\n=== WARNING LOGS ===');
    warnLogs.forEach((log, i) => console.log(`${i + 1}: ${log}`));
    
    // Final diagnosis
    console.log('\n=== FINAL DIAGNOSIS ===');
    console.log('Script loads:', true); // We know this from earlier test
    console.log('React init log appears:', hasReactInitLog);
    console.log('Root element populated:', rootElement.childNodes > 0);
    console.log('TypeScript errors present:', hasTypeScriptError);
    console.log('Total console messages:', allLogs.length);
    console.log('Error count:', errorLogs.length);
    console.log('Warning count:', warnLogs.length);
    
    if (hasTypeScriptError) {
      console.log('🚨 ROOT CAUSE: TypeScript compilation errors preventing React execution');
    } else if (!hasReactInitLog) {
      console.log('🚨 ROOT CAUSE: main.tsx script not executing (silent failure)');
    } else if (hasReactInitLog && rootElement.childNodes === 0) {
      console.log('🚨 ROOT CAUSE: React initializing but failing to mount (component error)');
    } else {
      console.log('✅ All systems appear functional');
    }
    
    // Take final screenshot
    await page.screenshot({ path: 'final-diagnosis.png' });
  });
});
