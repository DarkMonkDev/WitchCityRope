const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  // Navigate to login
  await page.goto('http://localhost:5651/login');
  
  // Login as admin
  await page.fill('input[name="Input.Email"]', 'admin@witchcityrope.com');
  await page.fill('input[name="Input.Password"]', 'Test123\!');
  await page.click('button[type="submit"]');
  
  // Wait for navigation
  await page.waitForURL(/\/admin/, { timeout: 10000 });
  
  // Take screenshot
  await page.screenshot({ path: 'admin-dashboard-current-state.png', fullPage: true });
  
  console.log('Screenshot saved to admin-dashboard-current-state.png');
  
  await browser.close();
})();
EOF < /dev/null
