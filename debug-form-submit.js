const { chromium } = require('playwright');

async function debugFormSubmit() {
    const browser = await chromium.launch({ headless: false });
    const context = await browser.newContext();
    const page = await context.newPage();

    // Capture console errors
    const errors = [];
    page.on('console', msg => {
        if (msg.type() === 'error') {
            errors.push(msg.text());
        }
        console.log(`${msg.type()}: ${msg.text()}`);
    });
    
    page.on('pageerror', error => {
        errors.push(error.message);
        console.log('PAGE ERROR:', error.message);
    });

    try {
        await page.goto('http://localhost:5651/login', { waitUntil: 'networkidle' });
        
        // Wait for Blazor to fully initialize
        console.log('⏳ Waiting for Blazor to initialize...');
        await page.waitForTimeout(5000);
        
        // Check form attributes
        const forms = await page.locator('form').count();
        console.log(`📝 Found ${forms} forms on page`);
        
        for (let i = 0; i < forms; i++) {
            const form = page.locator('form').nth(i);
            const action = await form.getAttribute('action');
            const method = await form.getAttribute('method');
            console.log(`Form ${i}: action="${action}", method="${method}"`);
        }
        
        // Check if form has proper Blazor attributes
        const loginForm = page.locator('form[method="post"]').first();
        const onsubmit = await loginForm.getAttribute('onsubmit');
        const blazorAttributes = await loginForm.evaluate(form => {
            const attrs = {};
            for (const attr of form.attributes) {
                if (attr.name.includes('blazor') || attr.name.includes('internal')) {
                    attrs[attr.name] = attr.value;
                }
            }
            return attrs;
        });
        console.log('🔥 Blazor form attributes:', blazorAttributes);
        console.log('📋 Form onsubmit:', onsubmit);
        
        // Try to submit form and capture network request
        console.log('📤 Attempting form submission...');
        
        await page.fill('input[type="email"]', 'admin@witchcityrope.com');
        await page.fill('input[type="password"]', 'Test123!');
        
        // Listen for form submission
        const requestPromise = page.waitForRequest(request => 
            request.url().includes('/login') && request.method() === 'POST'
        );
        
        await page.click('button:has-text("SIGN IN")');
        
        try {
            const request = await requestPromise;
            console.log('✅ POST request captured:', request.url());
            console.log('📋 Request headers:', await request.allHeaders());
            
            const response = await request.response();
            console.log('📨 Response status:', response?.status());
            console.log('📨 Response headers:', await response?.allHeaders());
            
        } catch (timeoutError) {
            console.log('❌ No POST request captured - form may not be submitting');
        }
        
        await page.waitForTimeout(3000);
        
        console.log('\n📊 JavaScript Errors Found:');
        errors.forEach(error => console.log('  ❌', error));
        
        // Take screenshot
        await page.screenshot({ path: 'form-debug.png', fullPage: true });
        
    } catch (error) {
        console.error('❌ Debug error:', error.message);
    } finally {
        await browser.close();
    }
}

debugFormSubmit();