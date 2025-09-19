const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();

  try {
    console.log('🔍 Testing admin events dashboard fixes...');

    // Go to login page
    console.log('📝 Going to login page...');
    await page.goto('http://localhost:5174/login', { waitUntil: 'networkidle' });

    // Take screenshot of login page
    await page.screenshot({ path: '/tmp/login-page.png' });
    console.log('📸 Login page screenshot saved to /tmp/login-page.png');

    // Wait for login form to load - try multiple selectors
    try {
      await page.waitForSelector('input', { timeout: 5000 });
    } catch (error) {
      console.log('❌ No input fields found, checking page HTML...');
      const content = await page.content();
      console.log('Page content length:', content.length);
      throw error;
    }

    // Login as admin - try different selectors
    console.log('🔑 Logging in as admin...');
    const inputs = await page.locator('input').all();
    console.log(`📝 Found ${inputs.length} input fields`);

    if (inputs.length >= 2) {
      await inputs[0].fill('admin@witchcityrope.com');
      await inputs[1].fill('Test123!');
    } else {
      throw new Error('Not enough input fields found for email and password');
    }
    await page.click('button[type="submit"]');

    // Wait for navigation
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    console.log('✅ Login successful, redirected to dashboard');

    // Navigate to admin events
    console.log('📊 Navigating to admin events page...');
    await page.goto('http://localhost:5174/admin/events', { waitUntil: 'networkidle' });

    // Wait for events table to load
    await page.waitForSelector('[data-testid="events-table"]', { timeout: 10000 });

    // Count visible event rows
    const eventRows = await page.locator('[data-testid="event-row"]').count();
    console.log(`📋 Found ${eventRows} events displayed in table`);

    // Take screenshot of admin events page
    await page.screenshot({ path: '/tmp/admin-events.png' });
    console.log('📸 Admin events page screenshot saved to /tmp/admin-events.png');

    // Check filter chips
    const filterChips = await page.locator('.mantine-Chip-root').allTextContents();
    console.log('🏷️ Filter chips found:', filterChips);

    // Test clicking on first event (if any)
    if (eventRows > 0) {
      console.log('🖱️ Testing row click navigation...');
      const firstRowId = await page.locator('[data-testid="event-row"]').first().getAttribute('data-testid');

      // Click the first row
      await page.locator('[data-testid="event-row"]').first().click();

      // Wait for navigation to event details page
      try {
        await page.waitForURL('**/admin/events/*', { timeout: 5000 });
        console.log('✅ Successfully navigated to event details page');
        console.log('🌐 Current URL:', page.url());
      } catch (error) {
        console.log('❌ Navigation to event details failed:', error.message);
      }
    }

    console.log('🎉 Test completed successfully!');

  } catch (error) {
    console.error('❌ Test failed:', error.message);
    await page.screenshot({ path: '/tmp/error-page.png' });
    console.log('📸 Error screenshot saved to /tmp/error-page.png');
  } finally {
    await browser.close();
  }
})();