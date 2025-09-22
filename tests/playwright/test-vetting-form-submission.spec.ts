import { test, expect } from '@playwright/test';

test.describe('Vetting Form Submission Test', () => {
  test('Test full vetting form interaction and API communication', async ({ page }) => {
    console.log('🔍 Testing vetting form submission flow...');

    // Navigate to join page
    await page.goto('http://localhost:5173/join');

    // Wait for page to load
    await page.waitForTimeout(2000);

    // Take screenshot of join page
    await page.screenshot({ path: '/tmp/join-form.png' });

    // Monitor network requests
    const apiRequests = [];
    page.on('request', (request) => {
      if (request.url().includes('/api/')) {
        apiRequests.push({
          method: request.method(),
          url: request.url(),
          postData: request.postData()
        });
        console.log(`🌐 API Request: ${request.method()} ${request.url()}`);
      }
    });

    const apiResponses = [];
    page.on('response', (response) => {
      if (response.url().includes('/api/')) {
        apiResponses.push({
          status: response.status(),
          url: response.url(),
          statusText: response.statusText()
        });
        console.log(`📡 API Response: ${response.status()} ${response.url()}`);
      }
    });

    // Monitor console errors
    const consoleErrors = [];
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
        console.log(`🚨 Console Error: ${msg.text()}`);
      }
    });

    // Check for form elements
    const formInputs = await page.locator('input, textarea, select').count();
    console.log(`📍 Found ${formInputs} form inputs`);

    // Look for submit button
    const submitButtons = await page.locator('button[type="submit"], button:has-text("Submit"), button:has-text("Apply")').count();
    console.log(`📍 Found ${submitButtons} submit buttons`);

    // Try to fill out a minimal form
    try {
      // Look for email field
      const emailField = page.locator('input[type="email"], input[name*="email"], [data-testid*="email"]').first();
      if (await emailField.isVisible()) {
        await emailField.fill('test@example.com');
        console.log('✅ Filled email field');
      }

      // Look for name fields
      const nameField = page.locator('input[name*="name"], input[placeholder*="name"], [data-testid*="name"]').first();
      if (await nameField.isVisible()) {
        await nameField.fill('Test User');
        console.log('✅ Filled name field');
      }

      // Try to submit form
      const submitButton = page.locator('button[type="submit"], button:has-text("Submit"), button:has-text("Apply")').first();
      if (await submitButton.isVisible()) {
        console.log('🔴 Attempting form submission...');
        await submitButton.click();

        // Wait for API request
        await page.waitForTimeout(3000);

        console.log(`📊 API requests made: ${apiRequests.length}`);
        console.log(`📊 API responses received: ${apiResponses.length}`);
        console.log(`📊 Console errors: ${consoleErrors.length}`);

        // Report details
        apiRequests.forEach((req, i) => {
          console.log(`   Request ${i + 1}: ${req.method} ${req.url}`);
        });

        apiResponses.forEach((res, i) => {
          console.log(`   Response ${i + 1}: ${res.status} ${res.url}`);
        });

        consoleErrors.forEach((error, i) => {
          console.log(`   Error ${i + 1}: ${error}`);
        });
      } else {
        console.log('❌ No submit button found');
      }

    } catch (error) {
      console.log(`❌ Error during form interaction: ${error.message}`);
    }

    // Take final screenshot
    await page.screenshot({ path: '/tmp/after-submission.png' });
  });

  test('Check vetting API endpoints from browser context', async ({ page }) => {
    console.log('🔍 Testing API endpoints from browser context...');

    // Test API health from browser
    const healthResponse = await page.evaluate(async () => {
      try {
        const response = await fetch('http://localhost:5655/health');
        return {
          status: response.status,
          ok: response.ok,
          data: await response.text()
        };
      } catch (error) {
        return { error: error.message };
      }
    });

    console.log(`📍 Health API from browser: ${JSON.stringify(healthResponse)}`);

    // Test vetting health from browser
    const vettingHealthResponse = await page.evaluate(async () => {
      try {
        const response = await fetch('http://localhost:5655/api/vetting/health');
        return {
          status: response.status,
          ok: response.ok,
          data: await response.text()
        };
      } catch (error) {
        return { error: error.message };
      }
    });

    console.log(`📍 Vetting Health API from browser: ${JSON.stringify(vettingHealthResponse)}`);

    // Test vetting POST from browser
    const vettingPostResponse = await page.evaluate(async () => {
      try {
        const response = await fetch('http://localhost:5655/api/vetting/applications', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({})
        });
        return {
          status: response.status,
          ok: response.ok,
          data: await response.text()
        };
      } catch (error) {
        return { error: error.message };
      }
    });

    console.log(`📍 Vetting POST API from browser: ${JSON.stringify(vettingPostResponse)}`);
  });
});