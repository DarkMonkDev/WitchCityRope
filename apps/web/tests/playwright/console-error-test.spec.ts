import { test, expect } from '@playwright/test';

test.describe('Console Error Investigation', () => {
    test('capture console errors and page content on port 5173', async ({ page }) => {
        const consoleMessages: string[] = [];
        const errors: string[] = [];

        // Listen for console messages
        page.on('console', (message) => {
            const text = `${message.type()}: ${message.text()}`;
            consoleMessages.push(text);
            console.log(`Console: ${text}`);
        });

        // Listen for page errors
        page.on('pageerror', (error) => {
            const errorText = `Page Error: ${error.message}`;
            errors.push(errorText);
            console.log(errorText);
        });

        console.log('🔍 Testing port 5173 (React dev server)...');
        await page.goto('http://localhost:5173');
        
        // Wait for potential loading
        await page.waitForTimeout(3000);

        // Take screenshot
        await page.screenshot({ path: 'test-results/port-5173-homepage.png' });
        console.log('📷 Screenshot saved: port-5173-homepage.png');

        // Check if root div exists and has content
        const rootDiv = page.locator('#root');
        const rootExists = await rootDiv.count() > 0;
        console.log(`Root div exists: ${rootExists}`);
        
        if (rootExists) {
            const rootContent = await rootDiv.innerHTML().catch(() => '');
            console.log(`Root div content length: ${rootContent.length}`);
            console.log(`Root div content (first 200 chars): ${rootContent.substring(0, 200)}`);
        }

        // Check for specific routes
        console.log('🔍 Testing /login route...');
        await page.goto('http://localhost:5173/login');
        await page.waitForTimeout(2000);
        await page.screenshot({ path: 'test-results/port-5173-login.png' });
        
        const loginHeading = await page.locator('h1').textContent().catch(() => null);
        console.log(`Login page h1: ${loginHeading}`);

        console.log('🔍 Testing /events route...');
        await page.goto('http://localhost:5173/events');
        await page.waitForTimeout(2000);
        await page.screenshot({ path: 'test-results/port-5173-events.png' });

        // Check for API calls in network tab
        await page.goto('http://localhost:5173/events');
        
        // Wait for potential API calls
        await page.waitForTimeout(5000);

        console.log(`Total console messages: ${consoleMessages.length}`);
        console.log(`Total errors: ${errors.length}`);
        
        // Report findings
        if (errors.length > 0) {
            console.log('🚨 JavaScript Errors Found:');
            errors.forEach(error => console.log(`  - ${error}`));
        }
        
        if (consoleMessages.length > 0) {
            console.log('📝 Console Messages:');
            consoleMessages.forEach(msg => console.log(`  - ${msg}`));
        }
    });
});