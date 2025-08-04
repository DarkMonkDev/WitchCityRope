const { chromium } = require('playwright');

async function testHomePage() {
    const browser = await chromium.launch({ headless: false });
    const context = await browser.newContext();
    const page = await context.newPage();

    try {
        console.log('🌐 Navigating to home page...');
        await page.goto('http://localhost:5651', { waitUntil: 'networkidle' });
        
        console.log('📖 Page title:', await page.title());
        console.log('🔗 Current URL:', page.url());
        
        // Check if page loaded successfully
        const bodyContent = await page.textContent('body');
        if (bodyContent && bodyContent.length > 0) {
            console.log('✅ Home page loaded successfully!');
            console.log('📄 Page contains content (first 200 chars):', bodyContent.substring(0, 200));
        } else {
            console.log('❌ Home page appears empty');
        }
        
        // Take a screenshot for verification
        await page.screenshot({ path: 'homepage-test.png', fullPage: true });
        console.log('📸 Screenshot saved as homepage-test.png');
        
        // Wait a moment to see the page
        await page.waitForTimeout(3000);
        
    } catch (error) {
        console.error('❌ Error testing home page:', error.message);
    } finally {
        await browser.close();
    }
}

testHomePage();