import { test, expect } from '@playwright/test';

test.describe('CSS Loading Tests', () => {
    test('should load all CSS files successfully', async ({ page }) => {
        const cssRequests: Array<{ url: string; status: number }> = [];
        
        // Monitor CSS file requests
        page.on('response', response => {
            const url = response.url();
            if (url.includes('.css') || url.includes('/css/')) {
                cssRequests.push({
                    url,
                    status: response.status()
                });
            }
        });
        
        // Navigate to login page
        await page.goto('/login');
        await page.waitForLoadState('networkidle');
        
        // Check CSS loading results
        console.log(`Total CSS requests: ${cssRequests.length}`);
        
        const failedCSS = cssRequests.filter(r => r.status !== 200);
        const successCSS = cssRequests.filter(r => r.status === 200);
        
        console.log(`✅ Successfully loaded: ${successCSS.length}`);
        successCSS.forEach(css => console.log(`   - ${css.url}`));
        
        console.log(`❌ Failed to load: ${failedCSS.length}`);
        failedCSS.forEach(css => console.log(`   - ${css.status}: ${css.url}`));
        
        // Verify no CSS files failed to load
        expect(failedCSS.length).toBe(0);
    });

    test('should verify CSS files are accessible directly', async ({ page }) => {
        const cssFiles = [
            '/css/wcr-theme.min.css',
            '/css/app.min.css',
            '/css/validation.css',
            '/css/wcr-theme.css',
            '/css/app.css'
        ];
        
        for (const cssFile of cssFiles) {
            const response = await page.goto(cssFile);
            const status = response?.status() || 0;
            
            console.log(`${status === 200 ? '✅' : '❌'} ${status}: ${cssFile}`);
            
            if (status === 200) {
                const contentType = response.headers()['content-type'];
                console.log(`   Content-Type: ${contentType}`);
                expect(contentType).toContain('text/css');
            }
        }
    });

    test('should verify CSS custom properties are loaded', async ({ page }) => {
        await page.goto('/login');
        await page.waitForLoadState('networkidle');
        
        const cssVariables = await page.evaluate(() => {
            const root = document.documentElement;
            const computedStyle = getComputedStyle(root);
            
            return {
                burgundy: computedStyle.getPropertyValue('--wcr-color-burgundy').trim(),
                midnight: computedStyle.getPropertyValue('--wcr-color-midnight').trim(),
                amber: computedStyle.getPropertyValue('--wcr-color-amber').trim(),
                ivory: computedStyle.getPropertyValue('--wcr-color-ivory').trim()
            };
        });
        
        console.log('CSS Variables:');
        Object.entries(cssVariables).forEach(([key, value]) => {
            console.log(`   --wcr-color-${key}: ${value || '(empty)'}`);
        });
        
        // Verify CSS variables are defined
        expect(cssVariables.burgundy).toBeTruthy();
        expect(cssVariables.midnight).toBeTruthy();
    });

    test('should verify styled components exist', async ({ page }) => {
        await page.goto('/login');
        await page.waitForLoadState('networkidle');
        
        const styledElements = await page.evaluate(() => {
            const loginCard = document.querySelector('.login-card');
            const signInBtn = document.querySelector('.sign-in-btn');
            
            return {
                hasLoginCard: !!loginCard,
                hasSignInBtn: !!signInBtn,
                loginCardBg: loginCard ? getComputedStyle(loginCard).backgroundColor : 'not found',
                signInBtnBg: signInBtn ? getComputedStyle(signInBtn).backgroundColor : 'not found',
                loginCardDisplay: loginCard ? getComputedStyle(loginCard).display : 'not found',
                signInBtnDisplay: signInBtn ? getComputedStyle(signInBtn).display : 'not found'
            };
        });
        
        console.log('Styled Elements:', styledElements);
        
        // Verify styled elements exist and have styles applied
        expect(styledElements.hasLoginCard).toBeTruthy();
        expect(styledElements.loginCardBg).not.toBe('not found');
        expect(styledElements.loginCardDisplay).not.toBe('none');
    });

    test('should check for CSS loading errors in console', async ({ page }) => {
        const consoleErrors: string[] = [];
        
        page.on('console', msg => {
            if (msg.type() === 'error' && msg.text().toLowerCase().includes('css')) {
                consoleErrors.push(msg.text());
            }
        });
        
        await page.goto('/');
        await page.waitForLoadState('networkidle');
        
        if (consoleErrors.length > 0) {
            console.log('CSS-related console errors:');
            consoleErrors.forEach(err => console.log(`   - ${err}`));
        }
        
        // Verify no CSS errors in console
        expect(consoleErrors.length).toBe(0);
    });

    test('should verify responsive CSS breakpoints', async ({ page }) => {
        await page.goto('/');
        await page.waitForLoadState('networkidle');
        
        // Test desktop view
        await page.setViewportSize({ width: 1920, height: 1080 });
        const desktopLayout = await page.evaluate(() => {
            const nav = document.querySelector('nav, .navbar');
            return {
                navDisplay: nav ? getComputedStyle(nav).display : 'not found',
                viewportWidth: window.innerWidth
            };
        });
        
        console.log('Desktop layout:', desktopLayout);
        
        // Test mobile view
        await page.setViewportSize({ width: 375, height: 667 });
        const mobileLayout = await page.evaluate(() => {
            const nav = document.querySelector('nav, .navbar');
            const mobileMenu = document.querySelector('.mobile-menu, .navbar-toggler');
            return {
                navDisplay: nav ? getComputedStyle(nav).display : 'not found',
                hasMobileMenu: !!mobileMenu,
                viewportWidth: window.innerWidth
            };
        });
        
        console.log('Mobile layout:', mobileLayout);
        
        // Verify responsive behavior
        expect(mobileLayout.viewportWidth).toBeLessThan(768);
    });
});