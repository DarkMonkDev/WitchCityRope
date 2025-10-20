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

    console.log('Logging in as admin...');
    await page.fill('input[type="email"]', 'admin@witchcityrope.com');
    await page.fill('input[type="password"]', 'Test123!');

    await page.click('button[type="submit"]');
    await page.waitForURL('**/dashboard', { timeout: 15000 });

    console.log('Navigating to admin vetting page...');
    await page.goto('http://localhost:5173/admin/vetting/applications', {
      waitUntil: 'networkidle',
      timeout: 30000
    });

    // Wait for the content to load
    await page.waitForSelector('[data-testid="vetting-applications-list"], .vetting-applications, h1, h2', { timeout: 10000 });

    console.log('Taking screenshot...');
    await page.screenshot({
      path: '/home/chad/repos/witchcityrope/session-work/2025-09-22/vetting-page-updated.png',
      fullPage: true
    });

    console.log('Screenshot saved successfully!');

  } catch (error) {
    console.error('Error taking screenshot:', error);
  } finally {
    await browser.close();
  }
}

takeVettingScreenshot();