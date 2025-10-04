import { test, expect } from '@playwright/test';

test('Visual check of events system', async ({ page }) => {
  // 1. Check events page
  await page.goto('http://localhost:5173/events');
  await page.waitForTimeout(2000);
  await page.screenshot({ path: 'test-results/current-events-page.png', fullPage: true });
  console.log('Events page captured');

  // 2. Check login page
  await page.goto('http://localhost:5173/login');
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'test-results/current-login-page.png', fullPage: true });
  
  // 3. Try to login
  await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
  await page.fill('[data-testid="password-input"]', 'Test123!');
  await page.click('[data-testid="login-button"]');
  await page.waitForTimeout(2000);
  
  // Take screenshot after login attempt
  await page.screenshot({ path: 'test-results/after-login-attempt.png', fullPage: true });
  const url = page.url();
  console.log('After login URL:', url);
  
  // 4. Try dashboard
  if (url.includes('dashboard')) {
    await page.screenshot({ path: 'test-results/dashboard-page.png', fullPage: true });
    console.log('Dashboard captured');
  }
  
  // 5. Check events page again
  await page.goto('http://localhost:5173/events');
  await page.waitForTimeout(2000);
  await page.screenshot({ path: 'test-results/events-after-login.png', fullPage: true });
});