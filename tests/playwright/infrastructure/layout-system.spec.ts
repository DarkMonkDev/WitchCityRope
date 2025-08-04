import { test, expect } from '@playwright/test';

test.describe('Layout System Tests', () => {
    test('should verify layout system works for regular Razor pages', async ({ page }) => {
        console.log('Testing simple Razor page with layout...');
        
        // Navigate to a test page (if it exists)
        const response = await page.goto('/test', { waitUntil: 'networkidle' });
        
        if (response?.status() === 404) {
            console.log('Test page not found, testing home page instead');
            await page.goto('/', { waitUntil: 'networkidle' });
        }
        
        const layoutAnalysis = await page.evaluate(() => {
            return {
                url: window.location.href,
                title: document.title,
                hasHead: !!document.head && document.head.children.length > 0,
                headChildren: document.head ? document.head.children.length : 0,
                hasStylesheets: document.querySelectorAll('link[rel="stylesheet"]').length,
                hasBlazorScripts: document.querySelectorAll('script[src*="blazor"]').length,
                hasSyncfusionScripts: document.querySelectorAll('script[src*="syncfusion"]').length,
                pageContent: document.body ? document.body.textContent?.substring(0, 200) : 'NO BODY',
                headHTMLPreview: document.head ? document.head.innerHTML.substring(0, 500) : 'NO HEAD'
            };
        });
        
        console.log('Page analysis:');
        console.log('   URL:', layoutAnalysis.url);
        console.log('   Title:', layoutAnalysis.title);
        console.log('   Has head:', layoutAnalysis.hasHead);
        console.log('   Head children:', layoutAnalysis.headChildren);
        console.log('   Stylesheets:', layoutAnalysis.hasStylesheets);
        console.log('   Blazor scripts:', layoutAnalysis.hasBlazorScripts);
        console.log('   Syncfusion scripts:', layoutAnalysis.hasSyncfusionScripts);
        console.log('   Content preview:', layoutAnalysis.pageContent?.substring(0, 100));
        
        // Verify layout is working
        expect(layoutAnalysis.hasHead).toBeTruthy();
        expect(layoutAnalysis.hasStylesheets).toBeGreaterThan(0);
        
        if (layoutAnalysis.hasHead && layoutAnalysis.hasStylesheets > 0) {
            console.log('✅ Layout system IS working for regular pages!');
        } else {
            console.log('❌ Layout system is NOT working');
        }
    });

    test('should compare layout between Razor and Blazor pages', async ({ page }) => {
        // Test a regular page first
        await page.goto('/');
        await page.waitForLoadState('networkidle');
        
        const regularPageLayout = await page.evaluate(() => {
            return {
                hasHead: !!document.head && document.head.children.length > 0,
                headChildren: document.head ? document.head.children.length : 0,
                hasStylesheets: document.querySelectorAll('link[rel="stylesheet"]').length,
                hasBlazorScripts: document.querySelectorAll('script[src*="blazor"]').length
            };
        });
        
        console.log('Regular page layout:', regularPageLayout);
        
        // Test a Blazor component page
        await page.goto('/login');
        await page.waitForLoadState('networkidle');
        
        const blazorPageLayout = await page.evaluate(() => {
            return {
                hasHead: !!document.head && document.head.children.length > 0,
                headChildren: document.head ? document.head.children.length : 0,
                hasStylesheets: document.querySelectorAll('link[rel="stylesheet"]').length,
                hasBlazorScripts: document.querySelectorAll('script[src*="blazor"]').length,
                pageContent: document.body ? document.body.textContent?.substring(0, 100) : 'NO BODY'
            };
        });
        
        console.log('Blazor page (login) layout:', blazorPageLayout);
        
        // Compare and diagnose
        console.log('\n🎯 Layout Comparison:');
        if (regularPageLayout.hasHead && !blazorPageLayout.hasHead) {
            console.log('   ✅ Layout works for regular pages');
            console.log('   ❌ Layout does NOT work for Blazor components');
            console.log('   🔧 Need to fix Blazor-specific routing/rendering');
        } else if (!regularPageLayout.hasHead && !blazorPageLayout.hasHead) {
            console.log('   ❌ Layout system is completely broken');
            console.log('   🔧 Need to fix fundamental layout configuration');
        } else if (regularPageLayout.hasHead && blazorPageLayout.hasHead) {
            console.log('   ✅ Layout works for both regular and Blazor pages');
        } else {
            console.log('   ⚠️ Inconsistent results - need further investigation');
        }
    });

    test('should check for layout components', async ({ page }) => {
        await page.goto('/');
        await page.waitForLoadState('networkidle');
        
        const layoutComponents = await page.evaluate(() => {
            return {
                hasHeader: !!document.querySelector('header, .header, nav'),
                hasFooter: !!document.querySelector('footer, .footer'),
                hasMainContent: !!document.querySelector('main, .main-content, #main'),
                hasSidebar: !!document.querySelector('aside, .sidebar'),
                hasNavigation: !!document.querySelector('nav, .navigation, .navbar'),
                layoutStructure: {
                    header: document.querySelector('header, .header')?.tagName,
                    nav: document.querySelector('nav, .navbar')?.tagName,
                    main: document.querySelector('main, .main-content')?.tagName,
                    footer: document.querySelector('footer, .footer')?.tagName
                }
            };
        });
        
        console.log('Layout components:', layoutComponents);
        console.log('Layout structure:', layoutComponents.layoutStructure);
        
        // Verify essential layout components exist
        expect(layoutComponents.hasMainContent).toBeTruthy();
        expect(layoutComponents.hasNavigation || layoutComponents.hasHeader).toBeTruthy();
    });

    test('should verify CSS and JavaScript loading in layout', async ({ page }) => {
        await page.goto('/');
        await page.waitForLoadState('networkidle');
        
        const resources = await page.evaluate(() => {
            const stylesheets = Array.from(document.querySelectorAll('link[rel="stylesheet"]')).map(link => ({
                href: link.getAttribute('href'),
                media: link.getAttribute('media') || 'all'
            }));
            
            const scripts = Array.from(document.querySelectorAll('script[src]')).map(script => ({
                src: script.getAttribute('src'),
                async: script.hasAttribute('async'),
                defer: script.hasAttribute('defer')
            }));
            
            return { stylesheets, scripts };
        });
        
        console.log(`Found ${resources.stylesheets.length} stylesheets:`);
        resources.stylesheets.forEach(css => {
            console.log(`   - ${css.href} (media: ${css.media})`);
        });
        
        console.log(`\nFound ${resources.scripts.length} scripts:`);
        resources.scripts.forEach(script => {
            const flags = [];
            if (script.async) flags.push('async');
            if (script.defer) flags.push('defer');
            console.log(`   - ${script.src} ${flags.length ? `[${flags.join(', ')}]` : ''}`);
        });
        
        // Verify essential resources
        const hasAppStyles = resources.stylesheets.some(css => 
            css.href?.includes('app.css') || css.href?.includes('wcr-theme')
        );
        const hasBlazorScript = resources.scripts.some(script => 
            script.src?.includes('blazor')
        );
        
        expect(hasAppStyles).toBeTruthy();
        expect(hasBlazorScript).toBeTruthy();
    });

    test('should check layout rendering across different pages', async ({ page }) => {
        const pagesToTest = [
            { path: '/', name: 'Home' },
            { path: '/events', name: 'Events' },
            { path: '/login', name: 'Login' },
            { path: '/register', name: 'Register' }
        ];
        
        const layoutResults: Array<{
            page: string;
            hasLayout: boolean;
            stylesheetCount: number;
            scriptCount: number;
        }> = [];
        
        for (const testPage of pagesToTest) {
            await page.goto(testPage.path);
            await page.waitForLoadState('networkidle');
            
            const layoutInfo = await page.evaluate(() => {
                return {
                    hasLayout: document.head.children.length > 0 && document.body.children.length > 0,
                    stylesheetCount: document.querySelectorAll('link[rel="stylesheet"]').length,
                    scriptCount: document.querySelectorAll('script[src]').length
                };
            });
            
            layoutResults.push({
                page: testPage.name,
                ...layoutInfo
            });
            
            console.log(`${testPage.name}: Layout: ${layoutInfo.hasLayout ? '✅' : '❌'}, CSS: ${layoutInfo.stylesheetCount}, JS: ${layoutInfo.scriptCount}`);
        }
        
        // Check for consistency
        const allHaveLayout = layoutResults.every(r => r.hasLayout);
        const cssCountsConsistent = new Set(layoutResults.map(r => r.stylesheetCount)).size <= 2; // Allow some variation
        
        console.log(`\nLayout consistency: ${allHaveLayout ? '✅' : '❌'}`);
        console.log(`CSS loading consistency: ${cssCountsConsistent ? '✅' : '❌'}`);
        
        expect(allHaveLayout).toBeTruthy();
    });

    test('should verify meta tags and SEO elements in layout', async ({ page }) => {
        await page.goto('/');
        await page.waitForLoadState('networkidle');
        
        const metaInfo = await page.evaluate(() => {
            const meta = {
                title: document.title,
                description: document.querySelector('meta[name="description"]')?.getAttribute('content'),
                viewport: document.querySelector('meta[name="viewport"]')?.getAttribute('content'),
                charset: document.querySelector('meta[charset]')?.getAttribute('charset'),
                ogTags: Array.from(document.querySelectorAll('meta[property^="og:"]')).map(tag => ({
                    property: tag.getAttribute('property'),
                    content: tag.getAttribute('content')
                }))
            };
            
            return meta;
        });
        
        console.log('Meta information:');
        console.log('   Title:', metaInfo.title);
        console.log('   Description:', metaInfo.description);
        console.log('   Viewport:', metaInfo.viewport);
        console.log('   Charset:', metaInfo.charset);
        console.log('   OG Tags:', metaInfo.ogTags.length);
        
        // Verify essential meta tags
        expect(metaInfo.title).toBeTruthy();
        expect(metaInfo.viewport).toBeTruthy();
        expect(metaInfo.charset).toBeTruthy();
    });
});