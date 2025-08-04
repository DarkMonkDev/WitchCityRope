import { test, expect } from '@playwright/test';
import { login, wait, log } from './test-helpers';

test.describe('Console Warning Diagnostics', () => {
    test('check for console warnings and deprecation notices', async ({ page }) => {
        const warnings: string[] = [];
        const errors: string[] = [];
        const deprecationWarnings: string[] = [];
        
        // Capture ALL console messages
        page.on('console', msg => {
            const type = msg.type();
            const text = msg.text();
            
            console.log(`[${type.toUpperCase()}] ${text}`);
            
            if (type === 'warning') {
                warnings.push(text);
                // Check for deprecation warnings
                if (text.toLowerCase().includes('deprecat') || text.toLowerCase().includes('unload')) {
                    deprecationWarnings.push(text);
                }
            } else if (type === 'error') {
                errors.push(text);
            }
        });
        
        // Capture page errors
        page.on('pageerror', error => {
            console.log('[PAGE ERROR]', error.message);
            errors.push(error.message);
        });
        
        console.log('Checking for console warnings/errors...\n');
        
        // Login
        await login(page, 'admin@witchcityrope.com', 'Test123!');
        
        // Navigate to event edit page
        console.log('\nNavigating to event edit page...');
        await page.goto('http://localhost:5651/admin/events/edit/4b279cb7-429c-402b-a018-b3c8e8a5e058', { 
            waitUntil: 'networkidle' 
        });
        await wait(5000);
        
        // Check Blazor initialization
        const blazorInfo = await page.evaluate(() => {
            // Try to manually start Blazor if it hasn't started
            if ((window as any).Blazor && !(window as any).Blazor._started) {
                try {
                    (window as any).Blazor.start();
                    return { manualStart: 'attempted' };
                } catch (e: any) {
                    return { manualStartError: e.message };
                }
            }
            
            return {
                hasBlazor: typeof (window as any).Blazor !== 'undefined',
                blazorStarted: (window as any).Blazor?._started || false
            };
        });
        
        console.log('\nBlazor info:', blazorInfo);
        
        console.log('\n=== SUMMARY ===');
        console.log(`Warnings found: ${warnings.length}`);
        warnings.forEach((w, i) => console.log(`  ${i + 1}. ${w}`));
        
        console.log(`\nErrors found: ${errors.length}`);
        errors.forEach((e, i) => console.log(`  ${i + 1}. ${e}`));
        
        console.log(`\nDeprecation warnings found: ${deprecationWarnings.length}`);
        deprecationWarnings.forEach((w, i) => console.log(`  ${i + 1}. ${w}`));
        
        // Check for blazor.server.js specifically
        const blazorServerInfo = await page.evaluate(() => {
            const scripts = Array.from(document.querySelectorAll('script[src*="blazor"]'));
            return scripts.map(s => s.getAttribute('src') || '');
        });
        
        console.log('\nBlazor scripts loaded:');
        blazorServerInfo.forEach(s => console.log(`  - ${s}`));
        
        // Take screenshot for reference
        await page.screenshot({ 
            path: 'screenshots/console-warnings-check.png', 
            fullPage: true 
        });
        
        // Log deprecation warnings as test output
        if (deprecationWarnings.length > 0) {
            log(`Found ${deprecationWarnings.length} deprecation warnings`, 'warning');
        }
    });
});