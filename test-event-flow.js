const puppeteer = require('puppeteer');

async function testEventFlow() {
    const browser = await puppeteer.launch({
        headless: false,
        args: ['--no-sandbox', '--disable-setuid-sandbox'],
        slowMo: 150,
        devtools: true
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1280, height: 800 });

        // Enable console logging
        page.on('console', msg => console.log('Browser:', msg.text()));
        page.on('pageerror', error => console.log('Page error:', error.message));

        console.log('=== Testing Event Management Flow ===\n');

        // 1. Check public events page
        console.log('1. Loading public events page...');
        await page.goto('http://localhost:5651/events', { waitUntil: 'networkidle2' });
        await new Promise(resolve => setTimeout(resolve, 2000));

        // Analyze the page structure
        const pageStructure = await page.evaluate(() => {
            const eventElements = document.querySelectorAll('.event-card, .event-item, [class*="event"]');
            const links = document.querySelectorAll('a[href*="/events/"]');
            const buttons = document.querySelectorAll('button');
            
            return {
                eventCount: eventElements.length,
                eventClasses: [...new Set([...eventElements].map(el => el.className))],
                linkCount: links.length,
                linkHrefs: [...links].slice(0, 3).map(a => a.href),
                buttonCount: buttons.length,
                buttonTexts: [...buttons].slice(0, 5).map(b => b.innerText)
            };
        });

        console.log('   Page structure:', JSON.stringify(pageStructure, null, 2));

        // 2. Try clicking on an event
        if (pageStructure.linkCount > 0) {
            console.log('\n2. Clicking on first event link...');
            const firstEventLink = await page.$('a[href*="/events/"]');
            if (firstEventLink) {
                await firstEventLink.click();
                await new Promise(resolve => setTimeout(resolve, 3000));
                console.log('   Current URL:', page.url());
            }
        }

        // 3. Go back and test member flow
        console.log('\n3. Testing member registration flow...');
        await page.goto('http://localhost:5651/login', { waitUntil: 'networkidle2' });
        await new Promise(resolve => setTimeout(resolve, 1500));

        // Login
        await page.type('input[type="email"]', 'member@witchcityrope.com');
        await page.type('input[type="password"]', 'Test123!');
        
        // Find submit button without using :has-text
        const submitButton = await page.evaluateHandle(() => {
            const buttons = document.querySelectorAll('button');
            for (const button of buttons) {
                if (button.type === 'submit' || button.innerText.includes('Log') || button.innerText.includes('Sign')) {
                    return button;
                }
            }
            return null;
        });

        if (submitButton) {
            await submitButton.click();
            await new Promise(resolve => setTimeout(resolve, 3000));
            console.log('   After login URL:', page.url());
        }

        // 4. Navigate to member events
        await page.goto('http://localhost:5651/member/events', { waitUntil: 'networkidle2' });
        await new Promise(resolve => setTimeout(resolve, 2000));

        // Analyze member events page
        const memberPageStructure = await page.evaluate(() => {
            const registerButtons = document.querySelectorAll('button, a');
            const registerElements = [...registerButtons].filter(el => 
                el.innerText && el.innerText.toLowerCase().includes('register')
            );
            
            return {
                registerButtonCount: registerElements.length,
                registerButtonInfo: registerElements.slice(0, 3).map(el => ({
                    tag: el.tagName,
                    text: el.innerText,
                    href: el.href || 'none',
                    onclick: el.onclick ? 'has onclick' : 'no onclick',
                    attributes: [...el.attributes].map(attr => `${attr.name}="${attr.value}"`)
                }))
            };
        });

        console.log('\n4. Member events page structure:', JSON.stringify(memberPageStructure, null, 2));

        // 5. Try clicking register if available
        if (memberPageStructure.registerButtonCount > 0) {
            console.log('\n5. Attempting to register for an event...');
            
            const registerButton = await page.evaluateHandle(() => {
                const buttons = document.querySelectorAll('button, a');
                for (const button of buttons) {
                    if (button.innerText && button.innerText.toLowerCase().includes('register')) {
                        return button;
                    }
                }
                return null;
            });

            if (registerButton) {
                await registerButton.click();
                await new Promise(resolve => setTimeout(resolve, 3000));
                
                // Check what happened
                const afterClick = await page.evaluate(() => {
                    const modal = document.querySelector('.modal, [role="dialog"], .modal-dialog');
                    const form = document.querySelector('form');
                    return {
                        url: window.location.href,
                        hasModal: !!modal,
                        hasForm: !!form,
                        modalDisplay: modal ? window.getComputedStyle(modal).display : 'none',
                        visibleElements: [...document.querySelectorAll('*')].filter(el => {
                            const style = window.getComputedStyle(el);
                            return style.display !== 'none' && style.visibility !== 'hidden' && 
                                   el.offsetHeight > 0 && el.innerText && 
                                   (el.innerText.includes('Register') || el.innerText.includes('Payment'));
                        }).slice(0, 5).map(el => ({
                            tag: el.tagName,
                            text: el.innerText.substring(0, 50)
                        }))
                    };
                });
                
                console.log('   After register click:', JSON.stringify(afterClick, null, 2));
            }
        }

        // 6. Admin event management
        console.log('\n6. Testing admin event management...');
        await page.goto('http://localhost:5651/logout');
        await new Promise(resolve => setTimeout(resolve, 1500));

        await page.goto('http://localhost:5651/login', { waitUntil: 'networkidle2' });
        await new Promise(resolve => setTimeout(resolve, 1500));

        await page.type('input[type="email"]', 'admin@witchcityrope.com');
        await page.type('input[type="password"]', 'Test123!');
        
        const adminSubmit = await page.evaluateHandle(() => {
            const buttons = document.querySelectorAll('button');
            for (const button of buttons) {
                if (button.type === 'submit') return button;
            }
        });
        
        if (adminSubmit) {
            await adminSubmit.click();
            await new Promise(resolve => setTimeout(resolve, 3000));
        }

        // Navigate to admin events
        await page.goto('http://localhost:5651/admin/events', { waitUntil: 'networkidle2' });
        await new Promise(resolve => setTimeout(resolve, 2000));

        // Analyze admin page
        const adminPageStructure = await page.evaluate(() => {
            const createElements = [...document.querySelectorAll('a, button')].filter(el => 
                el.innerText && (el.innerText.toLowerCase().includes('create') || 
                                el.innerText.toLowerCase().includes('new'))
            );
            
            return {
                createButtonCount: createElements.length,
                createButtonInfo: createElements.map(el => ({
                    tag: el.tagName,
                    text: el.innerText,
                    href: el.href || 'none',
                    attributes: [...el.attributes].map(attr => `${attr.name}="${attr.value}"`)
                }))
            };
        });

        console.log('\n7. Admin events page structure:', JSON.stringify(adminPageStructure, null, 2));

        // Take final screenshot
        await page.screenshot({ path: 'event-flow-analysis.png', fullPage: true });
        console.log('\nScreenshot saved to event-flow-analysis.png');

    } catch (error) {
        console.error('Test error:', error);
    } finally {
        console.log('\nTest completed. Browser will remain open for inspection.');
        // Keep browser open for manual inspection
        await new Promise(resolve => setTimeout(resolve, 60000));
        await browser.close();
    }
}

testEventFlow().catch(console.error);