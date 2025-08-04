import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth.helpers';

interface ErrorLog {
    time: string;
    text?: string;
    url?: string;
    status?: number;
    message?: string;
    type: 'console' | 'http' | 'page';
}

test.describe('Error Timing Tests', () => {
    test('should track error timing on event creation page', async ({ page }) => {
        const errors: ErrorLog[] = [];
        
        // Set up error monitoring
        page.on('console', msg => {
            if (msg.type() === 'error') {
                errors.push({
                    time: new Date().toISOString(),
                    text: msg.text(),
                    type: 'console'
                });
                console.log(`[${new Date().toLocaleTimeString()}] CONSOLE ERROR:`, msg.text());
            }
        });
        
        page.on('response', response => {
            if (response.status() >= 500) {
                errors.push({
                    time: new Date().toISOString(),
                    url: response.url(),
                    status: response.status(),
                    type: 'http'
                });
                console.log(`[${new Date().toLocaleTimeString()}] HTTP ${response.status()}:`, response.url());
            }
        });
        
        page.on('pageerror', error => {
            errors.push({
                time: new Date().toISOString(),
                message: error.message,
                type: 'page'
            });
            console.log(`[${new Date().toLocaleTimeString()}] PAGE ERROR:`, error.message);
        });
        
        // Login
        await login(page, 'admin@witchcityrope.com', 'Test123!');
        
        // Clear errors from login process
        errors.length = 0;
        
        console.log(`[${new Date().toLocaleTimeString()}] Starting navigation to event creation page`);
        
        // Navigate to event creation page
        await page.goto('/admin/events/new-standardized', {
            waitUntil: 'networkidle',
            timeout: 30000
        });
        
        console.log(`[${new Date().toLocaleTimeString()}] Page loaded`);
        
        // Wait for any delayed errors
        await page.waitForTimeout(3000);
        
        // Check page state
        const pageState = await page.evaluate(() => {
            return {
                url: window.location.href,
                title: document.title,
                hasForm: !!document.querySelector('form'),
                hasErrorUI: !!document.querySelector('.blazor-error-ui'),
                bodyTextPreview: document.body.innerText.substring(0, 500)
            };
        });
        
        console.log('\n📊 Page State:');
        console.log('URL:', pageState.url);
        console.log('Title:', pageState.title);
        console.log('Has form:', pageState.hasForm);
        console.log('Has error UI:', pageState.hasErrorUI);
        
        console.log('\n📋 Error Summary:');
        console.log(`Total errors captured: ${errors.length}`);
        
        errors.forEach((err, i) => {
            console.log(`\n${i + 1}. [${err.type.toUpperCase()}] at ${err.time}`);
            if (err.text) console.log('   Message:', err.text);
            if (err.url) console.log('   URL:', err.url);
            if (err.status) console.log('   Status:', err.status);
            if (err.message) console.log('   Message:', err.message);
        });
        
        // Check for specific error patterns
        const hasDateRangeError = errors.some(err => 
            (err.text && err.text.includes('DateRangePickerEvents')) ||
            (err.message && err.message.includes('DateRangePickerEvents'))
        );
        
        if (hasDateRangeError) {
            console.log('\n⚠️  DateRangePicker error detected!');
            console.log('This error occurs on page load, not from user interaction.');
        }
        
        // Try to interact with the page despite errors
        console.log('\n📍 Attempting to interact with form...');
        const nameInput = page.locator('input[placeholder*="Workshop"], input[type="text"]:not([type="hidden"])').first();
        
        if (await nameInput.count() > 0) {
            await nameInput.click();
            await nameInput.fill('Test Event');
            console.log('✅ Able to interact with form despite errors');
        } else {
            console.log('❌ Could not find form input');
        }
        
        // Take screenshot
        await page.screenshot({ path: 'test-results/error-timing-test.png', fullPage: true });
        
        // Verify page loaded despite errors
        expect(pageState.hasForm || pageState.hasErrorUI).toBeTruthy();
    });

    test('should monitor error patterns across different pages', async ({ page }) => {
        const errorsByPage: Record<string, ErrorLog[]> = {};
        
        // Set up error monitoring
        let currentPage = '';
        
        page.on('console', msg => {
            if (msg.type() === 'error' && currentPage) {
                if (!errorsByPage[currentPage]) errorsByPage[currentPage] = [];
                errorsByPage[currentPage].push({
                    time: new Date().toISOString(),
                    text: msg.text(),
                    type: 'console'
                });
            }
        });
        
        // Login
        await login(page, 'admin@witchcityrope.com', 'Test123!');
        
        // Test different pages
        const pagesToTest = [
            { path: '/admin/events', name: 'Admin Events' },
            { path: '/admin/events/new-standardized', name: 'Create Event' },
            { path: '/admin/users', name: 'Admin Users' },
            { path: '/events', name: 'Public Events' }
        ];
        
        for (const testPage of pagesToTest) {
            currentPage = testPage.name;
            errorsByPage[currentPage] = [];
            
            console.log(`\nTesting ${testPage.name}...`);
            
            try {
                await page.goto(testPage.path, { waitUntil: 'networkidle', timeout: 15000 });
                await page.waitForTimeout(2000);
                
                const errorCount = errorsByPage[currentPage].length;
                console.log(`   Errors: ${errorCount}`);
                
                if (errorCount > 0) {
                    errorsByPage[currentPage].forEach(err => {
                        console.log(`   - ${err.text}`);
                    });
                }
            } catch (error) {
                console.log(`   Navigation error: ${error.message}`);
            }
        }
        
        // Summary
        console.log('\n📊 Error Summary by Page:');
        Object.entries(errorsByPage).forEach(([page, errors]) => {
            console.log(`${page}: ${errors.length} errors`);
        });
    });

    test('should identify JavaScript initialization errors', async ({ page }) => {
        const initErrors: string[] = [];
        
        page.on('console', msg => {
            if (msg.type() === 'error') {
                const text = msg.text();
                // Look for initialization-related errors
                if (text.includes('Cannot read') || 
                    text.includes('is not defined') ||
                    text.includes('Failed to') ||
                    text.includes('ReferenceError')) {
                    initErrors.push(text);
                }
            }
        });
        
        await page.goto('/login');
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(2000);
        
        if (initErrors.length > 0) {
            console.log('JavaScript Initialization Errors:');
            initErrors.forEach(err => console.log(`   - ${err}`));
        } else {
            console.log('No JavaScript initialization errors detected');
        }
        
        // Verify page still functions
        const canInteract = await page.evaluate(() => {
            const inputs = document.querySelectorAll('input');
            const buttons = document.querySelectorAll('button');
            return {
                inputCount: inputs.length,
                buttonCount: buttons.length,
                hasInteractiveElements: inputs.length > 0 || buttons.length > 0
            };
        });
        
        console.log('Page interaction capability:', canInteract);
        expect(canInteract.hasInteractiveElements).toBeTruthy();
    });

    test('should track network request failures', async ({ page }) => {
        const failedRequests: Array<{ url: string; reason: string }> = [];
        
        page.on('requestfailed', request => {
            failedRequests.push({
                url: request.url(),
                reason: request.failure()?.errorText || 'Unknown'
            });
        });
        
        await page.goto('/');
        await page.waitForLoadState('networkidle');
        
        // Navigate through several pages
        const pages = ['/events', '/login', '/register'];
        
        for (const path of pages) {
            await page.goto(path);
            await page.waitForLoadState('networkidle');
        }
        
        if (failedRequests.length > 0) {
            console.log('Failed Network Requests:');
            failedRequests.forEach(req => {
                console.log(`   - ${req.url}`);
                console.log(`     Reason: ${req.reason}`);
            });
        } else {
            console.log('No network request failures detected');
        }
        
        // Verify critical resources loaded
        expect(failedRequests.filter(r => r.url.includes('.css') || r.url.includes('.js')).length).toBe(0);
    });
});