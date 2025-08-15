const { chromium } = require('playwright');

async function testLoginSimple() {
    const browser = await chromium.launch({ headless: false });
    const context = await browser.newContext();
    const page = await context.newPage();

    try {
        console.log('🌐 Navigating to login page...');
        await page.goto('http://localhost:5651/login', { waitUntil: 'networkidle' });
        
        console.log('📖 Page title:', await page.title());
        
        // Test our updated selectors
        console.log('🧪 Testing email input...');
        const emailInput = page.locator('input[type="email"], input#login-email, input[placeholder*="email"]').first();
        await emailInput.waitFor({ state: 'visible', timeout: 5000 });
        console.log('✅ Email input found');
        
        console.log('🧪 Testing password input...');
        const passwordInput = page.locator('input[type="password"], input#login-password, input[placeholder*="password"]').first();
        await passwordInput.waitFor({ state: 'visible', timeout: 5000 });
        console.log('✅ Password input found');
        
        console.log('🧪 Testing sign-in button...');
        const signInButton = page.locator('button[type="submit"]:has-text("SIGN IN"), button:has-text("SIGN IN")').first();
        await signInButton.waitFor({ state: 'visible', timeout: 5000 });
        console.log('✅ Sign-in button found');
        
        console.log('🧪 Testing form groups...');
        const formGroups = page.locator('.form-group');
        const formGroupCount = await formGroups.count();
        console.log(`✅ Found ${formGroupCount} form groups`);
        
        console.log('🧪 Testing checkbox...');
        const checkbox = page.locator('input[type="checkbox"]').first();
        const checkboxExists = await checkbox.count() > 0;
        console.log(`✅ Checkbox found: ${checkboxExists}`);
        
        console.log('🧪 Testing forgot password link...');
        const forgotLink = page.locator('a:has-text("Forgot your password?")').first();
        const forgotExists = await forgotLink.count() > 0;
        console.log(`✅ Forgot password link found: ${forgotExists}`);
        
        // Test simple form interaction
        await emailInput.fill('test@example.com');
        await passwordInput.fill('testpassword');
        console.log('✅ Form fields filled successfully');
        
        // Wait to see the page
        await page.waitForTimeout(3000);
        
    } catch (error) {
        console.error('❌ Error in test:', error.message);
    } finally {
        await browser.close();
    }
}

testLoginSimple();