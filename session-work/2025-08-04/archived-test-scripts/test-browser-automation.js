#!/usr/bin/env node

const puppeteer = require('puppeteer');

async function testBrowserAutomation() {
    console.log('=== WitchCityRope Browser Automation Test ===\n');
    
    let browser;
    try {
        // Test 1: Launch browser
        console.log('Test 1: Launching Chrome browser...');
        browser = await puppeteer.launch({
            headless: false,
            executablePath: '/usr/bin/google-chrome',
            args: [
                '--no-sandbox',
                '--disable-setuid-sandbox',
                '--disable-dev-shm-usage'
            ]
        });
        console.log('✓ Browser launched successfully\n');

        // Test 2: Create new page
        console.log('Test 2: Creating new page...');
        const page = await browser.newPage();
        await page.setViewport({ width: 1280, height: 720 });
        console.log('✓ Page created with viewport 1280x720\n');

        // Test 3: Navigate to a test page
        console.log('Test 3: Navigating to example.com...');
        await page.goto('https://example.com', { waitUntil: 'networkidle2' });
        console.log('✓ Successfully navigated to example.com\n');

        // Test 4: Take screenshot
        console.log('Test 4: Taking screenshot...');
        await page.screenshot({ path: 'test-navigation.png' });
        console.log('✓ Screenshot saved as test-navigation.png\n');

        // Test 5: Extract page title
        console.log('Test 5: Extracting page information...');
        const title = await page.title();
        const url = page.url();
        console.log(`✓ Page title: ${title}`);
        console.log(`✓ Current URL: ${url}\n`);

        // Test 6: Test page interaction
        console.log('Test 6: Testing page interaction...');
        const hasContent = await page.evaluate(() => {
            return document.querySelector('h1') !== null;
        });
        console.log(`✓ Page has H1 element: ${hasContent}\n`);

        // Test 7: Navigate to local development (if running)
        console.log('Test 7: Testing local development server...');
        try {
            await page.goto('http://localhost:5000', { 
                waitUntil: 'networkidle2',
                timeout: 5000 
            });
            console.log('✓ Local development server is running\n');
            await page.screenshot({ path: 'test-local-app.png' });
        } catch (error) {
            console.log('✗ Local development server not running (this is OK)\n');
        }

        // Test 8: Test form filling capabilities
        console.log('Test 8: Testing form interaction capabilities...');
        await page.goto('https://www.google.com');
        const searchBox = await page.$('textarea[name="q"]');
        if (searchBox) {
            await searchBox.type('WitchCityRope Salem');
            console.log('✓ Successfully typed into search box\n');
            await page.screenshot({ path: 'test-form-interaction.png' });
        }

        console.log('=== All Browser Automation Tests Completed ===');
        console.log('\nBrowser automation is working correctly!');
        console.log('You can now use:');
        console.log('- Browser-tools MCP for low-level automation');
        console.log('- Stagehand MCP for AI-powered automation');

    } catch (error) {
        console.error('✗ Test failed:', error.message);
    } finally {
        if (browser) {
            await browser.close();
            console.log('\n✓ Browser closed');
        }
    }
}

// Run the test
testBrowserAutomation().catch(console.error);