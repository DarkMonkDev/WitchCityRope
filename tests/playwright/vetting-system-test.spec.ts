import { test, expect } from '@playwright/test';

test.describe('Vetting System Vertical Slice', () => {
  
  test('Environment Health Check', async ({ page }) => {
    // Verify React app loads
    await page.goto('http://localhost:5173');
    await expect(page).toHaveTitle(/Witch City Rope/);
    
    // Verify API health
    const apiResponse = await page.request.get('http://localhost:5655/health');
    expect(apiResponse.status()).toBe(200);
    
    // Verify vetting API health
    const vettingResponse = await page.request.get('http://localhost:5655/api/vetting/health');
    expect(vettingResponse.status()).toBe(200);
    
    console.log('✅ Environment health check passed');
  });

  test('Vetting API Endpoints', async ({ page }) => {
    // Test my-application endpoint (should require auth)
    const myAppResponse = await page.request.get('http://localhost:5655/api/vetting/my-application');
    expect(myAppResponse.status()).toBe(401); // Expected - requires auth
    
    // Test simplified application endpoint (should require auth)
    const submitResponse = await page.request.post('http://localhost:5655/api/vetting/applications/simplified', {
      data: {
        realName: "Test User",
        preferredSceneName: "TestScene", 
        fetLifeHandle: "testhandle",
        email: "test@example.com",
        experienceWithRope: "Test experience",
        safetyTraining: "Test training",
        agreeToCommunityStandards: true
      }
    });
    expect(submitResponse.status()).toBe(401); // Expected - requires auth
    
    console.log('✅ Vetting API endpoints respond correctly');
  });

  test('Authentication and Vetting Flow', async ({ page }) => {
    // Navigate to homepage
    await page.goto('http://localhost:5173');
    
    // Login as admin user  
    const loginButton = page.locator('[data-testid="login-button"]').first();
    if (await loginButton.isVisible()) {
      await loginButton.click();
      
      await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
      await page.fill('[data-testid="password-input"]', 'Test123!');
      await page.click('[data-testid="login-button"]');
      
      // Wait for navigation
      await page.waitForURL(/dashboard/);
      console.log('✅ Login successful');
    }
    
    // Test authenticated vetting API call
    const context = page.context();
    const myAppResponse = await context.request.get('http://localhost:5655/api/vetting/my-application');
    expect(myAppResponse.status()).toBe(200);
    
    const responseData = await myAppResponse.json();
    console.log('✅ Authenticated vetting API call successful:', responseData);
  });

  test('Database Verification', async ({ page }) => {
    // This will be verified via API responses in the authenticated test
    const healthResponse = await page.request.get('http://localhost:5655/api/vetting/health');
    const healthData = await healthResponse.json();
    
    expect(healthData.status).toBe('Healthy');
    console.log('✅ Database verification passed via API health');
  });
});
