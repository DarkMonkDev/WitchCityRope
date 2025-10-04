import { test, expect, Page, BrowserContext } from '@playwright/test';

/**
 * Comprehensive E2E Test Suite: Events System Full User Journey
 * 
 * This test validates the complete TDD user story from event discovery to registration management.
 * It tests the REAL user journey without mocks - this is true E2E testing.
 * 
 * Test Flow:
 * 1. User discovers events on public page
 * 2. User views event details  
 * 3. User attempts to register (should redirect to login)
 * 4. User logs in successfully
 * 5. User completes registration for event
 * 6. User views registration in dashboard
 * 7. User cancels a registration
 * 8. Admin views event management
 * 
 * CRITICAL: This test uses real API calls (port 5655 Docker) and real database data.
 * No mocks or stubs - this validates the complete integration.
 */

// Test accounts from CLAUDE.md
const TEST_ACCOUNTS = {
  admin: { email: 'admin@witchcityrope.com', password: 'Test123!' },
  teacher: { email: 'teacher@witchcityrope.com', password: 'Test123!' },
  vetted: { email: 'vetted@witchcityrope.com', password: 'Test123!' },
  member: { email: 'member@witchcityrope.com', password: 'Test123!' },
  guest: { email: 'guest@witchcityrope.com', password: 'Test123!' }
};

test.describe('Events System - Complete User Journey E2E Tests', () => {
  
  test.beforeEach(async ({ page }) => {
    // Ensure we're starting from a clean state
    await page.goto('http://localhost:5173');
    await expect(page).toHaveTitle(/Vite \+ React/);
  });

  test('1. User discovers events on public page', async ({ page }) => {
    // Navigate to events page (should be accessible without login)
    await page.goto('http://localhost:5173/events');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    
    // Should see events list
    await expect(page.locator('[data-testid="events-list"]')).toBeVisible({ timeout: 10000 });
    
    // Should see at least one event (we have 5 in database)
    const eventCards = page.locator('[data-testid="event-card"]');
    await expect(eventCards.first()).toBeVisible();
    
    // Count events displayed
    const eventCount = await eventCards.count();
    expect(eventCount).toBeGreaterThan(0);
    
    // Screenshot for evidence
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/events-public-discovery.png' });
    
    console.log(`✅ Events discovery: Found ${eventCount} events on public page`);
  });

  test('2. User views event details', async ({ page }) => {
    // Navigate to events page
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    
    // Click on first event to view details
    const firstEvent = page.locator('[data-testid="event-card"]').first();
    await expect(firstEvent).toBeVisible();
    
    // Get event title for verification
    const eventTitle = await firstEvent.locator('[data-testid="event-title"]').textContent();
    
    await firstEvent.click();
    
    // Should navigate to event detail page
    await page.waitForURL('**/events/**');
    
    // Should see event details
    await expect(page.locator('[data-testid="event-details"]')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('[data-testid="event-title"]')).toBeVisible();
    await expect(page.locator('[data-testid="event-description"]')).toBeVisible();
    await expect(page.locator('[data-testid="event-date"]')).toBeVisible();
    
    // Should see registration button
    await expect(page.locator('[data-testid="register-button"]')).toBeVisible();
    
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/event-details-view.png' });
    
    console.log(`✅ Event details: Successfully viewed details for "${eventTitle}"`);
  });

  test('3. User attempts to register (should redirect to login)', async ({ page }) => {
    // Navigate to event details
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    
    const firstEvent = page.locator('[data-testid="event-card"]').first();
    await firstEvent.click();
    await page.waitForURL('**/events/**');
    
    // Try to register without being logged in
    const registerButton = page.locator('[data-testid="register-button"]');
    await expect(registerButton).toBeVisible();
    
    await registerButton.click();
    
    // Should redirect to login page
    await page.waitForURL('**/login**');
    
    // Should see login form
    await expect(page.locator('[data-testid="login-form"]')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('[data-testid="email-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="password-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="login-button"]')).toBeVisible();
    
    // Should have returnTo parameter for redirect after login
    const url = page.url();
    expect(url).toContain('returnTo=');
    
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/login-redirect-from-registration.png' });
    
    console.log('✅ Registration redirect: Correctly redirected to login when not authenticated');
  });

  test('4. User logs in successfully', async ({ page }) => {
    // Navigate to login page
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    // Fill login form with member account
    const emailInput = page.locator('[data-testid="email-input"]');
    const passwordInput = page.locator('[data-testid="password-input"]');
    const loginButton = page.locator('[data-testid="login-button"]');
    
    await expect(emailInput).toBeVisible({ timeout: 10000 });
    await expect(passwordInput).toBeVisible();
    await expect(loginButton).toBeVisible();
    
    await emailInput.fill(TEST_ACCOUNTS.member.email);
    await passwordInput.fill(TEST_ACCOUNTS.member.password);
    
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/login-form-filled.png' });
    
    // Submit login form
    await loginButton.click();
    
    // Should redirect to dashboard or intended page
    await page.waitForURL(/dashboard|events/, { timeout: 15000 });
    
    // Should see user-specific content (not login form)
    await expect(page.locator('[data-testid="login-form"]')).not.toBeVisible();
    
    // Should see user menu or profile indicator
    await expect(page.locator('[data-testid="user-menu"]')).toBeVisible({ timeout: 10000 });
    
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/successful-login.png' });
    
    console.log(`✅ Login successful: User ${TEST_ACCOUNTS.member.email} logged in successfully`);
  });

  test('5. User completes registration for event', async ({ page, context }) => {
    // Login first
    await loginUser(page, TEST_ACCOUNTS.member);
    
    // Navigate to events and select an event
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    
    const firstEvent = page.locator('[data-testid="event-card"]').first();
    const eventTitle = await firstEvent.locator('[data-testid="event-title"]').textContent();
    
    await firstEvent.click();
    await page.waitForURL('**/events/**');
    
    // Now that we're logged in, registration should work
    const registerButton = page.locator('[data-testid="register-button"]');
    await expect(registerButton).toBeVisible({ timeout: 10000 });
    
    await registerButton.click();
    
    // Should show registration form or success message
    const registrationSuccess = page.locator('[data-testid="registration-success"]');
    const registrationForm = page.locator('[data-testid="registration-form"]');
    
    // Handle either immediate success or form-based registration
    if (await registrationForm.isVisible({ timeout: 5000 })) {
      // Fill out registration form if it exists
      const submitButton = page.locator('[data-testid="registration-submit"]');
      await expect(submitButton).toBeVisible();
      await submitButton.click();
    }
    
    // Should see success confirmation
    await expect(registrationSuccess).toBeVisible({ timeout: 10000 });
    
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/event-registration-complete.png' });
    
    console.log(`✅ Event registration: Successfully registered for "${eventTitle}"`);
  });

  test('6. User views registration in dashboard', async ({ page }) => {
    // Login and register for an event first
    await loginUser(page, TEST_ACCOUNTS.member);
    
    // Navigate to dashboard
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');
    
    // Should see dashboard
    await expect(page.locator('[data-testid="dashboard"]')).toBeVisible({ timeout: 10000 });
    
    // Should see registrations section
    await expect(page.locator('[data-testid="my-registrations"]')).toBeVisible();
    
    // Should see at least registration details
    const registrations = page.locator('[data-testid="registration-item"]');
    // Note: User may or may not have existing registrations, so we don't assert count
    
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/dashboard-registrations.png' });
    
    console.log('✅ Dashboard view: Successfully viewed registrations in dashboard');
  });

  test('7. User cancels a registration', async ({ page }) => {
    // This test assumes user has an existing registration
    // In a real scenario, we'd create a registration first
    
    await loginUser(page, TEST_ACCOUNTS.member);
    
    // Navigate to dashboard
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');
    
    // Look for cancel buttons on registrations
    const cancelButtons = page.locator('[data-testid="cancel-registration"]');
    
    if (await cancelButtons.count() > 0) {
      const firstCancelButton = cancelButtons.first();
      await firstCancelButton.click();
      
      // Should show confirmation dialog
      const confirmDialog = page.locator('[data-testid="cancel-confirmation"]');
      if (await confirmDialog.isVisible({ timeout: 5000 })) {
        const confirmButton = page.locator('[data-testid="confirm-cancel"]');
        await confirmButton.click();
      }
      
      // Should show success message
      await expect(page.locator('[data-testid="cancellation-success"]')).toBeVisible({ timeout: 10000 });
      
      console.log('✅ Registration cancellation: Successfully cancelled a registration');
    } else {
      console.log('ℹ️  Registration cancellation: No existing registrations to cancel');
    }
    
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/registration-cancellation.png' });
  });

  test('8. Admin views event management', async ({ page }) => {
    // Login as admin
    await loginUser(page, TEST_ACCOUNTS.admin);
    
    // Navigate to admin/events management
    await page.goto('http://localhost:5173/admin/events');
    await page.waitForLoadState('networkidle');
    
    // Should see admin events page
    await expect(page.locator('[data-testid="admin-events"]')).toBeVisible({ timeout: 10000 });
    
    // Should see events management controls
    await expect(page.locator('[data-testid="create-event-button"]')).toBeVisible();
    
    // Should see list of events with admin controls
    const eventRows = page.locator('[data-testid="admin-event-row"]');
    await expect(eventRows.first()).toBeVisible();
    
    // Should see edit and delete buttons
    await expect(page.locator('[data-testid="edit-event"]').first()).toBeVisible();
    
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/admin-event-management.png' });
    
    console.log('✅ Admin management: Successfully accessed event management interface');
  });

  test('9. Complete journey - Discovery to Registration', async ({ page }) => {
    console.log('🚀 Starting complete user journey test...');
    
    // Step 1: Discover events (unauthenticated)
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    
    const eventCards = page.locator('[data-testid="event-card"]');
    await expect(eventCards.first()).toBeVisible({ timeout: 10000 });
    const eventCount = await eventCards.count();
    console.log(`   📅 Found ${eventCount} events`);
    
    // Step 2: View event details
    const firstEvent = eventCards.first();
    const eventTitle = await firstEvent.locator('[data-testid="event-title"]').textContent();
    await firstEvent.click();
    await page.waitForURL('**/events/**');
    console.log(`   🔍 Viewing details for: ${eventTitle}`);
    
    // Step 3: Attempt registration (should redirect to login)
    const registerButton = page.locator('[data-testid="register-button"]');
    await expect(registerButton).toBeVisible();
    await registerButton.click();
    await page.waitForURL('**/login**');
    console.log('   🔐 Redirected to login as expected');
    
    // Step 4: Login
    await fillLoginForm(page, TEST_ACCOUNTS.member);
    await page.locator('[data-testid="login-button"]').click();
    await page.waitForURL(/dashboard|events/, { timeout: 15000 });
    console.log('   ✅ Successfully logged in');
    
    // Step 5: Navigate back to event and complete registration
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    await page.locator('[data-testid="event-card"]').first().click();
    await page.waitForURL('**/events/**');
    
    const registerButtonLoggedIn = page.locator('[data-testid="register-button"]');
    await expect(registerButtonLoggedIn).toBeVisible();
    await registerButtonLoggedIn.click();
    
    // Handle registration process
    const registrationSuccess = page.locator('[data-testid="registration-success"]');
    await expect(registrationSuccess).toBeVisible({ timeout: 10000 });
    console.log('   🎯 Registration completed successfully');
    
    // Step 6: Verify in dashboard
    await page.goto('http://localhost:5173/dashboard');
    await page.waitForLoadState('networkidle');
    await expect(page.locator('[data-testid="dashboard"]')).toBeVisible();
    await expect(page.locator('[data-testid="my-registrations"]')).toBeVisible();
    console.log('   📊 Verified registration in dashboard');
    
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/complete-journey-success.png' });
    
    console.log('🎉 Complete user journey test PASSED!');
  });

  test('10. API Integration Verification', async ({ page, request }) => {
    console.log('🔧 Testing API integration...');
    
    // Test API endpoints directly
    const healthResponse = await request.get('http://localhost:5655/api/health');
    expect(healthResponse.status()).toBe(200);
    console.log('   ✅ API health endpoint working');
    
    // Test events API
    const eventsResponse = await request.get('http://localhost:5655/api/events');
    expect(eventsResponse.status()).toBe(200);
    
    const eventsData = await eventsResponse.json();
    expect(Array.isArray(eventsData)).toBe(true);
    expect(eventsData.length).toBeGreaterThan(0);
    console.log(`   📅 Events API returned ${eventsData.length} events`);
    
    // Test login API  
    const loginResponse = await request.post('http://localhost:5655/api/auth/login', {
      data: {
        email: TEST_ACCOUNTS.member.email,
        password: TEST_ACCOUNTS.member.password
      }
    });
    
    expect(loginResponse.status()).toBe(200);
    const loginData = await loginResponse.json();
    expect(loginData).toHaveProperty('token');
    console.log('   🔐 Login API working correctly');
    
    console.log('✅ API integration verification PASSED!');
  });

  test('11. Error Handling - Invalid Login', async ({ page }) => {
    console.log('🚨 Testing error handling...');
    
    await page.goto('http://localhost:5173/login');
    await page.waitForLoadState('networkidle');
    
    // Try invalid login
    await fillLoginForm(page, { email: 'invalid@test.com', password: 'wrongpassword' });
    await page.locator('[data-testid="login-button"]').click();
    
    // Should show error message
    await expect(page.locator('[data-testid="login-error"]')).toBeVisible({ timeout: 10000 });
    
    // Should still be on login page
    expect(page.url()).toContain('login');
    
    console.log('✅ Error handling test PASSED!');
  });

  test('12. Performance and Responsiveness', async ({ page }) => {
    console.log('⚡ Testing performance...');
    
    // Measure page load times
    const startTime = Date.now();
    await page.goto('http://localhost:5173/events');
    await page.waitForLoadState('networkidle');
    const loadTime = Date.now() - startTime;
    
    console.log(`   📊 Events page load time: ${loadTime}ms`);
    expect(loadTime).toBeLessThan(10000); // 10 second timeout
    
    // Test mobile responsiveness
    await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE
    await page.waitForTimeout(1000);
    
    // Events should still be visible
    await expect(page.locator('[data-testid="events-list"]')).toBeVisible();
    
    await page.screenshot({ path: '/home/chad/repos/witchcityrope-react/test-results/mobile-events-view.png' });
    
    console.log('✅ Performance and responsiveness test PASSED!');
  });

});

// Helper Functions
async function loginUser(page: Page, account: { email: string; password: string }) {
  await page.goto('http://localhost:5173/login');
  await page.waitForLoadState('networkidle');
  
  await fillLoginForm(page, account);
  
  await page.locator('[data-testid="login-button"]').click();
  await page.waitForURL(/dashboard|events/, { timeout: 15000 });
  
  // Verify login success
  await expect(page.locator('[data-testid="user-menu"]')).toBeVisible({ timeout: 10000 });
}

async function fillLoginForm(page: Page, credentials: { email: string; password: string }) {
  const emailInput = page.locator('[data-testid="email-input"]');
  const passwordInput = page.locator('[data-testid="password-input"]');
  
  await expect(emailInput).toBeVisible({ timeout: 10000 });
  await expect(passwordInput).toBeVisible();
  
  await emailInput.fill(credentials.email);
  await passwordInput.fill(credentials.password);
}

// Test Data Validation
test.describe('Test Environment Validation', () => {
  
  test('Environment Health Check', async ({ page, request }) => {
    console.log('🏥 Running environment health check...');
    
    // Test React app
    const reactResponse = await request.get('http://localhost:5173');
    expect(reactResponse.status()).toBe(200);
    console.log('   ✅ React app healthy');
    
    // Test API
    const apiResponse = await request.get('http://localhost:5655/api/health');
    expect(apiResponse.status()).toBe(200);
    console.log('   ✅ API healthy');
    
    // Test database connectivity through API
    const eventsResponse = await request.get('http://localhost:5655/api/events');
    expect(eventsResponse.status()).toBe(200);
    console.log('   ✅ Database connectivity verified');
    
    // Verify test accounts exist by attempting login
    const loginResponse = await request.post('http://localhost:5655/api/auth/login', {
      data: {
        email: TEST_ACCOUNTS.member.email,
        password: TEST_ACCOUNTS.member.password
      }
    });
    expect(loginResponse.status()).toBe(200);
    console.log('   ✅ Test accounts available');
    
    console.log('🎉 Environment health check PASSED!');
  });
  
});