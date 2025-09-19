import { test, expect } from '@playwright/test';

test('capture working dashboard screenshot', async ({ page }) => {
  console.log('🔗 Navigating to login page...');
  await page.goto('http://localhost:5173/login');
  
  console.log('📝 Filling login form...');
  await page.fill('input[name="email"]', 'admin@witchcityrope.com');
  await page.fill('input[name="password"]', 'Test123!');
  
  console.log('🚀 Submitting login...');
  await page.click('button[type="submit"]');
  
  // Wait for navigation to dashboard
  await page.waitForURL('**/dashboard');
  
  console.log('📸 Taking dashboard screenshot...');
  await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/working-dashboard-2025-09-18.png', fullPage: true });
  
  console.log('✅ Dashboard screenshot captured successfully');
});
