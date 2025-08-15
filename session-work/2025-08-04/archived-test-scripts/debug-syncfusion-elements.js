const { chromium } = require('playwright');

async function debugSyncfusionElements() {
    const browser = await chromium.launch({ headless: false });
    const context = await browser.newContext();
    const page = await context.newPage();

    try {
        await page.goto('http://localhost:5651/login', { waitUntil: 'networkidle' });
        
        // Wait for Syncfusion components to render
        await page.waitForTimeout(3000);
        
        console.log('🔍 Analyzing actual form structure...');
        
        // Check what the email field actually renders as
        console.log('\n📧 Email field analysis:');
        const emailElements = await page.locator('input[type="email"]').count();
        console.log(`Standard email inputs: ${emailElements}`);
        
        const syncfusionInputs = await page.locator('.e-textbox, .e-input-group input, [id="login-email"]').count();
        console.log(`Syncfusion inputs: ${syncfusionInputs}`);
        
        // Get the actual HTML structure of the form
        const formHTML = await page.locator('form[method="post"]').first().innerHTML();
        console.log('\n📋 Actual form HTML structure:');
        
        // Extract just the email input section
        const emailSection = formHTML.match(/<div class="form-group">[\s\S]*?Email Address[\s\S]*?<\/div>/);
        if (emailSection) {
            console.log('Email section HTML:');
            console.log(emailSection[0].substring(0, 300) + '...');
        }
        
        // Check what selectors actually work
        console.log('\n🎯 Testing different selectors:');
        const selectors = [
            'input[type="email"]',
            'input[id="login-email"]',
            '#login-email',
            '.e-textbox input',
            '.e-input-group input',
            'input[placeholder*="email"]',
            '.wcr-input input',
            '.form-group input'
        ];
        
        for (const selector of selectors) {
            const count = await page.locator(selector).count();
            if (count > 0) {
                const isVisible = await page.locator(selector).first().isVisible();
                console.log(`✅ ${selector}: ${count} elements (visible: ${isVisible})`);
            } else {
                console.log(`❌ ${selector}: 0 elements`);
            }
        }
        
        // Test password field too
        console.log('\n🔒 Password field analysis:');
        const passwordSelectors = [
            'input[type="password"]',
            'input[id="login-password"]',
            '#login-password',
            '.e-textbox input[type="password"]'
        ];
        
        for (const selector of passwordSelectors) {
            const count = await page.locator(selector).count();
            if (count > 0) {
                console.log(`✅ ${selector}: ${count} elements`);
            } else {
                console.log(`❌ ${selector}: 0 elements`);
            }
        }
        
        // Test submit button
        console.log('\n🔘 Submit button analysis:');
        const buttonSelectors = [
            'button[type="submit"]',
            '.btn-primary-full',
            '.e-btn[type="submit"]',
            'button:has-text("SIGN IN")'
        ];
        
        for (const selector of buttonSelectors) {
            const count = await page.locator(selector).count();
            if (count > 0) {
                const text = await page.locator(selector).first().textContent();
                console.log(`✅ ${selector}: ${count} elements (text: "${text?.trim()}")`);
            } else {
                console.log(`❌ ${selector}: 0 elements`);
            }
        }
        
        // Wait to see the page
        await page.waitForTimeout(5000);
        
    } catch (error) {
        console.error('❌ Error:', error.message);
    } finally {
        await browser.close();
    }
}

debugSyncfusionElements();