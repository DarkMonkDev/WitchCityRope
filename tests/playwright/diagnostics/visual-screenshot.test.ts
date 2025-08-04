import { test, expect } from '@playwright/test';
import path from 'path';

const BASE_URL = process.env.BASE_URL || 'http://localhost:5651';

test.describe('Visual Screenshot Diagnostics', () => {
    test.beforeEach(async ({ page }) => {
        // Set up console message capturing
        page.on('console', msg => {
            console.log(`${msg.type()}: ${msg.text()}`);
        });
        
        page.on('pageerror', error => {
            console.log('Page error:', error.message);
        });
    });

    test('capture home page screenshot and analyze styling', async ({ page }) => {
        console.log('Starting home page screenshot capture...');
        
        // Set viewport to capture desktop view
        await page.setViewportSize({ width: 1920, height: 1080 });
        
        console.log(`Navigating to ${BASE_URL}/...`);
        
        // Navigate to the home page
        await page.goto(`${BASE_URL}/`, { 
            waitUntil: 'networkidle',
            timeout: 30000 
        });
        
        // Wait a bit for any CSS animations or JavaScript to complete
        await page.waitForTimeout(2000);
        
        // Generate timestamp for unique filename
        const timestamp = Date.now();
        const screenshotPath = path.join('screenshots', `home-page-screenshot-${timestamp}.png`);
        
        console.log('Taking full page screenshot...');
        
        // Take full page screenshot
        await page.screenshot({
            path: screenshotPath,
            fullPage: true
        });
        
        console.log(`Screenshot saved to: ${screenshotPath}`);
        
        // Check for CSS and styling specific elements
        const stylingInfo = await page.evaluate(() => {
            const body = document.body;
            const heroSection = document.querySelector('.hero, .hero-section, #hero, [class*="hero"]');
            
            return {
                bodyBackgroundColor: window.getComputedStyle(body).backgroundColor,
                bodyBackground: window.getComputedStyle(body).background,
                heroElementFound: !!heroSection,
                heroStyles: heroSection ? {
                    background: window.getComputedStyle(heroSection).background,
                    backgroundColor: window.getComputedStyle(heroSection).backgroundColor,
                    backgroundImage: window.getComputedStyle(heroSection).backgroundImage,
                    color: window.getComputedStyle(heroSection).color,
                    width: window.getComputedStyle(heroSection).width,
                    height: window.getComputedStyle(heroSection).height
                } : null,
                totalElements: document.querySelectorAll('*').length,
                hasStylesheets: document.querySelectorAll('link[rel="stylesheet"]').length > 0,
                stylesheetCount: document.querySelectorAll('link[rel="stylesheet"]').length,
                customProperties: Array.from(document.documentElement.style).filter(prop => prop.startsWith('--'))
            };
        });
        
        console.log('Styling analysis:');
        console.log(JSON.stringify(stylingInfo, null, 2));
        
        // Verify basic styling requirements
        expect(stylingInfo.hasStylesheets).toBeTruthy();
        expect(stylingInfo.totalElements).toBeGreaterThan(10);
    });

    test('capture login page screenshot', async ({ page }) => {
        console.log('Starting login page screenshot capture...');
        
        await page.setViewportSize({ width: 1920, height: 1080 });
        
        await page.goto(`${BASE_URL}/login`, {
            waitUntil: 'networkidle',
            timeout: 30000
        });
        
        await page.waitForTimeout(2000);
        
        const timestamp = Date.now();
        const screenshotPath = path.join('screenshots', `login-page-screenshot-${timestamp}.png`);
        
        await page.screenshot({
            path: screenshotPath,
            fullPage: true
        });
        
        console.log(`Screenshot saved to: ${screenshotPath}`);
        
        // Check login form specific elements
        const loginFormInfo = await page.evaluate(() => {
            const form = document.querySelector('form');
            const emailInput = document.querySelector('input[type="email"], input[id*="email" i]');
            const passwordInput = document.querySelector('input[type="password"]');
            const submitButton = document.querySelector('button[type="submit"], input[type="submit"]');
            
            return {
                hasForm: !!form,
                hasEmailInput: !!emailInput,
                hasPasswordInput: !!passwordInput,
                hasSubmitButton: !!submitButton,
                formClasses: form?.className || '',
                submitButtonText: submitButton?.textContent?.trim() || ''
            };
        });
        
        console.log('Login form analysis:');
        console.log(JSON.stringify(loginFormInfo, null, 2));
        
        expect(loginFormInfo.hasForm).toBeTruthy();
        expect(loginFormInfo.hasEmailInput).toBeTruthy();
        expect(loginFormInfo.hasPasswordInput).toBeTruthy();
    });

    test('capture event form datetime picker screenshot', async ({ page }) => {
        console.log('Starting event form datetime picker screenshot capture...');
        
        // Login first (if needed)
        const ADMIN_EMAIL = 'admin@witchcityrope.com';
        const ADMIN_PASSWORD = 'Test123!';
        
        await page.goto(`${BASE_URL}/login`);
        await page.waitForTimeout(2000);
        
        const emailInput = await page.$('#Input_Email');
        if (emailInput) {
            await page.fill('#Input_Email', ADMIN_EMAIL);
            await page.fill('#Input_Password', ADMIN_PASSWORD);
            await page.click('.sign-in-btn');
            
            try {
                await page.waitForNavigation({ waitUntil: 'networkidle', timeout: 5000 });
            } catch (error) {
                // Continue if already navigated
            }
        }
        
        // Navigate to event creation form
        await page.goto(`${BASE_URL}/admin/events/new`, {
            waitUntil: 'networkidle',
            timeout: 30000
        });
        
        await page.waitForSelector('.event-editor-container', { state: 'visible', timeout: 10000 });
        
        // Focus on datetime inputs to potentially trigger any pickers
        const dateInputs = await page.$$('input[type="datetime-local"]');
        if (dateInputs.length > 0) {
            await dateInputs[0].click();
            await page.waitForTimeout(1000);
        }
        
        const timestamp = Date.now();
        const screenshotPath = path.join('screenshots', `event-form-datetime-${timestamp}.png`);
        
        await page.screenshot({
            path: screenshotPath,
            fullPage: true
        });
        
        console.log(`Screenshot saved to: ${screenshotPath}`);
        
        // Analyze datetime inputs
        const datetimeInfo = await page.evaluate(() => {
            const inputs = document.querySelectorAll('input[type="datetime-local"]');
            return {
                datetimeInputCount: inputs.length,
                datetimeInputs: Array.from(inputs).map(input => ({
                    id: input.id,
                    name: input.getAttribute('name'),
                    placeholder: input.getAttribute('placeholder'),
                    value: (input as HTMLInputElement).value,
                    min: input.getAttribute('min'),
                    max: input.getAttribute('max')
                }))
            };
        });
        
        console.log('Datetime input analysis:');
        console.log(JSON.stringify(datetimeInfo, null, 2));
        
        expect(datetimeInfo.datetimeInputCount).toBeGreaterThan(0);
    });
});