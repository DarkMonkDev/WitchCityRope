const { chromium } = require('playwright');

async function debugLoginPage() {
    const browser = await chromium.launch({ headless: false });
    const context = await browser.newContext();
    const page = await context.newPage();

    try {
        console.log('🌐 Navigating to login page...');
        await page.goto('http://localhost:5651/login', { waitUntil: 'networkidle' });
        
        console.log('📖 Page title:', await page.title());
        console.log('🔗 Current URL:', page.url());
        
        // Take a screenshot for debugging
        await page.screenshot({ path: 'debug-login-page.png', fullPage: true });
        console.log('📸 Screenshot saved as debug-login-page.png');
        
        // Check what form elements exist
        const forms = await page.locator('form').count();
        console.log('📝 Number of forms found:', forms);
        
        // Look for input fields
        const emailInputs = await page.locator('input[type="email"], input[type="text"], input[name*="email"], input[name*="Email"]').count();
        console.log('📧 Email input fields found:', emailInputs);
        
        const passwordInputs = await page.locator('input[type="password"], input[name*="password"], input[name*="Password"]').count();
        console.log('🔒 Password input fields found:', passwordInputs);
        
        // Check for any buttons
        const buttons = await page.locator('button').count();
        console.log('🔘 Buttons found:', buttons);
        
        // Look for specific content to understand page structure
        const headings = await page.locator('h1, h2, h3').allTextContents();
        console.log('📄 Headings found:', headings);
        
        // Check if this is actually a 404 or error page
        const bodyText = await page.textContent('body');
        if (bodyText.includes('404') || bodyText.includes('Not Found') || bodyText.includes('Error')) {
            console.log('❌ Page appears to be an error page');
            console.log('First 300 chars:', bodyText.substring(0, 300));
        } else {
            console.log('✅ Page appears to load correctly');
        }
        
        // Wait to see the page
        await page.waitForTimeout(5000);
        
    } catch (error) {
        console.error('❌ Error debugging login page:', error.message);
    } finally {
        await browser.close();
    }
}

debugLoginPage();