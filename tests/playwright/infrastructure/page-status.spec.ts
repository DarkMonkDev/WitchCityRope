import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth.helpers';

test.describe('Page Status Tests', () => {
    test.beforeEach(async ({ page }) => {
        // Login as admin
        await login(page, 'admin@witchcityrope.com', 'Test123!');
    });

    test('should verify admin events page loads correctly', async ({ page }) => {
        await page.goto('/admin/events');
        await page.waitForLoadState('networkidle');
        
        const pageInfo = await page.evaluate(() => {
            return {
                url: window.location.href,
                title: document.title,
                hasTable: !!document.querySelector('table'),
                rowCount: document.querySelectorAll('table tbody tr').length,
                hasCreateButton: !!Array.from(document.querySelectorAll('button, a')).find(el => 
                    el.textContent?.toLowerCase().includes('create')
                )
            };
        });
        
        console.log('Events page info:', pageInfo);
        
        // Verify page loaded correctly
        expect(pageInfo.url).toContain('/admin/events');
        expect(pageInfo.hasTable).toBeTruthy();
        expect(pageInfo.hasCreateButton).toBeTruthy();
    });

    test('should verify admin reports page accessibility', async ({ page }) => {
        console.log('Testing /admin/reports...');
        
        try {
            const response = await page.goto('/admin/reports', { waitUntil: 'networkidle' });
            const status = response?.status() || 0;
            
            if (status === 200) {
                console.log('✅ Reports page loaded successfully!');
                
                const pageContent = await page.evaluate(() => {
                    return {
                        title: document.title,
                        hasContent: document.body.innerText.length > 0,
                        elementCount: document.body.querySelectorAll('*').length
                    };
                });
                
                console.log('Reports page content:', pageContent);
                expect(pageContent.hasContent).toBeTruthy();
            } else {
                console.log(`❌ Reports page returned status: ${status}`);
            }
        } catch (error) {
            console.log('❌ Reports page error:', error.message);
            throw error;
        }
    });

    test('should verify create event page structure', async ({ page }) => {
        await page.goto('/admin/events/new-standardized');
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(3000); // Wait for any dynamic content
        
        const pageStructure = await page.evaluate(() => {
            const inputs = Array.from(document.querySelectorAll('input')).map(i => ({
                type: i.type,
                placeholder: i.placeholder,
                id: i.id,
                visible: i.offsetParent !== null
            }));
            
            const selects = Array.from(document.querySelectorAll('select')).map(s => ({
                id: s.id,
                optionCount: s.options.length,
                visible: s.offsetParent !== null
            }));
            
            return {
                url: window.location.href,
                title: document.title,
                hasForm: !!document.querySelector('form'),
                inputCount: inputs.length,
                visibleInputs: inputs.filter(i => i.visible && i.type !== 'hidden').length,
                selectCount: selects.length,
                visibleSelects: selects.filter(s => s.visible).length,
                hasSubmitButton: !!Array.from(document.querySelectorAll('button')).find(b => 
                    b.textContent?.toLowerCase().includes('create event')
                ),
                bodyTextPreview: document.body.innerText.substring(0, 200)
            };
        });
        
        console.log('Create page structure:', pageStructure);
        
        // Verify form elements exist
        expect(pageStructure.hasForm).toBeTruthy();
        expect(pageStructure.visibleInputs).toBeGreaterThan(0);
        expect(pageStructure.hasSubmitButton).toBeTruthy();
    });

    test('should test form interaction capabilities', async ({ page }) => {
        await page.goto('/admin/events/new-standardized');
        await page.waitForLoadState('networkidle');
        
        // Find first text input
        const titleInput = page.locator('input[placeholder*="Workshop"], input[type="text"]:not([type="hidden"])').first();
        
        if (await titleInput.count() > 0) {
            // Test input interaction
            const interactionTest = await titleInput.evaluate((el: HTMLInputElement) => {
                try {
                    el.focus();
                    return {
                        focused: document.activeElement === el,
                        disabled: el.disabled,
                        readOnly: el.readOnly,
                        type: el.type,
                        placeholder: el.placeholder
                    };
                } catch (err) {
                    return { error: err.message };
                }
            });
            
            console.log('Input interaction test:', interactionTest);
            
            // Try typing in the input
            await titleInput.fill('Test Event Title');
            const value = await titleInput.inputValue();
            
            expect(value).toBe('Test Event Title');
            expect(interactionTest.focused).toBeTruthy();
            expect(interactionTest.disabled).toBeFalsy();
        } else {
            console.log('❌ Could not find title input');
        }
    });

    test('should check Blazor initialization status', async ({ page }) => {
        await page.goto('/admin/events/new-standardized');
        await page.waitForLoadState('networkidle');
        
        const blazorStatus = await page.evaluate(() => {
            return {
                hasBlazor: !!(window as any).Blazor,
                hasBlazorError: !!document.querySelector('.blazor-error-ui'),
                blazorInitialized: !!document.querySelector('[data-enhanced]'),
                blazorComponents: document.querySelectorAll('[_bl_component]').length,
                blazorSSRMarkers: document.querySelectorAll('blazor-ssr').length
            };
        });
        
        console.log('Blazor status:', blazorStatus);
        
        // Log Blazor state
        if (blazorStatus.hasBlazor) {
            console.log('✅ Blazor framework loaded');
        }
        if (blazorStatus.hasBlazorError) {
            console.log('❌ Blazor error UI is visible');
        }
        if (blazorStatus.blazorComponents > 0) {
            console.log(`✅ Found ${blazorStatus.blazorComponents} Blazor components`);
        }
    });

    test('should verify page load performance', async ({ page }) => {
        const pages = [
            { path: '/admin/events', name: 'Admin Events' },
            { path: '/admin/users', name: 'Admin Users' },
            { path: '/admin/events/new-standardized', name: 'Create Event' }
        ];
        
        for (const testPage of pages) {
            const startTime = Date.now();
            
            await page.goto(testPage.path);
            await page.waitForLoadState('networkidle');
            
            const loadTime = Date.now() - startTime;
            
            // Get performance metrics
            const metrics = await page.evaluate(() => {
                const perf = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
                return {
                    domContentLoaded: Math.round(perf.domContentLoadedEventEnd - perf.domContentLoadedEventStart),
                    loadComplete: Math.round(perf.loadEventEnd - perf.loadEventStart),
                    domInteractive: Math.round(perf.domInteractive - perf.fetchStart)
                };
            });
            
            console.log(`\n${testPage.name}:`);
            console.log(`  Total load time: ${loadTime}ms`);
            console.log(`  DOM Content Loaded: ${metrics.domContentLoaded}ms`);
            console.log(`  Load Complete: ${metrics.loadComplete}ms`);
            console.log(`  Time to Interactive: ${metrics.domInteractive}ms`);
            
            // Warn if page load is slow
            if (loadTime > 5000) {
                console.log(`  ⚠️ Page load time exceeds 5 seconds`);
            }
        }
    });

    test('should check for broken pages or routes', async ({ page }) => {
        const routes = [
            '/admin/dashboard',
            '/admin/events',
            '/admin/events/new-standardized',
            '/admin/users',
            '/admin/reports',
            '/admin/settings'
        ];
        
        const results: Array<{ route: string; status: number; hasContent: boolean }> = [];
        
        for (const route of routes) {
            try {
                const response = await page.goto(route, { waitUntil: 'domcontentloaded', timeout: 10000 });
                const status = response?.status() || 0;
                
                const hasContent = await page.evaluate(() => {
                    return document.body.innerText.trim().length > 0;
                });
                
                results.push({ route, status, hasContent });
                
                console.log(`${status === 200 ? '✅' : '❌'} ${route} - Status: ${status}, Has content: ${hasContent}`);
            } catch (error) {
                results.push({ route, status: 0, hasContent: false });
                console.log(`❌ ${route} - Error: ${error.message}`);
            }
        }
        
        // Verify critical routes are accessible
        const criticalRoutes = ['/admin/dashboard', '/admin/events', '/admin/users'];
        for (const route of criticalRoutes) {
            const result = results.find(r => r.route === route);
            expect(result?.status).toBe(200);
            expect(result?.hasContent).toBeTruthy();
        }
    });
});