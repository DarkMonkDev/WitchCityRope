const puppeteer = require('puppeteer');
const fs = require('fs');

async function testLogin() {
    const browser = await puppeteer.launch({
        headless: false,
        devtools: true,
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    
    // Enable console logging
    page.on('console', msg => {
        console.log(`Browser Console [${msg.type()}]:`, msg.text());
    });

    // Enable error logging
    page.on('pageerror', error => {
        console.error('Page Error:', error.message);
    });

    // Track all network requests
    const networkRequests = [];
    const failedRequests = [];
    
    page.on('request', request => {
        const requestInfo = {
            url: request.url(),
            method: request.method(),
            headers: request.headers(),
            postData: request.postData(),
            timestamp: new Date().toISOString()
        };
        networkRequests.push(requestInfo);
        console.log(`📤 ${request.method()} ${request.url()}`);
        if (request.postData()) {
            console.log('   Post Data:', request.postData());
        }
    });

    page.on('requestfailed', request => {
        const failure = {
            url: request.url(),
            method: request.method(),
            failure: request.failure(),
            timestamp: new Date().toISOString()
        };
        failedRequests.push(failure);
        console.error(`❌ Failed: ${request.method()} ${request.url()}`);
        console.error('   Reason:', request.failure());
    });

    page.on('response', response => {
        console.log(`📥 ${response.status()} ${response.url()}`);
        if (response.status() >= 400) {
            console.error(`   Error Status: ${response.status()} ${response.statusText()}`);
        }
    });

    try {
        console.log('\n🚀 Navigating to login page...');
        await page.goto('http://localhost:5651/identity/account/login', {
            waitUntil: 'networkidle0',
            timeout: 30000
        });

        // Take initial screenshot
        await page.screenshot({ path: 'login-page-initial.png', fullPage: true });
        console.log('📸 Initial screenshot saved as login-page-initial.png');

        // Wait for the page to fully load
        await new Promise(resolve => setTimeout(resolve, 2000));

        // Check for Blazor
        const blazorLoaded = await page.evaluate(() => {
            return typeof window.Blazor !== 'undefined';
        });
        console.log('\n🔍 Blazor loaded:', blazorLoaded);

        // Check for any initial console errors
        const initialErrors = await page.evaluate(() => {
            const errors = [];
            if (window.console && window.console.error) {
                // This won't capture past errors, but we're already logging them
            }
            return errors;
        });

        console.log('\n📝 Filling in login form...');
        
        // Try different selectors for email input
        const emailSelectors = [
            'input[type="email"]',
            'input[name="email"]',
            'input[placeholder*="email" i]',
            '#email',
            '[data-testid="email"]',
            '.form-control[type="email"]'
        ];

        let emailInput = null;
        for (const selector of emailSelectors) {
            try {
                emailInput = await page.waitForSelector(selector, { timeout: 1000 });
                if (emailInput) {
                    console.log(`✅ Found email input with selector: ${selector}`);
                    break;
                }
            } catch (e) {
                // Continue to next selector
            }
        }

        if (!emailInput) {
            throw new Error('Could not find email input field');
        }

        await emailInput.type('admin@witchcityrope.com', { delay: 50 });

        // Try different selectors for password input
        const passwordSelectors = [
            'input[type="password"]',
            'input[name="password"]',
            'input[placeholder*="password" i]',
            '#password',
            '[data-testid="password"]',
            '.form-control[type="password"]'
        ];

        let passwordInput = null;
        for (const selector of passwordSelectors) {
            try {
                passwordInput = await page.waitForSelector(selector, { timeout: 1000 });
                if (passwordInput) {
                    console.log(`✅ Found password input with selector: ${selector}`);
                    break;
                }
            } catch (e) {
                // Continue to next selector
            }
        }

        if (!passwordInput) {
            throw new Error('Could not find password input field');
        }

        await passwordInput.type('Test123!', { delay: 50 });

        // Take screenshot after filling form
        await page.screenshot({ path: 'login-page-filled.png', fullPage: true });
        console.log('📸 Form filled screenshot saved as login-page-filled.png');

        // Clear previous network requests to focus on login attempt
        networkRequests.length = 0;
        failedRequests.length = 0;

        // Try different selectors for submit button
        const submitSelectors = [
            'button[type="submit"]',
            'button:contains("Sign In")',
            'button:contains("Login")',
            'button:contains("Submit")',
            '[data-testid="login-button"]',
            '.btn-primary',
            'button.btn'
        ];

        let submitButton = null;
        for (const selector of submitSelectors) {
            try {
                // Use evaluate to find button by text content
                submitButton = await page.evaluateHandle((sel) => {
                    if (sel.includes(':contains')) {
                        const searchText = sel.match(/:contains\("(.+)"\)/)[1];
                        const buttons = Array.from(document.querySelectorAll('button'));
                        return buttons.find(btn => btn.textContent.includes(searchText));
                    }
                    return document.querySelector(sel);
                }, selector);
                
                if (submitButton && await submitButton.evaluate(el => el !== null)) {
                    console.log(`✅ Found submit button with selector: ${selector}`);
                    break;
                }
            } catch (e) {
                // Continue to next selector
            }
        }

        if (!submitButton || await submitButton.evaluate(el => el === null)) {
            // Try to find any button on the page
            const allButtons = await page.$$eval('button', buttons => 
                buttons.map(btn => ({ text: btn.textContent.trim(), html: btn.outerHTML }))
            );
            console.log('All buttons on page:', allButtons);
            throw new Error('Could not find submit button');
        }

        console.log('\n🖱️ Clicking Sign In button...');
        
        // Click and wait for potential navigation or network activity
        await Promise.all([
            submitButton.click(),
            page.waitForNavigation({ waitUntil: 'networkidle0', timeout: 5000 }).catch(() => {}),
            new Promise(resolve => setTimeout(resolve, 5000)) // Wait 5 seconds to capture any async activity
        ]);

        console.log('\n📊 Network Activity Summary:');
        console.log(`Total requests: ${networkRequests.length}`);
        console.log(`Failed requests: ${failedRequests.length}`);

        // Check for API calls to port 5653
        const apiCalls = networkRequests.filter(req => req.url.includes(':5653'));
        console.log(`\nAPI calls to port 5653: ${apiCalls.length}`);
        apiCalls.forEach(call => {
            console.log(`  ${call.method} ${call.url}`);
            if (call.postData) {
                console.log(`  Data: ${call.postData}`);
            }
        });

        // Take final screenshot
        await page.screenshot({ path: 'login-page-after-click.png', fullPage: true });
        console.log('\n📸 After click screenshot saved as login-page-after-click.png');

        // Save network log
        const networkLog = {
            timestamp: new Date().toISOString(),
            allRequests: networkRequests,
            failedRequests: failedRequests,
            apiCalls: apiCalls
        };
        fs.writeFileSync('network-log.json', JSON.stringify(networkLog, null, 2));
        console.log('📄 Network log saved as network-log.json');

        // Check current URL
        console.log('\n🌐 Current URL:', page.url());

        // Check for any error messages on the page
        const errorMessages = await page.$$eval('[class*="error"], [class*="alert"], [class*="danger"], [class*="invalid"]', 
            elements => elements.map(el => ({ 
                text: el.textContent.trim(), 
                classes: el.className 
            }))
        );
        
        if (errorMessages.length > 0) {
            console.log('\n⚠️ Error messages found on page:');
            errorMessages.forEach(err => {
                console.log(`  "${err.text}" (classes: ${err.classes})`);
            });
        }

        // Keep browser open for manual inspection
        console.log('\n✅ Test complete. Browser will remain open for inspection.');
        console.log('Press Ctrl+C to close the browser and exit.');
        
        // Keep the script running
        await new Promise(() => {});

    } catch (error) {
        console.error('\n❌ Test failed:', error);
        await page.screenshot({ path: 'error-screenshot.png', fullPage: true });
        console.log('📸 Error screenshot saved as error-screenshot.png');
    }
}

testLogin().catch(console.error);