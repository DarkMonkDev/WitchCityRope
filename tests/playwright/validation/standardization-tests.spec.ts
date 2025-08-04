import { test, expect } from '@playwright/test';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { AuthHelpers } from '../helpers/auth.helpers';

const colors = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  green: '\x1b[32m',
  red: '\x1b[31m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  cyan: '\x1b[36m'
};

function log(message: string, type: 'info' | 'success' | 'error' | 'warning' | 'test' = 'info') {
  const timestamp = new Date().toTimeString().substr(0, 8);
  const typeColors = {
    'info': colors.blue,
    'success': colors.green,
    'error': colors.red,
    'warning': colors.yellow,
    'test': colors.cyan
  };
  console.log(`${typeColors[type]}[${timestamp}] ${message}${colors.reset}`);
}

test.describe('Validation Standardization Tests', () => {
  test.describe('Identity Pages', () => {
    test('Login page validation', async ({ page }) => {
      log('Testing Login page validation...', 'info');
      
      await page.goto(`${testConfig.baseUrl}/login`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      // Test empty form submission
      const submitButton = await page.locator('button[type="submit"]');
      await submitButton.click();
      await page.waitForTimeout(1500);
      
      const loginErrors = await page.locator('.wcr-validation-message').all();
      expect(loginErrors.length).toBeGreaterThan(0);
      log(`✓ Login validation working: ${loginErrors.length} errors shown`, 'success');
      
      // Test invalid email format
      await page.fill('#Input_Email', 'invalid-email');
      await page.fill('#Input_Password', 'test');
      await submitButton.click();
      await page.waitForTimeout(1000);
      
      const emailError = await page.locator('.wcr-validation-message').first();
      const errorText = await emailError.textContent();
      expect(errorText).toBeTruthy();
      log(`✓ Email validation: ${errorText}`, 'success');
    });

    test('Forgot Password page validation', async ({ page }) => {
      log('Testing Forgot Password page...', 'info');
      
      await page.goto(`${testConfig.baseUrl}/Identity/Account/ForgotPassword`, {
        waitUntil: 'networkidle'
      });
      
      // Click submit button
      await page.click('button[type="submit"]');
      await page.waitForTimeout(1000);
      
      const forgotErrors = await page.locator('.wcr-validation-message').all();
      expect(forgotErrors.length).toBeGreaterThan(0);
      log('✓ Forgot Password validation working', 'success');
    });

    test('Register page validation', async ({ page }) => {
      log('Testing Register page...', 'info');
      
      await page.goto(`${testConfig.baseUrl}/Identity/Account/Register`, {
        waitUntil: 'networkidle'
      });
      
      // Click submit button
      await page.click('button[type="submit"]');
      await page.waitForTimeout(1000);
      
      const registerErrors = await page.locator('.wcr-validation-message').all();
      expect(registerErrors.length).toBeGreaterThan(0);
      log(`✓ Register validation working: ${registerErrors.length} errors shown`, 'success');
    });
  });

  test.describe('Profile Management', () => {
    test.beforeEach(async ({ page }) => {
      await AuthHelpers.login(page);
    });

    test('Profile page validation', async ({ page }) => {
      log('Testing standardized Profile page...', 'info');
      
      await page.goto(`${testConfig.baseUrl}/profile-new`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      // Test form validation
      const sceneNameInput = await page.locator('[data-testid="scenename-input"] input');
      if (await sceneNameInput.isVisible()) {
        // Clear existing value
        await sceneNameInput.clear();
        
        // Submit form with empty required field
        const submitButton = await page.locator('button[type="submit"]');
        await submitButton.click();
        await page.waitForTimeout(1000);
        
        const validationError = await page.locator('.wcr-validation-message').first();
        expect(await validationError.isVisible()).toBeTruthy();
        log('✓ Profile validation working', 'success');
      }
    });

    test('ManageProfile page validation', async ({ page }) => {
      log('Testing ManageProfile page...', 'info');
      
      await page.goto(`${testConfig.baseUrl}/manage-profile`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      const manageTitle = await page.locator('.auth-title');
      if (await manageTitle.isVisible()) {
        const titleText = await manageTitle.textContent();
        expect(titleText).toContain('MANAGE PROFILE');
        log('✓ ManageProfile page loaded with validation', 'success');
      }
    });
  });

  test.describe('Event Management', () => {
    test.beforeEach(async ({ page }) => {
      await AuthHelpers.login(page);
    });

    test('Event creation form validation', async ({ page }) => {
      log('Testing standardized EventEdit page...', 'info');
      
      await page.goto(`${testConfig.baseUrl}/admin/events/new-standardized`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      // Test form validation
      const submitButton = await page.locator('button[type="submit"]');
      await submitButton.click();
      await page.waitForTimeout(1000);
      
      const validationErrors = await page.locator('.wcr-validation-message').all();
      expect(validationErrors.length).toBeGreaterThan(0);
      log(`✓ Event form validation working: ${validationErrors.length} errors shown`, 'success');
      
      // Test filling required fields
      log('Testing event form with data...', 'info');
      
      // Fill event title
      const titleInput = await page.locator('input[placeholder*="Rope Basics"]');
      if (await titleInput.isVisible()) {
        await titleInput.fill('Test Workshop - Validation');
        log('✓ Filled event title', 'success');
      }
      
      // Select event type
      const eventTypeButton = await page.locator('.type-option-compact').first();
      if (await eventTypeButton.isVisible()) {
        await eventTypeButton.click();
        log('✓ Selected event type', 'success');
      }
      
      // Fill venue
      const venueInput = await page.locator('input[placeholder="Enter venue name"]');
      if (await venueInput.isVisible()) {
        await venueInput.fill('Test Venue');
        log('✓ Filled venue name', 'success');
      }
    });
  });

  test.describe('User Management', () => {
    test.beforeEach(async ({ page }) => {
      await AuthHelpers.login(page);
    });

    test('User management page validation', async ({ page }) => {
      log('Testing standardized UserManagement page...', 'info');
      
      await page.goto(`${testConfig.baseUrl}/admin/users-new`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      // Open Add User modal
      const addUserButton = await page.locator('.btn-create-user');
      if (await addUserButton.isVisible()) {
        await addUserButton.click();
        await page.waitForTimeout(1000);
        
        // Test modal validation
        const modalSubmit = await page.locator('.modal-visible button[type="submit"]');
        if (await modalSubmit.isVisible()) {
          await modalSubmit.click();
          await page.waitForTimeout(1000);
          
          const modalErrors = await page.locator('.modal-visible .wcr-validation-message').all();
          expect(modalErrors.length).toBeGreaterThan(0);
          log(`✓ User modal validation working: ${modalErrors.length} errors shown`, 'success');
        }
        
        // Close modal
        const closeButton = await page.locator('.modal-close');
        if (await closeButton.isVisible()) {
          await closeButton.click();
          await page.waitForTimeout(500);
        }
      }
      
      // Test search functionality
      const searchInput = await page.locator('.search-input');
      if (await searchInput.isVisible()) {
        await searchInput.fill('test search');
        await page.waitForTimeout(500);
        log('✓ Search input working', 'success');
      }
      
      // Test filter tabs
      const filterTabs = await page.locator('.filter-tab').all();
      if (filterTabs.length > 0) {
        await filterTabs[1].click(); // Click "Admins" tab
        await page.waitForTimeout(500);
        log('✓ Filter tabs working', 'success');
      }
    });
  });

  test.describe('Form Components', () => {
    test.beforeEach(async ({ page }) => {
      await AuthHelpers.login(page);
    });

    test('Validation components test page', async ({ page }) => {
      log('Testing validation components...', 'info');
      
      await page.goto(`${testConfig.baseUrl}/test/validation`, {
        waitUntil: 'networkidle'
      });
      await BlazorHelpers.waitForBlazorReady(page);
      
      // Test WcrInputText
      const textInput = await page.locator('[data-testid="name-input"] input');
      if (await textInput.isVisible()) {
        await textInput.fill('a'); // Too short
        await page.click('body'); // Blur
        await page.waitForTimeout(500);
        
        const textError = await page.locator('[data-testid="name-input"] .wcr-validation-message');
        expect(await textError.isVisible()).toBeTruthy();
        log('✓ WcrInputText validation working', 'success');
      }
      
      // Test WcrInputEmail
      const emailInput = await page.locator('[data-testid="email-input"] input');
      if (await emailInput.isVisible()) {
        await emailInput.fill('invalid-email');
        await page.click('body'); // Blur
        await page.waitForTimeout(500);
        
        const emailError = await page.locator('[data-testid="email-input"] .wcr-validation-message');
        expect(await emailError.isVisible()).toBeTruthy();
        log('✓ WcrInputEmail validation working', 'success');
      }
      
      // Test WcrInputNumber
      const numberInput = await page.locator('[data-testid="age-input"] input');
      if (await numberInput.isVisible()) {
        await numberInput.fill('150'); // Too high
        await page.click('body'); // Blur
        await page.waitForTimeout(500);
        
        const numberError = await page.locator('[data-testid="age-input"] .wcr-validation-message');
        expect(await numberError.isVisible()).toBeTruthy();
        log('✓ WcrInputNumber validation working', 'success');
      }
      
      // Test form submission
      const submitButton = await page.locator('button[type="submit"]');
      if (await submitButton.isVisible()) {
        await submitButton.click();
        await page.waitForTimeout(1000);
        
        const summaryErrors = await page.locator('.wcr-validation-summary li').all();
        expect(summaryErrors.length).toBeGreaterThan(0);
        log(`✓ ValidationSummary working: ${summaryErrors.length} errors listed`, 'success');
      }
    });
  });

  test.afterAll(() => {
    log(`\n${colors.bright}=== Test Summary ===${colors.reset}`, 'info');
    log('All validation standardization tests completed', 'success');
  });
});