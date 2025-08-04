const { chromium } = require('playwright');

async function debugLoginDetailed() {
    const browser = await chromium.launch({ headless: false });
    const context = await browser.newContext();
    const page = await context.newPage();

    // Capture console logs and errors
    const logs = [];
    const errors = [];
    
    page.on('console', msg => {
        logs.push(`${msg.type()}: ${msg.text()}`);
    });
    
    page.on('pageerror', error => {
        errors.push(error.message);
    });

    try {
        console.log('🌐 Navigating to login page...');
        await page.goto('http://localhost:5651/login', { waitUntil: 'networkidle', timeout: 30000 });
        
        console.log('📖 Page title:', await page.title());
        console.log('🔗 Current URL:', page.url());
        
        // Wait a moment for any delayed errors
        await page.waitForTimeout(3000);
        
        // Report console logs and errors
        console.log('\n📋 Console logs:');
        logs.forEach(log => console.log('  ', log));
        
        console.log('\n❌ JavaScript errors:');
        errors.forEach(error => console.log('  ', error));
        
        // Check specific input field selectors
        console.log('\n🔍 Checking input field selectors:');
        
        // Test the exact selectors from the Playwright test
        const emailSelectors = [
            'input[name="email"]',
            'input#email',
            '.wcr-input[type="email"]',
            'input[type="email"]',
            'input[placeholder*="email"]'
        ];
        
        for (const selector of emailSelectors) {
            const count = await page.locator(selector).count();
            console.log(`  ${selector}: ${count} elements found`);
        }
        
        const passwordSelectors = [
            'input[name="password"]',
            'input#password',
            '.wcr-input[type="password"]',
            'input[type="password"]',
            'input[placeholder*="password"]'
        ];
        
        for (const selector of passwordSelectors) {
            const count = await page.locator(selector).count();
            console.log(`  ${selector}: ${count} elements found`);
        }
        
        // Check button selectors
        const buttonSelectors = [
            'button.sign-in-btn',
            'button[type="submit"]',
            'button:has-text("Sign in")',
            'button:has-text("SIGN IN")',
            'button[type="submit"]:has-text("Sign in")'
        ];
        
        console.log('\n🔘 Checking button selectors:');
        for (const selector of buttonSelectors) {
            const count = await page.locator(selector).count();
            console.log(`  ${selector}: ${count} elements found`);
        }
        
        // Get actual HTML structure around the form
        console.log('\n📄 Form HTML structure:');
        const formHTML = await page.locator('form').first().innerHTML();
        console.log(formHTML.substring(0, 500) + '...');
        
        // Wait to see the page
        await page.waitForTimeout(5000);
        
    } catch (error) {
        console.error('❌ Error debugging login page:', error.message);
    } finally {
        await browser.close();
    }
}

debugLoginDetailed();