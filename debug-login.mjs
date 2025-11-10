import { chromium } from 'playwright';

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  console.log('Navigating to login page...');
  await page.goto('http://localhost:5173/login');
  await page.waitForLoadState('networkidle');
  
  // Wait a bit for React to render
  await page.waitForTimeout(2000);
  
  console.log('Taking screenshot...');
  await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/login-page-debug.png', fullPage: true });
  
  // Check what selectors exist
  const form = await page.locator('[data-testid="login-form"]').count();
  const emailInput = await page.locator('[data-testid="email-or-scenename-input"]').count();
  const passwordInput = await page.locator('[data-testid="password-input"]').count();
  const loginButton = await page.locator('[data-testid="login-button"]').count();
  
  console.log('Selector counts:');
  console.log('  login-form:', form);
  console.log('  email-or-scenename-input:', emailInput);
  console.log('  password-input:', passwordInput);
  console.log('  login-button:', loginButton);
  
  // Try to fill and submit
  if (form > 0 && emailInput > 0 && passwordInput > 0 && loginButton > 0) {
    console.log('\nAttempting login...');
    await page.locator('[data-testid="email-or-scenename-input"]').fill('admin@witchcityrope.com');
    await page.locator('[data-testid="password-input"]').fill('Test123!');
    
    // Check values were set
    const emailValue = await page.locator('[data-testid="email-or-scenename-input"]').inputValue();
    const passwordValue = await page.locator('[data-testid="password-input"]').inputValue();
    console.log('  Email value:', emailValue);
    console.log('  Password value:', passwordValue);
    
    await page.locator('[data-testid="login-button"]').click();
    await page.waitForTimeout(3000);
    
    console.log('\nCurrent URL:', page.url());
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/login-after-submit.png', fullPage: true });
  } else {
    console.log('\n❌ Some selectors missing!');
  }
  
  await browser.close();
})();
