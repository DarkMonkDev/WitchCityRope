import { test, expect, ConsoleMessage } from '@playwright/test';

const BASE_URL = process.env.BASE_URL || 'http://localhost:8280';

interface ConsoleError {
    text: string;
    url?: string;
    lineNumber?: number;
    stack?: string;
}

test.describe('Console Error Diagnostics', () => {
    test('check for console errors on all pages', async ({ page }) => {
        // Collect console messages
        const consoleMessages: Array<{type: string, text: string, url?: string, lineNumber?: number}> = [];
        const consoleErrors: ConsoleError[] = [];
        
        page.on('console', (msg: ConsoleMessage) => {
            const type = msg.type();
            const text = msg.text();
            const location = msg.location();
            
            consoleMessages.push({
                type,
                text,
                url: location.url,
                lineNumber: location.lineNumber
            });
            
            if (type === 'error') {
                consoleErrors.push({
                    text,
                    url: location.url,
                    lineNumber: location.lineNumber
                });
            }
        });
        
        // Also catch page errors
        page.on('pageerror', error => {
            consoleErrors.push({
                text: error.message,
                stack: error.stack
            });
        });
        
        // Navigate to the home page
        console.log(`Navigating to ${BASE_URL}...`);
        await page.goto(BASE_URL, {
            waitUntil: 'networkidle',
            timeout: 30000
        });
        
        // Wait a bit for any delayed scripts
        await page.waitForTimeout(3000);
        
        // Print all console messages
        console.log('\n=== ALL CONSOLE MESSAGES ===');
        consoleMessages.forEach(msg => {
            console.log(`[${msg.type.toUpperCase()}] ${msg.text}`);
            if (msg.url && msg.lineNumber) {
                console.log(`  at ${msg.url}:${msg.lineNumber}`);
            }
        });
        
        // Print errors separately
        if (consoleErrors.length > 0) {
            console.log('\n=== CONSOLE ERRORS ===');
            consoleErrors.forEach((error, index) => {
                console.log(`\nError ${index + 1}:`);
                console.log(error.text);
                if (error.url && error.lineNumber) {
                    console.log(`Location: ${error.url}:${error.lineNumber}`);
                }
                if (error.stack) {
                    console.log('Stack:', error.stack);
                }
            });
        } else {
            console.log('\n✅ No console errors found!');
        }
        
        // Check specific pages that might have different scripts
        const pagesToCheck = [
            '/events',
            '/login',
            '/admin'
        ];
        
        for (const pageUrl of pagesToCheck) {
            console.log(`\n\nChecking ${pageUrl}...`);
            const pageErrors: ConsoleError[] = [];
            
            // Set up fresh listeners for each page
            page.removeAllListeners('console');
            page.removeAllListeners('pageerror');
            
            page.on('console', (msg: ConsoleMessage) => {
                if (msg.type() === 'error') {
                    const location = msg.location();
                    pageErrors.push({
                        text: msg.text(),
                        url: location.url,
                        lineNumber: location.lineNumber
                    });
                }
            });
            
            page.on('pageerror', error => {
                pageErrors.push({
                    text: error.message,
                    stack: error.stack
                });
            });
            
            try {
                await page.goto(`${BASE_URL}${pageUrl}`, {
                    waitUntil: 'networkidle',
                    timeout: 30000
                });
                
                await page.waitForTimeout(2000);
                
                if (pageErrors.length > 0) {
                    console.log(`Errors on ${pageUrl}:`);
                    pageErrors.forEach(error => {
                        console.log(`- ${error.text}`);
                        if (error.url && error.lineNumber) {
                            console.log(`  at ${error.url}:${error.lineNumber}`);
                        }
                    });
                } else {
                    console.log(`✅ No errors on ${pageUrl}`);
                }
            } catch (err: any) {
                console.log(`Failed to load ${pageUrl}: ${err.message}`);
            }
        }
        
        // Also check for missing resources
        console.log('\n\n=== CHECKING FOR 404s ===');
        const failedRequests: Array<{url: string, status: number, statusText: string}> = [];
        
        page.on('response', response => {
            if (response.status() >= 400) {
                failedRequests.push({
                    url: response.url(),
                    status: response.status(),
                    statusText: response.statusText()
                });
            }
        });
        
        // Reload the home page to catch any 404s
        await page.goto(BASE_URL, {
            waitUntil: 'networkidle',
            timeout: 30000
        });
        
        if (failedRequests.length > 0) {
            console.log('Failed requests:');
            failedRequests.forEach(req => {
                console.log(`- ${req.status} ${req.statusText}: ${req.url}`);
            });
        } else {
            console.log('✅ No failed requests found');
        }
        
        // Assert no critical errors for test status
        expect(consoleErrors.filter(e => !e.text.includes('404')).length).toBe(0);
    });
});