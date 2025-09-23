const { chromium } = require('playwright');

async function takeVettingScreenshot() {
  console.log('Starting Playwright browser...');
  const browser = await chromium.launch({
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  try {
    const context = await browser.newContext({
      viewport: { width: 1400, height: 900 }
    });

    const page = await context.newPage();

    console.log('Navigating to login page...');
    await page.goto('http://localhost:5173/login', {
      waitUntil: 'networkidle',
      timeout: 30000
    });

    // Just take a screenshot of the current page to see what's there
    console.log('Taking login page screenshot...');
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope-react/session-work/2025-09-22/current-login-page.png',
      fullPage: true
    });

    // Try to find any email input variant
    const emailInputs = await page.$$('input[type="email"], input[name="email"], input[placeholder*="email" i], [data-testid*="email"]');
    console.log(`Found ${emailInputs.length} email input elements`);

    if (emailInputs.length > 0) {
      console.log('Found email input, trying to login...');
      await emailInputs[0].fill('admin@witchcityrope.com');

      // Try to find password input
      const passwordInputs = await page.$$('input[type="password"], input[name="password"], [data-testid*="password"]');
      if (passwordInputs.length > 0) {
        await passwordInputs[0].fill('Test123!');

        // Try to find submit button
        const submitButtons = await page.$$('button[type="submit"], button:has-text("login"), button:has-text("sign in")');
        if (submitButtons.length > 0) {
          await submitButtons[0].click();

          // Wait for navigation or dashboard
          try {
            await page.waitForURL('**/dashboard', { timeout: 15000 });
            console.log('Navigated to dashboard, going to vetting page...');

            await page.goto('http://localhost:5173/admin/vetting', {
              waitUntil: 'networkidle',
              timeout: 30000
            });

            // Wait for content to load
            await page.waitForTimeout(3000);

            console.log('Taking vetting page screenshot...');
            await page.screenshot({
              path: '/home/chad/repos/witchcityrope-react/session-work/2025-09-22/vetting-page-updated.png',
              fullPage: true
            });

            console.log('Screenshot saved successfully!');
          } catch (navError) {
            console.log('Navigation to dashboard failed, taking current page screenshot...');
            await page.screenshot({
              path: '/home/chad/repos/witchcityrope-react/session-work/2025-09-22/after-login-attempt.png',
              fullPage: true
            });
          }
        } else {
          console.log('No submit button found');
        }
      } else {
        console.log('No password input found');
      }
    } else {
      console.log('No email input found - taking debug screenshot');
      await page.screenshot({
        path: '/home/chad/repos/witchcityrope-react/session-work/2025-09-22/login-page-debug.png',
        fullPage: true
      });
    }

  } catch (error) {
    console.error('Error taking screenshot:', error);
    // Take screenshot of current state for debugging
    try {
      await page.screenshot({
        path: '/home/chad/repos/witchcityrope-react/session-work/2025-09-22/error-state.png',
        fullPage: true
      });
    } catch (e) {
      console.log('Could not take error screenshot');
    }
  } finally {
    await browser.close();
  }
}

takeVettingScreenshot();