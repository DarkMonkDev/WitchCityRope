const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: true });
    const page = await browser.newPage();
    
    try {
        console.log('1. Navigating to login page...');
        await page.goto('http://localhost:5651/login');
        
        console.log('2. Filling login form...');
        await page.fill('input[id="login-email"]', 'admin@witchcityrope.com');
        await page.fill('input[id="login-password"]', 'Test123!');
        
        console.log('3. Clicking sign in button...');
        await page.click('button[type="submit"]');
        
        console.log('4. Waiting for navigation...');
        await page.waitForTimeout(3000);
        
        console.log('5. Current URL:', page.url());
        
        const cookies = await page.context().cookies();
        const authCookie = cookies.find(c => c.name.includes('.AspNetCore.Identity'));
        console.log('6. Auth cookie found:', !!authCookie);
        
        console.log('7. Navigating to admin dashboard...');
        await page.goto('http://localhost:5651/admin/dashboard');
        await page.waitForTimeout(2000);
        
        console.log('8. Final URL:', page.url());
        
        if (page.url().includes('/admin/dashboard')) {
            console.log('✅ SUCCESS: Admin dashboard accessible!');
        } else {
            console.log('❌ FAILED: Redirected to', page.url());
        }
        
    } catch (error) {
        console.error('Error:', error.message);
    } finally {
        await browser.close();
    }
})();
