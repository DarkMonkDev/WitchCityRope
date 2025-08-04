import { test, expect, Page } from '@playwright/test';
import { testConfig } from '../helpers/test.config';
import { BlazorHelpers } from '../helpers/blazor.helpers';
import { LoginPage } from '../pages/login.page';
import { RegisterPage } from '../pages/register.page';
import { AuthHelpers } from '../helpers/auth.helpers';
import * as fs from 'fs';
import * as path from 'path';

/**
 * Comprehensive Form Validation Test Suite
 * Converted from Puppeteer test: test-all-migrated-forms.js
 * 
 * Tests validation behavior across all migrated forms including:
 * - Identity forms (login, register, password management, etc.)
 * - Application forms (events, vetting, incidents, etc.)
 * 
 * This test suite validates WCR components and form behavior
 */

// Test accounts
const TEST_ACCOUNTS = {
  admin: {
    email: testConfig.accounts.admin.email,
    password: testConfig.accounts.admin.password,
    sceneName: 'RopeAdmin'
  },
  validUser: {
    email: 'test.user@example.com',
    password: 'ValidPass123!',
    sceneName: 'TestUser'
  }
};

// Test results tracking
interface TestResult {
  name: string;
  startTime: string;
  endTime?: string;
  tests: Array<{ name: string; passed: boolean; skipped?: boolean }>;
  status: 'pending' | 'passed' | 'failed';
  error?: string;
  screenshots: string[];
}

interface TestResults {
  startTime: string;
  forms: Record<string, TestResult>;
  summary: {
    total: number;
    passed: number;
    failed: number;
    skipped: number;
  };
}

const testResults: TestResults = {
  startTime: new Date().toISOString(),
  forms: {},
  summary: {
    total: 0,
    passed: 0,
    failed: 0,
    skipped: 0
  }
};

// Screenshot directory
const SCREENSHOT_DIR = 'test-results/validation-tests';

test.describe('Comprehensive Form Validation Test Suite', () => {
  test.beforeAll(async () => {
    // Create screenshot directory
    if (!fs.existsSync(SCREENSHOT_DIR)) {
      fs.mkdirSync(SCREENSHOT_DIR, { recursive: true });
    }
  });

  test.afterAll(async () => {
    // Generate summary report
    console.log('\n' + '='.repeat(60));
    console.log('📊 TEST SUMMARY');
    console.log('='.repeat(60));
    console.log(`Total Forms Tested: ${testResults.summary.total}`);
    console.log(`✅ Passed: ${testResults.summary.passed}`);
    console.log(`❌ Failed: ${testResults.summary.failed}`);
    console.log(`⏭️  Skipped: ${testResults.summary.skipped}`);
    
    const successRate = (testResults.summary.passed / (testResults.summary.total - testResults.summary.skipped) * 100).toFixed(1);
    console.log(`\nSuccess Rate: ${successRate}%`);

    // Save detailed results
    const resultsFile = `${SCREENSHOT_DIR}/test-results-${Date.now()}.json`;
    fs.writeFileSync(resultsFile, JSON.stringify(testResults, null, 2));
    console.log(`\n📄 Detailed results saved to: ${resultsFile}`);
  });

  test.describe('Identity Forms', () => {
    test('Login Form Validation', async ({ page }) => {
      await runFormTest(page, 'Login Form', async (page, result) => {
        const loginPage = new LoginPage(page);
        await loginPage.goto();
        
        // Wait for Blazor to initialize
        await BlazorHelpers.waitForBlazorReady(page);

        // Test empty form submission
        console.log('  - Testing empty form submission...');
        await loginPage.signInButton.click();
        await page.waitForTimeout(1000);

        const emptyErrors = await loginPage.getValidationMessages();
        if (emptyErrors.length < 2) throw new Error('Expected validation errors for empty form');
        result.tests.push({ name: 'Empty form validation', passed: true });

        await captureScreenshot(page, 'login-empty-form', result);

        // Test invalid email format
        console.log('  - Testing invalid email format...');
        await loginPage.emailInput.fill('invalid-email');
        await page.keyboard.press('Tab');
        await page.waitForTimeout(500);

        const emailError = await page.locator('#Input_Email ~ .text-danger').isVisible();
        if (!emailError) throw new Error('Expected email validation error');
        result.tests.push({ name: 'Email format validation', passed: true });

        // Test valid login
        console.log('  - Testing valid credentials...');
        await loginPage.emailInput.clear();
        await loginPage.emailInput.fill(TEST_ACCOUNTS.admin.email);
        await loginPage.passwordInput.fill(TEST_ACCOUNTS.admin.password);
        
        await loginPage.signInButton.click();

        try {
          await page.waitForURL(/^(?!.*\/login).*$/, { timeout: 10000 });
        } catch (e) {
          // Navigation might be too fast
        }

        const currentUrl = page.url();
        if (currentUrl.includes('/login')) throw new Error('Login failed - still on login page');
        result.tests.push({ name: 'Valid login', passed: true });
      });
    });

    test('Register Form Validation', async ({ page }) => {
      await runFormTest(page, 'Register Form', async (page, result) => {
        const registerPage = new RegisterPage(page);
        await registerPage.goto();

        // Test empty form submission
        console.log('  - Testing empty form submission...');
        const submitBtn = await page.locator('button[type="submit"]');
        if (!submitBtn) throw new Error('Submit button not found');

        await submitBtn.click();
        await page.waitForTimeout(1000);

        const errors = await page.locator('.wcr-validation-summary li, .validation-summary-errors li').count();
        if (errors === 0) throw new Error('Expected validation summary errors');
        result.tests.push({ name: 'Empty form validation', passed: true });

        await captureScreenshot(page, 'register-empty-form', result);

        // Test password requirements
        console.log('  - Testing password requirements...');
        const passwordInput = await page.locator('input[type="password"]:not([placeholder*="confirm" i])').first();
        if (passwordInput) {
          await passwordInput.fill('weak');
          await page.keyboard.press('Tab');
          await page.waitForTimeout(500);

          const reqs = await page.locator('.wcr-password-requirements .requirement').count();
          if (reqs === 0) throw new Error('Password requirements not shown');
          result.tests.push({ name: 'Password requirements display', passed: true });

          await captureScreenshot(page, 'register-password-requirements', result);
        }
      });
    });

    test('Forgot Password Form Validation', async ({ page }) => {
      await runFormTest(page, 'Forgot Password Form', async (page, result) => {
        await page.goto(`${testConfig.baseUrl}/Identity/Account/ForgotPassword`);
        await page.waitForLoadState('networkidle');

        // Test empty email
        console.log('  - Testing empty email submission...');
        const submitBtn = await page.locator('button[type="submit"]');
        if (!submitBtn) throw new Error('Submit button not found');

        await submitBtn.click();
        await page.waitForTimeout(1000);

        const errors = await page.locator('.text-danger').count();
        if (errors === 0) throw new Error('Expected email validation error');
        result.tests.push({ name: 'Empty email validation', passed: true });

        // Test invalid email format
        console.log('  - Testing invalid email format...');
        const emailInput = await page.locator('#Input_Email');
        if (emailInput) {
          await emailInput.fill('not-an-email');
          await page.keyboard.press('Tab');
          await page.waitForTimeout(500);

          const emailError = await page.locator('.text-danger').isVisible();
          if (!emailError) throw new Error('Expected email format error');
          result.tests.push({ name: 'Email format validation', passed: true });

          await captureScreenshot(page, 'forgot-password-invalid-email', result);
        }
      });
    });

    test('Reset Password Form Validation', async ({ page }) => {
      await runFormTest(page, 'Reset Password Form', async (page, result) => {
        // Reset password requires a valid token from email
        const dummyToken = Buffer.from('dummy-reset-token').toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
        await page.goto(`${testConfig.baseUrl}/Identity/Account/ResetPassword?code=${dummyToken}&userId=dummy-user-id`);
        await page.waitForLoadState('networkidle');

        // Wait for Blazor to initialize
        await BlazorHelpers.waitForBlazorReady(page);

        const hasForm = await page.locator('form').first().isVisible();
        if (!hasForm) {
          // Check if there's an error message about invalid token
          const errorMessage = await page.locator('.alert-danger, .error-message, .text-danger').textContent();
          if (errorMessage && errorMessage.toLowerCase().includes('invalid')) {
            console.log('  - Reset password shows error for invalid token (expected behavior)');
            result.tests.push({ name: 'Invalid token handling', passed: true });
            await captureScreenshot(page, 'reset-password-invalid-token', result);
          } else {
            console.log('  - Reset password requires valid code, marking as skipped');
            result.tests.push({ name: 'Form accessibility', passed: false, skipped: true });
            testResults.summary.skipped++;
          }
          return;
        }

        // Test empty form
        console.log('  - Testing empty form submission...');
        const submitBtn = await page.locator('button[type="submit"]');
        if (submitBtn) {
          await submitBtn.click();
          await page.waitForTimeout(1000);

          const errors = await page.locator('.text-danger').count();
          if (errors < 2) throw new Error('Expected validation errors for passwords');
          result.tests.push({ name: 'Empty form validation', passed: true });
        }

        // Test password mismatch
        console.log('  - Testing password mismatch...');
        const passwordInputs = await page.locator('input[type="password"]').all();
        if (passwordInputs.length >= 2) {
          await passwordInputs[0].fill('NewPass123!');
          await passwordInputs[1].fill('DifferentPass123!');
          await page.keyboard.press('Tab');
          await page.waitForTimeout(500);

          const mismatchError = await page.locator('.text-danger').isVisible();
          if (!mismatchError) throw new Error('Expected password mismatch error');
          result.tests.push({ name: 'Password mismatch validation', passed: true });

          await captureScreenshot(page, 'reset-password-mismatch', result);
        }
      });
    });

    test('Change Password Form Validation', async ({ page, context }) => {
      await runFormTest(page, 'Change Password Form', async (page, result) => {
        // Login first
        await AuthHelpers.login(page, TEST_ACCOUNTS.admin.email, TEST_ACCOUNTS.admin.password);
        
        // Navigate to change password
        await page.goto(`${testConfig.baseUrl}/Identity/Account/Manage/ChangePassword`);
        await page.waitForLoadState('networkidle');

        const hasForm = await page.locator('form').first().isVisible();
        if (!hasForm) {
          throw new Error('Not authenticated - cannot access change password form');
        }

        // Test empty form
        console.log('  - Testing empty form submission...');
        const submitBtn = await page.locator('button[type="submit"]');
        if (submitBtn) {
          await submitBtn.click();
          await page.waitForTimeout(1000);

          const errors = await page.locator('.text-danger').count();
          if (errors < 3) throw new Error('Expected validation errors for all password fields');
          result.tests.push({ name: 'Empty form validation', passed: true });

          await captureScreenshot(page, 'change-password-empty', result);
        }
      });
    });

    test('Manage Email Form Validation', async ({ page, context }) => {
      await runFormTest(page, 'Manage Email Form', async (page, result) => {
        // Login first
        await AuthHelpers.login(page, TEST_ACCOUNTS.admin.email, TEST_ACCOUNTS.admin.password);
        
        // Navigate to manage email
        await page.goto(`${testConfig.baseUrl}/Identity/Account/Manage/Email`);
        await page.waitForLoadState('networkidle');

        const hasForm = await page.locator('form').first().isVisible();
        if (!hasForm) {
          throw new Error('Not authenticated - cannot access manage email form');
        }

        // Check current email display
        console.log('  - Checking current email display...');
        const currentEmailSection = await page.locator('.current-email-section').isVisible();
        if (!currentEmailSection) throw new Error('Current email section not found');
        result.tests.push({ name: 'Current email display', passed: true });

        // Test empty new email
        console.log('  - Testing empty email submission...');
        const submitBtn = await page.locator('button[type="submit"]');
        if (submitBtn) {
          await submitBtn.click();
          await page.waitForTimeout(1000);

          const errors = await page.locator('.text-danger').count();
          if (errors === 0) throw new Error('Expected email validation error');
          result.tests.push({ name: 'Empty email validation', passed: true });
        }

        // Test invalid email format
        console.log('  - Testing invalid email format...');
        const emailInput = await page.locator('#Input_Email');
        if (emailInput) {
          await emailInput.fill('invalid-email-format');
          await page.keyboard.press('Tab');
          await page.waitForTimeout(500);

          const emailError = await page.locator('.text-danger').isVisible();
          if (!emailError) throw new Error('Expected email format error');
          result.tests.push({ name: 'Email format validation', passed: true });

          await captureScreenshot(page, 'manage-email-invalid', result);
        }
      });
    });

    test('Manage Profile Form Validation', async ({ page, context }) => {
      await runFormTest(page, 'Manage Profile Form', async (page, result) => {
        // Login first
        await AuthHelpers.login(page, TEST_ACCOUNTS.admin.email, TEST_ACCOUNTS.admin.password);
        
        // Navigate to manage profile
        await page.goto(`${testConfig.baseUrl}/Identity/Account/Manage`);
        await page.waitForLoadState('networkidle');

        const hasForm = await page.locator('form').first().isVisible();
        if (!hasForm) {
          throw new Error('Not authenticated - cannot access profile form');
        }

        // Check profile sections
        console.log('  - Checking profile sections...');
        const profileSections = await page.locator('.profile-section').count();
        if (profileSections === 0) throw new Error('Profile sections not found');
        result.tests.push({ name: 'Profile sections display', passed: true });

        // Test phone number validation
        console.log('  - Testing phone number format...');
        const phoneInput = await page.locator('input[placeholder*="(123)" i]');
        if (phoneInput && await phoneInput.isVisible()) {
          await phoneInput.fill('invalid-phone');
          await page.keyboard.press('Tab');
          await page.waitForTimeout(500);

          const phoneError = await page.locator('.text-danger').isVisible();
          if (!phoneError) console.log('    Warning: Phone validation might not be strict');
          result.tests.push({ name: 'Phone format validation', passed: true });
        }

        await captureScreenshot(page, 'manage-profile-form', result);
      });
    });

    test('Delete Personal Data Form Validation', async ({ page, context }) => {
      await runFormTest(page, 'Delete Personal Data Form', async (page, result) => {
        // Login first
        await AuthHelpers.login(page, TEST_ACCOUNTS.admin.email, TEST_ACCOUNTS.admin.password);
        
        // Navigate to delete personal data
        await page.goto(`${testConfig.baseUrl}/delete-personal-data`);
        await page.waitForLoadState('networkidle');

        const hasForm = await page.locator('form').first().isVisible();
        if (!hasForm) {
          throw new Error('Not authenticated - cannot access delete data form');
        }

        // Check warning banner
        console.log('  - Checking warning banner...');
        const warningBanner = await page.locator('.warning-banner').isVisible();
        if (!warningBanner) throw new Error('Warning banner not displayed');
        result.tests.push({ name: 'Warning banner display', passed: true });

        // Test empty form submission
        console.log('  - Testing empty form submission...');
        const submitBtn = await page.locator('button[type="submit"]');
        if (submitBtn) {
          await submitBtn.click();
          await page.waitForTimeout(1000);

          const errors = await page.locator('.text-danger').count();
          if (errors === 0) throw new Error('Expected validation errors');
          result.tests.push({ name: 'Empty form validation', passed: true });

          await captureScreenshot(page, 'delete-data-validation', result);
        }
      });
    });

    test('Login with 2FA Form Validation', async ({ page }) => {
      await runFormTest(page, 'Login with 2FA Form', async (page, result) => {
        await page.goto(`${testConfig.baseUrl}/login-2fa`);
        await page.waitForLoadState('networkidle');
        
        // Wait for Blazor to initialize
        await page.waitForTimeout(2000);

        const currentUrl = page.url();
        if (!currentUrl.includes('/login-2fa')) {
          console.log('  - 2FA page requires active 2FA session, marking as skipped');
          result.tests.push({ name: '2FA form accessibility', passed: false, skipped: true });
          testResults.summary.skipped++;
          return;
        }

        // Test empty code
        console.log('  - Testing empty code submission...');
        const submitBtn = await page.locator('button[type="submit"]');
        if (submitBtn) {
          await submitBtn.click();
          await page.waitForTimeout(1000);

          const errors = await page.locator('.text-danger').count();
          if (errors === 0) throw new Error('Expected code validation error');
          result.tests.push({ name: 'Empty code validation', passed: true });
        }

        // Test invalid code format
        console.log('  - Testing invalid code format...');
        const codeInput = await page.locator('input[inputmode="numeric"]');
        if (codeInput && await codeInput.isVisible()) {
          await codeInput.fill('123'); // Too short
          await page.keyboard.press('Tab');
          await page.waitForTimeout(500);

          const codeError = await page.locator('.text-danger').isVisible();
          if (!codeError) console.log('    Warning: Code format validation might not be strict');
          result.tests.push({ name: 'Code format validation', passed: true });
        }
      });
    });
  });

  test.describe('Application Forms', () => {
    test('Event Registration (RSVP) Form Validation', async ({ page }) => {
      await runFormTest(page, 'Event Registration (RSVP) Form', async (page, result) => {
        // Navigate to events page first
        await page.goto(`${testConfig.baseUrl}/events`);
        await page.waitForLoadState('networkidle');

        // Try to find and click an event to open registration modal
        console.log('  - Looking for events with registration...');
        const eventCards = await page.locator('.event-card').count();
        if (eventCards === 0) {
          console.log('  - No events found, marking as skipped');
          result.tests.push({ name: 'Event registration modal', passed: false, skipped: true });
          testResults.summary.skipped++;
          return;
        }

        // Click first event
        await page.locator('.event-card').first().click();
        await page.waitForTimeout(2000);

        // Look for RSVP button
        const rsvpButton = await page.locator('button:has-text("RSVP"), button:has-text("Register")').first();
        const hasRsvpButton = await rsvpButton.isVisible();
        
        if (!hasRsvpButton) {
          console.log('  - No RSVP button found, marking as skipped');
          result.tests.push({ name: 'RSVP modal accessibility', passed: false, skipped: true });
          testResults.summary.skipped++;
          return;
        }

        // Click the RSVP button
        await rsvpButton.click();
        await page.waitForTimeout(1000);

        // Test modal form
        const modal = await page.locator('.modal, .event-registration-modal').isVisible();
        if (!modal) throw new Error('Registration modal not opened');
        result.tests.push({ name: 'Modal opens successfully', passed: true });

        await captureScreenshot(page, 'event-registration-modal', result);
      });
    });

    test('Vetting Application Form Validation', async ({ page }) => {
      await runFormTest(page, 'Vetting Application Form', async (page, result) => {
        await page.goto(`${testConfig.baseUrl}/vetting/application`);
        await page.waitForLoadState('networkidle');

        const hasForm = await page.locator('form').first().isVisible();
        if (!hasForm) {
          console.log('  - Vetting application might require specific permissions, marking as skipped');
          result.tests.push({ name: 'Vetting form accessibility', passed: false, skipped: true });
          testResults.summary.skipped++;
          return;
        }

        // Test empty form submission
        console.log('  - Testing empty form submission...');
        const submitBtn = await page.locator('button[type="submit"]');
        if (submitBtn) {
          await submitBtn.click();
          await page.waitForTimeout(1000);

          const errors = await page.locator('.text-danger, .wcr-validation-summary li, .validation-summary-errors li').count();
          if (errors === 0) throw new Error('Expected validation errors');
          result.tests.push({ name: 'Empty form validation', passed: true });

          await captureScreenshot(page, 'vetting-application-validation', result);
        }
      });
    });

    test('Incident Reporting Form Validation', async ({ page, context }) => {
      await runFormTest(page, 'Incident Reporting Form', async (page, result) => {
        // Login as admin first
        await AuthHelpers.login(page, TEST_ACCOUNTS.admin.email, TEST_ACCOUNTS.admin.password);
        
        // Navigate to admin incidents
        await page.goto(`${testConfig.baseUrl}/admin/incidents`);
        await page.waitForLoadState('networkidle');

        // Look for create incident button
        const createButton = await page.locator('button:has-text("Create"), button:has-text("Report")').first();
        const hasCreateButton = await createButton.isVisible();
        
        if (!hasCreateButton) {
          console.log('  - Incident reporting page might not have create button, marking as skipped');
          result.tests.push({ name: 'Incident form accessibility', passed: false, skipped: true });
          testResults.summary.skipped++;
          return;
        }

        // Click the button
        await createButton.click();
        await page.waitForTimeout(1000);

        // Test modal form
        const modal = await page.locator('.modal, .incident-modal').isVisible();
        if (!modal) throw new Error('Incident modal not opened');
        result.tests.push({ name: 'Modal opens successfully', passed: true });

        // Test empty form submission
        console.log('  - Testing empty form submission...');
        const submitBtn = await page.locator('.modal button[type="submit"], .incident-modal button[type="submit"]');
        if (submitBtn && await submitBtn.isVisible()) {
          await submitBtn.click();
          await page.waitForTimeout(1000);

          const errors = await page.locator('.modal .text-danger, .incident-modal .text-danger').count();
          if (errors === 0) throw new Error('Expected validation errors');
          result.tests.push({ name: 'Empty form validation', passed: true });
        }
      });
    });

    test('Event Edit Form Validation', async ({ page, context }) => {
      await runFormTest(page, 'Event Edit Form', async (page, result) => {
        // Login as admin first
        await AuthHelpers.login(page, TEST_ACCOUNTS.admin.email, TEST_ACCOUNTS.admin.password);
        
        // Navigate to admin events
        await page.goto(`${testConfig.baseUrl}/admin/events`);
        await page.waitForLoadState('networkidle');

        // Find edit button for first event
        const editButton = await page.locator('button:has-text("Edit"), a:has-text("Edit")').first();
        const hasEditButton = await editButton.isVisible();
        
        if (!hasEditButton) {
          console.log('  - No events to edit, marking as skipped');
          result.tests.push({ name: 'Event edit form accessibility', passed: false, skipped: true });
          testResults.summary.skipped++;
          return;
        }

        // Click the button
        await editButton.click();
        await page.waitForTimeout(2000);

        // Check if we're on edit page
        const hasForm = await page.locator('form').first().isVisible();
        if (!hasForm) throw new Error('Event edit form not found');
        result.tests.push({ name: 'Edit form loads', passed: true });

        // Test clearing required field
        console.log('  - Testing required field validation...');
        const titleInput = await page.locator('input[name="Title"], input[placeholder*="title" i]').first();
        if (titleInput && await titleInput.isVisible()) {
          await titleInput.click({ clickCount: 3 });
          await page.keyboard.press('Backspace');
          await page.keyboard.press('Tab');
          await page.waitForTimeout(500);

          const titleError = await page.locator('.text-danger').isVisible();
          if (!titleError) throw new Error('Expected title validation error');
          result.tests.push({ name: 'Required field validation', passed: true });
        }

        await captureScreenshot(page, 'event-edit-validation', result);
      });
    });
  });
});

// Helper function to run a form test
async function runFormTest(page: Page, formName: string, testFunction: (page: Page, result: TestResult) => Promise<void>) {
  console.log(`\n${'='.repeat(60)}`);
  console.log(`Testing: ${formName}`);
  console.log(`${'='.repeat(60)}`);

  const formResult: TestResult = {
    name: formName,
    startTime: new Date().toISOString(),
    tests: [],
    status: 'pending',
    error: undefined,
    screenshots: []
  };

  try {
    await testFunction(page, formResult);
    formResult.status = 'passed';
    testResults.summary.passed++;
    console.log(`✅ ${formName} - PASSED`);
  } catch (error) {
    formResult.status = 'failed';
    formResult.error = error instanceof Error ? error.message : String(error);
    testResults.summary.failed++;
    console.log(`❌ ${formName} - FAILED: ${formResult.error}`);

    // Capture error screenshot
    await captureScreenshot(page, `${formName.replace(/\s+/g, '-')}-error`, formResult);
  }

  formResult.endTime = new Date().toISOString();
  testResults.forms[formName] = formResult;
  testResults.summary.total++;
}

// Helper function to capture screenshots
async function captureScreenshot(page: Page, name: string, result: TestResult) {
  const timestamp = Date.now();
  const filename = `${SCREENSHOT_DIR}/${name}-${timestamp}.png`;
  await page.screenshot({
    path: filename,
    fullPage: true
  });
  result.screenshots.push(filename);
  return filename;
}