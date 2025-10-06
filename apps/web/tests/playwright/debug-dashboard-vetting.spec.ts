import { test, expect } from '@playwright/test';

test('debug dashboard vetting status for test users', async ({ page, context }) => {
  const testUsers = [
    { email: 'vetted@witchcityrope.com', password: 'Test123!', name: 'Vetted' },
    { email: 'member@witchcityrope.com', password: 'Test123!', name: 'Member' },
    { email: 'guest@witchcityrope.com', password: 'Test123!', name: 'Guest' }
  ];

  for (const user of testUsers) {
    console.log(`\n=== Testing ${user.name} (${user.email}) ===`);

    // Login
    await page.goto('http://localhost:5173/login');
    await page.locator('[data-testid="email-input"]').fill(user.email);
    await page.locator('[data-testid="password-input"]').fill(user.password);

    // Intercept API calls
    const apiCalls: any[] = [];
    page.on('response', async (response) => {
      if (response.url().includes('/api/')) {
        try {
          const body = await response.text();
          apiCalls.push({
            url: response.url(),
            status: response.status(),
            body: body.substring(0, 500)
          });
        } catch (e) {
          // Ignore parse errors
        }
      }
    });

    await page.locator('[data-testid="login-button"]').click();
    await page.waitForTimeout(2000);

    // Take screenshot of dashboard
    const filename = `dashboard-${user.name.toLowerCase()}.png`;
    await page.screenshot({ path: `test-results/${filename}`, fullPage: true });
    console.log(`Screenshot saved: test-results/${filename}`);

    // Check vetting status section
    const vettingText = await page.textContent('body');
    console.log(`Dashboard contains "Submit Vetting Application": ${vettingText?.includes('Submit Vetting Application')}`);
    console.log(`Dashboard contains "Under Review": ${vettingText?.includes('Under Review')}`);
    console.log(`Dashboard contains "Ready to join": ${vettingText?.includes('Ready to join')}`);
    console.log(`Dashboard contains "Approved": ${vettingText?.includes('Approved')}`);

    // Print API calls
    console.log(`\nAPI Calls for ${user.name}:`);
    for (const call of apiCalls) {
      console.log(`  ${call.status} ${call.url}`);
      if (call.url.includes('dashboard') || call.url.includes('vetting')) {
        console.log(`  Response: ${call.body}`);
      }
    }

    // Logout
    await context.clearCookies();
    await page.goto('http://localhost:5173');
  }
});
