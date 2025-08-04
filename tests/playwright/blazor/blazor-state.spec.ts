import { test, expect, Page } from '@playwright/test';
import { login } from '../helpers/auth.helpers';

test.describe('Blazor State and Event Binding', () => {
    test('should verify Blazor initialization', async ({ page }) => {
        // Set up console monitoring
        page.on('console', msg => {
            console.log(`[${msg.type().toUpperCase()}] ${msg.text()}`);
        });
        
        page.on('pageerror', error => {
            console.log('[PAGE ERROR]', error.message);
        });
        
        // Login first
        await login(page, 'member@witchcityrope.com', 'Test123!');
        
        // Navigate to an event page
        await page.goto('/events/5f3f685a-1b07-4468-a927-e26292468312');
        await page.waitForLoadState('networkidle');
        
        // Wait for Blazor to initialize
        await page.waitForFunction(() => {
            const blazor = (window as any).Blazor;
            return blazor && blazor._internal;
        }, { timeout: 10000 });
        
        console.log('✅ Blazor initialized');
        
        // Check Blazor circuit state
        const blazorState = await page.evaluate(() => {
            const blazor = (window as any).Blazor;
            const circuit = blazor._internal?.circuitManager?.circuitsByUri;
            const hasCircuit = circuit && Object.keys(circuit).length > 0;
            
            // Check WebSocket
            const sockets = performance.getEntriesByType('resource')
                .filter(r => r.name.includes('_blazor'));
            
            return {
                blazorExists: !!blazor,
                hasInternal: !!blazor._internal,
                hasCircuit: hasCircuit,
                circuitCount: circuit ? Object.keys(circuit).length : 0,
                hasNavigationManager: !!blazor._internal?.navigationManager,
                webSocketEntries: sockets.length
            };
        });
        
        console.log('Blazor State:', JSON.stringify(blazorState, null, 2));
        
        // Verify Blazor is properly initialized
        expect(blazorState.blazorExists).toBeTruthy();
        expect(blazorState.hasInternal).toBeTruthy();
        expect(blazorState.hasCircuit).toBeTruthy();
    });

    test('should check RSVP button Blazor bindings', async ({ page }) => {
        // Login and navigate to event
        await login(page, 'member@witchcityrope.com', 'Test123!');
        await page.goto('/events/5f3f685a-1b07-4468-a927-e26292468312');
        await page.waitForLoadState('networkidle');
        
        // Wait for Blazor
        await page.waitForFunction(() => (window as any).Blazor?._internal, { timeout: 10000 });
        
        // Check button state and bindings
        const buttonState = await page.evaluate(() => {
            const buttons = Array.from(document.querySelectorAll('button'));
            const rsvpButton = buttons.find(b => b.textContent?.includes('RSVP') && !b.textContent?.includes('You'));
            
            if (!rsvpButton) return { found: false };
            
            // Check for Blazor event listeners
            const hasBlazorClickListener = (rsvpButton as any)._blazorEvents?.click !== undefined;
            
            // Check attributes
            const attributes: Record<string, string> = {};
            Array.from(rsvpButton.attributes).forEach(attr => {
                attributes[attr.name] = attr.value;
            });
            
            return {
                found: true,
                text: rsvpButton.textContent?.trim(),
                hasBlazorClickListener,
                hasOnclick: rsvpButton.onclick !== null,
                attributes,
                className: rsvpButton.className,
                disabled: rsvpButton.disabled,
                type: rsvpButton.type
            };
        });
        
        console.log('Button State:', JSON.stringify(buttonState, null, 2));
        
        if (buttonState.found) {
            // Try clicking the button
            const rsvpButton = page.locator('button').filter({ hasText: /^RSVP$/ }).first();
            await rsvpButton.click();
            await page.waitForTimeout(3000);
            
            // Check for any changes
            const afterClickState = await page.evaluate(() => {
                const notices = Array.from(document.querySelectorAll('.registered-notice')).map(n => n.textContent);
                const toasts = Array.from(document.querySelectorAll('.sf-toast, .e-toast')).map(t => t.textContent);
                const errors = Array.from(document.querySelectorAll('.alert-danger, .error')).map(e => e.textContent);
                
                return { notices, toasts, errors };
            });
            
            console.log('After Click State:', afterClickState);
        }
        
        // Take screenshot
        await page.screenshot({ path: 'test-results/blazor-state-test.png' });
    });

    test('should test Blazor component lifecycle', async ({ page }) => {
        // Navigate directly to a Blazor component page
        await page.goto('/Identity/Account/Login');
        await page.waitForLoadState('networkidle');
        
        // Check for Blazor component markers
        const componentInfo = await page.evaluate(() => {
            // Look for Blazor component attributes
            const blazorComponents = document.querySelectorAll('[_bl_component]');
            const componentIds = Array.from(blazorComponents).map(elem => 
                elem.getAttribute('_bl_component')
            );
            
            // Check for enhanced elements
            const enhancedElements = document.querySelectorAll('[data-enhanced]');
            
            return {
                blazorComponentCount: blazorComponents.length,
                componentIds,
                enhancedElementCount: enhancedElements.length,
                hasBlazorMarkers: blazorComponents.length > 0 || enhancedElements.length > 0
            };
        });
        
        console.log('Component Info:', componentInfo);
        
        // Verify Blazor components are present
        expect(componentInfo.hasBlazorMarkers).toBeTruthy();
    });

    test('should test Blazor interactivity modes', async ({ page }) => {
        await login(page, 'admin@witchcityrope.com', 'Test123!');
        await page.goto('/admin/events');
        await page.waitForLoadState('networkidle');
        
        // Check for different render modes
        const renderModeInfo = await page.evaluate(() => {
            const components = document.querySelectorAll('[rendermode], [data-rendermode]');
            const modes = Array.from(components).map(comp => ({
                tagName: comp.tagName,
                renderMode: comp.getAttribute('rendermode') || comp.getAttribute('data-rendermode'),
                hasBlazorComponent: comp.hasAttribute('_bl_component')
            }));
            
            // Check for SSR markers
            const ssrMarkers = document.querySelectorAll('blazor-ssr');
            const interactiveMarkers = document.querySelectorAll('[blazor\\:elementreference]');
            
            return {
                componentsWithRenderMode: modes,
                ssrMarkerCount: ssrMarkers.length,
                interactiveMarkerCount: interactiveMarkers.length
            };
        });
        
        console.log('Render Mode Info:', JSON.stringify(renderModeInfo, null, 2));
        
        // Log findings
        if (renderModeInfo.ssrMarkerCount > 0 && renderModeInfo.interactiveMarkerCount === 0) {
            console.log('Components appear to be using Static SSR - no interactivity');
        } else if (renderModeInfo.interactiveMarkerCount > 0) {
            console.log('Interactive components detected');
        }
    });

    test('should test Blazor event propagation', async ({ page }) => {
        await login(page, 'admin@witchcityrope.com', 'Test123!');
        await page.goto('/admin/events/new-standardized');
        await page.waitForLoadState('networkidle');
        
        // Set up event monitoring
        await page.evaluate(() => {
            let clickCount = 0;
            document.addEventListener('click', (e) => {
                const target = e.target as HTMLElement;
                if (target.tagName === 'BUTTON' || target.tagName === 'INPUT') {
                    clickCount++;
                    console.log(`Click captured on ${target.tagName}: ${target.textContent || target.id}`);
                }
            }, true);
            
            (window as any).clickCount = () => clickCount;
        });
        
        // Try clicking a form input
        const firstInput = page.locator('input[type="text"]').first();
        if (await firstInput.count() > 0) {
            await firstInput.click();
            await firstInput.fill('Test Input');
            
            const clicks = await page.evaluate(() => (window as any).clickCount());
            console.log(`Total clicks captured: ${clicks}`);
        }
        
        // Try clicking a button
        const button = page.locator('button').first();
        if (await button.count() > 0) {
            await button.click();
            
            const clicks = await page.evaluate(() => (window as any).clickCount());
            console.log(`Total clicks after button: ${clicks}`);
        }
    });

    test('should diagnose Blazor connection issues', async ({ page }) => {
        // Set up network monitoring
        const failedRequests: string[] = [];
        page.on('requestfailed', request => {
            failedRequests.push(`${request.method()} ${request.url()}`);
        });
        
        await page.goto('/');
        await page.waitForLoadState('networkidle');
        
        // Check for Blazor errors
        const blazorDiagnostics = await page.evaluate(() => {
            const blazor = (window as any).Blazor;
            const errors: string[] = [];
            
            // Check for error UI
            const errorUI = document.querySelector('.blazor-error-ui');
            if (errorUI) {
                errors.push('Blazor error UI is visible');
            }
            
            // Check console for Blazor errors
            const consoleErrors = (window as any).__blazorConsoleErrors || [];
            
            // Check for disconnection
            const isDisconnected = blazor?.defaultReconnectionHandler?._disconnected;
            
            return {
                hasBlazor: !!blazor,
                errorUIVisible: !!errorUI,
                isDisconnected,
                errors,
                consoleErrors
            };
        });
        
        console.log('Blazor Diagnostics:', blazorDiagnostics);
        console.log('Failed network requests:', failedRequests);
        
        // Assert no critical errors
        expect(blazorDiagnostics.errorUIVisible).toBeFalsy();
        expect(blazorDiagnostics.isDisconnected).not.toBeTruthy();
    });
});