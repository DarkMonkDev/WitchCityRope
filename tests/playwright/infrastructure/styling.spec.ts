import { test, expect } from '@playwright/test';

test.describe('Styling Tests', () => {
    test('should verify CSS variables are loaded on login page', async ({ page }) => {
        await page.goto('/login');
        await page.waitForLoadState('networkidle');
        
        // Check if CSS variables are available
        const cssVariables = await page.evaluate(() => {
            const style = getComputedStyle(document.body);
            return {
                burgundy: style.getPropertyValue('--wcr-color-burgundy'),
                midnight: style.getPropertyValue('--wcr-color-midnight'),
                amber: style.getPropertyValue('--wcr-color-amber'),
                ivory: style.getPropertyValue('--wcr-color-ivory')
            };
        });
        
        console.log('🎨 CSS Variables found:', cssVariables);
        
        // Verify CSS variables are defined
        expect(cssVariables.burgundy).toBeTruthy();
        expect(cssVariables.midnight).toBeTruthy();
        expect(cssVariables.amber).toBeTruthy();
        
        // Take screenshot for visual verification
        await page.screenshot({ path: 'test-results/login-styling-test.png', fullPage: true });
    });

    test('should verify login card styling', async ({ page }) => {
        await page.goto('/login');
        await page.waitForLoadState('networkidle');
        
        // Check for login card styling
        const cardStyles = await page.evaluate(() => {
            const card = document.querySelector('.login-card');
            if (!card) return null;
            
            const styles = getComputedStyle(card);
            return {
                exists: true,
                background: styles.backgroundColor,
                borderRadius: styles.borderRadius,
                boxShadow: styles.boxShadow,
                padding: styles.padding,
                display: styles.display
            };
        });
        
        console.log('🎨 Login Card Styles:', cardStyles);
        
        // Verify card exists and has styling
        expect(cardStyles).not.toBeNull();
        expect(cardStyles?.exists).toBeTruthy();
        expect(cardStyles?.display).not.toBe('none');
        
        // Check for specific style properties
        if (cardStyles?.borderRadius) {
            expect(parseFloat(cardStyles.borderRadius)).toBeGreaterThan(0);
        }
    });

    test('should verify button styling across pages', async ({ page }) => {
        const pagesToTest = [
            { path: '/login', selector: '.sign-in-btn, button[type="submit"]' },
            { path: '/register', selector: '.btn-primary, button[type="submit"]' },
            { path: '/events', selector: '.btn, button' }
        ];
        
        for (const testPage of pagesToTest) {
            await page.goto(testPage.path);
            await page.waitForLoadState('networkidle');
            
            const buttonStyles = await page.evaluate((selector) => {
                const button = document.querySelector(selector);
                if (!button) return null;
                
                const styles = getComputedStyle(button);
                return {
                    backgroundColor: styles.backgroundColor,
                    color: styles.color,
                    borderRadius: styles.borderRadius,
                    padding: styles.padding,
                    cursor: styles.cursor,
                    transition: styles.transition
                };
            }, testPage.selector);
            
            console.log(`\nButton styles on ${testPage.path}:`, buttonStyles);
            
            if (buttonStyles) {
                // Verify button has proper styling
                expect(buttonStyles.cursor).toBe('pointer');
                expect(buttonStyles.backgroundColor).not.toBe('rgba(0, 0, 0, 0)'); // Not transparent
            }
        }
    });

    test('should verify theme consistency', async ({ page }) => {
        // Navigate to multiple pages and check theme consistency
        const pages = ['/', '/events', '/login', '/register'];
        const themeData: Record<string, any> = {};
        
        for (const pagePath of pages) {
            await page.goto(pagePath);
            await page.waitForLoadState('networkidle');
            
            const pageTheme = await page.evaluate(() => {
                const root = document.documentElement;
                const body = document.body;
                const rootStyles = getComputedStyle(root);
                const bodyStyles = getComputedStyle(body);
                
                return {
                    backgroundColor: bodyStyles.backgroundColor,
                    textColor: bodyStyles.color,
                    fontFamily: bodyStyles.fontFamily,
                    fontSize: bodyStyles.fontSize,
                    primaryColor: rootStyles.getPropertyValue('--wcr-color-burgundy'),
                    secondaryColor: rootStyles.getPropertyValue('--wcr-color-midnight')
                };
            });
            
            themeData[pagePath] = pageTheme;
        }
        
        console.log('Theme data across pages:', themeData);
        
        // Verify theme consistency
        const primaryColors = Object.values(themeData).map(t => t.primaryColor);
        const uniquePrimaryColors = [...new Set(primaryColors)];
        
        expect(uniquePrimaryColors.length).toBe(1); // All pages should have same primary color
        
        const fontFamilies = Object.values(themeData).map(t => t.fontFamily);
        const uniqueFontFamilies = [...new Set(fontFamilies)];
        
        expect(uniqueFontFamilies.length).toBe(1); // Consistent font family
    });

    test('should verify responsive styling changes', async ({ page }) => {
        await page.goto('/');
        await page.waitForLoadState('networkidle');
        
        // Test desktop view
        await page.setViewportSize({ width: 1920, height: 1080 });
        const desktopStyles = await page.evaluate(() => {
            const nav = document.querySelector('nav, .navbar');
            const container = document.querySelector('.container, .content-wrapper');
            
            return {
                navDisplay: nav ? getComputedStyle(nav).display : null,
                containerWidth: container ? getComputedStyle(container).maxWidth : null
            };
        });
        
        // Test tablet view
        await page.setViewportSize({ width: 768, height: 1024 });
        await page.waitForTimeout(500); // Wait for any CSS transitions
        
        const tabletStyles = await page.evaluate(() => {
            const nav = document.querySelector('nav, .navbar');
            const container = document.querySelector('.container, .content-wrapper');
            
            return {
                navDisplay: nav ? getComputedStyle(nav).display : null,
                containerWidth: container ? getComputedStyle(container).maxWidth : null
            };
        });
        
        // Test mobile view
        await page.setViewportSize({ width: 375, height: 667 });
        await page.waitForTimeout(500);
        
        const mobileStyles = await page.evaluate(() => {
            const nav = document.querySelector('nav, .navbar');
            const container = document.querySelector('.container, .content-wrapper');
            const mobileMenu = document.querySelector('.mobile-menu-toggle, .navbar-toggler');
            
            return {
                navDisplay: nav ? getComputedStyle(nav).display : null,
                containerWidth: container ? getComputedStyle(container).maxWidth : null,
                hasMobileMenu: !!mobileMenu
            };
        });
        
        console.log('Desktop styles:', desktopStyles);
        console.log('Tablet styles:', tabletStyles);
        console.log('Mobile styles:', mobileStyles);
        
        // Verify responsive changes
        expect(mobileStyles.hasMobileMenu).toBeTruthy(); // Mobile menu should appear
    });

    test('should check for missing styles or broken CSS', async ({ page }) => {
        const brokenStyles: string[] = [];
        
        // Monitor for CSS errors
        page.on('console', msg => {
            if (msg.type() === 'error' && msg.text().includes('CSS')) {
                brokenStyles.push(msg.text());
            }
        });
        
        // Monitor network for failed CSS requests
        page.on('response', response => {
            if (response.url().includes('.css') && response.status() !== 200) {
                brokenStyles.push(`Failed to load CSS: ${response.url()} (${response.status()})`);
            }
        });
        
        await page.goto('/');
        await page.waitForLoadState('networkidle');
        
        // Check for elements that might indicate broken styling
        const styleIssues = await page.evaluate(() => {
            const issues: string[] = [];
            
            // Check for unstyled elements
            const allElements = document.querySelectorAll('*');
            allElements.forEach(element => {
                const styles = getComputedStyle(element);
                
                // Check for browser default fonts (might indicate CSS not loaded)
                if (styles.fontFamily === 'Times New Roman' || styles.fontFamily === 'serif') {
                    issues.push(`Possible unstyled element: ${element.tagName}.${element.className}`);
                }
            });
            
            // Check for missing background images
            const elementsWithBgImage = document.querySelectorAll('[style*="background-image"]');
            elementsWithBgImage.forEach(element => {
                const bgImage = getComputedStyle(element).backgroundImage;
                if (bgImage === 'none' || bgImage === '') {
                    issues.push(`Missing background image on: ${element.tagName}.${element.className}`);
                }
            });
            
            return issues;
        });
        
        if (brokenStyles.length > 0) {
            console.log('Broken styles detected:', brokenStyles);
        }
        
        if (styleIssues.length > 0) {
            console.log('Style issues found:', styleIssues);
        }
        
        // Verify no critical style issues
        expect(brokenStyles.length).toBe(0);
    });
});