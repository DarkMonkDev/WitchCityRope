const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('=== Debugging Dropdown Issue ===\n');
    
    // Enable console logging
    page.on('console', msg => console.log('Browser:', msg.text()));
    
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
    await page.waitForTimeout(3000);
    
    // Debug the button
    console.log('\n3. Debugging the button...');
    const buttonInfo = await page.evaluate(() => {
      const btn = document.querySelector('.user-menu-btn');
      if (!btn) return { exists: false };
      
      // Get all event listeners (this is a hack, won't show all)
      const hasOnclick = btn.hasAttribute('onclick');
      const blazorHandler = btn._blazorEvents_click ? true : false;
      
      return {
        exists: true,
        hasOnclick,
        blazorHandler,
        innerHTML: btn.innerHTML,
        className: btn.className,
        disabled: btn.disabled
      };
    });
    console.log('Button info:', JSON.stringify(buttonInfo, null, 2));
    
    // Try to click using JavaScript
    console.log('\n4. Clicking button via JavaScript...');
    await page.evaluate(() => {
      const btn = document.querySelector('.user-menu-btn');
      if (btn) {
        btn.click();
      }
    });
    await page.waitForTimeout(1000);
    
    // Check dropdown state
    const dropdownState = await page.evaluate(() => {
      const dropdown = document.querySelector('.user-dropdown');
      if (!dropdown) return { exists: false };
      
      return {
        exists: true,
        classList: Array.from(dropdown.classList),
        computedStyle: {
          visibility: window.getComputedStyle(dropdown).visibility,
          opacity: window.getComputedStyle(dropdown).opacity,
          display: window.getComputedStyle(dropdown).display
        }
      };
    });
    console.log('\nDropdown state:', JSON.stringify(dropdownState, null, 2));
    
    // Try Playwright click
    console.log('\n5. Clicking button via Playwright...');
    await page.click('.user-menu-btn');
    await page.waitForTimeout(1000);
    
    // Check dropdown state again
    const dropdownState2 = await page.evaluate(() => {
      const dropdown = document.querySelector('.user-dropdown');
      if (!dropdown) return { exists: false };
      
      return {
        exists: true,
        classList: Array.from(dropdown.classList),
        hasShow: dropdown.classList.contains('show')
      };
    });
    console.log('\nDropdown state after Playwright click:', JSON.stringify(dropdownState2, null, 2));
    
    await page.screenshot({ path: 'dropdown-debug.png' });
    console.log('\n✅ Screenshot saved as dropdown-debug.png');
    
  } catch (error) {
    console.log('❌ Error:', error.message);
  } finally {
    await page.waitForTimeout(5000);
    await browser.close();
  }
})();