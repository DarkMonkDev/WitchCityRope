import { test, expect } from '@playwright/test';

test.describe('Simple RSVP System Investigation', () => {

  test('1. Document what we actually see on the page', async ({ page }) => {
    console.log('🔍 Documenting actual page content');

    // Go to the main page
    await page.goto('http://localhost:5173');
    await page.waitForLoadState('networkidle');

    // Take screenshot of what we actually see
    await page.screenshot({ path: './test-results/01-actual-homepage.png', fullPage: true });

    // Get basic page info
    const title = await page.title();
    const rootContent = await page.locator('#root').innerHTML();

    console.log(`📊 Page title: "${title}"`);
    console.log(`📊 Root content length: ${rootContent.length} characters`);

    // Look for any login-related elements (without specific selectors)
    const allButtons = await page.locator('button').all();
    const buttonTexts = [];

    for (const button of allButtons) {
      const text = await button.textContent();
      if (text) buttonTexts.push(text.trim());
    }

    console.log(`📊 Found ${allButtons.length} buttons with texts: ${buttonTexts.join(', ')}`);

    // Look for any links
    const allLinks = await page.locator('a').all();
    const linkTexts = [];

    for (const link of allLinks) {
      const text = await link.textContent();
      if (text) linkTexts.push(text.trim());
    }

    console.log(`📊 Found ${allLinks.length} links with texts: ${linkTexts.slice(0, 10).join(', ')}`);

    // Check if there's any authentication state
    const bodyText = await page.textContent('body');
    const hasLogin = bodyText?.toLowerCase().includes('login');
    const hasLogout = bodyText?.toLowerCase().includes('logout');
    const hasUser = bodyText?.toLowerCase().includes('user');
    const hasAdmin = bodyText?.toLowerCase().includes('admin');

    console.log(`📊 Page contains: login=${hasLogin}, logout=${hasLogout}, user=${hasUser}, admin=${hasAdmin}`);

    // Save the full page HTML for analysis
    const fullHTML = await page.content();
    const htmlFile = './test-results/01-full-page-html.html';
    require('fs').writeFileSync(htmlFile, fullHTML);
    console.log(`📄 Full HTML saved to: ${htmlFile}`);
  });

  test('2. Test API endpoints directly', async ({ page }) => {
    console.log('🔍 Testing API endpoints directly');

    const endpoints = [
      'http://localhost:5655/health',
      'http://localhost:5655/api/events',
      'http://localhost:5655/api/admin/events',
      'http://localhost:5655/api/auth/user',
      'http://localhost:5655/api/dashboard/events'
    ];

    const results = [];

    for (const endpoint of endpoints) {
      try {
        const response = await page.request.get(endpoint);
        const status = response.status();

        let data = 'N/A';
        try {
          data = await response.json();
        } catch (e) {
          data = await response.text();
        }

        results.push({
          endpoint: endpoint.replace('http://localhost:5655/', ''),
          status: status,
          data: data
        });

        console.log(`📊 ${endpoint}: ${status}`);
        if (status === 200) {
          console.log(`    Data: ${JSON.stringify(data).substring(0, 200)}...`);
        }

      } catch (error) {
        results.push({
          endpoint: endpoint.replace('http://localhost:5655/', ''),
          status: 'ERROR',
          error: error.message
        });
        console.log(`❌ ${endpoint}: ${error.message}`);
      }
    }

    // Save API results
    const apiFile = './test-results/02-api-results.json';
    require('fs').writeFileSync(apiFile, JSON.stringify(results, null, 2));
    console.log(`📄 API results saved to: ${apiFile}`);
  });

  test('3. Try to interact with any visible elements', async ({ page }) => {
    console.log('🔍 Trying to interact with visible elements');

    await page.goto('http://localhost:5173');
    await page.waitForLoadState('networkidle');

    // Take initial screenshot
    await page.screenshot({ path: './test-results/03-before-interaction.png', fullPage: true });

    // Try clicking the first button (if any)
    const allButtons = await page.locator('button').all();

    if (allButtons.length > 0) {
      console.log(`📊 Found ${allButtons.length} buttons. Trying to click first one.`);

      const firstButton = allButtons[0];
      const buttonText = await firstButton.textContent();
      console.log(`📊 First button text: "${buttonText}"`);

      try {
        await firstButton.click();
        await page.waitForTimeout(2000);

        // Take screenshot after click
        await page.screenshot({ path: './test-results/03-after-first-button-click.png', fullPage: true });

        // Check if anything changed
        const newContent = await page.content();
        console.log(`📊 Button click resulted in content change`);

      } catch (error) {
        console.log(`❌ Failed to click first button: ${error.message}`);
      }
    }

    // Try accessing common paths
    const commonPaths = ['/events', '/dashboard', '/admin', '/login'];

    for (const path of commonPaths) {
      try {
        await page.goto(`http://localhost:5173${path}`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(1000);

        const pathSafe = path.replace('/', 'root');
        await page.screenshot({ path: `./test-results/03-path-${pathSafe}.png`, fullPage: true });

        const pageText = await page.textContent('body');
        const hasContent = pageText && pageText.length > 1000;

        console.log(`📊 ${path}: Has substantial content: ${hasContent}`);

      } catch (error) {
        console.log(`❌ Failed to access ${path}: ${error.message}`);
      }
    }
  });
});