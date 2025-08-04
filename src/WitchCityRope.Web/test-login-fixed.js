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

        console.log('\n📝 Filling in login form...');
        
        // Correct selectors for ASP.NET Core Identity form fields
        const emailOrUsernameSelectors = [
            'input[name="Input.EmailOrUsername"]',
            'input#Input_EmailOrUsername',
            'input[asp-for="Input.EmailOrUsername"]'
        ];

        let emailInput = null;
        for (const selector of emailOrUsernameSelectors) {
            try {
                emailInput = await page.$(selector);
                if (emailInput) {
                    console.log(`✅ Found email/username input with selector: ${selector}`);
                    break;
                }
            } catch (e) {
                // Continue to next selector
            }
        }

        if (!emailInput) {
            // Debug: log all input fields on the page
            const allInputs = await page.$$eval('input', inputs => 
                inputs.map(input => ({ 
                    name: input.name, 
                    id: input.id, 
                    type: input.type,
                    placeholder: input.placeholder 
                }))
            );
            console.log('All input fields on page:', allInputs);
            throw new Error('Could not find email/username input field');
        }

        await emailInput.type('admin@witchcityrope.com', { delay: 50 });

        // Correct selectors for password field
        const passwordSelectors = [
            'input[name="Input.Password"]',
            'input#Input_Password',
            'input[asp-for="Input.Password"]'
        ];

        let passwordInput = null;
        for (const selector of passwordSelectors) {
            try {
                passwordInput = await page.$(selector);
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

        await passwordInput.type('Admin123!', { delay: 50 });

        // Take screenshot after filling form
        await page.screenshot({ path: 'login-page-filled.png', fullPage: true });
        console.log('📸 Form filled screenshot saved as login-page-filled.png');

        // Clear previous network requests to focus on login attempt
        networkRequests.length = 0;
        failedRequests.length = 0;

        // Find the submit button
        const submitButton = await page.$('button[type="submit"]#login-submit') || 
                           await page.$('button[type="submit"]');

        if (!submitButton) {
            // Debug: log all buttons on the page
            const allButtons = await page.$$eval('button', buttons => 
                buttons.map(btn => ({ 
                    text: btn.textContent.trim(), 
                    type: btn.type,
                    id: btn.id,
                    classes: btn.className 
                }))
            );
            console.log('All buttons on page:', allButtons);
            throw new Error('Could not find submit button');
        }

        console.log('\n🖱️ Clicking Sign In button...');
        
        // Click and wait for navigation
        await Promise.all([
            submitButton.click(),
            page.waitForNavigation({ waitUntil: 'networkidle0', timeout: 10000 }).catch(() => {
                console.log('⚠️ Navigation timeout - page may have stayed on login page');
            })
        ]);

        // Wait a bit more to ensure any redirects complete
        await new Promise(resolve => setTimeout(resolve, 2000));

        console.log('\n📊 Network Activity Summary:');
        console.log(`Total requests: ${networkRequests.length}`);
        console.log(`Failed requests: ${failedRequests.length}`);

        // Check for form POST requests
        const postRequests = networkRequests.filter(req => req.method === 'POST');
        console.log(`\nPOST requests: ${postRequests.length}`);
        postRequests.forEach(req => {
            console.log(`  ${req.url}`);
            if (req.postData) {
                console.log(`  Data: ${req.postData}`);
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
            postRequests: postRequests
        };
        fs.writeFileSync('network-log.json', JSON.stringify(networkLog, null, 2));
        console.log('📄 Network log saved as network-log.json');

        // Check current URL
        const currentUrl = page.url();
        console.log('\n🌐 Current URL:', currentUrl);
        
        // Check if we're still on login page
        if (currentUrl.includes('/login') || currentUrl.includes('/identity/account/login')) {
            console.log('⚠️ Still on login page - checking for errors...');
            
            // Check for any error messages
            const errorMessages = await page.$$eval('.wcr-validation-summary, .wcr-validation-message, [class*="error"], [class*="alert"]', 
                elements => elements.map(el => ({ 
                    text: el.textContent.trim(), 
                    classes: el.className 
                })).filter(el => el.text.length > 0)
            );
            
            if (errorMessages.length > 0) {
                console.log('\n⚠️ Error messages found on page:');
                errorMessages.forEach(err => {
                    console.log(`  "${err.text}" (classes: ${err.classes})`);
                });
            }
        } else {
            console.log('✅ Successfully redirected to:', currentUrl);
        }

        // Check authentication cookie
        const cookies = await page.cookies();
        const authCookie = cookies.find(c => c.name.includes('AspNetCore') || c.name.includes('Identity'));
        if (authCookie) {
            console.log('\n🍪 Authentication cookie found:', authCookie.name);
        } else {
            console.log('\n⚠️ No authentication cookie found');
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