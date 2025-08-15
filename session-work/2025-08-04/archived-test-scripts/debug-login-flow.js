const { chromium } = require('playwright');

async function debugLoginFlow() {
    const browser = await chromium.launch({ headless: false });
    const context = await browser.newContext();
    const page = await context.newPage();

    // Capture all console logs and network requests
    page.on('console', msg => console.log('CONSOLE:', msg.text()));
    page.on('response', response => {
        if (response.url().includes('login')) {
            console.log('RESPONSE:', response.status(), response.url());
        }
    });

    try {
        console.log('🔐 Testing actual login flow...');
        
        // Navigate to login page
        await page.goto('http://localhost:5651/login', { waitUntil: 'networkidle' });
        console.log('✅ Login page loaded');
        
        // Fill credentials using actual admin account
        console.log('📝 Filling login form with admin credentials...');
        await page.fill('input[type="email"]', 'admin@witchcityrope.com');
        await page.fill('input[type="password"]', 'Test123!');
        
        console.log('🔘 Submitting form...');
        await page.click('button:has-text("SIGN IN")');
        
        // Wait and see what happens
        await page.waitForTimeout(3000);
        
        console.log('🔗 Current URL after submit:', page.url());
        console.log('📖 Current page title:', await page.title());
        
        // Check if we're still on login page
        if (page.url().includes('/login')) {
            console.log('❌ Still on login page - checking for errors...');
            
            // Look for validation messages
            const validationMessages = await page.locator('.validation-message, .alert-danger, .error').allTextContents();
            console.log('📋 Validation messages:', validationMessages);
            
            // Check form state
            const emailValue = await page.inputValue('input[type="email"]');
            const passwordValue = await page.inputValue('input[type="password"]');
            console.log('📧 Email field value:', emailValue);
            console.log('🔒 Password field cleared:', passwordValue === '');
            
            // Take screenshot for debugging
            await page.screenshot({ path: 'login-debug-failed.png', fullPage: true });
            
        } else {
            console.log('✅ Login successful! Redirected to:', page.url());
            
            // Check if we can access admin area
            await page.goto('http://localhost:5651/admin');
            await page.waitForTimeout(2000);
            console.log('🔗 Admin page URL:', page.url());
            console.log('📖 Admin page title:', await page.title());
            
            if (page.url().includes('/admin') && !page.url().includes('/login')) {
                console.log('🎉 Admin access successful!');
            } else {
                console.log('❌ Admin access failed - redirected back to login');
            }
        }
        
        // Wait to see the final state
        await page.waitForTimeout(5000);
        
    } catch (error) {
        console.error('❌ Error in login flow test:', error.message);
    } finally {
        await browser.close();
    }
}

debugLoginFlow();