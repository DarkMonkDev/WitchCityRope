import { test, expect } from '@playwright/test';

interface ScriptRequest {
    url: string;
    method: string;
}

interface FailedScript {
    url: string;
    status: number;
    statusText: string;
}

interface ConsoleError {
    text: string;
    location?: {
        url: string;
        lineNumber: number;
    };
    stack?: string;
}

test.describe('Script Loading Diagnostics', () => {
    test('analyze script loading and jQuery validation setup', async ({ page }) => {
        const scriptRequests: ScriptRequest[] = [];
        const failedScripts: FailedScript[] = [];
        const consoleErrors: ConsoleError[] = [];
        
        // Track script loading through requests
        page.on('request', request => {
            if (request.resourceType() === 'script') {
                scriptRequests.push({
                    url: request.url(),
                    method: request.method()
                });
            }
        });
        
        page.on('response', response => {
            if (response.request().resourceType() === 'script' && response.status() !== 200) {
                failedScripts.push({
                    url: response.url(),
                    status: response.status(),
                    statusText: response.statusText()
                });
            }
        });
        
        // Check console for errors
        page.on('console', msg => {
            if (msg.type() === 'error') {
                const location = msg.location();
                consoleErrors.push({
                    text: msg.text(),
                    location: {
                        url: location.url,
                        lineNumber: location.lineNumber
                    }
                });
            }
        });
        
        page.on('pageerror', error => {
            consoleErrors.push({
                text: error.message,
                stack: error.stack
            });
        });
        
        // Navigate to login page
        console.log('Navigating to Login page...');
        await page.goto('http://localhost:8280/login', {
            waitUntil: 'networkidle',
            timeout: 30000
        });
        
        // Wait for potential script execution
        await page.waitForTimeout(3000);
        
        // Check what scripts were loaded
        console.log('\n=== SCRIPT LOADING SUMMARY ===');
        console.log(`Total scripts requested: ${scriptRequests.length}`);
        
        console.log('\n=== LOADED SCRIPTS ===');
        scriptRequests.forEach(script => {
            console.log(`- ${script.url}`);
        });
        
        if (failedScripts.length > 0) {
            console.log('\n=== FAILED SCRIPTS ===');
            failedScripts.forEach(script => {
                console.log(`- ${script.status} ${script.statusText}: ${script.url}`);
            });
        }
        
        // Check if jQuery is loaded
        console.log('\n=== JQUERY STATUS ===');
        const jqueryLoaded = await page.evaluate(() => {
            return typeof (window as any).jQuery !== 'undefined';
        });
        console.log(`jQuery loaded: ${jqueryLoaded}`);
        
        if (jqueryLoaded) {
            const jqueryVersion = await page.evaluate(() => (window as any).jQuery.fn.jquery);
            console.log(`jQuery version: ${jqueryVersion}`);
        }
        
        // Check for jQuery validation
        const validationLoaded = await page.evaluate(() => {
            return typeof (window as any).jQuery !== 'undefined' && 
                   typeof (window as any).jQuery.validator !== 'undefined';
        });
        console.log(`jQuery Validation loaded: ${validationLoaded}`);
        
        // Check for unobtrusive validation
        const unobtrusiveLoaded = await page.evaluate(() => {
            return typeof (window as any).jQuery !== 'undefined' && 
                   typeof (window as any).jQuery.validator !== 'undefined' && 
                   typeof (window as any).jQuery.validator.unobtrusive !== 'undefined';
        });
        console.log(`jQuery Unobtrusive Validation loaded: ${unobtrusiveLoaded}`);
        
        // Check console errors
        if (consoleErrors.length > 0) {
            console.log('\n=== CONSOLE ERRORS ===');
            consoleErrors.forEach((error, idx) => {
                console.log(`\nError ${idx + 1}:`);
                console.log(error.text);
                if (error.location) {
                    console.log(`Location: ${error.location.url}:${error.location.lineNumber}`);
                }
            });
        }
        
        // Try to identify the specific issue
        console.log('\n=== DIAGNOSTIC CHECK ===');
        const diagnostics = await page.evaluate(() => {
            const results: any = {
                jQueryExists: typeof (window as any).jQuery !== 'undefined',
                dollarExists: typeof (window as any).$ !== 'undefined',
                jQueryValidatorExists: false,
                jQueryUnobtrusiveExists: false,
                windowJQueryExists: typeof (window as any).jQuery !== 'undefined',
                documentReady: document.readyState,
                scriptsInDOM: [] as string[]
            };
            
            if (typeof (window as any).jQuery !== 'undefined') {
                results.jQueryValidatorExists = typeof (window as any).jQuery.validator !== 'undefined';
                if ((window as any).jQuery.validator) {
                    results.jQueryUnobtrusiveExists = typeof (window as any).jQuery.validator.unobtrusive !== 'undefined';
                }
            }
            
            // Get all script tags
            const scripts = document.querySelectorAll('script');
            scripts.forEach(script => {
                if (script.src) {
                    results.scriptsInDOM.push(script.src);
                }
            });
            
            return results;
        });
        
        console.log('jQuery exists:', diagnostics.jQueryExists);
        console.log('$ exists:', diagnostics.dollarExists);
        console.log('jQuery.validator exists:', diagnostics.jQueryValidatorExists);
        console.log('jQuery.validator.unobtrusive exists:', diagnostics.jQueryUnobtrusiveExists);
        console.log('window.jQuery exists:', diagnostics.windowJQueryExists);
        console.log('Document ready state:', diagnostics.documentReady);
        
        console.log('\nScripts in DOM:');
        diagnostics.scriptsInDOM.forEach((script: string) => {
            console.log(`- ${script}`);
        });
        
        // Take screenshot for reference
        await page.screenshot({ 
            path: 'screenshots/script-loading-diagnostic.png', 
            fullPage: true 
        });
        
        // Basic assertions
        expect(failedScripts.length).toBe(0);
        expect(jqueryLoaded).toBeTruthy();
    });
});