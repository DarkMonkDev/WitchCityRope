import { test, expect, Page } from '@playwright/test';
import { login } from '../helpers/auth.helpers';

test.describe('User Dropdown Menu', () => {
    let page: Page;

    test.beforeEach(async ({ page: testPage }) => {
        page = testPage;
        
        // Set up console monitoring
        page.on('console', msg => {
            if (msg.type() === 'error') {
                console.log(`[CONSOLE ERROR] ${msg.text()}`);
            }
        });
        
        page.on('pageerror', error => {
            console.error('Page Error:', error.message);
        });
    });

    test('should display user menu after login', async () => {
        // Login as admin
        await login(page, 'admin@witchcityrope.com', 'Test123!');
        
        // Wait for potential navigation
        await page.waitForTimeout(3000);
        
        // Check if user menu button exists
        const userMenuButton = await page.locator('.user-menu-btn').first();
        const userMenuExists = await userMenuButton.count() > 0;
        
        if (!userMenuExists) {
            // Try navigating to member dashboard
            await page.goto('/member/dashboard');
            await page.waitForTimeout(3000);
        }
        
        // Re-check for user menu
        await expect(page.locator('.user-menu-btn')).toBeVisible({ timeout: 10000 });
        
        // Take screenshot for debugging
        await page.screenshot({ path: 'test-results/user-dropdown-after-login.png', fullPage: true });
    });

    test('should toggle dropdown menu on click', async () => {
        // Login first
        await login(page, 'admin@witchcityrope.com', 'Test123!');
        await page.waitForTimeout(3000);
        
        // Ensure we're on a page with the user menu
        const userMenuButton = await page.locator('.user-menu-btn').first();
        if (await userMenuButton.count() === 0) {
            await page.goto('/member/dashboard');
            await page.waitForTimeout(3000);
        }
        
        // Click user menu button
        await page.locator('.user-menu-btn').first().click();
        await page.waitForTimeout(500); // Wait for animation
        
        // Check if dropdown is visible
        const dropdown = page.locator('.user-dropdown');
        await expect(dropdown).toHaveClass(/show/, { timeout: 5000 });
        
        // Take screenshot of open dropdown
        await page.screenshot({ path: 'test-results/user-dropdown-open.png', fullPage: true });
        
        // Click outside to close
        await page.click('body', { position: { x: 100, y: 100 } });
        await page.waitForTimeout(500);
        
        // Check if dropdown is closed
        await expect(dropdown).not.toHaveClass(/show/);
    });

    test('should work on mobile view', async () => {
        // Set mobile viewport
        await page.setViewportSize({ width: 375, height: 667 });
        
        // Login
        await login(page, 'admin@witchcityrope.com', 'Test123!');
        await page.waitForTimeout(3000);
        
        // Check for mobile menu toggle
        const mobileMenuToggle = page.locator('.mobile-menu-toggle');
        const hasMobileMenu = await mobileMenuToggle.count() > 0;
        
        if (hasMobileMenu) {
            await mobileMenuToggle.click();
            await page.waitForTimeout(500);
            
            // Check if mobile menu is active
            const mobileMenu = page.locator('.mobile-menu');
            await expect(mobileMenu).toHaveClass(/active/);
            
            // Take screenshot
            await page.screenshot({ path: 'test-results/user-dropdown-mobile.png', fullPage: true });
        }
    });

    test('should handle Blazor interactivity', async () => {
        // Login
        await login(page, 'admin@witchcityrope.com', 'Test123!');
        await page.waitForTimeout(3000);
        
        // Navigate to a page with user menu
        if (await page.locator('.user-menu-btn').count() === 0) {
            await page.goto('/member/dashboard');
            await page.waitForTimeout(3000);
        }
        
        // Check Blazor state
        const blazorState = await page.evaluate(() => {
            return {
                hasBlazor: typeof (window as any).Blazor !== 'undefined',
                hasDotNet: typeof (window as any).DotNet !== 'undefined',
                blazorInitialized: !!(window as any).Blazor?._internal
            };
        });
        
        console.log('Blazor State:', blazorState);
        
        // Try JavaScript click if normal click doesn't work
        const userMenuBtn = page.locator('.user-menu-btn').first();
        await userMenuBtn.click();
        await page.waitForTimeout(500);
        
        const dropdown = page.locator('.user-dropdown');
        const isVisible = await dropdown.evaluate(el => el.classList.contains('show'));
        
        if (!isVisible && blazorState.hasBlazor) {
            console.log('Trying JavaScript click...');
            await page.evaluate(() => {
                const btn = document.querySelector('.user-menu-btn') as HTMLElement;
                if (btn) btn.click();
            });
            await page.waitForTimeout(500);
        }
        
        // Verify final state
        const finalState = await dropdown.evaluate(el => ({
            visible: el.classList.contains('show'),
            classes: el.className
        }));
        
        console.log('Final dropdown state:', finalState);
    });

    test('should show admin dashboard link for admin users', async () => {
        // Login as admin
        await login(page, 'admin@witchcityrope.com', 'Test123!');
        await page.waitForTimeout(3000);
        
        // Navigate to a page with user menu if needed
        if (await page.locator('.user-menu-btn').count() === 0) {
            await page.goto('/member/dashboard');
            await page.waitForTimeout(3000);
        }
        
        // Click user menu button to open dropdown
        const userMenuBtn = page.locator('.user-menu-btn').first();
        await expect(userMenuBtn).toBeVisible({ timeout: 10000 });
        await userMenuBtn.click();
        await page.waitForTimeout(500);
        
        // Verify dropdown is open
        const dropdown = page.locator('.user-dropdown');
        await expect(dropdown).toHaveClass(/show/, { timeout: 5000 });
        
        // Check for admin dashboard link
        const adminDashboardLink = dropdown.locator('a[href="/admin"]');
        await expect(adminDashboardLink).toBeVisible({ timeout: 5000 });
        
        // Verify link text and icon
        const linkText = await adminDashboardLink.textContent();
        expect(linkText).toContain('Admin Dashboard');
        
        // Verify the shield icon is present
        const shieldIcon = adminDashboardLink.locator('i.fa-shield-alt');
        await expect(shieldIcon).toBeVisible();
        
        // Take screenshot showing admin link
        await page.screenshot({ path: 'test-results/user-dropdown-admin-link.png', fullPage: true });
        
        // Test clicking the admin link
        await adminDashboardLink.click();
        await page.waitForLoadState('networkidle');
        
        // Verify navigation to admin dashboard
        await expect(page).toHaveURL(/\/admin/);
        console.log('✅ Successfully navigated to admin dashboard via dropdown link');
    });

    test('should NOT show admin dashboard link for non-admin users', async () => {
        // Login as regular member
        await login(page, 'member@witchcityrope.com', 'Test123!');
        await page.waitForTimeout(3000);
        
        // Navigate to a page with user menu if needed
        if (await page.locator('.user-menu-btn').count() === 0) {
            await page.goto('/member/dashboard');
            await page.waitForTimeout(3000);
        }
        
        // Click user menu button to open dropdown
        const userMenuBtn = page.locator('.user-menu-btn').first();
        await expect(userMenuBtn).toBeVisible({ timeout: 10000 });
        await userMenuBtn.click();
        await page.waitForTimeout(500);
        
        // Verify dropdown is open
        const dropdown = page.locator('.user-dropdown');
        await expect(dropdown).toHaveClass(/show/, { timeout: 5000 });
        
        // Check that admin dashboard link is NOT present
        const adminDashboardLink = dropdown.locator('a[href="/admin"]');
        await expect(adminDashboardLink).not.toBeVisible();
        
        // Verify other standard links are present
        const myDashboardLink = dropdown.locator('a[href="/member/dashboard"]');
        const profileLink = dropdown.locator('a[href="/profile"]');
        const logoutLink = dropdown.locator('a[href="/logout"]');
        
        await expect(myDashboardLink).toBeVisible();
        await expect(profileLink).toBeVisible();
        await expect(logoutLink).toBeVisible();
        
        console.log('✅ Admin dashboard link correctly hidden for non-admin user');
    });
});

test.describe('User Dropdown Diagnostics', () => {
    test('should diagnose dropdown issues', async ({ page }) => {
        // Login
        await login(page, 'admin@witchcityrope.com', 'Test123!');
        await page.waitForTimeout(3000);
        
        // Collect diagnostic information
        const diagnostics = await page.evaluate(() => {
            const userMenu = document.querySelector('.user-menu');
            const userMenuBtn = document.querySelector('.user-menu-btn');
            const dropdown = document.querySelector('.user-dropdown');
            
            return {
                userMenuExists: !!userMenu,
                userMenuBtnExists: !!userMenuBtn,
                dropdownExists: !!dropdown,
                userMenuHTML: userMenu ? userMenu.outerHTML.substring(0, 200) : null,
                buttonAttributes: userMenuBtn ? Array.from(userMenuBtn.attributes).map(attr => ({
                    name: attr.name,
                    value: attr.value
                })) : [],
                dropdownClasses: dropdown ? dropdown.className : null,
                blazorEventBindings: userMenuBtn ? (userMenuBtn as any)._blazorEvents : null
            };
        });
        
        console.log('User Dropdown Diagnostics:', JSON.stringify(diagnostics, null, 2));
        
        // Check for common issues
        const issues: string[] = [];
        
        if (!diagnostics.userMenuExists) {
            issues.push('USER_MENU_NOT_FOUND: User menu component not rendering');
        }
        
        if (diagnostics.userMenuBtnExists && !diagnostics.blazorEventBindings) {
            issues.push('NO_BLAZOR_BINDINGS: Button exists but no Blazor event handlers attached');
        }
        
        if (diagnostics.dropdownExists && !diagnostics.dropdownClasses?.includes('show')) {
            issues.push('DROPDOWN_HIDDEN: Dropdown exists but not visible');
        }
        
        if (issues.length > 0) {
            console.log('Issues found:', issues);
        } else {
            console.log('No issues detected');
        }
        
        // Take diagnostic screenshot
        await page.screenshot({ path: 'test-results/user-dropdown-diagnostics.png', fullPage: true });
    });
});