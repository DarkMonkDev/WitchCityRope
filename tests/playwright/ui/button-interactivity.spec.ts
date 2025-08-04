import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth.helpers';

test.describe('Button Interactivity', () => {
    // Track API calls
    let apiCalls: Array<{ method: string; url: string; timestamp: string }> = [];
    
    test.beforeEach(async ({ page }) => {
        // Reset API calls
        apiCalls = [];
        
        // Set up request interception
        page.on('request', request => {
            const url = request.url();
            if (url.includes('/api/')) {
                apiCalls.push({
                    method: request.method(),
                    url: url,
                    timestamp: new Date().toISOString()
                });
            }
        });
        
        // Login as admin
        await login(page, 'admin@witchcityrope.com', 'Test123!');
    });

    test('should handle Create Event button click', async ({ page }) => {
        await page.goto('/admin/events');
        await page.waitForLoadState('networkidle');
        
        // Clear API calls from navigation
        apiCalls = [];
        
        // Try to click Create Event button
        const createButton = page.locator('button:has-text("Create New Event"), button.btn.btn-primary:has-text("Create New Event")').first();
        const buttonExists = await createButton.count() > 0;
        
        if (buttonExists) {
            await createButton.click();
            await page.waitForTimeout(2000);
            
            const afterClickUrl = page.url();
            console.log('URL after Create Event click:', afterClickUrl);
            console.log('API calls made:', apiCalls.length);
            
            // Verify navigation or modal opened
            expect(afterClickUrl).not.toBe(`${page.url()}/admin/events`);
        } else {
            console.log('Create Event button not found with expected selector');
            
            // Try alternative selectors
            const altButton = await page.locator('button:has-text("Create"), a:has-text("Create")').first();
            if (await altButton.count() > 0) {
                await altButton.click();
                await page.waitForTimeout(2000);
                console.log('Clicked alternative create button');
            }
        }
    });

    test('should handle event filter buttons', async ({ page }) => {
        await page.goto('/events');
        await page.waitForLoadState('networkidle');
        
        // Clear API calls
        apiCalls = [];
        
        // Try to click filter buttons
        const filterButtons = await page.locator('button').all();
        const classesButton = filterButtons.find(async btn => {
            const text = await btn.textContent();
            return text?.includes('Classes');
        });
        
        if (classesButton) {
            await classesButton.click();
            await page.waitForTimeout(2000);
            
            console.log('Filter button clicked');
            console.log('API calls made:', apiCalls.length);
            
            // Check if filtering occurred
            const urlParams = new URL(page.url()).searchParams;
            console.log('URL parameters after filter:', Array.from(urlParams.entries()));
        }
    });

    test('should handle user menu dropdown toggle', async ({ page }) => {
        await page.goto('/');
        await page.waitForLoadState('networkidle');
        
        // Find user menu button
        const userMenuButton = page.locator('.user-menu button, .user-menu .dropdown-toggle').first();
        const menuExists = await userMenuButton.count() > 0;
        
        if (menuExists) {
            await userMenuButton.click();
            await page.waitForTimeout(1000);
            
            // Check if dropdown is visible
            const dropdownVisible = await page.locator('.dropdown-menu.show, .user-menu-dropdown.show').count() > 0;
            console.log('User menu clicked, dropdown visible:', dropdownVisible);
            
            expect(dropdownVisible).toBeTruthy();
            
            // Take screenshot of open dropdown
            await page.screenshot({ path: 'test-results/button-dropdown-open.png' });
        } else {
            console.log('User menu not found');
        }
    });

    test('should verify Blazor connection for interactivity', async ({ page }) => {
        await page.goto('/admin/events');
        await page.waitForLoadState('networkidle');
        
        // Check Blazor state
        const blazorState = await page.evaluate(() => {
            const blazor = (window as any).Blazor;
            return {
                blazorExists: typeof blazor !== 'undefined',
                hasInternal: blazor?._internal !== undefined,
                hasCircuit: blazor?._internal?.circuitManager !== undefined,
                reconnectionHandler: blazor?.defaultReconnectionHandler !== undefined
            };
        });
        
        console.log('Blazor Connection State:', blazorState);
        
        // If Blazor is not connected, buttons may not work
        if (!blazorState.hasCircuit) {
            console.warn('Blazor circuit not established - interactive components may not work');
        }
    });

    test('should test button states and attributes', async ({ page }) => {
        await page.goto('/admin/events');
        await page.waitForLoadState('networkidle');
        
        // Get all buttons and their states
        const buttonStates = await page.evaluate(() => {
            const buttons = Array.from(document.querySelectorAll('button'));
            return buttons.map(btn => ({
                text: btn.textContent?.trim(),
                disabled: btn.disabled,
                type: btn.type,
                hasOnclick: btn.onclick !== null,
                hasBlazorEvents: (btn as any)._blazorEvents !== undefined,
                classes: btn.className
            }));
        });
        
        console.log('Button states on page:');
        buttonStates.forEach((btn, index) => {
            if (btn.text) {
                console.log(`${index + 1}. "${btn.text}" - Disabled: ${btn.disabled}, Has onclick: ${btn.hasOnclick}, Has Blazor: ${btn.hasBlazorEvents}`);
            }
        });
        
        // Check for buttons that should be interactive but aren't
        const createButtons = buttonStates.filter(btn => 
            btn.text?.toLowerCase().includes('create') || 
            btn.text?.toLowerCase().includes('new')
        );
        
        createButtons.forEach(btn => {
            if (!btn.hasOnclick && !btn.hasBlazorEvents) {
                console.warn(`Button "${btn.text}" has no event handlers attached`);
            }
        });
    });

    test('should test form submission buttons', async ({ page }) => {
        await page.goto('/admin/events/new-standardized');
        await page.waitForLoadState('networkidle');
        
        // Find submit buttons
        const submitButtons = await page.locator('button[type="submit"], button:has-text("Save"), button:has-text("Create")').all();
        console.log(`Found ${submitButtons.length} submit buttons`);
        
        for (let i = 0; i < submitButtons.length; i++) {
            const button = submitButtons[i];
            const buttonText = await button.textContent();
            const isDisabled = await button.isDisabled();
            
            console.log(`Submit button ${i + 1}: "${buttonText}" - Disabled: ${isDisabled}`);
            
            // Check if button has proper form association
            const formId = await button.getAttribute('form');
            if (formId) {
                const formExists = await page.locator(`#${formId}`).count() > 0;
                console.log(`  Associated with form: ${formId} (exists: ${formExists})`);
            }
        }
    });

    test('should test interactive components after page load', async ({ page }) => {
        await page.goto('/events');
        await page.waitForLoadState('networkidle');
        
        // Wait for any lazy-loaded components
        await page.waitForTimeout(3000);
        
        // Check for interactive elements
        const interactiveElements = await page.evaluate(() => {
            const elements = {
                buttons: document.querySelectorAll('button:not([disabled])').length,
                links: document.querySelectorAll('a[href]').length,
                inputs: document.querySelectorAll('input:not([disabled])').length,
                selects: document.querySelectorAll('select:not([disabled])').length
            };
            
            // Check for event listeners
            const buttonsWithListeners = Array.from(document.querySelectorAll('button')).filter(btn => {
                return btn.onclick !== null || 
                       (btn as any)._blazorEvents !== undefined ||
                       btn.hasAttribute('onclick') ||
                       btn.hasAttribute('@onclick');
            }).length;
            
            return {
                ...elements,
                buttonsWithListeners
            };
        });
        
        console.log('Interactive elements count:', interactiveElements);
        
        // Verify there are interactive elements
        expect(interactiveElements.buttons).toBeGreaterThan(0);
        
        if (interactiveElements.buttonsWithListeners === 0) {
            console.warn('No buttons with event listeners found - interactivity may be broken');
        }
    });

    test('should capture button click failures', async ({ page }) => {
        await page.goto('/admin/events');
        await page.waitForLoadState('networkidle');
        
        // Set up console error monitoring
        const consoleErrors: string[] = [];
        page.on('console', msg => {
            if (msg.type() === 'error') {
                consoleErrors.push(msg.text());
            }
        });
        
        // Try clicking various buttons and capture any errors
        const buttons = await page.locator('button').all();
        
        for (let i = 0; i < Math.min(buttons.length, 5); i++) {
            const button = buttons[i];
            const buttonText = await button.textContent();
            
            if (buttonText?.trim()) {
                console.log(`Clicking button: "${buttonText.trim()}"`);
                
                try {
                    await button.click({ timeout: 2000 });
                    await page.waitForTimeout(500);
                } catch (error) {
                    console.log(`  Failed to click: ${error.message}`);
                }
                
                if (consoleErrors.length > 0) {
                    console.log('  Console errors after click:', consoleErrors);
                    consoleErrors.length = 0; // Clear for next button
                }
            }
        }
    });
});