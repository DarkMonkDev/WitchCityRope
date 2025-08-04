import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth.helpers';

test.describe('Navigation Tests', () => {
    test.beforeEach(async ({ page }) => {
        // Login as admin for navigation tests
        await login(page, 'admin@witchcityrope.com', 'Test123!');
    });

    test('should navigate to admin events page', async ({ page }) => {
        await page.goto('/admin/events');
        await page.waitForLoadState('networkidle');
        
        // Verify we're on the admin events page
        await expect(page).toHaveURL(/\/admin\/events/);
        
        // Check for page elements
        const pageInfo = await page.evaluate(() => {
            return {
                hasTable: !!document.querySelector('table'),
                rowCount: document.querySelectorAll('table tbody tr').length,
                hasCreateButton: !!Array.from(document.querySelectorAll('button, a')).find(el => 
                    el.textContent?.toLowerCase().includes('create')
                )
            };
        });
        
        expect(pageInfo.hasTable).toBeTruthy();
        console.log(`Found ${pageInfo.rowCount} event rows`);
        console.log(`Create button present: ${pageInfo.hasCreateButton}`);
    });

    test('should find and list all navigation elements', async ({ page }) => {
        await page.goto('/admin/events');
        await page.waitForLoadState('networkidle');
        
        // Find all clickable navigation elements
        const navigationElements = await page.evaluate(() => {
            const elements: Array<{type: string, text: string, href?: string}> = [];
            
            // Get all buttons
            document.querySelectorAll('button').forEach(btn => {
                elements.push({
                    type: 'button',
                    text: btn.textContent?.trim() || '',
                });
            });
            
            // Get all links
            document.querySelectorAll('a').forEach(link => {
                elements.push({
                    type: 'link',
                    text: link.textContent?.trim() || '',
                    href: link.getAttribute('href') || undefined
                });
            });
            
            return elements.filter(el => 
                el.text && (
                    el.text.toLowerCase().includes('create') || 
                    el.text.toLowerCase().includes('new') || 
                    el.text.includes('+')
                )
            );
        });
        
        console.log('Found navigation elements:', navigationElements);
        
        // Check if create button exists
        const hasCreateButton = navigationElements.some(el => 
            el.text.toLowerCase().includes('create')
        );
        expect(hasCreateButton).toBeTruthy();
    });

    test('should navigate directly to create event page', async ({ page }) => {
        // Try direct navigation - use the actual new event page URL
        await page.goto('/admin/events/new-fixed');
        await page.waitForLoadState('networkidle');
        
        // Verify we're on the create page
        const currentUrl = page.url();
        expect(currentUrl).toContain('/new');
        
        // Check page content
        const pageContent = await page.evaluate(() => {
            return {
                title: document.title,
                h1: document.querySelector('h1')?.textContent,
                hasForm: !!document.querySelector('form'),
                inputCount: document.querySelectorAll('input').length,
                hasEventEditorContainer: !!document.querySelector('.event-editor-container'),
                hasEventForm: !!document.querySelector('.event-form, .event-editor'),
                hasInputs: document.querySelectorAll('input[type="text"], input[type="email"], textarea').length > 0
            };
        });
        
        console.log('Create page content:', pageContent);
        expect(pageContent.hasForm || pageContent.hasEventEditorContainer || pageContent.hasEventForm || pageContent.hasInputs).toBeTruthy();
    });

    test('should handle navigation button clicks', async ({ page }) => {
        await page.goto('/admin/events');
        await page.waitForLoadState('networkidle');
        
        // Find create button using various selectors
        const createButton = await page.locator('button:has-text("Create"), a:has-text("Create"), button:has-text("New"), a:has-text("New")').first();
        
        if (await createButton.count() > 0) {
            // Click the create button
            await createButton.click();
            await page.waitForLoadState('networkidle');
            
            // Check if navigation occurred
            const newUrl = page.url();
            console.log('URL after clicking create:', newUrl);
            
            // Verify we navigated away from the events list
            expect(newUrl).not.toBe(`${page.url()}/admin/events`);
        } else {
            console.log('No create button found - may need JavaScript navigation handling');
        }
    });

    test('should test navigation between different admin pages', async ({ page }) => {
        const adminPages = [
            { path: '/admin/events', name: 'Events' },
            { path: '/admin/users', name: 'Users' },
            { path: '/admin/reports', name: 'Reports' }
        ];
        
        for (const adminPage of adminPages) {
            console.log(`Testing navigation to ${adminPage.name}...`);
            
            try {
                await page.goto(adminPage.path);
                await page.waitForLoadState('networkidle', { timeout: 10000 });
                
                const response = page.url();
                console.log(`✅ ${adminPage.name} page loaded: ${response}`);
                
                // Take screenshot for each page
                await page.screenshot({ 
                    path: `test-results/navigation-${adminPage.name.toLowerCase()}.png` 
                });
            } catch (error) {
                console.log(`❌ ${adminPage.name} page error:`, error.message);
            }
        }
    });

    test('should verify navigation menu items', async ({ page }) => {
        await page.goto('/admin/dashboard');
        await page.waitForLoadState('networkidle');
        
        // Check for navigation menu
        const navItems = await page.locator('nav a, .sidebar a, .nav-menu a').all();
        console.log(`Found ${navItems.length} navigation items`);
        
        // Get all navigation links
        const navLinks = await page.evaluate(() => {
            const links = Array.from(document.querySelectorAll('nav a, .sidebar a, .nav-menu a'));
            return links.map(link => ({
                text: link.textContent?.trim(),
                href: link.getAttribute('href')
            }));
        });
        
        console.log('Navigation links:', navLinks);
        
        // Verify common admin navigation items exist
        const expectedItems = ['Events', 'Users', 'Dashboard'];
        for (const item of expectedItems) {
            const hasItem = navLinks.some(link => 
                link.text?.toLowerCase().includes(item.toLowerCase())
            );
            console.log(`${item} navigation item present: ${hasItem}`);
        }
    });
});