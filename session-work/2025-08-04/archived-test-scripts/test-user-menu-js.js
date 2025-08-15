const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('=== Testing User Menu JavaScript ===\n');
    
    // Enable console logging
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.log('Browser console error:', msg.text());
      }
    });
    
    // 1. Login
    console.log('1. Logging in...');
    await page.goto('http://localhost:5651/Identity/Account/Login');
    await page.fill('input[name="Input.EmailOrUsername"]', 'admin@witchcityrope.com');
    await page.fill('input[name="Input.Password"]', 'Test123!');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(3000);
    
    // 2. Go to home page
    console.log('\n2. Navigating to home page...');
    await page.goto('http://localhost:5651/');
    await page.waitForTimeout(2000);
    
    // Check if JavaScript is loaded
    const hasAddClickOutsideListener = await page.evaluate(() => {
      return typeof window.addClickOutsideListener === 'function';
    });
    console.log('addClickOutsideListener function exists:', hasAddClickOutsideListener);
    
    // Try to manually toggle the dropdown
    console.log('\n3. Manually toggling dropdown via JavaScript...');
    await page.evaluate(() => {
      const dropdown = document.querySelector('.user-dropdown');
      if (dropdown) {
        dropdown.classList.add('show');
        return true;
      }
      return false;
    });
    
    await page.waitForTimeout(1000);
    await page.screenshot({ path: 'user-menu-manual-toggle.png' });
    
    // Check if dropdown is visible
    const dropdownVisible = await page.evaluate(() => {
      const dropdown = document.querySelector('.user-dropdown.show');
      if (dropdown) {
        const style = window.getComputedStyle(dropdown);
        return style.visibility === 'visible' && style.opacity === '1';
      }
      return false;
    });
    console.log('Dropdown visible after manual toggle:', dropdownVisible);
    
    // Check for event listeners
    console.log('\n4. Checking event listeners...');
    const hasClickListener = await page.evaluate(() => {
      const button = document.querySelector('.user-menu-btn');
      // This is a hack to check if Blazor attached event listeners
      return button && button.outerHTML.includes('onclick');
    });
    console.log('Button has onclick handler:', hasClickListener);
    
    console.log('\n✅ Screenshot saved as user-menu-manual-toggle.png');
    
  } catch (error) {
    console.log('❌ Error:', error.message);
  } finally {
    await page.waitForTimeout(5000);
    await browser.close();
  }
})();