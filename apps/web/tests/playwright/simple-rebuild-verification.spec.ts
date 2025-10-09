import { test, expect } from '@playwright/test';

test.describe('Simple Rebuild Verification', () => {
  test('Check admin dashboard loads after rebuild', async ({ page }) => {
    // Login as admin
    await page.goto('http://localhost:5173/login');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');

    // Wait for dashboard
    await page.waitForURL('**/dashboard', { timeout: 15000 });

    // Take screenshot of user dashboard
    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/user-dashboard-after-rebuild.png', fullPage: true });

    console.log('Current URL after login:', page.url());

    // Try to navigate to admin dashboard
    const adminLink = page.locator('text=Admin').first();
    if (await adminLink.count() > 0) {
      console.log('Admin link found, clicking...');
      await adminLink.click();
      await page.waitForTimeout(2000);
      await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/after-admin-click.png', fullPage: true });
      console.log('URL after admin click:', page.url());
    } else {
      console.log('Admin link not found - checking visible links...');
      const allLinks = page.locator('a');
      const linkCount = await allLinks.count();
      console.log(`Found ${linkCount} links on the page`);
    }

    // Verify page loaded
    await expect(page.locator('body')).toBeVisible();
  });

  test('Direct navigation to admin dashboard', async ({ page }) => {
    // Login as admin
    await page.goto('http://localhost:5173/login');
    await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password-input"]', 'Test123!');
    await page.click('[data-testid="login-button"]');

    await page.waitForURL('**/dashboard', { timeout: 15000 });

    // Try direct navigation
    console.log('Trying direct navigation to admin dashboard...');
    await page.goto('http://localhost:5173/admin/dashboard');
    await page.waitForTimeout(2000);

    await page.screenshot({ path: '/home/chad/repos/witchcityrope/test-results/admin-dashboard-direct-nav.png', fullPage: true });

    console.log('Current URL:', page.url());

    // Get page content
    const pageText = await page.locator('body').textContent();
    console.log('Page content preview:', pageText?.substring(0, 500));

    // Check for "Pending Vetting" text
    if (pageText?.includes('Pending Vetting')) {
      console.log('✅ Found "Pending Vetting" text - admin dashboard loaded');
    } else {
      console.log('❌ Did not find "Pending Vetting" text');
    }
  });

  test('API vetting endpoint check', async ({ request }) => {
    // Login first to get cookies
    const loginResponse = await request.post('http://localhost:5655/api/auth/login', {
      data: {
        email: 'admin@witchcityrope.com',
        password: 'Test123!'
      }
    });

    console.log('Login response status:', loginResponse.status());

    if (loginResponse.ok()) {
      // Try vetting endpoint
      const vettingResponse = await request.get('http://localhost:5655/api/admin/vetting/applications');
      console.log('Vetting API status:', vettingResponse.status());
      console.log('Vetting API status text:', vettingResponse.statusText());

      if (vettingResponse.ok()) {
        const data = await vettingResponse.json();
        console.log('Vetting applications count:', Array.isArray(data) ? data.length : 'not an array');
        if (Array.isArray(data) && data.length > 0) {
          console.log('First application preview:', JSON.stringify(data[0], null, 2).substring(0, 500));
        }
      } else {
        const errorText = await vettingResponse.text();
        console.log('Vetting API error:', errorText);
      }
    }
  });
});
